using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Globalization;
using System.Text;
using UnityEditor.Graphing;
using UnityEditor.Graphing.Util;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor.ShaderGraph;
using UnityEditor.VFX;
using UnityEngine.Experimental.Rendering.HDPipeline;
using UnityEngine.Experimental.VFX;
using UnityEditor.ShaderGraph.Drawing.Controls;

namespace UnityEditor.Experimental.Rendering.HDPipeline.VFXSG
{
    public static class VFXSGHDRPShaderGenerator
    {
        public class Graph
        {
            internal GraphData graphData;
            internal List<MaterialSlot> slots;
            internal string shaderCode;

            internal struct Function
            {
                internal List<AbstractMaterialNode> nodes;
                internal ShaderGraphRequirements requirements;
                internal List<MaterialSlot> slots;
            }

            internal struct Pass
            {
                internal Function vertex;
                internal Function pixel;
            }

            internal Pass[] passes;



            internal class PassInfo
            {
                public PassInfo(string name, FunctionInfo pixel, FunctionInfo vertex)
                {
                    this.name = name;
                    this.pixel = pixel;
                    this.vertex = vertex;
                }
                public readonly string name;
                public readonly FunctionInfo pixel;
                public readonly FunctionInfo vertex;
            }
            internal class FunctionInfo
            {
                public FunctionInfo(List<int> activeSlots)
                {
                    this.activeSlots = activeSlots;
                }
                public readonly List<int> activeSlots;
            }

            internal readonly static PassInfo[] litPassInfos = new PassInfo[]
                {
                //GBuffer
                new PassInfo("GBuffer",new FunctionInfo(HDLitSubShader.passGBuffer.PixelShaderSlots),new FunctionInfo(HDLitSubShader.passGBuffer.VertexShaderSlots)),
                //ShadowCaster
                new PassInfo("ShadowCaster",new FunctionInfo(HDLitSubShader.passShadowCaster.PixelShaderSlots),new FunctionInfo(HDLitSubShader.passShadowCaster.VertexShaderSlots)),
                new PassInfo("DepthOnly",new FunctionInfo(HDLitSubShader.passDepthOnly.PixelShaderSlots),new FunctionInfo(HDLitSubShader.passDepthOnly.VertexShaderSlots)),
                new PassInfo("SceneSelectionPass",new FunctionInfo(HDLitSubShader.passSceneSelection.PixelShaderSlots),new FunctionInfo(HDLitSubShader.passSceneSelection.VertexShaderSlots)),
                new PassInfo("META",new FunctionInfo(HDLitSubShader.passMETA.PixelShaderSlots),new FunctionInfo(HDLitSubShader.passMETA.VertexShaderSlots)),
                new PassInfo("MotionVectors",new FunctionInfo(HDLitSubShader.passMotionVector.PixelShaderSlots),new FunctionInfo(HDLitSubShader.passMotionVector.VertexShaderSlots)),
                new PassInfo("DistortionVectors",new FunctionInfo(HDLitSubShader.passDistortion.PixelShaderSlots),new FunctionInfo(HDLitSubShader.passDistortion.VertexShaderSlots)),
                new PassInfo("TransparentDepthPrepass",new FunctionInfo(HDLitSubShader.passTransparentPrepass.PixelShaderSlots),new FunctionInfo(HDLitSubShader.passTransparentPrepass.VertexShaderSlots)),
                new PassInfo("TransparentBackface",new FunctionInfo(HDLitSubShader.passTransparentBackface.PixelShaderSlots),new FunctionInfo(HDLitSubShader.passTransparentBackface.VertexShaderSlots)),
                new PassInfo("Forward",new FunctionInfo(HDLitSubShader.passForward.PixelShaderSlots),new FunctionInfo(HDLitSubShader.passForward.VertexShaderSlots)),
                new PassInfo("TransparentDepthPostpass",new FunctionInfo(HDLitSubShader.passTransparentDepthPostpass.PixelShaderSlots),new FunctionInfo(HDLitSubShader.passTransparentDepthPostpass.VertexShaderSlots)),
                };

            internal readonly static PassInfo[] unlitPassInfos = new PassInfo[]
                {
                new PassInfo("ShadowCaster",new FunctionInfo(HDUnlitSubShader.passShadowCaster.PixelShaderSlots),new FunctionInfo(HDUnlitSubShader.passShadowCaster.VertexShaderSlots)),
                new PassInfo("DepthForwardOnly",new FunctionInfo(HDUnlitSubShader.passDepthForwardOnly.PixelShaderSlots),new FunctionInfo(HDUnlitSubShader.passDepthForwardOnly.VertexShaderSlots)),
                new PassInfo("SceneSelectionPass",new FunctionInfo(HDUnlitSubShader.passSceneSelection.PixelShaderSlots),new FunctionInfo(HDUnlitSubShader.passSceneSelection.VertexShaderSlots)),
                new PassInfo("META",new FunctionInfo(HDUnlitSubShader.passMETA.PixelShaderSlots),new FunctionInfo(HDUnlitSubShader.passMETA.VertexShaderSlots)),
                new PassInfo("MotionVectors",new FunctionInfo(HDUnlitSubShader.passMotionVectors.PixelShaderSlots),new FunctionInfo(HDUnlitSubShader.passMotionVectors.VertexShaderSlots)),
                new PassInfo("DistortionVectors",new FunctionInfo(HDUnlitSubShader.passDistortion.PixelShaderSlots),new FunctionInfo(HDUnlitSubShader.passDistortion.VertexShaderSlots)),
                new PassInfo("ForwardOnly",new FunctionInfo(HDUnlitSubShader.passForwardOnly.PixelShaderSlots),new FunctionInfo(HDUnlitSubShader.passForwardOnly.VertexShaderSlots)),
                };
        }

        public static Graph LoadShaderGraph(Shader shader)
        {
            string shaderGraphPath = AssetDatabase.GetAssetPath(shader);

            if (Path.GetExtension(shaderGraphPath).Equals(".shadergraph", StringComparison.InvariantCultureIgnoreCase))
            {
                MasterNodeInfo masterNodeInfo;
                return LoadShaderGraph(shaderGraphPath, out masterNodeInfo);
            }
            return null;
        }

        static Graph LoadShaderGraph(Shader shader, out MasterNodeInfo masterNodeInfo)
        {
            string shaderGraphPath = AssetDatabase.GetAssetPath(shader);

            if (Path.GetExtension(shaderGraphPath).Equals(".shadergraph", StringComparison.InvariantCultureIgnoreCase))
            {
                return LoadShaderGraph(shaderGraphPath, out masterNodeInfo);
            }
            masterNodeInfo = new MasterNodeInfo();
            return null;
        }

