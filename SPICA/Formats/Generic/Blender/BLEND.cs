using SPICA.Formats.CtrH3D;
using SPICA.Formats.CtrH3D.Model;
using SPICA.Formats.CtrH3D.Model.Mesh;
using SPICA.PICA.Converters;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;

namespace SPICA.Formats.Generic.Blender
{
	public class BLEND
	{
		const float SCALE = 0.01f;

		private readonly string blenderPath;
		private StringBuilder pythonScript;

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

				CleanUpScene(model);

				if (model.Materials.Count > 0)
					BuildMaterials(model);

				if (model.Skeleton.Count > 0)
					BuildArmature(model);

				if (model.Meshes.Count > 0)
					BuildModel(model);
			}
		}

		public void Save(string fileName)
		{
			if (!File.Exists(blenderPath))
				throw new FileNotFoundException("Blender not found");

			pythonScript.Append($"bpy.ops.wm.save_as_mainfile(filepath='{fileName}')\n");

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
		private void CleanUpScene(H3DModel model)
		{
			var armature = model.Skeleton.Count == 0 ? "None" : $"bpy.data.armatures.new('{model.Skeleton[0].Name}')";
			var modelName = model.Skeleton.Count == 0 ? model.Name : model.Skeleton[0].Name;

			pythonScript = new StringBuilder(
$@"import bpy
import bmesh
bpy.context.scene.render.engine = 'BLENDER_RENDER'
bpy.ops.object.select_all()
bpy.ops.object.delete()
root = bpy.data.objects.new('{modelName}', {armature})
bpy.context.scene.objects.link(root)
"
			);
		}

		private void BuildMaterials(H3DModel model)
		{
			for (var mi = 0; mi < model.Materials.Count; ++mi)
			{
				var mat = model.Materials[mi];
				pythonScript.Append($"mat{mi} = bpy.data.materials.new('{mat.Name}')\n");

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

					pythonScript.Append($"tex{mi}_{ti} = bpy.data.textures.new('{tex}', type='IMAGE')\n");
					pythonScript.Append($"slt{mi}_{ti} = mat{mi}.texture_slots.add()\n");
					pythonScript.Append($"slt{mi}_{ti}.texture = tex{mi}_{ti}\n");
				}
			}
		}

		private void BuildArmature(H3DModel model)
		{
			pythonScript.Append("bpy.context.scene.objects.active = root\n");
			pythonScript.Append("bpy.ops.object.editmode_toggle()\n");

			// First bone is the armature
			for (var bi = 1; bi < model.Skeleton.Count; ++bi)
			{
				var bone = model.Skeleton[bi];

				pythonScript.Append($"b{bi} = root.data.edit_bones.new('{bone.Name}')\n");
				pythonScript.Append($"b{bi}.tail = (0,0,1)\n");
			}

			// We have to do it a second time to set the parents
			for (var bi = 1; bi < model.Skeleton.Count; ++bi)
			{
				var bone = model.Skeleton[bi];

				if (bone.ParentIndex == 0) continue;

				pythonScript.Append($"b{bi}.parent = b{bone.ParentIndex}\n");
			}

			pythonScript.Append("bpy.ops.object.editmode_toggle()\n");

			// And a third time for the pose bones, which are different entities in blender
			for (var bi = 1; bi < model.Skeleton.Count; ++bi)
			{
				var bone = model.Skeleton[bi];
				var t = bone.Transform;

				pythonScript.Append($"b{bi} = root.pose.bones[{bi - 1}]\n");
				pythonScript.Append($"b{bi}.matrix_basis = (({t.M11},{t.M12},{t.M13},{t.M14}),({t.M21},{t.M22},{t.M23},{t.M24}),({t.M31},{t.M32},{t.M33},{t.M34}),({t.M41},{t.M42},{t.M43},{t.M44}))\n");
			}

			pythonScript.Append("bpy.ops.object.select_all(action='DESELECT')\n");
		}

		private void BuildModel(H3DModel model)
		{
			pythonScript.Append($"m = bpy.data.meshes.new('{model.Name}')\n");
			pythonScript.Append($"o = bpy.data.objects.new('{model.Name}', m)\n");
			pythonScript.Append($"o.parent = root\n");
			pythonScript.Append($"bpy.context.scene.objects.link(o)\n");
			pythonScript.Append($"bm = bmesh.new()\n");
			pythonScript.Append($"uvl = bm.loops.layers.uv.new()\n");

			var vertices = new List<BLENDVertex>();
			var tris = new List<int[]>();
			var loops = new List<BLENDLoop>();
			var groups = new List<string>();
			var materials = new List<int>();

			foreach (var mesh in model.Meshes)
			{
				if (mesh.Type == H3DMeshType.Silhouette) continue;

				if (!materials.Contains(mesh.MaterialIndex))
					materials.Add(mesh.MaterialIndex);

				var name = model.MeshNodesTree.Find(mesh.NodeIndex);

				if (!groups.Contains(name))
					groups.Add(name);

				var groupIndex = groups.IndexOf(name);

				var newVertices = mesh.GetVertices();
				var ids = new Dictionary<int, int>();
				for (var vi = 0; vi < newVertices.Length; ++vi)
				{
					var vert = newVertices[vi];
					var index = vertices.FindIndex(x => x.Matches(vert));

					if (index == -1) // New vertex
					{
						index = vertices.Count;
						vertices.Add(new BLENDVertex(vert, groupIndex));
					}

					ids.Add(vi, index);
				}

				var newTris = new List<int[]>();
				foreach (var subMesh in mesh.SubMeshes)
				{
					for (var ti = 0; ti < subMesh.Indices.Length; ti += 3)
					{
						var tri = new int[] { subMesh.Indices[ti], subMesh.Indices[ti + 1], subMesh.Indices[ti + 2] };

						if (newTris.Any(x => x.SequenceEqual(tri))) continue;

						newTris.Add(tri);
						var formattedTri = tri.Select(x => ids[x]).ToArray();

						for (var i = 0; i < tri.Length; ++i)
						{
							var vert = newVertices[tri[i]];
							loops.Add(new BLENDLoop
							{
								vert = formattedTri[i],
								face = tris.Count,
								uv = new Vector2(vert.TexCoord0.X, vert.TexCoord0.Y)
							});
						}

						tris.Add(formattedTri);
					}
				}
			}

			pythonScript.Append($"vs = []\n");

			// Y and Z are swapped in blender's space
			foreach (var v in vertices)
			{
				pythonScript.Append($"v = bm.verts.new([{v.vert.Position.X},{-v.vert.Position.Z},{v.vert.Position.Y}])\n");
				pythonScript.Append($"v.normal = [{v.vert.Normal.X},{-v.vert.Normal.Z},{v.vert.Normal.Y}]\n");
				pythonScript.Append($"vs.append(v)\n");
			}

			pythonScript.Append("bm.verts.index_update()\n");

			for (var ti = 0; ti < tris.Count; ++ti)
			{
				var tri = tris[ti];

				pythonScript.Append($"f = bm.faces.new([vs[{tri[0]}],vs[{tri[1]}],vs[{tri[2]}]])\n");
				pythonScript.Append("f.smooth = True\n");
				pythonScript.Append("lv = {}\n");

				foreach (var loop in loops.Where(x => x.face == ti))
					pythonScript.Append($"lv['{loop.vert}'] = [{loop.uv.X},{loop.uv.Y}]\n");

				pythonScript.Append($"for loop in f.loops: loop[uvl].uv = lv[str(loop.vert.index)]\n");
			}

			foreach (var mat in materials)
				pythonScript.Append($"o.data.materials.append(mat{mat})\n");

			pythonScript.Append($"bm.to_mesh(m)\n");
			pythonScript.Append($"bm.free()\n");
			pythonScript.Append($"bpy.context.scene.objects.active = o\n");
			pythonScript.Append($"o.select = True\n");
			pythonScript.Append($"bpy.ops.object.editmode_toggle()\n");
			pythonScript.Append($"bpy.ops.object.editmode_toggle()\n");
			pythonScript.Append($"bpy.ops.object.select_all(action='DESELECT')\n");

			for (var gi = 0; gi < groups.Count; ++gi)
			{
				var indexes = vertices.Where(x => x.vertGroup == gi).Select(x => vertices.IndexOf(x));
			
				pythonScript.Append($"vg = o.vertex_groups.new('{groups[gi]}')\n");
				pythonScript.Append($"vg.add([{string.Join(",", indexes)}], 1, 'ADD')\n");
			}
		}
		#endregion
	}
}
