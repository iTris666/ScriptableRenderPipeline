#if LWRP_HAS_VFX
using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using UnityEditor.ShaderGraph;
using UnityEditor.VFX;
using UnityEditor.VFX.SG;

using PassInfo = UnityEditor.VFX.SG.VFXSGShaderGenerator.Graph.PassInfo;
using FunctionInfo = UnityEditor.VFX.SG.VFXSGShaderGenerator.Graph.FunctionInfo;
using MasterNodeInfo = UnityEditor.VFX.SG.VFXSGShaderGenerator.MasterNodeInfo;
using UnityEngine.Rendering.LWRP;

namespace UnityEditor.RenderPipeline.LWpipeline
{
    internal class LWRPPipelineInfo : VFXSGShaderGenerator.PipelineInfo
    {

        internal readonly static PassInfo[] unlitPassInfo = new PassInfo[]
        {
                new PassInfo("ShadowCaster",new FunctionInfo(new List<int>(new int[]{UnlitMasterNode.AlphaSlotId,UnlitMasterNode.AlphaThresholdSlotId})),new FunctionInfo(new List<int>(new int[]{UnlitMasterNode.PositionSlotId }))),
                new PassInfo("SceneSelectionPass",new FunctionInfo(new List<int>(new int[]{UnlitMasterNode.AlphaSlotId,UnlitMasterNode.AlphaThresholdSlotId})),new FunctionInfo(new List<int>(new int[]{UnlitMasterNode.PositionSlotId }))),
                new PassInfo("DepthForwardOnly",new FunctionInfo(new List<int>(new int[]{UnlitMasterNode.AlphaSlotId,UnlitMasterNode.AlphaThresholdSlotId})),new FunctionInfo(new List<int>(new int[]{UnlitMasterNode.PositionSlotId }))),
                new PassInfo("MotionVectors",new FunctionInfo(new List<int>(new int[]{UnlitMasterNode.AlphaSlotId,UnlitMasterNode.AlphaThresholdSlotId})),new FunctionInfo(new List<int>(new int[]{UnlitMasterNode.PositionSlotId }))),
                new PassInfo("ForwardOnly",new FunctionInfo(new List<int>(new int[]{UnlitMasterNode.AlphaSlotId,UnlitMasterNode.AlphaThresholdSlotId,UnlitMasterNode.ColorSlotId})),new FunctionInfo(new List<int>(new int[]{UnlitMasterNode.PositionSlotId }))),
                new PassInfo("META",new FunctionInfo(new List<int>(new int[]{UnlitMasterNode.AlphaSlotId,UnlitMasterNode.AlphaThresholdSlotId,UnlitMasterNode.ColorSlotId})),new FunctionInfo(new List<int>(new int[]{UnlitMasterNode.PositionSlotId }))),
        };
        internal override Dictionary<string, string> GetDefaultShaderVariables()
        {
            return new Dictionary<string, string>();
        }

        internal override IEnumerable<string> GetSpecificIncludes()
        {
            return Enumerable.Empty<string>();
        }

        internal override IEnumerable<string> GetPerPassSpecificIncludes()
        {
            return new string[]{ @"#define UNITY_VERTEX_INPUT_INSTANCE_ID uint particleID : SV_InstanceID; ParticleVaryings vparticle;",
                        @"#define UNITY_TRANSFER_INSTANCE_ID(a,b) b.particleID = a.particuleID"};
        }

        internal override bool ModifyPass(PassPart pass, ref VFXInfos vfxInfos, List<VFXSGShaderGenerator.VaryingAttribute> varyingAttributes, GraphData graphData)
        {
            //Replace CBUFFER and TEXTURE bindings by the one from the VFX
            int cBuffer = pass.IndexOfLineMatching(@"CBUFFER_START");
            if (cBuffer != -1)
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

            int surfaceDescCall = pass.IndexOfLineMatching(@"SurfaceDescription\s+surf\s*=\s*PopulateSurfaceData\s*\(\s*surfaceInput\s*\)\;");
            if (surfaceDescCall != -1)
            {
                pass.shaderCode[surfaceDescCall] = @"SurfaceDescription surf = PopulateSurfaceData(surfaceInput, IN.particleID, IN.vparticle);";
                return true;
            }
            /*
            // Inject attribute load code to SurfaceDescriptionFunction
            List<string> functionSurfaceDefinition = pass.ExtractFunction("SurfaceDescription", "SurfaceDescriptionFunction", out functionIndex, "SurfaceDescriptionInputs", "IN");

            if (functionSurfaceDefinition != null)
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

                // override attribute load with value from varyings in case of attribute values modified in output context
                foreach (var varyingAttribute in varyingAttributes)
                {
                    pass.InsertShaderLine((cptLine++) + functionIndex, string.Format("{0} = vParticle.{0};", varyingAttribute.name));
                }

                // define variable for each value that is a vfx attribute
                PropertyCollector shaderProperties = new PropertyCollector();
                graphData.CollectShaderProperties(shaderProperties, GenerationMode.ForReals);
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
                if (vfxInfos.attributes.Contains("alpha"))
                    pass.InsertShaderLine((cptLine++) + functionIndex, "                        surface.Alpha *= alpha;");

                for (int i = functionSurfaceDefinition.Count - 2; i < functionSurfaceDefinition.Count; ++i)
                {
                    pass.InsertShaderLine((cptLine++) + functionIndex, functionSurfaceDefinition[i]);
                }
            }*/
            return false;
        }

        static readonly Dictionary<Type, MasterNodeInfo> s_MasterNodeInfos = new Dictionary<Type, MasterNodeInfo>
        {
            {typeof(UnlitMasterNode), new MasterNodeInfo(unlitPassInfo,null) },
        };
        internal override Dictionary<Type, MasterNodeInfo> masterNodes => s_MasterNodeInfos;
    }

    [InitializeOnLoad]
    public static class VFXSGLWRPShaderGenerator
    {
        static VFXSGLWRPShaderGenerator()
        {
            VFXSGShaderGenerator.RegisterPipeline(typeof(LightweightRenderPipelineAsset), new LWRPPipelineInfo());
        }
    }

}


#endif