        public static Dictionary<string,Texture> GetUsedTextures(Graph graph)
        {
            var shaderProperties = new PropertyCollector();
            foreach( var node in graph.passes.SelectMany(t => t.pixel.nodes.Concat(t.vertex.nodes)))
            {
                node.CollectShaderProperties(shaderProperties, GenerationMode.ForReals);
            }

            return shaderProperties.GetConfiguredTexutres().ToDictionary(t=>t.name,t=>(Texture)EditorUtility.InstanceIDToObject(t.textureId));
        }

        static Graph LoadShaderGraph(string shaderFilePath, out MasterNodeInfo masterNodeInfo)
        {
            var textGraph = File.ReadAllText(shaderFilePath, Encoding.UTF8);

            Graph graph = new Graph();
            graph.graphData = JsonUtility.FromJson<GraphData>(textGraph);
            graph.graphData.OnEnable();
            graph.graphData.ValidateGraph();


            List<PropertyCollector.TextureInfo> textureInfos;
            graph.shaderCode = (graph.graphData.outputNode as IMasterNode).GetShader(GenerationMode.ForReals, graph.graphData.outputNode.name, out textureInfos);

            if (!s_MasterNodeInfos.TryGetValue(graph.graphData.outputNode.GetType(), out masterNodeInfo))
                return null;

            graph.slots = new List<MaterialSlot>();
            foreach (var activeNode in ((AbstractMaterialNode)graph.graphData.outputNode).ToEnumerable())
            {
                if (activeNode is IMasterNode || activeNode is SubGraphOutputNode)
                    graph.slots.AddRange(activeNode.GetInputSlots<MaterialSlot>());
                else
                    graph.slots.AddRange(activeNode.GetOutputSlots<MaterialSlot>());
            }

            var passInfos = masterNodeInfo.passInfos;
            graph.passes = new Graph.Pass[passInfos.Length];

            for (int currentPass = 0; currentPass < passInfos.Length; ++currentPass)
            {
                graph.passes[currentPass].pixel.nodes = ListPool<AbstractMaterialNode>.Get();
                NodeUtils.DepthFirstCollectNodesFromNode(graph.passes[currentPass].pixel.nodes, ((AbstractMaterialNode)graph.graphData.outputNode), NodeUtils.IncludeSelf.Include, passInfos[currentPass].pixel.activeSlots);
                graph.passes[currentPass].vertex.nodes = ListPool<AbstractMaterialNode>.Get();
                NodeUtils.DepthFirstCollectNodesFromNode(graph.passes[currentPass].vertex.nodes, ((AbstractMaterialNode)graph.graphData.outputNode), NodeUtils.IncludeSelf.Include, passInfos[currentPass].vertex.activeSlots);

                graph.passes[currentPass].pixel.requirements = ShaderGraphRequirements.FromNodes(graph.passes[currentPass].pixel.nodes, ShaderStageCapability.Fragment, false);
                graph.passes[currentPass].vertex.requirements = ShaderGraphRequirements.FromNodes(graph.passes[currentPass].vertex.nodes, ShaderStageCapability.Vertex, false);

                graph.passes[currentPass].pixel.requirements.requiresPosition |= NeededCoordinateSpace.View | NeededCoordinateSpace.World;
                graph.passes[currentPass].vertex.requirements.requiresPosition |= NeededCoordinateSpace.Object;

                graph.passes[currentPass].pixel.slots = graph.slots.Where(t => passInfos[currentPass].pixel.activeSlots.Contains(t.id)).ToList();
                graph.passes[currentPass].vertex.slots = graph.slots.Where(t => passInfos[currentPass].vertex.activeSlots.Contains(t.id)).ToList();
            }

            return graph;
        }
        public static List<string> GetPropertiesExcept(Graph graph, List<string> attributes)
        {
            var shaderProperties = new PropertyCollector();
            graph.graphData.CollectShaderProperties(shaderProperties, GenerationMode.ForReals);
            var vfxAttributesToshaderProperties = new StringBuilder();

            List<string> remainingProperties = new List<string>();
            foreach (var prop in shaderProperties.properties)
            {
                string matchingAttribute = attributes.FirstOrDefault(t => prop.displayName.Equals(t, StringComparison.InvariantCultureIgnoreCase));
                if (matchingAttribute == null)
                {
                    remainingProperties.Add(string.Format("{0} {1}", prop.propertyType.ToString(), prop.referenceName));
                }
            }

            return remainingProperties;
        }

        static readonly Dictionary<string, string> s_ShaderGraphToSurfaceDescriptionName = new Dictionary<string, string>()
            {
                {"Albedo" ,"baseColor"},
                {"Smoothness", "perceptualSmoothness" },
                {"Occlusion", "ambientOcclusion" },
                {"Specular", "specularColor" }
        };

        static string ShaderGraphToSurfaceDescriptionName(string name)
        {
            string result;
            if (s_ShaderGraphToSurfaceDescriptionName.TryGetValue(name, out result))
                return result;
            return name.Substring(0,1).ToLower() + name.Substring(1);
        }

        static string GetSlotValue(string slotName, IEnumerable<MaterialSlot> slots, Graph graph)
        {
            var slot = slots.FirstOrDefault(t => t.shaderOutputName == slotName);
            if (slot == null)
                return null;
            return GetSlotValue(slot, graph);
        }

        static string GetSlotValue(MaterialSlot slot, Graph graph)
        {
            string value;
            var foundEdges = graph.graphData.GetEdges(slot.slotReference).ToArray();
            if (foundEdges.Any())
                value = graph.graphData.outputNode.GetSlotValue(slot.id, GenerationMode.ForReals);
            else
                value = slot.GetDefaultValue(GenerationMode.ForReals);
            return value;
        }

        static bool AddCodeIfSlotExist(Graph graph,ShaderStringBuilder builder,string slotName,string existsFormat, string dontExistStr, IEnumerable<MaterialSlot> slots)
        {
            var slot = slots.FirstOrDefault(t => t.shaderOutputName == slotName );

            if (slot != null)
            {
                if(existsFormat != null )
                    builder.AppendLine(existsFormat, GetSlotValue(slot, graph));
                return true;
            }
            else if(dontExistStr != null)
                builder.AppendLine(dontExistStr);
            return false;
        }

        static readonly HashSet<string> customBehaviourSlots = new HashSet<string>(new[]{ "Normal", "BentNormal", "Emission" , "Alpha" , "AlphaClipThreshold" , "SpecularOcclusion" , "Tangent", "DepthOffset", "SpecularAAScreenSpaceVariance", "SpecularAAThreshold", "RefractionIndex" , "RefractionColor" , "RefractionDistance" });


        struct VaryingAttribute
        {
            public string name;
            public int type;
        }

