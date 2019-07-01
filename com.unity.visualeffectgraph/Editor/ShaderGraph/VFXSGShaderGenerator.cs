#if VFX_HAS_SG
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using UnityEditor.ShaderGraph;
using UnityEditor.VFX;
using UnityEditor.Graphing.Util;
using UnityEditor.Graphing;
using UnityEngine;

using UnlitMasterNode = UnityEditor.ShaderGraph.UnlitMasterNode;
using UnityEngine.Rendering;

namespace UnityEditor.VFX.SG
{
    public static class VFXSGShaderGenerator
    {
        public class Graph
        {
            internal GraphData graphData;
            internal List<MaterialSlot> slots;
            internal string shaderCode;

            internal struct Function
            {
                internal List<AbstractMaterialNode> nodes;
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

            internal readonly static PassInfo[] unlitPassInfo = new PassInfo[]
            {
                new PassInfo("ShadowCaster",new FunctionInfo(new List<int>(new int[]{UnlitMasterNode.AlphaSlotId,UnlitMasterNode.AlphaThresholdSlotId})),new FunctionInfo(new List<int>(new int[]{UnlitMasterNode.PositionSlotId }))),
                new PassInfo("SceneSelectionPass",new FunctionInfo(new List<int>(new int[]{UnlitMasterNode.AlphaSlotId,UnlitMasterNode.AlphaThresholdSlotId})),new FunctionInfo(new List<int>(new int[]{UnlitMasterNode.PositionSlotId }))),
                new PassInfo("DepthForwardOnly",new FunctionInfo(new List<int>(new int[]{UnlitMasterNode.AlphaSlotId,UnlitMasterNode.AlphaThresholdSlotId})),new FunctionInfo(new List<int>(new int[]{UnlitMasterNode.PositionSlotId }))),
                new PassInfo("MotionVectors",new FunctionInfo(new List<int>(new int[]{UnlitMasterNode.AlphaSlotId,UnlitMasterNode.AlphaThresholdSlotId})),new FunctionInfo(new List<int>(new int[]{UnlitMasterNode.PositionSlotId }))),
                new PassInfo("ForwardOnly",new FunctionInfo(new List<int>(new int[]{UnlitMasterNode.AlphaSlotId,UnlitMasterNode.AlphaThresholdSlotId,UnlitMasterNode.ColorSlotId})),new FunctionInfo(new List<int>(new int[]{UnlitMasterNode.PositionSlotId }))),
                new PassInfo("META",new FunctionInfo(new List<int>(new int[]{UnlitMasterNode.AlphaSlotId,UnlitMasterNode.AlphaThresholdSlotId,UnlitMasterNode.ColorSlotId})),new FunctionInfo(new List<int>(new int[]{UnlitMasterNode.PositionSlotId }))),
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
                graph.passes[currentPass].pixel.slots = graph.slots.Where(t => passInfos[currentPass].pixel.activeSlots.Contains(t.id)).ToList();
                graph.passes[currentPass].vertex.slots = graph.slots.Where(t => passInfos[currentPass].vertex.activeSlots.Contains(t.id)).ToList();
            }

            return graph;
        }

