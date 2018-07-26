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

namespace SPICA.Formats.Generic.MaterialScript
{
    public class MaterialScript
    {
        private List<string> mats;

        //MaxScript variable names to be used for the various sources
        private Dictionary<int, string> sources = new Dictionary<int, string>()
        {
            { 0,  "vtxColor" },
            { 1,  "fragPrimary" },
            { 2,  "fragSecondary" },
            { 3,  "txt0" },
            { 4,  "txt1" },
            { 5,  "txt2" },
            { 6,  "txt3" },
            { 13, "buffer" },
            { 14, "const" },
            { 15, "prev" }
        };

        
        //number of sources used by a combiner
        private int[] combinerTxtCount = new int[] {1, 2, 2, 2, 3, 2, 3, 3, 3, 3};

        //3DS Max composite blend mode id for each source of a combiner
        private int[,] combinerOps = new int[,] 
        { 
            { 0, 0, 0 },  //Replace
            { 0, 5, 0 },  //Modulate
            { 0, 2, 0 },  //Add
            { 0, 2, 0 },  //AddSigned
            { 0, 0, 0 },  //Interpolate
            { 0, 3, 0 },  //Subtract
            { 0, 5, 5 },  //DotProduct3Rgb
            { 0, 5, 5 },  //DotProduct3Rgba
            { 0, 5, 2 },  //MultAdd
            { 0, 2, 5 }   //AddMult
        };

        //3DS Max color correction rbg channel ids for a given operand
        private Dictionary<int, int[]> operandChannels = new Dictionary<int, int[]>()
        {
            { 1, new int[] {4,5,6} },   //OneMinusColor
            { 2, new int[] {3,3,3} },   //Alpha
            { 3, new int[] {7,7,7} },   //OneMinusAlpha
            { 4, new int[] {0,0,0} },   //Red
            { 5, new int[] {4,4,4} },   //OneMinusRed
            { 8, new int[] {1,1,1} },   //Green
            { 9, new int[] {5,5,5} },   //OneMinusGreen
            {12, new int[] {2,2,2} },   //Blue
            {13, new int[] {6,6,6} },   //OneMinusBlue
        };

        public MaterialScript() { }

        public MaterialScript(H3D Scene, int MdlIndex, int AnimIndex = -1)
        {
            if (MdlIndex != -1)
            {
                H3DModel Mdl = Scene.Models[MdlIndex];

                if (Mdl.Materials.Count < 1) //Check for existence of model? (also move this up?)
                    return;

                mats = new List<string>(Mdl.Materials.Count);
                string mat;     //TODO: switch to StringBuilder?
                string temp;
            
                int stageA;
                int stageC;

                mats.Add("vtxColor = VertexColor()\n");

                mats.Add("const = rgbmult()\n");

                foreach (H3DMaterial Mtl in Mdl.Materials)
                {
                    //Basic Material Setup
                    mat = $"{Mtl.Name}_mat = standardMaterial()\n"
                        + $"{Mtl.Name}_mat.name = \"{Mtl.Name}_mat\"\n"
                        + $"{Mtl.Name}_mat.shaderByName = \"phong\"\n"
                        + $"{Mtl.Name}_mat.showInViewport = true\n"
                        + $"{Mtl.Name}_mat.ambient = {GetMaxColor(Mtl.MaterialParams.AmbientColor)}\n"
                        + $"{Mtl.Name}_mat.diffuse = {GetMaxColor(Mtl.MaterialParams.DiffuseColor)}\n"
                        + $"{Mtl.Name}_mat.specular = {GetMaxColor(Mtl.MaterialParams.Specular0Color)}\n"  //TODO: use specular alpha as specular level?
                        + '\n';

                    //create bitmap maps for textures
                    if (Mtl.Texture0Name != null && Mtl.Texture0Name.Length > 0) mat += GetTextureString(0, Mtl);
                    if (Mtl.Texture1Name != null && Mtl.Texture1Name.Length > 0) mat += GetTextureString(1, Mtl);
                    if (Mtl.Texture2Name != null && Mtl.Texture2Name.Length > 0) mat += GetTextureString(2, Mtl);
                    mat += '\n';

                    //create diffuse composite map
                    mat += "map = compositeMap()\n";

                    //Setup diffuse map stages
                    stageC = 1;
                    foreach (PICATexEnvStage stage in Mtl.MaterialParams.TexEnvStages)
                    {
                        if (stage.IsColorPassThrough) continue; //if passthrough, skip
                        if (stage.UpdateColorBuffer)            //if "UpdateBuffer"
                        {
                            mat += "buffer = copy(map)\n";      //store copy of composite as "buffer"
                            if (stage.Source.Color[0] != PICATextureCombinerSource.Previous)    //if not using previous, start a new composite map  //TODO: Check for "Previous" in 1 or 2
                            {
                                mat += "map = compositeMap()\n";
                                stageC = 1;
                            }
                        }

                        //create layers based on combiner type
                        for (int i = 0; i < combinerTxtCount[(int)(stage.Combiner.Color)]; i++)
                        {
                            if (stage.Source.Color[i] == PICATextureCombinerSource.Previous) continue;  //TODO: Throw exception if 1 or 2 is "Previous"

                            if (i == 2 && stage.Combiner.Color == PICATextureCombinerMode.Interpolate)
                            {
                                temp = ChannelSelectColor(stage, i, ref mat);
                                mat += $"map.mask[{stageC-1}] = {temp}\n";
                            }
                            else
                            {
                                temp = ChannelSelectColor(stage, i, ref mat);
                                mat += $"map.mapList[{stageC}] = {temp}\n";
                                mat += $"map.blendMode[{stageC}] = {combinerOps[(int)(stage.Combiner.Color), i]}\n";
                                stageC++;
                            }
                        }
                    }

                    //assign composite map to main material
                    mat += $"{Mtl.Name}_mat.maps[2] = map";

                    stageA = 0;
                    //TODO: create alpha composite map


                    mats.Add(mat+"\n\n");                 
                }

                //create material assignment loop
                mat = "for OBJ in Geometry do\n(\nif OBJ.material != undefined then\n(\n";
                foreach (H3DMaterial Mtl in Mdl.Materials)
                {
                    mat += $"if OBJ.material.name == \"{Mtl.Name}_mat\" then OBJ.material = {Mtl.Name}_mat\n";
                }
                mat += ")\n)\n";

                mats.Add(mat);

            } //MdlIndex != -1
        }

