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
        private StringBuilder script;

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

        public MaterialScript(H3D Scene, int MdlIndex, int AnimIndex = -1)  //TODO: Needs more object-oriented-ness
        {
            if (MdlIndex != -1)
            {
                H3DModel Mdl = Scene.Models[MdlIndex];

                if (Mdl.Materials.Count < 1) return; //if model has no materials, abort
            
                int stageN;

                script = new StringBuilder("vtxColor = VertexColor()\n\n");

                foreach (H3DMaterial Mtl in Mdl.Materials)
                {
                    //Basic Material Setup
                    script.Append(
                          $"{Mtl.Name}_mat = standardMaterial()\n"
                        + $"{Mtl.Name}_mat.name = \"{Mtl.Name}_mat\"\n"
                        + $"{Mtl.Name}_mat.shaderByName = \"phong\"\n"
                        + $"{Mtl.Name}_mat.showInViewport = true\n"
                        + $"{Mtl.Name}_mat.ambient = {GetMaxColor(Mtl.MaterialParams.AmbientColor)}\n"
                        + $"{Mtl.Name}_mat.diffuse = {GetMaxColor(Mtl.MaterialParams.DiffuseColor)}\n"      //TODO: should these colors include the alpha value?
                        + $"{Mtl.Name}_mat.specular = {GetMaxColor(Mtl.MaterialParams.Specular0Color)}\n"
                        + $"{Mtl.Name}_mat.specularLevel = {Mtl.MaterialParams.Specular0Color.A/2.55d}"
                        + '\n');

                    //create bitmap maps for textures
                    if (Mtl.Texture0Name != null && Mtl.Texture0Name.Length > 0) script.Append(GetTextureString(Mtl, 0));
                    if (Mtl.Texture1Name != null && Mtl.Texture1Name.Length > 0) script.Append(GetTextureString(Mtl, 1));
                    if (Mtl.Texture2Name != null && Mtl.Texture2Name.Length > 0) script.Append(GetTextureString(Mtl, 2));
                    script.Append('\n');

                    //create diffuse composite map
                    script.Append("map = compositeMap()\n");

                    //Setup diffuse map stages
                    stageN = 1;
                    foreach (PICATexEnvStage stage in Mtl.MaterialParams.TexEnvStages)
                    {
                        if (stage.IsColorPassThrough) continue;                                 //if passthrough stage, skip
                        if (stage.UpdateColorBuffer)                                            //if this stage updates the color Buffer
                        {
                            script.Append("buffer = copy(map)\n");                                 //  store copy of composite map as "buffer"
                            if (stage.Source.Color[0] != PICATextureCombinerSource.Previous)    //  if current stage is not using previous   //TODO: Check for "Previous" in 1 or 2?
                            {                                                                       
                                script.Append("map = compositeMap()\n");                           //    start a new composite map
                                stageN = 1;
                            }
                        }

                        //assign the stage's const  //TODO: this isn't always the right color, figure out when to us MaterialParams.Constant#Color instead (if Constant#Adssignment == stage number, use that one?)
                        script.Append($"const = rgbMult color1: [{stage.Color.R},{stage.Color.G},{stage.Color.B}]\n");

                        //create layers based on combiner type //TODO: put more comments here
                        for (int i = 0; i < combinerTxtCount[(int)(stage.Combiner.Color)]; i++)
                        {
                            //if operand is "Previous" don't add a layer for it
                            if (stage.Source.Color[i] == PICATextureCombinerSource.Previous) continue;  //TODO: Throw exception if 1 or 2 is "Previous"?

                            if (i == 2 && stage.Combiner.Color == PICATextureCombinerMode.Interpolate)  //if combiner mode is "Interpolate", assign the last operand as a mask to the previous layer
                            {
                                script.Append($"map.mask[{stageN-1}] = {GetSourceStringColor(stage, i, script)}\n");
                            }
                            else  //otherwise add a layer for the operand and set the layer's blend mode based on the stage's combiner mode
                            {
                                script.Append($"map.mapList[{stageN}] = {GetSourceStringColor(stage, i, script)}\n");
                                script.Append($"map.blendMode[{stageN}] = {combinerOps[(int)(stage.Combiner.Color), i]}\n");
                                stageN++;
                            }
                        }
                    }

                    //assign composite map to main material
                    script.Append($"{Mtl.Name}_mat.maps[2] = map\n");


                    //TODO: only create/assign alpha if it's used
                    //create alpha composite map
                    script.Append("map = compositeMap()\n");

                    //Setup alpha map stages
                    stageN = 1;
                    foreach (PICATexEnvStage stage in Mtl.MaterialParams.TexEnvStages)
                    {   
                        if (stage.IsAlphaPassThrough) continue;                                 //if passthrough stage, skip
                        if (stage.UpdateAlphaBuffer)                                            //if this stage updates the color Buffer
                        {
                            script.Append("buffer = copy(map)\n");                                 //  store copy of composite map as "buffer"
                            if (stage.Source.Alpha[0] != PICATextureCombinerSource.Previous)    //  if current stage is not using previous   //TODO: Check for "Previous" in 1 or 2
                            {
                                script.Append("map = compositeMap()\n");                           //    start a new composite map
                                stageN = 1;
                            }
                        }

                        //TODO: create alpha composite map

                    }


                    script.Append("\n\n");                 
                }

                //create material assignment loop
                script.Append("for OBJ in Geometry do\n(\n  if OBJ.material != undefined then\n  (\n");
                foreach (H3DMaterial Mtl in Mdl.Materials)
                {
                    script.Append($"    if OBJ.material.name == \"{Mtl.Name}_mat\" then OBJ.material = {Mtl.Name}_mat\n");
                }
                script.Append("  )\n)\n");

            } //MdlIndex != -1
        }

        public void Save(string FileName)
        {
            File.WriteAllText(FileName, script.ToString());
        }




        private string GetMaxColor(RGBA color)
        {
            return $"color {color.R} {color.G} {color.B} {color.A}";
        }

        /// <summary>
        /// Creates MaxScript code to create a bitmap texture map for the selected texture in the given material
        /// </summary>
        /// <param name="mat">The H3D material containing the desired texture</param>
        /// <param name="idx">the index of the desired texture</param>
        /// <returns>MaxScript code string</returns>
        private string GetTextureString(H3DMaterial mat, int idx)
        {
            StringBuilder txtString = new StringBuilder($"txt{idx} = bitmapTexture()\n");

            switch(idx) //Select the texture from the material
            {
                case 1:  txtString.AppendLine($"txt{idx}.filename = \"./{mat.Texture1Name}.png\""); break;
                case 2:  txtString.AppendLine($"txt{idx}.filename = \"./{mat.Texture2Name}.png\""); break;
                default: txtString.AppendLine($"txt{idx}.filename = \"./{mat.Texture0Name}.png\""); break;
            }
            txtString.AppendLine($"txt{idx}.name = \"{mat.Name}_d_txt{idx}\""); //set map name
            txtString.AppendLine($"txt{idx}.preMultAlpha = false");             //disable pre-multiplied alpha

            //if texture uses non-default map channel, add the code to set it
            if (mat.MaterialParams.TextureSources[idx] > 0)
                txtString.AppendLine($"txt{idx}.coordinates.mapChannel = {mat.MaterialParams.TextureSources[idx]+1}");

            //TODO: add support for additional mapping settings

            return txtString.ToString();
        }

        /// <summary>
        /// returns the name of the indicated source from the given stage and adds a color correction map if needed
        /// </summary>
        /// <param name="stage">The stage containing the desired source</param>
        /// <param name="srcIdx">the index of the desired source</param>
        /// <param name="mat">the StringBuilder to add the collor correction map code to</param>
        /// <returns>MaxScript variable name of source</returns>
        private string GetSourceStringColor(PICATexEnvStage stage, int srcIdx, StringBuilder mat)
        {
            //if source is vertex colors and operand is alpha (Workaround for Max's inablity to use vertex alpha in materials.  Copy of vertex alpha must be in last map channel)
            if (stage.Source.Color[srcIdx] == PICATextureCombinerSource.PrimaryColor && (stage.Operand.Color[srcIdx] == PICATextureCombinerColorOp.Alpha || stage.Operand.Color[srcIdx] == PICATextureCombinerColorOp.OneMinusAlpha))
            {
                mat.Append("vtxAlpha = copy(vtxColor)\n"
                        + $"vtxAlpha.map = {2}\n"   //TODO: get last+1 map channel id
                        +  "vtxAlpha.subid = 1\n");
                return "vtxAlpha";
            }

            //if operand is not just color
            else if (stage.Operand.Color[srcIdx] != PICATextureCombinerColorOp.Color)
            {
                //TODO: don't color correct constants, just make new ones

                //create a collor correction map to select the color channels
                mat.Append("ccmap = ColorCorrection()\n"
                        + $"ccmap.map = {sources[(int)(stage.Source.Color[srcIdx])]}\n"
                        + $"ccmap.rewireR = {operandChannels[(int)(stage.Operand.Color[srcIdx])][0]}\n"
                        + $"ccmap.rewireG = {operandChannels[(int)(stage.Operand.Color[srcIdx])][1]}\n"
                        + $"ccmap.rewireB = {operandChannels[(int)(stage.Operand.Color[srcIdx])][2]}\n");
                return "ccmap"; //return the color map
            }

            //otherwise just return the source
            return sources[(int)(stage.Source.Color[srcIdx])];
        }

    }
}
