using SPICA.Formats.Common;
using SPICA.Formats.CtrH3D;
using SPICA.Formats.CtrH3D.Model;
using SPICA.Formats.CtrH3D.Model.Material;
using SPICA.Formats.CtrH3D.Model.Mesh;
using SPICA.Formats.CtrH3D.Texture;
using SPICA.Math3D;
using SPICA.PICA.Commands;
using SPICA.PICA.Converters;


using System;
using System.Text;
using System.Collections.Generic;
using System.IO;
using System.Numerics;

namespace SPICA.Formats.Generic.MaterialScript
{
    public class MaterialDump
    {
        private StringBuilder text;

        //public MaterialScript() { }

        public MaterialDump(H3D Scene, int MdlIndex, int AnimIndex = -1)  //TODO: Needs more object-oriented-ness
        {
            if (MdlIndex != -1)
            {
                H3DModel Mdl = Scene.Models[MdlIndex];

                //Initialize text Stringbuilder
                text = new StringBuilder($"Material data for {Mdl.Name}\n\n");

                //if model has no materials, abort
                if (Mdl.Materials.Count < 1)
                {
                    text.AppendLine("None");
                    return;
                }

                //Write details for each material
                foreach (H3DMaterial Mtl in Mdl.Materials)
                {
                    text.AppendLine(Mtl.Name);

                    //Write color properties
                    text.AppendLine($"  Ambient Color: {Mtl.MaterialParams.AmbientColor}");
                    text.AppendLine($"  Diffuse Color: {Mtl.MaterialParams.DiffuseColor}");
                    text.AppendLine($"  Specular 0 Color: {Mtl.MaterialParams.Specular0Color}");
                    text.AppendLine($"  Specular 1 Color: {Mtl.MaterialParams.Specular1Color}");
                    text.AppendLine($"  Emission Color: {Mtl.MaterialParams.EmissionColor}");
                    text.Append('\n');

                    //TODO: write constants listed directly in Material? (they don't always match those in the stages)

                    //Write Texture properties
                    if (Mtl.Texture0Name != null && Mtl.Texture0Name.Length > 0) WriteTextureString(Mtl, 0);
                    if (Mtl.Texture1Name != null && Mtl.Texture1Name.Length > 0) WriteTextureString(Mtl, 1);
                    if (Mtl.Texture2Name != null && Mtl.Texture2Name.Length > 0) WriteTextureString(Mtl, 2);
                    //text.Append('\n');

                    //write Alpha test properties
                    text.AppendLine("  Alpha Test");
                    text.AppendLine($"    Enabled: {Mtl.MaterialParams.AlphaTest.Enabled}");
                    text.AppendLine($"    Function: {Mtl.MaterialParams.AlphaTest.Function}");
                    text.AppendLine($"    Reference Value: {Mtl.MaterialParams.AlphaTest.Reference}\n");

                    //Write blend function properties
                    text.AppendLine("  Blending");
                    text.AppendLine($"    Blend Color: {Mtl.MaterialParams.BlendColor}");
                    text.AppendLine($"    Blend Color Dst Function: {Mtl.MaterialParams.BlendFunction.ColorDstFunc}");
                    text.AppendLine($"    Blend Color Src Function: {Mtl.MaterialParams.BlendFunction.ColorSrcFunc}");
                    text.AppendLine($"    Blend Color Equation: {Mtl.MaterialParams.BlendFunction.ColorEquation}");
                    text.AppendLine($"    Blend Alpha Dst Function: {Mtl.MaterialParams.BlendFunction.AlphaDstFunc}");
                    text.AppendLine($"    Blend Alpha Src Function: {Mtl.MaterialParams.BlendFunction.AlphaSrcFunc}");
                    text.AppendLine($"    Blend Alpha Equation: {Mtl.MaterialParams.BlendFunction.AlphaEquation}\n");

                    //Write Texture stages (layers)
                    text.AppendLine($"  TexEnv Stages:");
                    foreach (PICATexEnvStage stage in Mtl.MaterialParams.TexEnvStages)
                    {
                        WriteStageString(stage);
                        text.Append('\n');
                    }


                    text.Append("\n\n");
                }
            } //MdlIndex != -1
        }

