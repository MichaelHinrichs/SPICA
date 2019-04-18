using SPICA.Formats.CtrH3D;
using SPICA.Formats.CtrH3D.Model;
using SPICA.Formats.CtrH3D.Model.Mesh;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading;

namespace SPICA.Formats.Generic.Blender
{
	public class BLEND
	{
		const float SCALE = 0.01f;

		private readonly string blenderPath;
		private StringBuilder pythonScript;

		public BLEND(H3D scene, int mdlIndex, int animIndex = -1)
		{
			Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo("en-US");

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

				if ((model.Skeleton?.Count ?? 0) > 0)
					BuildArmature(model);

				if (model.Meshes.Count > 0)
					BuildModel(model);
			}
		}

		public void Save(string fileName)
		{
			if (!File.Exists(blenderPath))
				throw new FileNotFoundException("Blender not found");

			pythonScript.AppendLine($"bpy.ops.wm.save_as_mainfile(filepath='{fileName}')");

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

			#region Base Script
			pythonScript = new StringBuilder(
$@"import bpy
import bmesh
import math
import mathutils
def vec_roll_to_mat3(vec, roll):
	target = mathutils.Vector((0,1,0))
	nor = vec.normalized()
	axis = target.cross(nor)
	if axis.dot(axis) > 0.0000000001:
		axis.normalize()
		theta = target.angle(nor)
		bMatrix = mathutils.Matrix.Rotation(theta, 3, axis)
	else:
		updown = 1 if target.dot(nor) > 0 else -1
		bMatrix = mathutils.Matrix.Scale(updown, 3)
		bMatrix[2][2] = 1.0
	rMatrix = mathutils.Matrix.Rotation(roll, 3, nor)
	mat = rMatrix * bMatrix
	return mat
def mat3_to_vec_roll(mat):
	vec = mat.col[1]
	vecmat = vec_roll_to_mat3(mat.col[1], 0)
	vecmatinv = vecmat.inverted()
	rollmat = vecmatinv * mat
	roll = math.atan2(rollmat[0][2], rollmat[2][2])
	return vec, roll
bpy.context.scene.render.engine = 'BLENDER_RENDER'
bpy.ops.object.select_all()
bpy.ops.object.delete()
root = bpy.data.objects.new('{modelName}', {armature})
bpy.context.scene.objects.link(root)
"
			);
			#endregion
		}