        public void Save(string FileName)
        {
            File.WriteAllLines(FileName, mats.ToArray());
        }

        private string StageToString(PICATexEnvStage stage)
        {
            string stageStr = "Color: ";

            stageStr += stage.Combiner.Color; stageStr += ", ";
            stageStr = stageStr + stage.Source.Color[0] + '-' + stage.Operand.Color[0] + ", ";
            stageStr = stageStr + stage.Source.Color[1] + '-' + stage.Operand.Color[1] + ", ";
            stageStr = stageStr + stage.Source.Color[2] + '-' + stage.Operand.Color[2];

            return stageStr;
        }

        private string GetMaxColor(RGBA color)
        {
            return $"color {color.R} {color.G} {color.B} {color.A}";
        }

        private string GetTextureString(int idx, H3DMaterial mat)
        {
            //TODO: add mapping setting support
            StringBuilder txtString = new StringBuilder($"txt{idx} = bitmapTexture()\n");

            switch(idx)
            {
                case 1:  txtString.AppendLine($"txt{idx}.filename = \"./{mat.Texture1Name}.png\""); break;
                case 2:  txtString.AppendLine($"txt{idx}.filename = \"./{mat.Texture2Name}.png\""); break;
                default: txtString.AppendLine($"txt{idx}.filename = \"./{mat.Texture0Name}.png\""); break;
            }
            txtString.AppendLine($"txt{idx}.name = \"{mat.Name}_d_txt{idx}\"");
            if (mat.MaterialParams.TextureSources[idx] > 0)
                txtString.AppendLine($"txt{idx}.coordinates.mapChannel = {mat.MaterialParams.TextureSources[idx]+1}");
            txtString.AppendLine($"txt{idx}.preMultAlpha = false");
            return txtString.ToString();
        }

        private string ChannelSelectColor(PICATexEnvStage stage, int srcIdx, ref string mat)
        {
            //TODO: add Max vertex alpha workaround
            if (stage.Operand.Color[srcIdx] != PICATextureCombinerColorOp.Color)
            {
                mat = mat +  "ccmap = ColorCorrection()\n"
                          + $"ccmap.map = {sources[(int)(stage.Source.Color[srcIdx])]}\n"
                          + $"ccmap.rewireR = {operandChannels[(int)(stage.Operand.Color[srcIdx])][0]}\n"
                          + $"ccmap.rewireG = {operandChannels[(int)(stage.Operand.Color[srcIdx])][1]}\n"
                          + $"ccmap.rewireB = {operandChannels[(int)(stage.Operand.Color[srcIdx])][2]}\n";
                return "ccmap";
            }
            return sources[(int)(stage.Source.Color[srcIdx])];
        }

    }
}