        public static Dictionary<string, Texture> GetUsedTextures(Graph graph)
        {
            var shaderProperties = new PropertyCollector();
            foreach (var node in graph.passes.SelectMany(t => t.pixel.nodes.Concat(t.vertex.nodes)).Distinct())
            {
                node.CollectShaderProperties(shaderProperties, GenerationMode.ForReals);
            }

            return shaderProperties.GetConfiguredTextures().ToDictionary(t => t.name, t => (Texture)EditorUtility.InstanceIDToObject(t.textureId));
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

        struct VaryingAttribute
        {
            public string name;
            public int type;
        }

        static List<VaryingAttribute> ComputeVaryingAttribute(Graph graph, VFXInfos vfxInfos)
        {
            var shaderProperties = new PropertyCollector();
            graph.graphData.CollectShaderProperties(shaderProperties, GenerationMode.ForReals);


            // In the varying we must put all attributes that are modified by a block from this outputcontext and that are used by the shadergraph
            //Alpha is a special case that is always used (alpha from SG is multiplied by alpha from VFX) .

            List<VaryingAttribute> result = new List<VaryingAttribute>();
            foreach (var info in vfxInfos.attributes.Zip(vfxInfos.attributeTypes.Cast<int>(), (string a, int b) => new KeyValuePair<string, int>(a, (int)b)).Where(t => vfxInfos.modifiedByOutputAttributes.Contains(t.Key) && (t.Key == "alpha" || shaderProperties.properties.Any(u => u.displayName.Equals(t.Key, StringComparison.InvariantCultureIgnoreCase)))))
            {
                result.Add(new VaryingAttribute { name = info.Key, type = info.Value });
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


        static string GenerateVaryingVFXAttribute(Graph graph, VFXInfos vfxInfos, List<VaryingAttribute> varyingAttributes)
        {
            var sb = new StringBuilder();

            sb.Append(@"
struct ParticleMeshToPS
{
");
            // In the varying we must put all attributes that are modified by a block from this outputcontext and that are used by the shadergraph
            //Alpha is a special case that is always used (alpha from SG is multiplied by alpha from VFX) .
            int colorSemNum = 1;
            foreach (var info in varyingAttributes)
            {
                if (colorSemNum < 10)
                    sb.AppendFormat("    nointerpolation {0} {1} : COLOR{2};\n", TypeToCode(info.type), info.name, colorSemNum++);
                else
                    sb.AppendFormat("    nointerpolation {0} {1} : NORMAL{2};\n", TypeToCode(info.type), info.name, (colorSemNum++) - 10 + 2); //Start with NORMAL3
            }
            sb.Append(@"
};");
            return sb.ToString();
        }


        internal delegate void PrepareMasterNodeDelegate(Graph graph, Dictionary<string, string> guiVariablest, Dictionary<string, int> defines);

        internal struct MasterNodeInfo
        {
            public MasterNodeInfo(Graph.PassInfo[] passInfos, PrepareMasterNodeDelegate prepare)
            {
                this.passInfos = passInfos;
                this.prepare = prepare;
            }
            public readonly Graph.PassInfo[] passInfos;
            public readonly PrepareMasterNodeDelegate prepare;
        }

        static readonly Dictionary<System.Type, MasterNodeInfo> s_MasterNodeInfos = new Dictionary<Type, MasterNodeInfo>
        {
            {typeof(UnlitMasterNode), new MasterNodeInfo(Graph.unlitPassInfo,null) },
        };

        internal static void RegisterMasterNode(Type masterNodeType, MasterNodeInfo infos)
        {
            s_MasterNodeInfos.Add(masterNodeType, infos);
        }

        static readonly Dictionary<System.Type, PipelineInfo> s_PipelineInfos = new Dictionary<Type, PipelineInfo>();

        internal static void RegisterPipeline(Type pipelineAssetType,PipelineInfo info)
        {
            s_PipelineInfos[pipelineAssetType] = info;
        }

       public abstract class PipelineInfo
        { 
            public abstract Dictionary<string, string> GetDefaultShaderVariables();

            public abstract IEnumerable<string> GetSpecificIncludes();

            public abstract IEnumerable<string> GetPerPassSpecificIncludes();
        }

        internal static string GenerateShader(Shader shaderGraph, ref VFXInfos vfxInfos)
        {
            MasterNodeInfo masterNodeInfo;
            Graph graph = LoadShaderGraph(shaderGraph, out masterNodeInfo);
            if (graph == null) return null;

            ShaderDocument document = new ShaderDocument();
            document.Parse(graph.shaderCode);

            var defines = new Dictionary<string, int>();

            Type pipelineAssetType = GraphicsSettings.renderPipelineAsset?.GetType();
            if (pipelineAssetType == null)
                return null;

            PipelineInfo pipelineInfos = null;
            if (!s_PipelineInfos.TryGetValue(pipelineAssetType, out pipelineInfos) || pipelineInfos == null)
                return null;

            var guiVariables = pipelineInfos.GetDefaultShaderVariables();

            if (masterNodeInfo.prepare != null)
                masterNodeInfo.prepare(graph, guiVariables, defines);

            int cptLine = 0;
            foreach( var include in pipelineInfos.GetSpecificIncludes())
            {
                document.InsertShaderLine(cptLine++, include);
            }

            defines["UNITY_VFX_ACTIVE"] = 1;

            List<VaryingAttribute> varyingAttributes = ComputeVaryingAttribute(graph, vfxInfos);

            foreach (var pass in document.passes)
            {
                int currentPass = Array.FindIndex(masterNodeInfo.passInfos, t => t.name == pass.name);
                if (currentPass == -1)
                    continue;

                GeneratePass(vfxInfos, graph, pipelineInfos,guiVariables, defines, varyingAttributes, pass, currentPass, ref masterNodeInfo);
            }
            foreach (var define in defines)
                document.InsertShaderCode(0, string.Format("#define {0} {1}", define.Key, define.Value));

            document.ReplaceParameterVariables(guiVariables);

            return document.ToString(false).Replace("\r", "");
        }

        private static void GeneratePass(VFXInfos vfxInfos, Graph graph, PipelineInfo pipelineInfos,Dictionary<string, string> guiVariables, Dictionary<string, int> defines, List<VaryingAttribute> varyingAttributes, PassPart pass, int currentPass, ref MasterNodeInfo masterNodeInfo)
        {
            pass.InsertShaderCode(0, GenerateVaryingVFXAttribute(graph, vfxInfos, varyingAttributes));

            foreach( var include in pipelineInfos.GetPerPassSpecificIncludes())
            {
                pass.InsertShaderLine(-1, include);
            }

            var sb = new StringBuilder();
            GenerateParticleVert(graph, vfxInfos, sb, currentPass, varyingAttributes);
            pass.InsertShaderCode(-1, sb.ToString());
            pass.RemoveShaderCodeContaining("#pragma vertex Vert");

            // Pass particleID to pixel shader function : SurfaceDescriptionFunction
            int functionIndex;
            List<string> functionFragInputsToSurfaceDescriptionInputs = pass.ExtractFunction("SurfaceDescriptionInputs", "FragInputsToSurfaceDescriptionInputs", out functionIndex, "FragInputs", "input", "float3", "viewWS");

            if(functionFragInputsToSurfaceDescriptionInputs != null)
            {
                for (int i = 0; i < functionFragInputsToSurfaceDescriptionInputs.Count - 2; ++i)
                { 
                   pass.InsertShaderLine(i + functionIndex,functionFragInputsToSurfaceDescriptionInputs[i]);
                }
                pass.InsertShaderLine(functionIndex + functionFragInputsToSurfaceDescriptionInputs.Count - 2, "                output.particleID = input.particleID;");
                for (int i = functionFragInputsToSurfaceDescriptionInputs.Count - 2; i < functionFragInputsToSurfaceDescriptionInputs.Count; ++i)
                {
                    pass.InsertShaderLine(i + functionIndex + 1, functionFragInputsToSurfaceDescriptionInputs[i]);
                }
            }

            //Replace CBUFFER and TEXTURE bindings by the one from the VFX
            int cBuffer = pass.IndexOfLineMatching(@"CBUFFER_START");
            if( cBuffer != -1)
            {
                int cBufferEnd = pass.IndexOfLineMatching(@"CBUFFER_END", cBuffer);

                if (cBufferEnd != -1)
                {
                    ++cBufferEnd;

                    while (string.IsNullOrWhiteSpace(pass.shaderCode[cBufferEnd]) || pass.shaderCode[cBufferEnd].Contains("TEXTURE2D("))
                    {
                        pass.shaderCode.RemoveAt(cBufferEnd);
                    }
                    pass.shaderCode.RemoveRange(cBuffer, cBufferEnd - cBuffer + 1);
                }

                pass.InsertShaderCode(cBuffer, vfxInfos.parameters);
            }
            // pass VParticle varyings as additionnal parameter to SurfaceDescriptionFunction
            int surfaceDescCall = pass.IndexOfLineMatching(@"SurfaceDescription\s+surfaceDescription\s*=\s*SurfaceDescriptionFunction\s*\(\s*surfaceDescriptionInputs\s*\)\;");
            if(surfaceDescCall != -1)
            {
                pass.shaderCode[surfaceDescCall] = @"SurfaceDescription surfaceDescription = SurfaceDescriptionFunction(surfaceDescriptionInputs,fragInputs.vparticle);";
            }

            // Inject attribute load code to SurfaceDescriptionFunction
            List<string> functionSurfaceDefinition = pass.ExtractFunction("SurfaceDescription", "SurfaceDescriptionFunction", out functionIndex, "SurfaceDescriptionInputs", "IN");

            if(functionSurfaceDefinition != null)
            {
                pass.InsertShaderLine(functionIndex - 1, "ByteAddressBuffer attributeBuffer;");

                functionSurfaceDefinition[0] = "SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN,ParticleMeshToPS vParticle)";

                for (int i = 0; i < 2; ++i)
                {
                    pass.InsertShaderLine(i + functionIndex, functionSurfaceDefinition[i]);
                }
                int cptLine = 2;
                //Load attributes from the ByteAddressBuffer
                pass.InsertShaderLine((cptLine++) + functionIndex, "                                    uint index = IN.particleID;");
                pass.InsertShaderLine((cptLine++) + functionIndex, "                                    " + vfxInfos.loadAttributes.Replace("\n", "\n                                    "));

                // override attribute load with value from varyings in case of attriibute values modified in output context
                foreach (var varyingAttribute in varyingAttributes)
                {
                    pass.InsertShaderLine((cptLine++) + functionIndex, string.Format("{0} = vParticle.{0};", varyingAttribute.name));
                }

                // define variable for each value that is a vfx attribute
                PropertyCollector shaderProperties = new PropertyCollector();
                graph.graphData.CollectShaderProperties(shaderProperties, GenerationMode.ForReals);
                foreach (var prop in shaderProperties.properties)
                {
                    string matchingAttribute = vfxInfos.attributes.FirstOrDefault(t => prop.displayName.Equals(t, StringComparison.InvariantCultureIgnoreCase));
                    if (matchingAttribute != null)
                    {
                        if (matchingAttribute == "color")
                            pass.InsertShaderLine((cptLine++) + functionIndex, "    " + prop.GetPropertyDeclarationString("") + " = float4(color,1);");
                        else
                            pass.InsertShaderLine((cptLine++) + functionIndex, "    " + prop.GetPropertyDeclarationString("") + " = " + matchingAttribute + ";");
                    }
                }
                pass.InsertShaderLine((cptLine++) + functionIndex, @"

    if( !alive) discard;
    ");

                for (int i = 2; i < functionSurfaceDefinition.Count - 2; ++i)
                {
                    pass.InsertShaderLine((cptLine++) + functionIndex, functionSurfaceDefinition[i]);
                }
                if ( vfxInfos.attributes.Contains("alpha") )
                    pass.InsertShaderLine((cptLine++) + functionIndex, "                        surface.Alpha *= alpha;");

                for (int i = functionSurfaceDefinition.Count - 2; i < functionSurfaceDefinition.Count; ++i)
                {
                    pass.InsertShaderLine((cptLine++) + functionIndex, functionSurfaceDefinition[i]);
                }
            }

        }

        private static void GenerateParticleVert(Graph graph,VFXInfos vfxInfos, StringBuilder shader, int currentPass, List<VaryingAttribute> varyingAttributes)
        {
            // ParticleVert will replace the standard HDRP Vert function as vertex function

            // add functions from the vfx nodes
            shader.Append(vfxInfos.vertexFunctions);

            var sb = new StringBuilder();

            ShaderStringBuilder functionsString = new ShaderStringBuilder();
            FunctionRegistry functionRegistry = new FunctionRegistry(functionsString);

            var sg = new ShaderStringBuilder();
            // add function from the sg nodes
            shader.AppendLine(functionsString.ToString());
            functionRegistry.builder.currentNode = null;

            sb.Append(sg.ToString());
            shader.Append(s_GenerateVertex[vfxInfos.renderingType](vfxInfos));
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

            // override the positionOS with the particle position and call the standard Vert function
            shader.Append(@"float3 particlePos = mul(elementToVFX,float4(objectPos,1)).xyz;
    inputMesh.positionOS = particlePos;
    PackedVaryingsType result = Vert(inputMesh);
");

            //transfer modified attributes in the vfx output as varyings
            foreach (var varyingAttribute in varyingAttributes)
            {
                shader.AppendFormat(@"
    result.vparticle.{0} = {0};", varyingAttribute.name);
            }

            // transfer particle ID
            shader.Append(@"
    result.vmesh.particleID = inputMesh.particleID; // transmit the instanceID to the pixel shader through the varyings
    return result;
}
");
            shader.AppendLine("#pragma vertex ParticleVert");
        }

        delegate string GenerateVertexPartDelegate(VFXInfos vfxInfos);

        static readonly Dictionary<VFXTaskType, GenerateVertexPartDelegate> s_GenerateVertex = new Dictionary<VFXTaskType, GenerateVertexPartDelegate>
        {
            { VFXTaskType.ParticleMeshOutput,GenerateVertexPartMesh },
            { VFXTaskType.ParticleTriangleOutput,GenerateVertexPartTri },
            { VFXTaskType.ParticleQuadOutput,GenerateVertexPartQuad },
            { VFXTaskType.ParticleOctagonOutput,GenerateVertexPartOct },
        };

        private static string GenerateVertexPartMesh(VFXInfos vfxInfos)
        {
            return @"

PackedVaryingsType ParticleVert(AttributesMesh inputMesh)
{
    uint index = inputMesh.particleID;
";
        }

        private static string GenerateVertexPartQuad(VFXInfos vfxInfos)
        {
            return @"

PackedVaryingsType ParticleVert(uint id : SV_VertexID,uint instID : SV_InstanceID)
{
	uint particleID = (id >> 2) + instID * 2048;
    uint index = particleID;
    AttributesMesh inputMesh = (AttributesMesh)0;
    float2 uv;
    uv.x = float(id & 1);
	uv.y = float((id & 2) >> 1);
#ifdef ATTRIBUTES_NEED_TEXCOORD0
	inputMesh.uv0.xy = uv;
#endif
    inputMesh.positionOS = float3(uv - 0.5f,0);
    inputMesh.particleID = particleID;
";
        }

        private static string GenerateVertexPartTri(VFXInfos vfxInfos)
        {
            return @"

PackedVaryingsType ParticleVert(uint id : SV_VertexID)
{
	uint particleID = id / 3;
    uint index = particleID;
    AttributesMesh inputMesh = (AttributesMesh)0;
	const float2 kOffsets[] = {
		float2(-0.5f, 	-0.288675129413604736328125f),
		float2(0.0f, 	0.57735025882720947265625f),
		float2(0.5f,	-0.288675129413604736328125f),
	};
	
	const float kUVScale = 0.866025388240814208984375f;

    inputMesh.positionOS = float3(kOffsets[id % 3],0);
#ifdef ATTRIBUTES_NEED_TEXCOORD0
	inputMesh.uv0.xy = (inputMesh.positionOS.xy * kUVScale) + 0.5f;
#endif
    inputMesh.particleID = particleID;
";
        }

        private static string GenerateVertexPartOct(VFXInfos vfxInfos)
        {
            return @"

PackedVaryingsType ParticleVert(uint id : SV_VertexID,uint instID : SV_InstanceID)
{
	uint particleID = (id >> 3) + instID * 1024;
    uint index = particleID;
    AttributesMesh inputMesh = (AttributesMesh)0;
	const float2 kUvs[8] = 
	{
		float2(-0.5f,	0.0f),
		float2(-0.5f,	0.5f),
		float2(0.0f,	0.5f),
		float2(0.5f,	0.5f),
		float2(0.5f,	0.0f),
		float2(0.5f,	-0.5f),
		float2(0.0f,	-0.5f),
		float2(-0.5f,	-0.5f),
	};
	
	float cf = id & 1 ? 1.0f - cropFactor : 1.0f;
    inputMesh.positionOS =  float3(kUvs[id & 7]  * cf,0);
#ifdef ATTRIBUTES_NEED_TEXCOORD0
	inputMesh.uv0.xy = inputMesh.positionOS.xy + 0.5f;
#endif
    inputMesh.particleID = particleID;
";
        }
    }
}
#endif
