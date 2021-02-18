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


//TODO: add projection mapping compatibility
//TODO: Fix model alpha not drawing from Vtx Color alpha (notable on town water meshes)

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

        //the highest map channel used by the currently processed texture
        private int maxMapChannel;

        //constant color for the currently processed stage
        private RGBA stageColor;

        //public MaterialScript() { }

        public MaterialScript(H3D Scene, int MdlIndex, int AnimIndex = -1)  //TODO: Needs more object-oriented-ness?
        {
            if (MdlIndex != -1)
            {
                H3DModel Mdl = Scene.Models[MdlIndex];

                if (Mdl.Materials.Count < 1) return; //if model has no materials, abort
            
                //
                int stageN;
                PICATexEnvStage stage;

                //Initialize script Stringbuilder
                script = new StringBuilder("vtxColor = VertexColor()\n");
                script.Append("scriptPath = getThisScriptFilename()\nscriptPath = getFilenamePath scriptPath\n\n");
                //TODO: add script path get, append it to texture files

                foreach (H3DMaterial Mtl in Mdl.Materials)
                {
                    #region >=============(Basic Material Setup)=============<
                    //Assign basic properties
                    script.Append(
                          $"{Mtl.Name}_mat = standardMaterial()\n"
                        + $"{Mtl.Name}_mat.name = \"{Mtl.Name}_mat\"\n"
                        + $"{Mtl.Name}_mat.shaderByName = \"phong\"\n"
                        + $"{Mtl.Name}_mat.showInViewport = true\n"
                        + $"{Mtl.Name}_mat.ambient = {GetMaxColorString(Mtl.MaterialParams.AmbientColor)}\n"
                        + $"{Mtl.Name}_mat.diffuse = {GetMaxColorString(Mtl.MaterialParams.DiffuseColor)}\n"      //TODO: should these colors include the alpha value?
                        + $"{Mtl.Name}_mat.specular = {GetMaxColorString(Mtl.MaterialParams.Specular0Color)}\n"
                        + $"{Mtl.Name}_mat.specularLevel = {Mtl.MaterialParams.Specular0Color.A/2.55d}\n"
                        //TODO: set 2-sidedness
                        + '\n');

                    //create bitmap maps for textures
                    if (Mtl.Texture0Name != null && Mtl.Texture0Name.Length > 0) script.Append(GetTextureString(Mtl, 0));
                    if (Mtl.Texture1Name != null && Mtl.Texture1Name.Length > 0) script.Append(GetTextureString(Mtl, 1));
                    if (Mtl.Texture2Name != null && Mtl.Texture2Name.Length > 0) script.Append(GetTextureString(Mtl, 2));
                    script.Append('\n');

                    //find the greatest used map channel (may be unreliable, assumes all map channels on the mesh are used by at least one texture)
                    maxMapChannel = 0;
                    for (int i = 0; i < Mtl.MaterialParams.TextureCoords.Length; i++)
                    {//TODO: is scale != <0,0> the best way to tell if the texture coord is used?
                        if (Mtl.MaterialParams.TextureSources[i]+1 > maxMapChannel && Mtl.MaterialParams.TextureCoords[i].MappingType == H3DTextureMappingType.UvCoordinateMap && Mtl.MaterialParams.TextureCoords[i].Scale != Vector2.Zero)
                            maxMapChannel = (int)Mtl.MaterialParams.TextureSources[i] + 1;
                    }

                    #endregion


                    #region >=============(Diffuse Map Creation)=============<
                    //create diffuse composite map
                    script.Append("comp = compositeMap()\n");

                    //Setup diffuse map stages
                    stageN = 1;
                    for (int j=0; j < Mtl.MaterialParams.TexEnvStages.Length; j++)
                    {
                        stage = Mtl.MaterialParams.TexEnvStages[j];
                        if (stage.IsColorPassThrough) continue;                                 //if passthrough stage, skip
                        if (stage.UpdateColorBuffer)                                            //if this stage updates the color Buffer
                        {
                            script.Append("buffer = copy(comp)\n");                             //  store copy of composite map as "buffer"
                            if (stage.Source.Color[0] != PICATextureCombinerSource.Previous)    //  if current stage is not using previous   //TODO: Check for "Previous" in 1 or 2?
                            {                                                                       
                                script.Append("comp = compositeMap()\n");                       //    start a new composite map
                                stageN = 1;
                            }
                        }

                        //assign the stage's const color
                        stageColor = GetConstColor(Mtl, j);

                        //create layers based on combiner type //TODO: put more comments here
                        for (int i = 0; i < combinerTxtCount[(int)(stage.Combiner.Color)]; i++)
                        {
                            //if operand is "Previous" don't add a layer for it
                            if (stage.Source.Color[i] == PICATextureCombinerSource.Previous) continue;  //TODO: Throw exception if 1 or 2 is "Previous"?

                            if (i == 2 && stage.Combiner.Color == PICATextureCombinerMode.Interpolate)  //if combiner mode is "Interpolate", assign the last source as a mask to the previous layer
                            {
                                script.Append($"comp.mask[{stageN-1}] = {GetSourceStringColor(stage, i)}\n");
                            }
                            else  //otherwise add a layer for the source and set the layer's blend mode based on the stage's combiner mode
                            {
                                script.Append($"comp.mapList[{stageN}] = {GetSourceStringColor(stage, i)}\n");
                                script.Append($"comp.blendMode[{stageN}] = {combinerOps[(int)(stage.Combiner.Color), i]}\n");
                                stageN++;
                            }
                        }
                    }

                    //assign composite map to main material
                    script.Append($"{Mtl.Name}_mat.diffuseMap = comp\n");
                    #endregion


                    #region >=============(Alpha Map Creation)=============<
                    //only create/assign alpha if it's used
                    if (!Mtl.MaterialParams.AlphaTest.Enabled && Mtl.MaterialParams.BlendFunction.ColorDstFunc == PICABlendFunc.Zero && Mtl.MaterialParams.BlendFunction.AlphaDstFunc == PICABlendFunc.Zero)
                    {
                        //Alpha source of "DestinationColor" is a special exception
                        if(Mtl.MaterialParams.BlendFunction.AlphaSrcFunc == PICABlendFunc.DestinationColor)//if alpha src = DestinationColor, use vtx alpha
                        {
                            script.Append("vtxAlpha = VertexColor()\n"
                                + $"vtxAlpha.map = {maxMapChannel + 1}\n"
                                + "vtxAlpha.subid = 1\n");
                            script.Append($"{Mtl.Name}_mat.opacityMap = vtxAlpha\n");
                        }
                        script.Append("\n\n");
                        continue;   //skip to next material
                    }

                    //reenable texture alpha if alpha test is enabled
                    if (Mtl.MaterialParams.AlphaTest.Enabled)  //TODO: this could be better somehow
                    {
                        if (Mtl.Texture0Name != null && Mtl.Texture0Name.Length > 0) script.AppendLine($"txt{0}.alphasource = 0");
                        if (Mtl.Texture1Name != null && Mtl.Texture1Name.Length > 0) script.AppendLine($"txt{1}.alphasource = 0");
                        if (Mtl.Texture2Name != null && Mtl.Texture2Name.Length > 0) script.AppendLine($"txt{2}.alphasource = 0");
                        script.Append('\n');
                    }
                        

                    //create alpha composite map
                    script.Append("comp = compositeMap()\n");

                    //Setup alpha map stages
                    stageN = 1;
                    for (int j= 0; j < Mtl.MaterialParams.TexEnvStages.Length; j++)
                    {
                        stage = Mtl.MaterialParams.TexEnvStages[j];
                        if (stage.IsAlphaPassThrough) continue;                                 //if passthrough stage, skip
                        if (stage.UpdateAlphaBuffer)                                            //if this stage updates the alpha Buffer
                        {
                            script.Append("buffer = copy(comp)\n");                             //  store copy of composite map as "buffer"
                            if (stage.Source.Alpha[0] != PICATextureCombinerSource.Previous)    //  if current stage is not using previous   //TODO: Check for "Previous" in 1 or 2
                            {
                                script.Append("comp = compositeMap()\n");                       //    start a new composite map
                                stageN = 1;
                            }
                        }

                        //assign the stage's const color
                        stageColor = GetConstColor(Mtl, j);

                        //create layers based on combiner type
                        for (int i = 0; i < combinerTxtCount[(int)(stage.Combiner.Alpha)]; i++)
                        {
                            //if operand is "Previous" don't add a layer for it
                            if (stage.Source.Alpha[i] == PICATextureCombinerSource.Previous) continue;  //TODO: Throw exception if 1 or 2 is "Previous"?

                            if (i == 2 && stage.Combiner.Alpha == PICATextureCombinerMode.Interpolate)  //if combiner mode is "Interpolate", assign the last source as a mask to the previous layer
                            {
                                script.Append($"comp.mask[{stageN - 1}] = {GetSourceStringAlpha(stage, i)}\n");
                            }
                            else  //otherwise add a layer for the source and set the layer's blend mode based on the stage's combiner mode
                            {
                                script.Append($"comp.mapList[{stageN}] = {GetSourceStringAlpha(stage, i)}\n");
                                script.Append($"comp.blendMode[{stageN}] = {combinerOps[(int)(stage.Combiner.Alpha), i]}\n");
                                stageN++;
                            }
                        }
                    }

                    //assign composite map to main material
                    script.Append($"{Mtl.Name}_mat.opacityMap = comp\n");
                    #endregion

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



        #region Utility Functions
        private string GetMaxColorString(RGBA color)
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
                case 1:  txtString.AppendLine($"txt{idx}.filename = scriptPath + \"/{mat.Texture1Name}.png\""); break;
                case 2:  txtString.AppendLine($"txt{idx}.filename = scriptPath + \"/{mat.Texture2Name}.png\""); break;
                default: txtString.AppendLine($"txt{idx}.filename = scriptPath + \"/{mat.Texture0Name}.png\""); break;
            }

            //set map name and disable pre-multiplied alpha
            txtString.AppendLine($"txt{idx}.name = \"{mat.Name}_txt{idx}\""); 
            txtString.AppendLine($"txt{idx}.preMultAlpha = false");

            //if alpha test is enabled, disable alpha on the diffuse texture
            if (mat.MaterialParams.AlphaTest.Enabled)                           
                txtString.AppendLine($"txt{idx}.alphasource = 2");              //TODO: ensure this doesn't interfere with decal textures (or anything else for that matter)

            //if texture uses non-default map channel, add the code to set it
            if (mat.MaterialParams.TextureSources[idx] > 0)                     
                txtString.AppendLine($"txt{idx}.coordinates.mapChannel = {mat.MaterialParams.TextureSources[idx]+1}");

            //set translation if non-zero
            if (mat.MaterialParams.TextureCoords[idx].Translation != new Vector2(0))
            {
                txtString.AppendLine($"txt{idx}.coordinates.U_Offset = {mat.MaterialParams.TextureCoords[idx].Translation.X}");
                txtString.AppendLine($"txt{idx}.coordinates.V_Offset = {mat.MaterialParams.TextureCoords[idx].Translation.Y}");
            }

            //set rotation if non-zero (Max needs degrees, so convert)
            if (mat.MaterialParams.TextureCoords[idx].Rotation != 0)
                txtString.AppendLine($"txt{idx}.coordinates.W_Angle = {180*mat.MaterialParams.TextureCoords[idx].Rotation/Math.PI}");

            //set scale if not one
            if (mat.MaterialParams.TextureCoords[idx].Scale != new Vector2(1))
            {
                txtString.AppendLine($"txt{idx}.coordinates.U_Tiling = {mat.MaterialParams.TextureCoords[idx].Scale.X}");
                txtString.AppendLine($"txt{idx}.coordinates.V_Tiling = {mat.MaterialParams.TextureCoords[idx].Scale.Y}");
            }

            //set map wrapping  //TODO: this sucks, do it better
            //TODO: mirroring seems to mess with scaling. Correct it in here?
            switch (mat.TextureMappers[idx].WrapU)
            {
                //SPICA.PICA.Commands.PICATextureWrap.Repeat //Max's Default
                case PICATextureWrap.Mirror:
                    txtString.AppendLine($"txt{idx}.coordinates.U_Mirror = true");
                    break;
                case PICATextureWrap.ClampToEdge:
                    txtString.AppendLine($"--TODO: txt{idx} ClampToEdge U");
                    break;
                case PICATextureWrap.ClampToBorder:
                    txtString.AppendLine($"--TODO: txt{idx} ClampToBorder U");
                    break;
            }
            switch (mat.TextureMappers[idx].WrapV)
            {
                //SPICA.PICA.Commands.PICATextureWrap.Repeat //Max's Default
                case PICATextureWrap.Mirror:
                    txtString.AppendLine($"txt{idx}.coordinates.V_Mirror = true");
                    break;
                case PICATextureWrap.ClampToEdge:
                    txtString.AppendLine($"--TODO: txt{idx} ClampToEdge V");
                    break;
                case PICATextureWrap.ClampToBorder:
                    txtString.AppendLine($"--TODO: txt{idx} ClampToBorder V");
                    break;
            }

            txtString.AppendLine($"--TODO: txt{idx} Mapping Type: {mat.MaterialParams.TextureCoords[idx].MappingType}");

            //TODO: add support for additional mapping settings?

            return txtString.ToString();
        }

        /// <summary>
        /// Returns the name of the indicated color source from the given stage and adds a color correction map if needed
        /// Member StringBuilder 'script' must be valid
        /// </summary>
        /// <param name="stage">The stage containing the desired source</param>
        /// <param name="srcIdx">the index of the desired source</param>
        /// <returns>MaxScript variable name of the source</returns>
        private string GetSourceStringColor(PICATexEnvStage stage, int srcIdx)
        {
            //TODO: Ensure validity of script member var

            int op = (int)(stage.Operand.Color[srcIdx]);

            //source 2 of "Interpolate" combiner must be inverted oppositely to everything else
            if (stage.Combiner.Color == PICATextureCombinerMode.Interpolate && srcIdx == 2)
                op = op + 1 - 2*(op % 2);

            //if source is vertex colors and operand is alpha (Workaround for Max's inablity to use vertex alpha in materials.  Copy of vertex alpha must be in last map channel)
            if (stage.Source.Color[srcIdx] == PICATextureCombinerSource.PrimaryColor && (op == (int)PICATextureCombinerColorOp.Alpha || op == (int)PICATextureCombinerColorOp.OneMinusAlpha))
            {
                script.Append("vtxAlpha = VertexColor()\n"
                        + $"vtxAlpha.map = {maxMapChannel+1}\n"
                        +  "vtxAlpha.subid = 1\n");

                //if inverse alpha, apply color correction map for inversion
                if (op == (int)PICATextureCombinerColorOp.OneMinusAlpha)
                {
                    script.Append("ccmap = ColorCorrection()\n"
                                + "ccmap.map = vtxAlpha\n"
                                + "ccmap.rewireMode = 2\n");
                    return "ccmap";
                }

                return "vtxAlpha";
            }

            //if source is a constant, create one based on the operand
            if (stage.Source.Color[srcIdx] == PICATextureCombinerSource.Constant)
            {
                switch (op)
                {
                    case (int)PICATextureCombinerColorOp.OneMinusColor: return $"rgbMult color1: [{255-stageColor.R},{255-stageColor.G},{255-stageColor.B}]\n";
                    case (int)PICATextureCombinerColorOp.Alpha: return $"rgbMult color1: [{stageColor.A},{stageColor.A},{stageColor.A}]\n";
                    case (int)PICATextureCombinerColorOp.OneMinusAlpha: return $"rgbMult color1: [{255-stageColor.A},{255-stageColor.A},{255-stageColor.A}]\n";
                    case (int)PICATextureCombinerColorOp.Red: return $"rgbMult color1: [{stageColor.R},{stageColor.R},{stageColor.R}]\n";
                    case (int)PICATextureCombinerColorOp.OneMinusRed: return $"rgbMult color1: [{255 - stageColor.R},{255 - stageColor.R},{255 - stageColor.R}]\n";
                    case (int)PICATextureCombinerColorOp.Green: return $"rgbMult color1: [{stageColor.G},{stageColor.G},{stageColor.G}]\n";
                    case (int)PICATextureCombinerColorOp.OneMinusGreen: return $"rgbMult color1: [{255 - stageColor.G},{255 - stageColor.G},{255 - stageColor.G}]\n";
                    case (int)PICATextureCombinerColorOp.Blue: return $"rgbMult color1: [{stageColor.B},{stageColor.B},{stageColor.B}]\n";
                    case (int)PICATextureCombinerColorOp.OneMinusBlue: return $"rgbMult color1: [{255 - stageColor.B},{255 - stageColor.B},{255 - stageColor.B}]\n";
                    default: return $"rgbMult color1: [{stageColor.R},{stageColor.G},{stageColor.B}]\n";
                }
            }

            //if operand is not just color
            if (op != (int)PICATextureCombinerColorOp.Color)    //else?
            {
                //create a color correction map to select the color channels
                script.Append("ccmap = ColorCorrection()\n"
                        + $"ccmap.map = {sources[(int)(stage.Source.Color[srcIdx])]}\n"
                        + $"ccmap.rewireR = {operandChannels[op][0]}\n"
                        + $"ccmap.rewireG = {operandChannels[op][1]}\n"
                        + $"ccmap.rewireB = {operandChannels[op][2]}\n");
                return "ccmap"; //return the color map
            }

            //otherwise just return the source
            return sources[(int)(stage.Source.Color[srcIdx])];
        }

        /// <summary>
        /// Returns the name of the indicated alpha source from the given stage and adds a color correction map if needed
        /// Member StringBuilder 'script' must be valid
        /// </summary>
        /// <param name="stage">The stage containing the desired source</param>
        /// <param name="srcIdx">the index of the desired source</param>
        /// <returns>MaxScript variable name of the source</returns>
        private string GetSourceStringAlpha(PICATexEnvStage stage, int srcIdx)
        {
            //TODO: Ensure validity of script member var

            int op = (int)stage.Operand.Alpha[srcIdx];

            //source 2 of "Interpolate" combiner must be inverted oppositely to everything else
            if (stage.Combiner.Alpha == PICATextureCombinerMode.Interpolate && srcIdx == 2)
                op = op + 1 - 2 * (op % 2);

            //if source is vertex colors and operand is alpha (Workaround for Max's inablity to use vertex alpha in materials.  Copy of vertex alpha must be in last map channel)
            if (stage.Source.Alpha[srcIdx] == PICATextureCombinerSource.PrimaryColor && (op == (int)PICATextureCombinerAlphaOp.Alpha || op == (int)PICATextureCombinerAlphaOp.OneMinusAlpha))
            {
                script.Append("vtxAlpha = VertexColor()\n"
                        + $"vtxAlpha.map = {maxMapChannel + 1}\n"
                        + "vtxAlpha.subid = 1\n");

                //if inverse alpha, apply color correction map for inversion
                if (op == (int)PICATextureCombinerAlphaOp.OneMinusAlpha)
                {
                    script.Append("ccmap = ColorCorrection()\n"
                                + "ccmap.map = vtxAlpha\n"
                                + "ccmap.rewireMode = 2\n");
                    return "ccmap";
                }

                return "vtxAlpha";
            }

            //No color correction for buffer
            if (stage.Source.Alpha[srcIdx] == PICATextureCombinerSource.PreviousBuffer)
                return "buffer";

            //if source is a constant, create one based on the operand
            if (stage.Source.Color[srcIdx] == PICATextureCombinerSource.Constant)
            {
                switch (op)
                {
                    case (int)PICATextureCombinerAlphaOp.OneMinusAlpha: return $"rgbMult color1: [{255 - stageColor.A},{255 - stageColor.A},{255 - stageColor.A}]\n";
                    case (int)PICATextureCombinerAlphaOp.Red: return $"rgbMult color1: [{stageColor.R},{stageColor.R},{stageColor.R}]\n";
                    case (int)PICATextureCombinerAlphaOp.OneMinusRed: return $"rgbMult color1: [{255 - stageColor.R},{255 - stageColor.R},{255 - stageColor.R}]\n";
                    case (int)PICATextureCombinerAlphaOp.Green: return $"rgbMult color1: [{stageColor.G},{stageColor.G},{stageColor.G}]\n";
                    case (int)PICATextureCombinerAlphaOp.OneMinusGreen: return $"rgbMult color1: [{255 - stageColor.G},{255 - stageColor.G},{255 - stageColor.G}]\n";
                    case (int)PICATextureCombinerAlphaOp.Blue: return $"rgbMult color1: [{stageColor.B},{stageColor.B},{stageColor.B}]\n";
                    case (int)PICATextureCombinerAlphaOp.OneMinusBlue: return $"rgbMult color1: [{255 - stageColor.B},{255 - stageColor.B},{255 - stageColor.B}]\n";
                    default: return $"rgbMult color1: [{stageColor.R},{stageColor.G},{stageColor.B}]\n";
                }
            }


            //create a color correction map to select the color channels
            op = op + 2 + ((op > 3)?2:0) + ((op > 5)?2:0);
            script.Append("ccmap = ColorCorrection()\n"
                    + $"ccmap.map = {sources[(int)(stage.Source.Alpha[srcIdx])]}\n"
                    + $"ccmap.rewireR = {operandChannels[op][0]}\n"
                    + $"ccmap.rewireG = {operandChannels[op][1]}\n"
                    + $"ccmap.rewireB = {operandChannels[op][2]}\n"
                    + $"ccmap.rewireA = 9\n");
            return "ccmap"; //return the color map
        }

        private RGBA GetConstColor(H3DMaterial mtl, int stage) //TODO: this isn't always the right color, figure out when to us MaterialParams.Constant#Color instead (if Constant#Adssignment == stage number, use that one?)
        {
            if (mtl.MaterialParams.Constant0Assignment == stage) return mtl.MaterialParams.Constant0Color;
            if (mtl.MaterialParams.Constant1Assignment == stage) return mtl.MaterialParams.Constant1Color;
            if (mtl.MaterialParams.Constant2Assignment == stage) return mtl.MaterialParams.Constant2Color;
            if (mtl.MaterialParams.Constant3Assignment == stage) return mtl.MaterialParams.Constant3Color;
            if (mtl.MaterialParams.Constant4Assignment == stage) return mtl.MaterialParams.Constant4Color;
            if (mtl.MaterialParams.Constant5Assignment == stage) return mtl.MaterialParams.Constant5Color;
            return mtl.MaterialParams.TexEnvStages[stage].Color;
        }

        #endregion

    }
}