        static List<VaryingAttribute> ComputeVaryingAttribute(Graph graph,VFXInfos vfxInfos)
        {
            var shaderProperties = new PropertyCollector();
            graph.graphData.CollectShaderProperties(shaderProperties, GenerationMode.ForReals);


            // In the varying we must put all attributes that are modified by a block from this outputcontext and that are used by the shadergraph
            //Alpha is a special case that is always used (alpha from SG is multiplied by alpha from VFX) .

            List<VaryingAttribute> result = new List<VaryingAttribute>();
            foreach (var info in vfxInfos.attributes.Zip(vfxInfos.attributeTypes.Cast<int>(), (string a,int b) => new KeyValuePair<string, int>(a, (int)b)).Where(t => vfxInfos.modifiedByOutputAttributes.Contains(t.Key) && (t.Key == "alpha" || shaderProperties.properties.Any(u => u.displayName.Equals(t.Key, StringComparison.InvariantCultureIgnoreCase)))))
            {
                result.Add(new VaryingAttribute { name = info.Key, type = info.Value});
            }

            return result;
        }

        enum ValueType
        {
            None = 0,
            Float = 1,
            Float2 = 2,
            Float3 = 3,
            Float4 = 4,
            Int32 = 5,
            Uint32 = 6,
            Texture2D = 7,
            Texture2DArray = 8,
            Texture3D = 9,
            TextureCube = 10,
            TextureCubeArray = 11,
            Matrix4x4 = 12,
            Curve = 13,
            ColorGradient = 14,
            Mesh = 15,
            Spline = 16,
            Boolean = 17
        }

        public static string TypeToCode(int type)
        {
            switch ((ValueType)type)
            {
                case ValueType.Float: return "float";
                case ValueType.Float2: return "float2";
                case ValueType.Float3: return "float3";
                case ValueType.Float4: return "float4";
                case ValueType.Int32: return "int";
                case ValueType.Uint32: return "uint";
                case ValueType.Texture2D: return "Texture2D";
                case ValueType.Texture2DArray: return "Texture2DArray";
                case ValueType.Texture3D: return "Texture3D";
                case ValueType.TextureCube: return "TextureCube";
                case ValueType.TextureCubeArray: return "TextureCubeArray";
                case ValueType.Matrix4x4: return "float4x4";
                case ValueType.Boolean: return "bool";
            }
            throw new NotImplementedException(type.ToString());
        }