		private void BuildMaterials(H3DModel model)
		{
			for (var mi = 0; mi < model.Materials.Count; ++mi)
			{
				var mat = model.Materials[mi];
				pythonScript.AppendLine($"mat{mi} = bpy.data.materials.new('{mat.Name}')");

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

					pythonScript.AppendLine($"tex{mi}_{ti} = bpy.data.textures.new('{tex}', type='IMAGE')");
					pythonScript.AppendLine($"slt{mi}_{ti} = mat{mi}.texture_slots.add()");
					pythonScript.AppendLine($"slt{mi}_{ti}.texture = tex{mi}_{ti}");
				}
			}
		}

		private void BuildArmature(H3DModel model)
		{
			pythonScript.AppendLine("bpy.context.scene.objects.active = root");
			pythonScript.AppendLine("bpy.ops.object.editmode_toggle()");

			var parentString = new StringBuilder();

			for (var bi = 0; bi < model.Skeleton.Count; ++bi)
			{
				var bone = model.Skeleton[bi];
				var t = bone.GetWorldTransform(model.Skeleton)
					* new Matrix4x4(
						SCALE, 0f, 0f, 0f,
						0f, SCALE, 0f, 0f,
						0f, 0f, SCALE, 0f,
						0f, 0f, 0f, 1f
					)
					* new Matrix4x4(
						1f, 0f, 0f, 0f,
						0f, (float)Math.Cos(-Math.PI / 2), -(float)Math.Sin(-Math.PI / 2), 0f,
						0f, (float)Math.Sin(-Math.PI / 2), (float)Math.Cos(-Math.PI / 2), 0f,
						0f, 0f, 0f, 1f
					);
				var pos = t.Translation;

				if (bone.ParentIndex != -1)
					parentString.AppendLine($"b{bi}.parent = b{bone.ParentIndex}");

				pythonScript.AppendLine($"axis, roll = mat3_to_vec_roll(mathutils.Matrix((({t.M11},{t.M12},{t.M13},{t.M14}),({t.M21},{t.M22},{t.M23},{t.M24}),({t.M31},{t.M32},{t.M33},{t.M34}),({t.M41},{t.M42},{t.M43},{t.M44}))).to_3x3())");
				pythonScript.AppendLine($"pos = mathutils.Vector([{pos.X},{pos.Y},{pos.Z}])");
				pythonScript.AppendLine($"b{bi} = root.data.edit_bones.new('{bone.Name}')");
				pythonScript.AppendLine($"b{bi}.head = pos");
				pythonScript.AppendLine($"b{bi}.tail = pos + axis");
				pythonScript.AppendLine($"b{bi}.roll = roll");
			}

			pythonScript.Append(parentString);
			pythonScript.AppendLine("bpy.ops.object.editmode_toggle()");
			pythonScript.AppendLine("bpy.ops.object.select_all(action='DESELECT')");
		}

		private void BuildModel(H3DModel model)
		{
			pythonScript.AppendLine($"m = bpy.data.meshes.new('{model.Name}')");
			pythonScript.AppendLine($"o = bpy.data.objects.new('{model.Name}', m)");
			pythonScript.AppendLine($"o.parent = root");
			pythonScript.AppendLine($"bpy.context.scene.objects.link(o)");
			pythonScript.AppendLine($"bm = bmesh.new()");
			pythonScript.AppendLine($"uvl = bm.loops.layers.uv.new()");

			var vertices = new List<BLENDVertex>();
			var tris = new List<int[]>();
			var loops = new List<BLENDLoop>();
			var groups = new List<BLENDVertGroup>();
			var materials = new List<int>();

			foreach (var mesh in model.Meshes)
			{
				if (mesh.Type == H3DMeshType.Silhouette) continue;

				if (!materials.Contains(mesh.MaterialIndex))
					materials.Add(mesh.MaterialIndex);

				var newVertices = mesh.GetVertices();
				var ids = new Dictionary<int, int>();
				for (var vi = 0; vi < newVertices.Length; ++vi)
				{
					var vert = newVertices[vi];
					var index = vertices.FindIndex(x => x.Matches(vert));

					if (index == -1) // New vertex
					{
						index = vertices.Count;
						vertices.Add(new BLENDVertex(vert));
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

					// Has armature and controller
					if (subMesh.BoneIndicesCount > 0 && (model.Skeleton?.Count ?? 0) > 0)
					{
						if (subMesh.Skinning == H3DSubMeshSkinning.Smooth)
						{
							for (int vi = 0; vi < newVertices.Length; ++vi)
							{
								var vertex = newVertices[vi];

								for (int i = 0; i < 4; i++)
								{
									var index = vertex.Indices[i];
									var weight = vertex.Weights[i];

									if (weight == 0) break;

									if (index < subMesh.BoneIndices.Length && index >= 0)
										index = subMesh.BoneIndices[index];
									else
										index = 0;

									var gName = model.Skeleton[index].Name;
									var gIndex = groups.FindIndex(x => x.name == gName);

									if (gIndex == -1)
									{
										gIndex = groups.Count;
										groups.Add(new BLENDVertGroup(gName));
									}

									var realVertex = vertices[ids[vi]];

									if (!realVertex.vertGroup.ContainsKey(gIndex))
										realVertex.vertGroup.Add(gIndex, weight);
								}
							}
						}
						else
						{
							for (int vi = 0; vi < newVertices.Length; ++vi)
							{
								var vertex = newVertices[vi];
								var index = vertex.Indices[0];

								if (index < subMesh.BoneIndices.Length && index >= 0)
									index = subMesh.BoneIndices[index];
								else
									index = 0;

								var gName = model.Skeleton[index].Name;

								if (!groups.Any(x => x.name == gName))
									groups.Add(new BLENDVertGroup(gName));

								var gIndex = groups.FindIndex(x => x.name == gName);
								var realVertex = vertices[ids[vi]];

								if (!realVertex.vertGroup.ContainsKey(gIndex))
									realVertex.vertGroup.Add(gIndex, 1f);
							}
						}
					}
				}
			}

			pythonScript.AppendLine($"vs = []");

			// Y and Z are swapped in blender's space
			foreach (var v in vertices)
			{
				var pos = new Vector3(v.vert.Position.X, -v.vert.Position.Z, v.vert.Position.Y) * SCALE;
				var normal = new Vector3(v.vert.Normal.X, -v.vert.Normal.Z, v.vert.Normal.Y);

				pythonScript.AppendLine($"v = bm.verts.new([{pos.X},{pos.Y},{pos.Z}])");
				pythonScript.AppendLine($"v.normal = [{normal.X},{normal.Y},{normal.Z}]");
				pythonScript.AppendLine($"vs.append(v)");
			}

			pythonScript.AppendLine("bm.verts.index_update()");

			for (var ti = 0; ti < tris.Count; ++ti)
			{
				var tri = tris[ti];

				pythonScript.AppendLine($"f = bm.faces.new([vs[{tri[0]}],vs[{tri[1]}],vs[{tri[2]}]])");
				pythonScript.AppendLine("f.smooth = True");
				pythonScript.AppendLine("lv = {}");

				foreach (var loop in loops.Where(x => x.face == ti))
					pythonScript.AppendLine($"lv['{loop.vert}'] = [{loop.uv.X},{loop.uv.Y}]");

				pythonScript.AppendLine($"for loop in f.loops: loop[uvl].uv = lv[str(loop.vert.index)]");
			}

			foreach (var mat in materials)
				pythonScript.AppendLine($"o.data.materials.append(mat{mat})");

			pythonScript.AppendLine($"bm.to_mesh(m)");
			pythonScript.AppendLine($"bm.free()");
			pythonScript.AppendLine($"bpy.context.scene.objects.active = o");
			pythonScript.AppendLine($"o.select = True");
			pythonScript.AppendLine($"bpy.ops.object.editmode_toggle()");
			pythonScript.AppendLine($"bpy.ops.object.editmode_toggle()");
			pythonScript.AppendLine($"bpy.ops.object.select_all(action='DESELECT')");

			for (var gi = 0; gi < groups.Count; ++gi)
			{
				pythonScript.AppendLine($"vg = o.vertex_groups.new('{groups[gi].name}')");

				foreach (var vert in vertices.Where(x => x.vertGroup.ContainsKey(gi)))
				{
					var weight = vert.vertGroup[gi];
					var index = vertices.IndexOf(vert);

					pythonScript.AppendLine($"vg.add([{index}], {weight}, 'ADD')");
				}
			}

			if ((model.Skeleton?.Count ?? 0) > 0)
			{
				pythonScript.AppendLine($"mod = o.modifiers.new(name='Skeleton', type='ARMATURE')");
				pythonScript.AppendLine($"mod.object = root");
				pythonScript.AppendLine($"mod.use_deform_preserve_volume = True");
			}
		}
		#endregion
	}
}