        public void Save(string FileName)
        {
            File.WriteAllText(FileName, text.ToString());
        }



        #region Utility Functions


        private void WriteTextureString(H3DMaterial mat, int idx)
        {
            text.AppendLine($"  Texture{idx}");
            text.Append($"    Name: ");
            switch (idx) //Select the texture from the material
            {
                case 1: text.AppendLine(mat.Texture1Name); break;
                case 2: text.AppendLine(mat.Texture2Name); break;
                default: text.AppendLine(mat.Texture0Name); break;
            }

            text.AppendLine($"    Map Channel: {mat.MaterialParams.TextureSources[idx]}");
            text.AppendLine($"    Mapping Type: {mat.MaterialParams.TextureCoords[idx].MappingType}");
            text.AppendLine($"    Wrap U/V: {mat.TextureMappers[idx].WrapU}/{mat.TextureMappers[idx].WrapV}");
            text.AppendLine($"    Map Scale: {mat.MaterialParams.TextureCoords[idx].Scale}");
            text.AppendLine($"    Map Translation: {mat.MaterialParams.TextureCoords[idx].Translation}");
            text.AppendLine($"    Map Rotation: {mat.MaterialParams.TextureCoords[idx].Rotation}");

            //TODO: add support for additional mapping settings?

            text.Append('\n');
        }

        private void WriteStageString(PICATexEnvStage stage)
        {
            text.Append("    Color = ");
            if (stage.IsColorPassThrough) text.AppendLine("Passthrough");
            else
            {
                WriteCombinerString(stage.Combiner.Color, stage.Source.Color, stage.Operand.Color);
                text.Append($", (scale: {stage.Scale.Color})");
                text.Append('\n');
            }

            text.Append("    Alpha = ");
            if (stage.IsAlphaPassThrough) text.AppendLine("Passthrough");
            else
            {
                WriteCombinerString(stage.Combiner.Alpha, stage.Source.Alpha, stage.Operand.Alpha);
                text.Append($", (scale: {stage.Scale.Color})");
                text.Append('\n');
            }

            text.AppendLine($"    Update Color Buffer: {stage.UpdateColorBuffer}");
            text.AppendLine($"    Update Alpha Buffer: {stage.UpdateAlphaBuffer}");
            text.AppendLine($"    Constant: {stage.Color}");
        }

        private void WriteCombinerString<T>(PICATextureCombinerMode mode, PICATextureCombinerSource[] sources, T[] ops)
        {
            switch (mode)
            {
                case PICATextureCombinerMode.Replace:
                    text.Append($"{sources[0]}({ops[0]})");
                    break;
                case PICATextureCombinerMode.Add:
                    text.Append($"{sources[0]}({ops[0]}) + {sources[1]}({ops[1]})");
                    break;
                case PICATextureCombinerMode.AddSigned:
                    text.Append($"{sources[0]}({ops[0]}) + {sources[1]}({ops[1]})  (signed)");
                    break;
                case PICATextureCombinerMode.Subtract:
                    text.Append($"{sources[0]}({ops[0]}) - {sources[1]}({ops[1]})");
                    break;
                case PICATextureCombinerMode.Modulate:
                    text.Append($"{sources[0]}({ops[0]}) * {sources[1]}({ops[1]})");
                    break;
                case PICATextureCombinerMode.AddMult:
                    text.Append($"( {sources[0]}({ops[0]}) + {sources[1]}({ops[1]}) ) * {sources[2]}({ops[2]})");
                    break;
                case PICATextureCombinerMode.MultAdd:
                    text.Append($"{sources[0]}({ops[0]}) * {sources[1]}({ops[1]}) + {sources[2]}({ops[2]})");
                    break;

                //TODO: print details for these
                case PICATextureCombinerMode.Interpolate:
                case PICATextureCombinerMode.DotProduct3Rgb:
                case PICATextureCombinerMode.DotProduct3Rgba:
                default:
                    text.Append($"{mode}(");
                    for (int i = 0; i < 3; i++)
                    {
                        text.Append($"{sources[i]}({ops[i]}), ");
                    }
                    text.Append(')');
                    break;
            }
        }
        #endregion

    }
}