        static string GenerateVaryingVFXAttribute(Graph graph,VFXInfos vfxInfos,List<VaryingAttribute> varyingAttributes)
        {
            var sb = new StringBuilder();

            sb.Append(@"
struct ParticleMeshToPS
{
");
            // In the varying we must put all attributes that are modified by a block from this outputcontext and that are used by the shadergraph
            //Alpha is a special case that is always used (alpha from SG is multiplied by alpha from VFX) .
            int texCoordNum = 6;
            foreach (var info in varyingAttributes)
            {
                if( texCoordNum < 10)
                    sb.AppendFormat("    nointerpolation {0} {1} : TEXCOORD{2};\n",TypeToCode(info.type),info.name,texCoordNum++);
                else
                    sb.AppendFormat("    nointerpolation {0} {1} : NORMAL{2};\n", TypeToCode(info.type), info.name, (texCoordNum++) - 10 + 2); //Start with NORMAL3
            }
            sb.Append(@"
            uint instanceID : SV_InstanceID;
};");
            return sb.ToString();
        }


        delegate void PrepareMasterNodeDelegate(Graph graph, Dictionary<string, string> guiVariablest, Dictionary<string, int> defines);

        struct MasterNodeInfo
        {
            public MasterNodeInfo(string mainShaderPath, string litDataIncludePath, string VFXSGcommon, PrepareMasterNodeDelegate prepareMasterNode, bool hasNormalWS,bool hasRefraction, Graph.PassInfo[] passInfos)
            {
                this.mainShaderPath = mainShaderPath;
                this.litDataIncludePath = litDataIncludePath;
                this.prepareMasterNode = prepareMasterNode;
                this.VFXSGcommon = VFXSGcommon;
                this.hasNormalWS = hasNormalWS;
                this.hasRefraction = hasRefraction;
                this.passInfos = passInfos;
            }
            public readonly string mainShaderPath;
            public readonly string litDataIncludePath;
            public readonly string VFXSGcommon;
            public readonly bool hasNormalWS;
            public readonly bool hasRefraction;
            public readonly PrepareMasterNodeDelegate prepareMasterNode;
            public readonly Graph.PassInfo[] passInfos;
        }

        static readonly Dictionary<System.Type, MasterNodeInfo> s_MasterNodeInfos = new Dictionary<Type, MasterNodeInfo>
        {
            {typeof(HDLitMasterNode), new MasterNodeInfo(
                "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/Lit.shader",
                "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/LitData.hlsl",
                "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/VFX/VFXSGCommonLit.hlsl",
                PrepareHDLitMasterNode,
                true,
                true,
                Graph.litPassInfos) },
            {typeof(HDUnlitMasterNode), new MasterNodeInfo(
                "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Unlit/Unlit.shader",
                "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Unlit/UnlitData.hlsl",
                "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/VFX/VFXSGCommonUnlit.hlsl",
                PrepareHDUnlitMasterNode,
                false,
                false,
                Graph.unlitPassInfos) },
        };


        internal static string NewGenerateShader(Shader shaderGraph, ref VFXInfos vfxInfos)
        {
            MasterNodeInfo masterNodeInfo;
            Graph graph = LoadShaderGraph(shaderGraph, out masterNodeInfo);
            if (graph == null) return null;

            Dictionary<string, string> guiVariables = new Dictionary<string, string>()
            {
                {"_StencilRef","2" },
                {"_StencilRefDepth","0" },
                {"_StencilRefDistortionVec","64" },
                {"_StencilRefGBuffer", "2"},
                {"_StencilRefMV","128" },
                {"_StencilWriteMask","3" },
                {"_StencilWriteMaskDepth","48" },
                {"_StencilMaskDistortionVec","64" },
                {"_StencilWriteMaskGBuffer", "51"},
                {"_StencilWriteMaskMV","176" },

                {"_CullMode","Back" },
                {"_CullModeForward","Back" },
                {"_SrcBlend","One" },
                {"_DstBlend","Zero" },
                {"_AlphaSrcBlend","One" },
                {"_AlphaDstBlend","Zero" },
                {"_ZWrite","On" },
                {"_ColorMaskTransparentVel","RGBA" },
                {"_ZTestDepthEqualForOpaque","Equal" },
                {"_ZTestGBuffer","LEqual"},
                {"_DistortionSrcBlend","One" },
                {"_DistortionDstBlend","Zero" },
                {"_DistortionBlurBlendOp","Add" },
                {"_ZTestModeDistortion","Always" },
                {"_DistortionBlurSrcBlend","One" },
                {"_DistortionBlurDstBlend","Zero" },
            };

            ShaderDocument document = new ShaderDocument();
            document.Parse(graph.shaderCode);


            File.WriteAllText("c:\\unity\\toto.shader", document.ToString(true));

            var defines = new Dictionary<string, int>();
            var killPasses = new HashSet<string>();

            int cptLine = 0;
            document.InsertShaderLine(cptLine++, "#include \"Packages/com.unity.visualeffectgraph/Shaders/RenderPipeline/HDRP/VFXDefines.hlsl\"");

            defines["VFX_ACTIVE"] = 1;

            masterNodeInfo.prepareMasterNode(graph, guiVariables, defines);

            List<VaryingAttribute> varyingAttributes = ComputeVaryingAttribute(graph, vfxInfos);


            foreach (var pass in document.passes)
            {
                int currentPass = Array.FindIndex(masterNodeInfo.passInfos, t => t.name == pass.name);
                if (currentPass == -1)
                    continue;

                GeneratePass(vfxInfos, graph, guiVariables, defines, varyingAttributes, pass, currentPass, ref masterNodeInfo);
            }
            foreach (var define in defines)
                document.InsertShaderCode(0, string.Format("#define {0} {1}", define.Key, define.Value));

            document.ReplaceParameterVariables(guiVariables);

            return document.ToString(false).Replace("$precision", "float").Replace("\r", "");
        }

        private static void PrepareHDLitMasterNode(Graph graph, Dictionary<string, string> guiVariables, Dictionary<string, int> defines)
        {
            var masterNode = graph.graphData.outputNode as HDLitMasterNode;

            if (masterNode != null)
            {
                if (masterNode.doubleSidedMode != DoubleSidedMode.Disabled)
                {
                    guiVariables["_CullMode"] = "Off";
                    guiVariables["_CullModeForward"] = "Off";
                }

                // Taken from BaseUI.cs
                int stencilRef = (int)StencilLightingUsage.RegularLighting; // Forward case
                int stencilWriteMask = (int)HDRenderPipeline.StencilBitMask.LightingMask;
                int stencilRefDepth = 0;
                int stencilWriteMaskDepth = 0;
                int stencilRefGBuffer = (int)StencilLightingUsage.RegularLighting;
                int stencilWriteMaskGBuffer = (int)HDRenderPipeline.StencilBitMask.LightingMask;
                int stencilRefMV = (int)HDRenderPipeline.StencilBitMask.ObjectMotionVectors;
                int stencilWriteMaskMV = (int)HDRenderPipeline.StencilBitMask.ObjectMotionVectors;

                if (masterNode.materialType == HDLitMasterNode.MaterialType.SubsurfaceScattering)
                {
                    stencilRefGBuffer = stencilRef = (int)StencilLightingUsage.SplitLighting;
                }

                if (!masterNode.receiveSSR.isOn)
                {
                    stencilRefDepth |= (int)HDRenderPipeline.StencilBitMask.DoesntReceiveSSR;
                    stencilRefGBuffer |= (int)HDRenderPipeline.StencilBitMask.DoesntReceiveSSR;
                    stencilRefMV |= (int)HDRenderPipeline.StencilBitMask.DoesntReceiveSSR;
                }

                stencilWriteMaskDepth |= (int)HDRenderPipeline.StencilBitMask.DoesntReceiveSSR | (int)HDRenderPipeline.StencilBitMask.DecalsForwardOutputNormalBuffer;
                stencilWriteMaskGBuffer |= (int)HDRenderPipeline.StencilBitMask.DoesntReceiveSSR | (int)HDRenderPipeline.StencilBitMask.DecalsForwardOutputNormalBuffer;
                stencilWriteMaskMV |= (int)HDRenderPipeline.StencilBitMask.DoesntReceiveSSR | (int)HDRenderPipeline.StencilBitMask.DecalsForwardOutputNormalBuffer;

                // As we tag both during motion vector pass and Gbuffer pass we need a separate state and we need to use the write mask
                guiVariables["_StencilRef"] = stencilRef.ToString();
                guiVariables["_StencilWriteMask"] = stencilWriteMask.ToString();
                guiVariables["_StencilRefDepth"] = stencilRefDepth.ToString();
                guiVariables["_StencilWriteMaskDepth"] = stencilWriteMaskDepth.ToString();
                guiVariables["_StencilRefGBuffer"] = stencilRefGBuffer.ToString();
                guiVariables["_StencilWriteMaskGBuffer"] = stencilWriteMaskGBuffer.ToString();
                guiVariables["_StencilRefMV"] = stencilRefMV.ToString();
                guiVariables["_StencilWriteMaskMV"] = stencilWriteMaskMV.ToString();
                guiVariables["_StencilRefDistortionVec"] = ((int)HDRenderPipeline.StencilBitMask.DistortionVectors).ToString();
                guiVariables["_StencilWriteMaskDistortionVec"] = ((int)HDRenderPipeline.StencilBitMask.DistortionVectors).ToString();

                if (masterNode.surfaceType == SurfaceType.Opaque)
                {
                    guiVariables["_SrcBlend"] = "One";
                    guiVariables["_DstBlend"] = "Zero";
                    guiVariables["_ZWrite"] = "On";
                    guiVariables["_ZTestDepthEqualForOpaque"] = "Equal";
                }
                else
                {
                    guiVariables["_ZTestDepthEqualForOpaque"] = "LEqual";
                    guiVariables["_ZWrite"] = "Off";

                    var blendMode = masterNode.alphaMode;

                    // When doing off-screen transparency accumulation, we change blend factors as described here: https://developer.nvidia.com/gpugems/GPUGems3/gpugems3_ch23.html
                    switch (blendMode)
                    {
                    // PremultipliedAlpha
                    // color: src * src_a + dst * (1 - src_a)
                    // src is supposed to have been multiplied by alpha in the texture on artists side.
                    case AlphaMode.Premultiply:
                    // Alpha
                    // color: src * src_a + dst * (1 - src_a)
                    // src * src_a is done in the shader as it allow to reduce precision issue when using _BLENDMODE_PRESERVE_SPECULAR_LIGHTING (See Material.hlsl)
                    case AlphaMode.Alpha:
                        guiVariables["_SrcBlend"] = "One";
                        guiVariables["_DstBlend"] = "OneMinusSrcAlpha";
                        if (masterNode.renderingPass == HDRenderQueue.RenderQueueType.LowTransparent)
                        {
                            guiVariables["_AlphaSrcBlend"] = "Zero";
                            guiVariables["_AlphaDstBlend"] = "OneMinusSrcAlpha";
                        }
                        else
                        {
                            guiVariables["_AlphaSrcBlend"] = "One";
                            guiVariables["_AlphaDstBlend"] = "OneMinusSrcAlpha";
                        }
                        break;

                    // Additive
                    // color: src * src_a + dst
                    // src * src_a is done in the shader
                    case AlphaMode.Additive:
                        guiVariables["_SrcBlend"] = "One";
                        guiVariables["_DstBlend"] = "One";
                        if (masterNode.renderingPass == HDRenderQueue.RenderQueueType.LowTransparent)
                        {
                            guiVariables["_AlphaSrcBlend"] = "Zero";
                            guiVariables["_AlphaDstBlend"] = "One";
                        }
                        else
                        {
                            guiVariables["_AlphaSrcBlend"] = "One";
                            guiVariables["_AlphaDstBlend"] = "One";
                        }
                        break;
                    }
                }
            }
        }

        private static void PrepareHDUnlitMasterNode(Graph graph, Dictionary<string, string> guiVariables, Dictionary<string, int> defines)
        {
            var masterNode = graph.graphData.outputNode as HDUnlitMasterNode;

            if (masterNode != null)
            {
                if (masterNode.doubleSided.isOn)
                {
                    guiVariables["_CullMode"] = "Off";
                    guiVariables["_CullModeForward"] = "Off";
                }

                // Taken from BaseUI.cs
                int stencilRef = (int)StencilLightingUsage.RegularLighting; // Forward case
                int stencilWriteMask = (int)HDRenderPipeline.StencilBitMask.LightingMask;
                int stencilRefDepth = 0;
                int stencilWriteMaskDepth = 0;
                int stencilRefGBuffer = (int)StencilLightingUsage.RegularLighting;
                int stencilWriteMaskGBuffer = (int)HDRenderPipeline.StencilBitMask.LightingMask;
                int stencilRefMV = (int)HDRenderPipeline.StencilBitMask.ObjectMotionVectors;
                int stencilWriteMaskMV = (int)HDRenderPipeline.StencilBitMask.ObjectMotionVectors;

                stencilWriteMaskDepth |= (int)HDRenderPipeline.StencilBitMask.DoesntReceiveSSR | (int)HDRenderPipeline.StencilBitMask.DecalsForwardOutputNormalBuffer;
                stencilWriteMaskGBuffer |= (int)HDRenderPipeline.StencilBitMask.DoesntReceiveSSR | (int)HDRenderPipeline.StencilBitMask.DecalsForwardOutputNormalBuffer;
                stencilWriteMaskMV |= (int)HDRenderPipeline.StencilBitMask.DoesntReceiveSSR | (int)HDRenderPipeline.StencilBitMask.DecalsForwardOutputNormalBuffer;

                // As we tag both during motion vector pass and Gbuffer pass we need a separate state and we need to use the write mask
                guiVariables["_StencilRef"] = stencilRef.ToString();
                guiVariables["_StencilWriteMask"] = stencilWriteMask.ToString();
                guiVariables["_StencilRefDepth"] = stencilRefDepth.ToString();
                guiVariables["_StencilWriteMaskDepth"] = stencilWriteMaskDepth.ToString();
                guiVariables["_StencilRefGBuffer"] = stencilRefGBuffer.ToString();
                guiVariables["_StencilWriteMaskGBuffer"] = stencilWriteMaskGBuffer.ToString();
                guiVariables["_StencilRefMV"] = stencilRefMV.ToString();
                guiVariables["_StencilWriteMaskMV"] = stencilWriteMaskMV.ToString();
                guiVariables["_StencilRefDistortionVec"] = ((int)HDRenderPipeline.StencilBitMask.DistortionVectors).ToString();
                guiVariables["_StencilWriteMaskDistortionVec"] = ((int)HDRenderPipeline.StencilBitMask.DistortionVectors).ToString();


                if (masterNode.surfaceType == SurfaceType.Opaque)
                {
                    guiVariables["_SrcBlend"] = "One";
                    guiVariables["_DstBlend"] = "Zero";
                    guiVariables["_ZWrite"] = "On";
                    guiVariables["_ZTestDepthEqualForOpaque"] = "Equal";
                }
                else
                {
                    guiVariables["_ZTestDepthEqualForOpaque"] = "LEqual";
                    guiVariables["_ZWrite"] = "Off";

                    var blendMode = masterNode.alphaMode;

                    // When doing off-screen transparency accumulation, we change blend factors as described here: https://developer.nvidia.com/gpugems/GPUGems3/gpugems3_ch23.html
                    switch (blendMode)
                    {
                        // PremultipliedAlpha
                        // color: src * src_a + dst * (1 - src_a)
                        // src is supposed to have been multiplied by alpha in the texture on artists side.
                        case AlphaMode.Premultiply:
                        // Alpha
                        // color: src * src_a + dst * (1 - src_a)
                        // src * src_a is done in the shader as it allow to reduce precision issue when using _BLENDMODE_PRESERVE_SPECULAR_LIGHTING (See Material.hlsl)
                        case AlphaMode.Alpha:
                            guiVariables["_SrcBlend"] = "One";
                            guiVariables["_DstBlend"] = "OneMinusSrcAlpha";
                            if (masterNode.renderingPass == HDRenderQueue.RenderQueueType.LowTransparent)
                            {
                                guiVariables["_AlphaSrcBlend"] = "Zero";
                                guiVariables["_AlphaDstBlend"] = "OneMinusSrcAlpha";
                            }
                            else
                            {
                                guiVariables["_AlphaSrcBlend"] = "One";
                                guiVariables["_AlphaDstBlend"] = "OneMinusSrcAlpha";
                            }
                            break;

                        // Additive
                        // color: src * src_a + dst
                        // src * src_a is done in the shader
                        case AlphaMode.Additive:
                            guiVariables["_SrcBlend"] = "One";
                            guiVariables["_DstBlend"] = "One";
                            if (masterNode.renderingPass == HDRenderQueue.RenderQueueType.LowTransparent)
                            {
                                guiVariables["_AlphaSrcBlend"] = "Zero";
                                guiVariables["_AlphaDstBlend"] = "One";
                            }
                            else
                            {
                                guiVariables["_AlphaSrcBlend"] = "One";
                                guiVariables["_AlphaDstBlend"] = "One";
                            }
                            break;
                    }
                }
            }
        }

        private static void GeneratePass(VFXInfos vfxInfos, Graph graph, Dictionary<string, string> guiVariables, Dictionary<string, int> defines, List<VaryingAttribute> varyingAttributes, PassPart pass, int currentPass, ref MasterNodeInfo masterNodeInfo)
        {
            Dictionary<string, int> passDefines = new Dictionary<string, int>();
            for (int i = 0; i < 4; ++i)
            {
                if (graph.passes[currentPass].pixel.requirements.requiresMeshUVs.Contains((UVChannel)i))
                {
                    passDefines["ATTRIBUTES_NEED_TEXCOORD" + i] = 1;
                    passDefines["VARYINGS_NEED_TEXCOORD" + i] = 1;
                    
                }
                else if (graph.passes[currentPass].vertex.requirements.requiresMeshUVs.Contains((UVChannel)i))
                    passDefines["ATTRIBUTES_NEED_TEXCOORD" + i] = 1;
            }
            if (graph.passes[currentPass].pixel.requirements.requiresVertexColor)
            {
                passDefines["ATTRIBUTES_NEED_COLOR"] = 1;
                passDefines["VARYINGS_NEED_COLOR"] = 1;
            }

            var sb = new StringBuilder();
            GenerateParticleVert(graph, vfxInfos, sb, currentPass, passDefines, varyingAttributes);

            pass.InsertShaderCode(0, GenerateVaryingVFXAttribute(graph, vfxInfos, varyingAttributes));

            foreach (var define in passDefines)
                pass.InsertShaderCode(0, string.Format("#define {0} {1}", define.Key, define.Value));

            /*string getSurfaceDataFunction = GenerateParticleGetSurfaceAndBuiltinData(graph, ref vfxInfos, currentPass, pass, guiVariables, defines, varyingAttributes,ref masterNodeInfo);

            pass.ReplaceInclude(masterNodeInfo.litDataIncludePath, getSurfaceDataFunction);*/
            pass.InsertShaderCode(-1, sb.ToString());
            pass.RemoveShaderCodeContaining("#pragma vertex Vert");

            // The hard part : replace the pass specific include with its contents where the call to GetSurfaceAndBuiltinData is replaced by a call to ParticleGetSurfaceAndBuiltinData
            // with an additionnal second parameter ( the instanceID )
            /*int index = pass.IndexOfLineMatching(@"\s*#include\s*""Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.*\.hlsl""\s*");

            if (index >= 0)
            {
                string line = pass.shaderCode[index];
                pass.shaderCode.RemoveAt(index);

                int firstQuote = line.IndexOf('"');
                string filePath = line.Substring(firstQuote + 1, line.LastIndexOf('"') - firstQuote - 1);

                string passFile = File.ReadAllText(filePath);

                // Replace calls to GetSurfaceAndBuiltinData to calls to ParticleGetSurfaceAndBuiltinData with an additionnal parameter
                int callIndex = passFile.IndexOf("GetSurfaceAndBuiltinData(");
                if (callIndex != -1)
                {
                    int endCallIndex = passFile.IndexOf(';', callIndex + 1);
                    endCallIndex = passFile.LastIndexOf(')', endCallIndex) - 1;
                    int paramStartIndex = callIndex + "GetSurfaceAndBuiltinData(".Length;

                    string[] parameters = passFile.Substring(paramStartIndex, endCallIndex - paramStartIndex).Split(',');

                    var ssb = new StringBuilder();

                    ssb.Append(passFile.Substring(0, callIndex));
                    ssb.Append("ParticleGetSurfaceAndBuiltinData(");

                    var args = parameters.Take(1).Concat(Enumerable.Repeat("packedInput.vmesh.instanceID", 1)).Concat(Enumerable.Repeat("packedInput.vmesh.vfxAttributes", 1)).Concat(parameters.Skip(1));

                    ssb.Append(args.Aggregate((a, b) => a + "," + b));

                    ssb.Append(passFile.Substring(endCallIndex));

                    pass.InsertShaderCode(index, ssb.ToString());
                }

            }*/
        }

        static string GenerateParticleGetSurfaceAndBuiltinData(Graph graph, ref VFXInfos vfxInfos, int currentPass, PassPart pass,Dictionary<string, string> guiVariables,Dictionary<string, int> defines , List<VaryingAttribute> varyingAttributes,ref MasterNodeInfo masterNodeInfo)
        {
            var getSurfaceDataFunction = new ShaderStringBuilder();
            getSurfaceDataFunction.AppendLine(@"#include """+ masterNodeInfo.VFXSGcommon + @"""");

            getSurfaceDataFunction.Append(vfxInfos.parameters);
            string slotAssignations;

            IEnumerable<MaterialSlot> usedSlots;

            PropertyCollector shaderProperties = new PropertyCollector();
            {   // inspired by GenerateSurfaceDescriptionFunction
                ShaderStringBuilder functionsString = new ShaderStringBuilder();
                FunctionRegistry functionRegistry = new FunctionRegistry(functionsString);

                graph.graphData.CollectShaderProperties(shaderProperties, GenerationMode.ForReals);

                var sg = new ShaderStringBuilder();

                GraphContext graphContext = new GraphContext("SurfaceDescriptionInputs");
                //Generate code for nodes.
                foreach (var activeNode in graph.passes[currentPass].pixel.nodes.OfType<AbstractMaterialNode>())
                {
                    if (activeNode is IGeneratesFunction)
                    {
                        functionRegistry.builder.currentNode = activeNode;
                        (activeNode as IGeneratesFunction).GenerateNodeFunction(functionRegistry, graphContext, GenerationMode.ForReals);
                    }
                    if (activeNode is IGeneratesBodyCode)
                        (activeNode as IGeneratesBodyCode).GenerateNodeCode(sg, graphContext, GenerationMode.ForReals);

                    activeNode.CollectShaderProperties(shaderProperties, GenerationMode.ForReals);
                }

                //Add node functions first
                getSurfaceDataFunction.AppendLines(functionsString.ToString());
                functionRegistry.builder.currentNode = null;

                var stdSlotAssignation = new StringBuilder();
                stdSlotAssignation.Append(sg.ToString());

                //Explicitely excluded slots are used later in a custom fashion.
                usedSlots = graph.passes[currentPass].pixel.slots.Where(t => !customBehaviourSlots.Contains(t.shaderOutputName));

                foreach (var input in usedSlots)
                {
                    if (input != null)
                    {
                        stdSlotAssignation.AppendFormat("surfaceData.{0} = {1};\n", ShaderGraphToSurfaceDescriptionName(NodeUtils.GetHLSLSafeName(input.shaderOutputName)), GetSlotValue(input, graph));
                    }
                }

                slotAssignations = stdSlotAssignation.ToString();
            }
            getSurfaceDataFunction.Append(@"

void ParticleGetSurfaceAndBuiltinData(FragInputs input, uint index,VaryingVFXAttribute vfxAttributes,float3 V, inout PositionInputs posInput, out SurfaceData surfaceData, out BuiltinData builtinData)
{
    FragInputForSG IN = InitializeFragStructs(input, posInput,V, surfaceData, builtinData);

    #ifdef _DOUBLESIDED_ON
      #ifdef _DOUBLESIDED_FLIP
        float3 doubleSidedConstants = float3(-1.0, -1.0, -1.0);
      #elif defined(_DOUBLESIDED_MIRROR)
        float3 doubleSidedConstants = float3(1.0, 1.0, -1.0);
      #else
        float3 doubleSidedConstants = float3(1.0, 1.0, 1.0);
      #endif
    #else
        float3 doubleSidedConstants = float3(1.0, 1.0, 1.0);
    #endif
    ApplyDoubleSidedFlipOrMirror(input, doubleSidedConstants);
");
            getSurfaceDataFunction.AppendLine("    " + vfxInfos.loadAttributes.Replace("\n", "\n    "));

            getSurfaceDataFunction.Append(@"

    if( !alive) discard;
    ");
            // override attribute load with value from varyings in case of attriibute values modified in output context
            foreach (var varyingAttribute in varyingAttributes)
            {
                getSurfaceDataFunction.AppendLine("{0} = vfxAttributes.{0};", varyingAttribute.name);
            }


            // override attribute load with values from shader graph properties
            foreach (var prop in shaderProperties.properties)
            {
                string matchingAttribute = vfxInfos.attributes.FirstOrDefault(t => prop.displayName.Equals(t, StringComparison.InvariantCultureIgnoreCase));
                if (matchingAttribute != null)
                {
                    if (matchingAttribute == "color")
                        getSurfaceDataFunction.AppendLine("    " + prop.GetPropertyDeclarationString("") + " = float4(color,1);");
                    else
                        getSurfaceDataFunction.AppendLine("    " + prop.GetPropertyDeclarationString("") + " = " + matchingAttribute + ";");
                }
            }

            getSurfaceDataFunction.AppendLine("\n    " + slotAssignations.Replace("\n", "\n    "));
            // if shader graph has an Alpha slot, multiply the alpha attribute value by it.
            AddCodeIfSlotExist(graph, getSurfaceDataFunction, "Alpha", "    float alphaSG = {0};\n    alpha *= alphaSG;\n", null, graph.passes[currentPass].pixel.slots);

            //alpha cutoff as soon as we have alpha value
            bool alphaThresholdExist = AddCodeIfSlotExist(graph, getSurfaceDataFunction, "AlphaClipThreshold", "\tfloat alphaCutoff = {0};\nDoAlphaTest(alpha, alphaCutoff);\n", null, graph.passes[currentPass].pixel.slots);
            if( alphaThresholdExist)
            {
                guiVariables["_ZTestGBuffer"] = "Equal";
                pass.AddTag("Queue", "AlphaTest + 0");
                defines["_ALPHATEST_ON"] = 1;
            }
            else
            {
                guiVariables["_ZTestGBuffer"] = "LEqual";
            }

            //Apply depth offset if the slot exist
            AddCodeIfSlotExist(graph, getSurfaceDataFunction, "DepthOffset", "    float depthOffset = {0};\n    ApplyDepthOffsetPositionInput(V, depthOffset, GetViewForwardDir(), GetWorldToHClipMatrix(), posInput);\n", null, graph.passes[currentPass].pixel.slots);

            //Add the clear coat feature define if the coatMask slot is linked to something or if its constant value is diferent from 0
            var coatMask = graph.passes[currentPass].pixel.slots.FirstOrDefault(t => t.shaderOutputName == "CoatMask");
            if (coatMask != null)
            {
                var foundEdges = graph.graphData.GetEdges(coatMask.slotReference).ToArray();
                if (foundEdges.Any())
                    defines["_MATERIAL_FEATURE_CLEAR_COAT"]= 1;
                else
                {
                    float value;
                    if (float.TryParse(coatMask.GetDefaultValue(GenerationMode.ForReals), out value) && value > 0)
                        defines["_MATERIAL_FEATURE_CLEAR_COAT"] = 1;
                }
            }
            //define the normal to be the one from the slot if it exist or a good default value
            AddCodeIfSlotExist(graph, getSurfaceDataFunction, "Normal", "    float3 normalTS = {0};\n", "    float3 normalTS = float3(0.0,0.0,1.0);", graph.passes[currentPass].pixel.slots);

            getSurfaceDataFunction.AppendLine(@"

    #if HAVE_DECALS
            if (_EnableDecals)
            {
                DecalSurfaceData decalSurfaceData = GetDecalSurfaceData(posInput, alpha);
                ApplyDecalToSurfaceData(decalSurfaceData, surfaceData);
            }
    #endif");
            //define the specular occlusion
            AddCodeIfSlotExist(graph, getSurfaceDataFunction, "SpecularOcclusion", "surfaceData.specularOcclusion = {0}", "", graph.passes[currentPass].pixel.slots);

            if (masterNodeInfo.hasNormalWS)
            {
                getSurfaceDataFunction.AppendLine(@"
    float3 bentNormalTS;
    bentNormalTS = normalTS;
    float3 bentNormalWS;
    GetNormalWS(input, normalTS, surfaceData.normalWS, doubleSidedConstants);
");
                //define the bendNormal if it exists
                AddCodeIfSlotExist(graph, getSurfaceDataFunction, "BentNormal", "    bentNormalTS = {0};", "    bentNormalWS = surfaceData.normalWS;", graph.passes[currentPass].pixel.slots);

                getSurfaceDataFunction.AppendLine(@"
    GetNormalWS(input, bentNormalTS, bentNormalWS, doubleSidedConstants);
");

            }
            else
            {
                getSurfaceDataFunction.AppendLine(@"
    float3 bentNormalWS = (float3)0;");
            }
            //define Specular AA
            var SAAVariance = GetSlotValue("SpecularAAScreenSpaceVariance", graph.passes[currentPass].pixel.slots, graph);
            var SAAThreshold = GetSlotValue("SpecularAAThreshold", graph.passes[currentPass].pixel.slots, graph);
            if (SAAVariance != null && SAAThreshold != null)
            {
                getSurfaceDataFunction.AppendLine("surfaceData.perceptualSmoothness = GeometricNormalFiltering(surfaceData.perceptualSmoothness, input.worldToTangent[2], {0}, {1});", SAAVariance, SAAThreshold);
            }

            if (masterNodeInfo.hasRefraction)
            {
                //define refraction parameter
                var refractionIndex = GetSlotValue("RefractionIndex", graph.passes[currentPass].pixel.slots, graph);
                var refractionColor = GetSlotValue("RefractionColor", graph.passes[currentPass].pixel.slots, graph);
                var refractionDistance = GetSlotValue("RefractionDistance", graph.passes[currentPass].pixel.slots, graph);

                if (refractionIndex != null && refractionColor != null && refractionDistance != null)
                {
                    getSurfaceDataFunction.AppendLine(@"
#ifdef _HAS_REFRACTION
if (_EnableSSRefraction)
{{
    surfaceData.ior =                       {0};
    surfaceData.transmittanceColor =        {1};
    surfaceData.atDistance =                {2};
        
    surfaceData.transmittanceMask = (1.0 - alpha);
    alpha = 1.0;
}}
else
{{
    surfaceData.ior = 1.0;
    surfaceData.transmittanceColor = float3(1.0, 1.0, 1.0);
    surfaceData.atDistance = 1.0;
    surfaceData.transmittanceMask = 0.0;
    alpha = 1.0;
}}
#endif
", refractionIndex, refractionColor, refractionDistance);
                }
            }

            getSurfaceDataFunction.AppendLine(@"
    InitBuiltinData(posInput, alpha, bentNormalWS, -input.worldToTangent[2], input.texCoord1, input.texCoord2, builtinData); ");

            AddCodeIfSlotExist(graph, getSurfaceDataFunction, "Emission", "\tbuiltinData.emissiveColor = {0};\n", null, graph.passes[currentPass].pixel.slots);

            AddCodeIfSlotExist(graph, getSurfaceDataFunction, "Tangent", "TransformTangentToWorld({0}, input.worldToTangent);", null, graph.passes[currentPass].pixel.slots);


            string specularOcclusionSlot = GetSlotValue("SpecularOcclusion", graph.passes[currentPass].pixel.slots,graph);

            if (specularOcclusionSlot != null)
            {
                getSurfaceDataFunction.AppendLine(@"
 #if defined(_SPECULAR_OCCLUSION_CUSTOM)");
                AddCodeIfSlotExist(graph, getSurfaceDataFunction, "SpecularOcclusion", "surfaceData.specularOcclusion = {0}", "", graph.passes[currentPass].pixel.slots);
                getSurfaceDataFunction.AppendLine(@"
 #elif defined(_SPECULAR_OCCLUSION_FROM_AO_BENT_NORMAL)
    // If we have bent normal and ambient occlusion, process a specular occlusion
    surfaceData.specularOcclusion = GetSpecularOcclusionFromBentAO(V, bentNormalWS, surfaceData.normalWS, surfaceData.ambientOcclusion, PerceptualSmoothnessToPerceptualRoughness(surfaceData.perceptualSmoothness));
 #elif defined(_AMBIENT_OCCLUSION) && defined(_SPECULAR_OCCLUSION_FROM_AO)
    surfaceData.specularOcclusion = GetSpecularOcclusionFromAmbientOcclusion(ClampNdotV(dot(surfaceData.normalWS, V)), surfaceData.ambientOcclusion, PerceptualSmoothnessToRoughness(surfaceData.perceptualSmoothness));
 #endif
");
            }
            getSurfaceDataFunction.AppendLine(@"
    PostInit(input, surfaceData, builtinData, posInput,bentNormalWS,alpha,V);
}
void ApplyVertexModification(AttributesMesh input, float3 normalWS, inout float3 positionRWS, float4 time)
{
}
                ");

            return getSurfaceDataFunction.ToString();
        }

        private static void GenerateParticleVert(Graph graph,VFXInfos vfxInfos, StringBuilder shader, int currentPass, Dictionary<string, int> defines, List<VaryingAttribute> varyingAttributes)
        {
            shader.Append(vfxInfos.vertexFunctions);

            PropertyCollector shaderProperties = new PropertyCollector();
            var sb = new StringBuilder();
            // inspired by GenerateSurfaceDescriptionFunction

            ShaderStringBuilder functionsString = new ShaderStringBuilder();
            FunctionRegistry functionRegistry = new FunctionRegistry(functionsString);

            graph.graphData.CollectShaderProperties(shaderProperties, GenerationMode.ForReals);

            var sg = new ShaderStringBuilder();

            GraphContext graphContext = new GraphContext("SurfaceDescriptionInputs");

            foreach (var activeNode in graph.passes[currentPass].vertex.nodes.OfType<AbstractMaterialNode>())
            {
                if (activeNode is IGeneratesFunction)
                {
                    functionRegistry.builder.currentNode = activeNode;
                    (activeNode as IGeneratesFunction).GenerateNodeFunction(functionRegistry, graphContext, GenerationMode.ForReals);
                }
                if (activeNode is IGeneratesBodyCode)
                    (activeNode as IGeneratesBodyCode).GenerateNodeCode(sg, graphContext, GenerationMode.ForReals);

                activeNode.CollectShaderProperties(shaderProperties, GenerationMode.ForReals);
            }

            shader.AppendLine(functionsString.ToString());
            functionRegistry.builder.currentNode = null;

            sb.Append(sg.ToString());
            var usedSlots = /*slots ?? */graph.graphData.outputNode.GetInputSlots<MaterialSlot>().Where(t => t.shaderOutputName != "Position").Intersect(graph.passes[currentPass].vertex.slots);

            foreach (var input in usedSlots)
                if (input != null)
                {
                    var foundEdges = graph.graphData.GetEdges(input.slotReference).ToArray();
                    if (foundEdges.Any())
                        sb.AppendFormat("surfaceData.{0} = {1};\n", ShaderGraphToSurfaceDescriptionName(NodeUtils.GetHLSLSafeName(input.shaderOutputName)), graph.graphData.outputNode.GetSlotValue(input.id, GenerationMode.ForReals));
                    else
                        sb.AppendFormat("surfaceData.{0} = {1};\n", ShaderGraphToSurfaceDescriptionName(NodeUtils.GetHLSLSafeName(input.shaderOutputName)), input.GetDefaultValue(GenerationMode.ForReals));
                }

            shader.AppendLine(@"
#define VFX_VARYING_PS_INPUTS AttributesMesh
#define VFX_VARYING_POSCS PositionOS
#include ""Packages/com.unity.visualeffectgraph/Shaders/RenderPipeline/HDRP/VFXCommon.cginc""
#include ""Packages/com.unity.visualeffectgraph/Shaders/VFXCommon.cginc""
ByteAddressBuffer attributeBuffer;
PackedVaryingsType ParticleVert(AttributesMesh inputMesh, uint instanceID : SV_InstanceID)
{
    uint index = instanceID;
".Replace("\n", "\n"));
            shader.Append("    " + vfxInfos.loadAttributes.Replace("\n", "\n    "));

            shader.AppendLine(@"
    float3 size3 = float3(size,size,size);
    #if VFX_USE_SCALEX_CURRENT
    size3.x *= scaleX;
    #endif
    #if VFX_USE_SCALEY_CURRENT
    size3.y *= scaleY;
    #endif
    #if VFX_USE_SCALEZ_CURRENT
    size3.z *= scaleZ;
    #endif");

            shader.Append("\t" + vfxInfos.vertexShaderContent.Replace("\n", "\n\t"));

            shader.AppendLine(@"
    float4x4 elementToVFX = GetElementToVFXMatrix(axisX,axisY,axisZ,float3(angleX,angleY,angleZ),float3(pivotX,pivotY,pivotZ),size3,position);

    float3 particlePos;
    float3 objectPos = inputMesh.positionOS;
");

            // add shader code to compute Position if any
            shader.AppendLine(sb.ToString());
            // add shader code to take new objectPos into account if the position slot is linked to something
            var slot = graph.passes[currentPass].vertex.slots.FirstOrDefault(t => t.shaderOutputName == "Position");
            if (slot != null)
            {
                var foundEdges = graph.graphData.GetEdges(slot.slotReference).ToArray();
                if (foundEdges.Any())
                {
                    shader.AppendFormat("objectPos = {0};\n", graph.graphData.outputNode.GetSlotValue(slot.id, GenerationMode.ForReals));
                }
            }

            shader.Append(@"    particlePos = mul(elementToVFX,float4(objectPos,1)).xyz;
    inputMesh.positionOS = particlePos;

    PackedVaryingsType result = Vert(inputMesh);
");

            foreach (var varyingAttribute in varyingAttributes)
            {
                shader.AppendFormat(@"
    result.vparticle.vfxAttributes.{0} = {0};", varyingAttribute.name);
            }

            shader.Append(@"
    result.vparticle.instanceID = instanceID; // transmit the instanceID to the pixel shader through the varyings
    return result;
}
");
            shader.AppendLine("#pragma vertex ParticleVert");
        }
    }
}
