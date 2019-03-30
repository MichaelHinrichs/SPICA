using SPICA.Formats.CtrH3D;
using SPICA.Formats.CtrH3D.Model;
using SPICA.Formats.CtrH3D.Model.Mesh;
using SPICA.PICA.Converters;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace SPICA.Formats.Generic.Blender
{
	public class BLEND
	{
		const float SCALE = 0.01f;

		private readonly string blenderPath;
		private string pythonScript;

		public BLEND(H3D scene, int mdlIndex, int animIndex = -1)
		{
			switch (Environment.OSVersion.Platform)
			{
				case PlatformID.Win32NT:
				case PlatformID.Win32S:
				case PlatformID.Win32Windows:
				case PlatformID.WinCE:
					blenderPath = Environment.ExpandEnvironmentVariables(@"%programfiles%\Blender Foundation\Blender\blender.exe");
					break;
				case PlatformID.Unix:
					throw new NotImplementedException();
				case PlatformID.MacOSX:
					throw new NotImplementedException();
				default:
					throw new PlatformNotSupportedException();
			}

			if (mdlIndex != -1)
			{
				var model = scene.Models[mdlIndex];

				pythonScript = CleanUpScene(model);

				if (model.Materials.Count > 0)
					pythonScript += BuildMaterials(model);

				if (model.Skeleton.Count > 0)
					pythonScript += BuildArmature(model);

				if (model.Meshes.Count > 0)
					pythonScript += BuildModel(model);
			}
		}

		public void Save(string fileName)
		{
			if (!File.Exists(blenderPath))
				throw new FileNotFoundException("Blender not found");

			pythonScript += $"bpy.ops.wm.save_as_mainfile(filepath='{fileName}')\n";

			var scriptPath = Path.Combine(Path.GetTempPath(), "export_" + Path.GetFileNameWithoutExtension(fileName) + ".py");
			using (StreamWriter sw = new StreamWriter(scriptPath))
			{
				sw.Write(pythonScript);
			}

			var startInfo = new ProcessStartInfo
			{
				FileName = blenderPath,
				Arguments = $"--python \"{scriptPath}\""
			};

			Process.Start(startInfo).WaitForExit();
		}

		#region Private Methods
		private string CleanUpScene(H3DModel model)
		{
			var armature = model.Skeleton.Count == 0 ? "None" : $"bpy.data.armatures.new('{model.Skeleton[0].Name}')";
			var modelName = model.Skeleton.Count == 0 ? model.Name : model.Skeleton[0].Name;

			return
				"import bpy\n" +
				"import bmesh\n" +
				"import math\n" +
				"import mathutils\n" +
				"bpy.context.scene.render.engine = 'BLENDER_RENDER'\n" +
				"bpy.ops.object.select_all()\n" +
				"bpy.ops.object.delete()\n" +
				$"root = bpy.data.objects.new('{modelName}', {armature})\n" +
				"bpy.context.scene.objects.link(root)\n";
		}

		private string BuildMaterials(H3DModel model)
		{
			var res = "";

			for (var mi = 0; mi < model.Materials.Count; ++mi)
			{
				var mat = model.Materials[mi];
				res += $"mat{mi} = bpy.data.materials.new('{mat.Name}')\n";

				for (var ti = 0; ti < 3; ++ti)
				{
					if (!mat.EnabledTextures[ti]) continue;

					string tex = null;
					switch (ti)
					{
						case 0: tex = mat.Texture0Name; break;
						case 1: tex = mat.Texture1Name; break;
						case 2: tex = mat.Texture2Name; break;
					}

					if (tex == null) continue;

					res += $"tex{mi}_{ti} = bpy.data.textures.new('{tex}', type='IMAGE')\n";
					res += $"slt{mi}_{ti} = mat{mi}.texture_slots.add()\n";
					res += $"slt{mi}_{ti}.texture = tex{mi}_{ti}\n";
				}
			}

			return res;
		}

		private string BuildArmature(H3DModel model)
		{
			var res = "bpy.context.scene.objects.active = root\n";
			res += "bpy.ops.object.editmode_toggle()\n";

			// First bone is the armature
			for (var bi = 1; bi < model.Skeleton.Count; ++bi)
			{
				var bone = model.Skeleton[bi];

				res += $"b{bi} = root.data.edit_bones.new('{bone.Name}')\n";
				res += $"b{bi}.tail = (0,0,1)\n";
			}

			// We have to do it a second time to set the parents
			for (var bi = 1; bi < model.Skeleton.Count; ++bi)
			{
				var bone = model.Skeleton[bi];

				if (bone.ParentIndex == 0) continue;

				res += $"b{bi}.parent = b{bone.ParentIndex}\n";
			}

			res += "bpy.ops.object.editmode_toggle()\n";

			// And a third time for the pose bones, which are different entities in blender
			for (var bi = 1; bi < model.Skeleton.Count; ++bi)
			{
				var bone = model.Skeleton[bi];
				var t = bone.Transform;

				res += $"b{bi} = root.pose.bones[{bi - 1}]\n";
				res += $"b{bi}.matrix_basis = (({t.M11},{t.M12},{t.M13},{t.M14}),({t.M21},{t.M22},{t.M23},{t.M24}),({t.M31},{t.M32},{t.M33},{t.M34}),({t.M41},{t.M42},{t.M43},{t.M44}))\n";
			}

			res += "bpy.ops.object.select_all(action='DESELECT')\n";

			return res;
		}

		private string BuildModel(H3DModel model)
		{
			var res = "";
			res += $"m = bpy.data.meshes.new('{model.Name}')\n";
			res += $"o = bpy.data.objects.new('{model.Name}', m)\n";
			res += $"o.parent = root\n";
			res += $"bpy.context.scene.objects.link(o)\n";
			res += $"bm = bmesh.new()\n";

			var vertices = new List<BLENDVertex>();
			var tris = new List<int[]>();
			var groups = new List<string>();
			var materials = new List<int>();

			foreach (var mesh in model.Meshes)
			{
				if (mesh.Type == H3DMeshType.Silhouette) continue;

				materials.Add(mesh.MaterialIndex);

				var newVertices = mesh.GetVertices();
				var ids = new Dictionary<int, int>();
				for (var vi = 0; vi < newVertices.Length; ++vi)
				{
					var vert = newVertices[vi];
					var index = vertices.FindIndex(x => x.vert.Position.X == vert.Position.X && x.vert.Position.Y == vert.Position.Y && x.vert.Position.Z == vert.Position.Z);

					if (index == -1) // New vertex
					{
						ids.Add(vi, vertices.Count);

						var name = model.MeshNodesTree.Find(mesh.NodeIndex);
						var groupIndex = groups.IndexOf(name);

						if (groupIndex == -1)
						{
							vertices.Add(new BLENDVertex(vert, groups.Count));
							groups.Add(name);
						}
						else
						{
							vertices.Add(new BLENDVertex(vert, groupIndex));
						}
					}
					else
					{
						ids.Add(vi, index);
					}
				}

				var newTris = new List<int[]>();
				foreach (var subMesh in mesh.SubMeshes)
				{
					for (var ti = 0; ti < subMesh.Indices.Length; ti += 3)
					{
						var tri = new int[] { subMesh.Indices[ti], subMesh.Indices[ti + 1], subMesh.Indices[ti + 2] };

						if (newTris.Any(x => x.SequenceEqual(tri))) continue;

						newTris.Add(tri);
						tris.Add(new[] { ids[tri[0]], ids[tri[1]], ids[tri[2]] });
					}
				}
			}

			res += $"vs = []\n";
			res += $"uv = []\n";

			// Y and Z are swapped in blender's space
			foreach (var v in vertices)
			{
				res += $"v = bm.verts.new([{v.vert.Position.X},{-v.vert.Position.Z},{v.vert.Position.Y}])\n";
				res += $"v.normal = [{v.vert.Normal.X},{-v.vert.Normal.Z},{v.vert.Normal.Y}]\n";
				res += $"vs.append(v)\n";
				res += $"uv.append(({v.vert.TexCoord0.X},{v.vert.TexCoord0.Y}))\n";
			}

			foreach (var tri in tris)
				res += $"bm.faces.new([vs[{tri[0]}],vs[{tri[1]}],vs[{tri[2]}]]).smooth = True\n";

			// UV coords in blender are mapped per loop
			res += $"bm.verts.index_update()\n";
			res += $"uvl = bm.loops.layers.uv.new()\n";
			res += $"for face in bm.faces:\n\tfor loop in face.loops:\n\t\t";
			res += $"loop[uvl].uv = uv[loop.vert.index]\n";

			foreach (var mat in materials)
				res += $"o.data.materials.append(mat{mat})\n";

			res += $"bm.to_mesh(m)\n";
			res += $"bm.free()\n";
			res += $"bpy.context.scene.objects.active = o\n";
			res += $"o.select = True\n";
			res += $"bpy.ops.object.editmode_toggle()\n";
			res += $"bpy.ops.object.editmode_toggle()\n";
			res += $"bpy.ops.object.select_all(action='DESELECT')\n";

			for (var gi = 0; gi < groups.Count; ++gi)
			{
				var indexes = vertices.Where(x => x.vertGroup == gi).Select(x => vertices.IndexOf(x));
			
				res += $"vg = o.vertex_groups.new('{groups[gi]}')\n";
				res += $"vg.add([{string.Join(",", indexes)}], 1, 'ADD')\n";
			}

			return res;
		}
		#endregion
	}
}
