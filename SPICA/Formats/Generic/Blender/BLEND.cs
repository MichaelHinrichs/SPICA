using SPICA.Formats.CtrH3D;
using SPICA.Formats.CtrH3D.Model;
using SPICA.Formats.CtrH3D.Model.Mesh;
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

			pythonScript += $"bpy.ops.wm.save_as_mainfile(filepath = '{fileName}')\n";

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

			return
				"import bpy\n" +
				"import bmesh\n" +
				"import math\n" +
				"import mathutils\n" +
				"bpy.context.scene.render.engine = 'BLENDER_RENDER'\n" +
				"bpy.ops.object.select_all()\n" +
				"bpy.ops.object.delete()\n" +
				$"root = bpy.data.objects.new('{model.Name}', {armature})\n" +
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
				//res += $"b{bi}.use_connect = True\n";
			}

			res += "bpy.ops.object.editmode_toggle()\n";

			// And a third time for the pose bones, which are different entities in blender
			for (var bi = 1; bi < model.Skeleton.Count; ++bi)
			{
				var bone = model.Skeleton[bi];
				var t = bone.Transform;

				res += $"b{bi} = root.pose.bones[{bi-1}]\n";
				res += $"b{bi}.matrix_basis = (({t.M11},{t.M12},{t.M13},{t.M14}),({t.M21},{t.M22},{t.M23},{t.M24}),({t.M31},{t.M32},{t.M33},{t.M34}),({t.M41},{t.M42},{t.M43},{t.M44}))\n";
			}

			res += "bpy.ops.object.select_all(action='DESELECT')\n";

			return res;
		}

		private string BuildModel(H3DModel model)
		{
			var res = "";

			for (var mi = 0; mi < model.Meshes.Count; ++mi)
			{
				var mesh = model.Meshes[mi];

				if (mesh.Type == H3DMeshType.Silhouette) continue;

				var vertices = mesh.GetVertices();

				res += $"m{mi} = bpy.data.meshes.new('{model.MeshNodesTree.Find(mesh.NodeIndex)}')\n";
				res += $"o{mi} = bpy.data.objects.new('{model.MeshNodesTree.Find(mesh.NodeIndex)}', m{mi})\n";
				res += $"o{mi}.parent = root\n";
				res += $"bpy.context.scene.objects.link(o{mi})\n";
				res += $"vs{mi} = []\n";
				res += $"uv{mi} = []\n";
				res += $"bm{mi} = bmesh.new()\n";

				for (var smi = 0; smi < mesh.SubMeshes.Count; ++smi)
				{
					var subMesh = mesh.SubMeshes[smi];
					var tris = subMesh.Indices;

					// Y and Z are swapped in blender's space
					for (var vi = 0; vi < vertices.Length; ++vi)
					{
						var vert = vertices[vi];
					
						res += $"v{vi} = bm{mi}.verts.new([{vert.Position.X},{-vert.Position.Z},{vert.Position.Y}])\n";
						res += $"v{vi}.normal = [{vert.Normal.X},{-vert.Normal.Z},{vert.Normal.Y}]\n";
						res += $"vs{mi}.append(v{vi})\n";
						res += $"uv{mi}.append(({vert.TexCoord0.X},{vert.TexCoord0.Y}))\n";
					}

					// Blender does not like duplicate tris, so we need to add an additional check
					var placedTris = new List<ushort[]>();
					for (ushort i = 0; i < tris.Length; i += 3)
					{
						var tri = new ushort[] { tris[i], tris[i + 1], tris[i + 2] };

						if (placedTris.Any(x => x.SequenceEqual(tri)))
							continue;

						res += $"bm{mi}.faces.new([vs{mi}[{tri[0]}],vs{mi}[{tri[1]}],vs{mi}[{tri[2]}]]).smooth = True\n";
						placedTris.Add(tri);
					}
				}

				// UV coords in blender are mapped per loop
				res += $"bm{mi}.verts.index_update()\n";
				res += $"uvl{mi} = bm{mi}.loops.layers.uv.new()\n";
				res += $"for face in bm{mi}.faces:\n\tfor loop in face.loops:\n\t\t";
				res += $"loop[uvl{mi}].uv = uv{mi}[loop.vert.index]\n";

				res += $"bm{mi}.to_mesh(m{mi})\n";
				res += $"bm{mi}.free()\n";
				res += $"o{mi}.data.materials.append(mat{mesh.MaterialIndex})\n";

				// For some reason, blender struggles with custom normals, until you toggle edit mode
				res += $"bpy.context.scene.objects.active = o{mi}\n";
				res += $"o{mi}.select = True\n";
				res += $"bpy.ops.object.editmode_toggle()\n";
				res += $"bpy.ops.object.editmode_toggle()\n";
				res += $"bpy.ops.object.select_all(action='DESELECT')\n";
			}

			return res;
		}
		#endregion
	}
}
