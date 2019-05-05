using SPICA.Formats.CtrH3D;
using SPICA.Formats.CtrH3D.Animation;
using SPICA.Formats.CtrH3D.Model;
using SPICA.Formats.CtrH3D.Model.Mesh;
using SPICA.Math3D;
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
		const float PI = (float)Math.PI;
		const float SCALE = 0.025f;
		const float ANGLE = -PI / 2;

		private readonly Matrix4x4 MTX_SCALE = new Matrix4x4(
											SCALE, 0f, 0f, 0f,
											0f, SCALE, 0f, 0f,
											0f, 0f, SCALE, 0f,
											0f, 0f, 0f, 1f
										);

		private readonly Matrix4x4 MTX_ANGLE = new Matrix4x4(
											1f, 0f, 0f, 0f,
											0f, (float)Math.Cos(ANGLE), (float)-Math.Sin(ANGLE), 0f,
											0f, (float)Math.Sin(ANGLE), (float)Math.Cos(ANGLE), 0f,
											0f, 0f, 0f, 1f
									);

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

				if (animIndex != -1)
					BuildAnimations(scene.SkeletalAnimations[animIndex], model);
			}
		}

		public void Save(string fileName)
		{
			if (!File.Exists(blenderPath))
				throw new FileNotFoundException("Blender not found");

			pythonScript.AppendLine($"bpy.ops.wm.save_as_mainfile(filepath='{fileName}')");

			var scriptPath = Path.Combine(Path.GetTempPath(), Path.GetFileNameWithoutExtension(fileName) + ".py");
			File.WriteAllText(scriptPath, pythonScript.ToString());

			var startInfo = new ProcessStartInfo
			{
				FileName = blenderPath,
				Arguments = $"-P \"{scriptPath}\" --factory-startup"
			};

			Process.Start(startInfo).WaitForExit();
			//File.Delete(scriptPath);
		}

		#region Private Methods
		private void CleanUpScene(H3DModel model)
		{
			var armature = model.Skeleton.Count == 0 ? "None" : $"bpy.data.armatures.new('{model.Skeleton[0].Name}')";
			var modelName = model.Skeleton.Count == 0 ? model.Name : model.Skeleton[0].Name;

			pythonScript = new StringBuilder($"import bpy\nimport bmesh\nfor o in bpy.data.objects: bpy.data.objects.remove(o, do_unlink=True)\nroot = bpy.data.objects.new('{modelName}', {armature})\nbpy.context.scene.collection.objects.link(root)\nbpy.context.scene.render.fps = 29\nbpy.context.scene.render.fps_base = {29f / 29.97f}\n");
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

					//pythonScript.AppendLine($"tex{mi}_{ti} = bpy.data.textures.new('{tex}', type='IMAGE')");
					//pythonScript.AppendLine($"slt{mi}_{ti} = mat{mi}.texture_slots.add()");
					//pythonScript.AppendLine($"slt{mi}_{ti}.texture = tex{mi}_{ti}");
				}
			}
		}

		private void BuildArmature(H3DModel model)
		{
			pythonScript.AppendLine("bpy.context.view_layer.objects.active = root");
			pythonScript.AppendLine("bpy.ops.object.editmode_toggle()");

			var parentString = new StringBuilder();

			for (var bi = 0; bi < model.Skeleton.Count; ++bi)
			{
				var bone = model.Skeleton[bi];
				var t = bone.GetWorldTransform(model.Skeleton) * MTX_ANGLE * MTX_SCALE;

				var head = t.Translation;
				var tail = head + Vector3.UnitZ;

				if (bone.ParentIndex != -1)
					parentString.AppendLine($"b{bi}.parent = b{bone.ParentIndex}");

				pythonScript.AppendLine($"b{bi} = root.data.edit_bones.new('{bone.Name}')");
				pythonScript.AppendLine($"b{bi}.head = [{head.X},{head.Y},{head.Z}]");
				pythonScript.AppendLine($"b{bi}.tail = [{tail.X},{tail.Y},{tail.Z}]");
				//pythonScript.AppendLine($"b{bi}.roll = {roll}");
			}

			pythonScript.Append(parentString);
			pythonScript.AppendLine("bpy.ops.object.editmode_toggle()");
			pythonScript.AppendLine("bpy.ops.object.select_all(action='DESELECT')");
		}

		private void BuildAnimations(H3DAnimation anim, H3DModel model)
		{
			pythonScript.AppendLine($"act1 = bpy.data.actions.new('{anim.Name}')");
			pythonScript.AppendLine($"root.animation_data_create()");
			pythonScript.AppendLine($"root.animation_data.action = act1");

			foreach (var element in anim.Elements)
			{
				var fc = new BLENDFCurve(element.Content);

				if (fc.IsNull || fc.NothingExists()) continue;

				var bone = model.Skeleton.FirstOrDefault(x => x.Name == element.Name);
				Matrix4x4.Decompose(bone.Transform, out var _, out var lr, out var ll);
				Matrix4x4.Decompose(bone.GetWorldTransform(model.Skeleton), out var _, out var wr, out var wl);

				H3DBone parentBone = null;

				if (bone.ParentIndex != -1)
					parentBone = model.Skeleton[bone.ParentIndex];

				var bData = $"pose.bones[\"{element.Name}\"]";

				pythonScript.AppendLine($"flx = act1.fcurves.new(data_path='{bData}.location', index=0)");
				pythonScript.AppendLine($"fly = act1.fcurves.new(data_path='{bData}.location', index=1)");
				pythonScript.AppendLine($"flz = act1.fcurves.new(data_path='{bData}.location', index=2)");

				if (fc.IsQuaternion)
				{
					pythonScript.AppendLine($"root.pose.bones['{element.Name}'].rotation_mode = 'QUATERNION'");
					pythonScript.AppendLine($"frw = act1.fcurves.new(data_path='{bData}.rotation_quaternion', index=0)");
					pythonScript.AppendLine($"frx = act1.fcurves.new(data_path='{bData}.rotation_quaternion', index=1)");
					pythonScript.AppendLine($"fry = act1.fcurves.new(data_path='{bData}.rotation_quaternion', index=2)");
					pythonScript.AppendLine($"frz = act1.fcurves.new(data_path='{bData}.rotation_quaternion', index=3)");
				}
				else
				{
					pythonScript.AppendLine($"root.pose.bones['{element.Name}'].rotation_mode = 'XYZ'");
					pythonScript.AppendLine($"frx = act1.fcurves.new(data_path='{bData}.rotation_euler', index=0)");
					pythonScript.AppendLine($"fry = act1.fcurves.new(data_path='{bData}.rotation_euler', index=1)");
					pythonScript.AppendLine($"frz = act1.fcurves.new(data_path='{bData}.rotation_euler', index=2)");
				}

				pythonScript.AppendLine($"fsx = act1.fcurves.new(data_path='{bData}.scale', index=0)");
				pythonScript.AppendLine($"fsy = act1.fcurves.new(data_path='{bData}.scale', index=1)");
				pythonScript.AppendLine($"fsz = act1.fcurves.new(data_path='{bData}.scale', index=2)");

				for (int frame = 0; frame <= anim.FramesCount; ++frame)
				{
					var l = (ll - fc.GetLocationAtFrame(frame)) * SCALE;
					pythonScript.AppendLine($"flx.keyframe_points.insert({frame + 1}, {l.Z})");
					pythonScript.AppendLine($"fly.keyframe_points.insert({frame + 1}, {-l.Y})");
					pythonScript.AppendLine($"flz.keyframe_points.insert({frame + 1}, {-l.X})");

					var r = fc.GetRotationAtFrame(frame);

					if (r is Vector3 rv)
					{
						pythonScript.AppendLine($"frx.keyframe_points.insert({frame + 1}, {rv.Z})");
						pythonScript.AppendLine($"fry.keyframe_points.insert({frame + 1}, {-rv.Y})");
						pythonScript.AppendLine($"frz.keyframe_points.insert({frame + 1}, {-rv.X})");
					}
					else if (r is Quaternion rq)
					{
						rq = Quaternion.Multiply(lr, Quaternion.Inverse(rq));
						pythonScript.AppendLine($"frw.keyframe_points.insert({frame + 1}, {rq.W})");
						pythonScript.AppendLine($"frx.keyframe_points.insert({frame + 1}, {rq.Z})");
						pythonScript.AppendLine($"fry.keyframe_points.insert({frame + 1}, {-rq.Y})");
						pythonScript.AppendLine($"frz.keyframe_points.insert({frame + 1}, {-rq.X})");
					}

					var s = fc.GetScaleAtFrame(frame);
					pythonScript.AppendLine($"fsx.keyframe_points.insert({frame + 1}, {s.X})");
					pythonScript.AppendLine($"fsy.keyframe_points.insert({frame + 1}, {s.Y})");
					pythonScript.AppendLine($"fsz.keyframe_points.insert({frame + 1}, {s.Z})");
				}
			}
		}

		private void BuildModel(H3DModel model)
		{
			pythonScript.AppendLine($"m = bpy.data.meshes.new('{model.Name}')");
			pythonScript.AppendLine($"o = bpy.data.objects.new('{model.Name}', m)");
			pythonScript.AppendLine($"o.parent = root");
			pythonScript.AppendLine($"bpy.context.scene.collection.objects.link(o)");
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
			pythonScript.AppendLine($"bpy.context.view_layer.objects.active = o");
			pythonScript.AppendLine($"bpy.ops.object.editmode_toggle()");
			pythonScript.AppendLine($"bpy.ops.object.editmode_toggle()");
			pythonScript.AppendLine($"bpy.ops.object.select_all(action='DESELECT')");

			for (var gi = 0; gi < groups.Count; ++gi)
			{
				pythonScript.AppendLine($"vg = o.vertex_groups.new(name='{groups[gi].name}')");

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
