﻿//------------------------------------------------------------------------------
// <auto-generated>
//     O código foi gerado por uma ferramenta.
//     Versão de Tempo de Execução:4.0.30319.42000
//
//     As alterações ao arquivo poderão causar comportamento incorreto e serão perdidas se
//     o código for gerado novamente.
// </auto-generated>
//------------------------------------------------------------------------------

namespace SPICA.Rendering.Properties {
    using System;
    
    
    /// <summary>
    ///   Uma classe de recurso de tipo de alta segurança, para pesquisar cadeias de caracteres localizadas etc.
    /// </summary>
    // Essa classe foi gerada automaticamente pela classe StronglyTypedResourceBuilder
    // através de uma ferramenta como ResGen ou Visual Studio.
    // Para adicionar ou remover um associado, edite o arquivo .ResX e execute ResGen novamente
    // com a opção /str, ou recrie o projeto do VS.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "4.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class Resources {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Resources() {
        }
        
        /// <summary>
        ///   Retorna a instância de ResourceManager armazenada em cache usada por essa classe.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("SPICA.Rendering.Properties.Resources", typeof(Resources).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Substitui a propriedade CurrentUICulture do thread atual para todas as
        ///   pesquisas de recursos que usam essa classe de recurso de tipo de alta segurança.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Consulta uma cadeia de caracteres localizada semelhante a //SPICA auto-generated code
        /////This code was translated from a MAESTRO Vertex Shader
        /////This file was also hand modified to improve compatibility
        ///#version 330 core
        ///precision highp float;
        ///
        ///uniform vec4 WrldMtx[3];
        ///uniform vec4 NormMtx[3];
        ///uniform vec4 PosOffs;
        ///uniform vec4 IrScale[2];
        ///uniform vec4 TexcMap;
        ///uniform vec4 TexMtx0[3];
        ///uniform vec4 TexMtx1[3];
        ///uniform vec4 TexMtx2[2];
        ///uniform vec4 TexTran;
        ///uniform vec4 MatAmbi;
        ///uniform vec4 MatDiff;
        ///uniform vec4 HslGCol;
        ///uniform vec4 HslSCol;
        ///uni [o restante da cadeia de caracteres foi truncado]&quot;;.
        /// </summary>
        internal static string DefaultVertexShader {
            get {
                return ResourceManager.GetString("DefaultVertexShader", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Consulta uma cadeia de caracteres localizada semelhante a #version 150
        ///
        ///precision highp float;
        ///
        ///uniform sampler2D LUTs[6];
        ///
        ///uniform sampler2D Textures[3];
        ///
        ///uniform samplerCube TextureCube;
        ///
        ///struct Light_t {
        ///	vec3 Position;
        ///	vec4 Ambient;
        ///	vec4 Diffuse;
        ///	vec4 Specular;
        ///};
        ///
        ///uniform int LightsCount;
        ///
        ///uniform Light_t Lights[8];
        ///
        ///uniform vec4 SAmbient;
        ///
        ///vec3 QuatRotate(vec4 q, vec3 v) {
        ///    return v + 2 * cross(q.xyz, cross(q.xyz, v) + q.w * v);
        ///}.
        /// </summary>
        internal static string FragmentShaderBase {
            get {
                return ResourceManager.GetString("FragmentShaderBase", resourceCulture);
            }
        }
    }
}
