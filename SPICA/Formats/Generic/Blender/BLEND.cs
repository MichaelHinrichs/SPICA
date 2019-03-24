using SPICA.Formats.Common;
using SPICA.Formats.CtrH3D;
using SPICA.Formats.CtrH3D.Model;
using System;
using System.Diagnostics;
using System.IO;

namespace SPICA.Formats.Generic.Blender
{
	public class BLEND
	{
		private readonly string blenderPath = "";
		private string pythonScript = "import bpy\nimport bmesh\n";

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

			Process.Start(startInfo);
		}

		#region Private Methods
		private string BuildModel(H3DModel model)
		{
			if (model.Meshes.Count == 0)
				return "";

			var res =
				$"root = bpy.data.objects.new('{model.Name}', None)\n" +
				$"bpy.context.scene.objects.link(root)\n";

			for (var mi = 0; mi < model.Meshes.Count; ++mi)
			{
				var mesh = model.Meshes[mi];
				var vertices = MeshTransform.GetWorldSpaceVertices(model.Skeleton, mesh);

				for (var smi = 0; smi < mesh.SubMeshes.Count; ++smi)
				{
					var subMesh = mesh.SubMeshes[smi];

					res += $"m{mi}_{smi} = bpy.data.meshes.new('{model.MeshNodesTree.Find(mesh.NodeIndex)}_{mi}_{smi}')\n";
					res += $"o{mi}_{smi} = bpy.data.objects.new('{model.MeshNodesTree.Find(mesh.NodeIndex)}_{mi}_{smi}', m{mi}_{smi})\n";
					res += $"o{mi}_{smi}.parent = root\n";
					res += $"bpy.context.scene.objects.link(o{mi}_{smi})\n";

					res += $"bm{mi}_{smi} = bmesh.new()\n";

					foreach (var vert in vertices)
						res += $"bm{mi}_{smi}.verts.new(({vert.Position.X},{vert.Position.Y},{vert.Position.Z}))\n";

					res += $"bm{mi}_{smi}.to_mesh(m{mi}_{smi})\n";
					res += $"bm{mi}_{smi}.free()\n";
				}
			}

			return res;
		}
		#endregion
	}
}
