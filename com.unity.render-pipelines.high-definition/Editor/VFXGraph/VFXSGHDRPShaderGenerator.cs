using System;
using System.Collections.Generic;
using System.Linq;

using UnityEditor.ShaderGraph;
using UnityEditor.VFX.SG;
using UnityEngine.Experimental.Rendering.HDPipeline;

using UnlitMasterNode = UnityEditor.ShaderGraph.UnlitMasterNode;

using PassInfo = UnityEditor.VFX.SG.VFXSGShaderGenerator.Graph.PassInfo;
using FunctionInfo = UnityEditor.VFX.SG.VFXSGShaderGenerator.Graph.FunctionInfo;
using Graph = UnityEditor.VFX.SG.VFXSGShaderGenerator.Graph;
using MasterNodeInfo  = UnityEditor.VFX.SG.VFXSGShaderGenerator.MasterNodeInfo;
using UnityEditor.VFX;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    internal class HDRPPipelineInfo : VFXSGShaderGenerator.PipelineInfo
    {
        internal readonly static PassInfo[] HDlitPassInfos = new PassInfo[]
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

        internal readonly static PassInfo[] HDfabricPassInfos = new PassInfo[]
            {
            new PassInfo("ShadowCaster",new FunctionInfo(FabricSubShader.passShadowCaster.PixelShaderSlots),new FunctionInfo(FabricSubShader.passShadowCaster.VertexShaderSlots)),
            new PassInfo("DepthForwardOnly",new FunctionInfo(FabricSubShader.passDepthForwardOnly.PixelShaderSlots),new FunctionInfo(FabricSubShader.passDepthForwardOnly.VertexShaderSlots)),
            new PassInfo("SceneSelectionPass",new FunctionInfo(FabricSubShader.passSceneSelection.PixelShaderSlots),new FunctionInfo(FabricSubShader.passSceneSelection.VertexShaderSlots)),
            new PassInfo("META",new FunctionInfo(FabricSubShader.passMETA.PixelShaderSlots),new FunctionInfo(FabricSubShader.passMETA.VertexShaderSlots)),
            new PassInfo("MotionVectors",new FunctionInfo(FabricSubShader.passMotionVectors.PixelShaderSlots),new FunctionInfo(FabricSubShader.passMotionVectors.VertexShaderSlots)),
            new PassInfo("ForwardOnly",new FunctionInfo(FabricSubShader.passForwardOnly.PixelShaderSlots),new FunctionInfo(FabricSubShader.passForwardOnly.VertexShaderSlots)),
            };

        internal readonly static PassInfo[] HDunlitPassInfos = new PassInfo[]
            {
            new PassInfo("ShadowCaster",new FunctionInfo(HDUnlitSubShader.passShadowCaster.PixelShaderSlots),new FunctionInfo(HDUnlitSubShader.passShadowCaster.VertexShaderSlots)),
            new PassInfo("DepthForwardOnly",new FunctionInfo(HDUnlitSubShader.passDepthForwardOnly.PixelShaderSlots),new FunctionInfo(HDUnlitSubShader.passDepthForwardOnly.VertexShaderSlots)),
            new PassInfo("SceneSelectionPass",new FunctionInfo(HDUnlitSubShader.passSceneSelection.PixelShaderSlots),new FunctionInfo(HDUnlitSubShader.passSceneSelection.VertexShaderSlots)),
            new PassInfo("META",new FunctionInfo(HDUnlitSubShader.passMETA.PixelShaderSlots),new FunctionInfo(HDUnlitSubShader.passMETA.VertexShaderSlots)),
            new PassInfo("MotionVectors",new FunctionInfo(HDUnlitSubShader.passMotionVectors.PixelShaderSlots),new FunctionInfo(HDUnlitSubShader.passMotionVectors.VertexShaderSlots)),
            new PassInfo("DistortionVectors",new FunctionInfo(HDUnlitSubShader.passDistortion.PixelShaderSlots),new FunctionInfo(HDUnlitSubShader.passDistortion.VertexShaderSlots)),
            new PassInfo("ForwardOnly",new FunctionInfo(HDUnlitSubShader.passForwardOnly.PixelShaderSlots),new FunctionInfo(HDUnlitSubShader.passForwardOnly.VertexShaderSlots)),
            };

        internal readonly static PassInfo[] HDhairPassInfos = new PassInfo[]
            {
            new PassInfo("ShadowCaster",new FunctionInfo(HairSubShader.passShadowCaster.PixelShaderSlots),new FunctionInfo(HairSubShader.passShadowCaster.VertexShaderSlots)),
            new PassInfo("SceneSelectionPass",new FunctionInfo(HairSubShader.passSceneSelection.PixelShaderSlots),new FunctionInfo(HairSubShader.passSceneSelection.VertexShaderSlots)),
            new PassInfo("META",new FunctionInfo(HairSubShader.passMETA.PixelShaderSlots),new FunctionInfo(HairSubShader.passMETA.VertexShaderSlots)),
            new PassInfo("DepthForwardOnly",new FunctionInfo(HairSubShader.passDepthForwardOnly.PixelShaderSlots),new FunctionInfo(HairSubShader.passDepthForwardOnly.VertexShaderSlots)),
            new PassInfo("MotionVectors",new FunctionInfo(HairSubShader.passMotionVectors.PixelShaderSlots),new FunctionInfo(HairSubShader.passMotionVectors.VertexShaderSlots)),
            new PassInfo("TransparentDepthPrepass",new FunctionInfo(HairSubShader.passTransparentDepthPrepass.PixelShaderSlots),new FunctionInfo(HairSubShader.passTransparentDepthPrepass.VertexShaderSlots)),
            new PassInfo("TransparentBackface",new FunctionInfo(HairSubShader.passTransparentBackface.PixelShaderSlots),new FunctionInfo(HairSubShader.passTransparentBackface.VertexShaderSlots)),
            new PassInfo("ForwardOnly",new FunctionInfo(HairSubShader.passForwardOnly.PixelShaderSlots),new FunctionInfo(HairSubShader.passForwardOnly.VertexShaderSlots)),
            new PassInfo("TransparentDepthPostpass",new FunctionInfo(HairSubShader.passTransparentDepthPostpass.PixelShaderSlots),new FunctionInfo(HairSubShader.passTransparentDepthPostpass.VertexShaderSlots)),
            };

        static Dictionary<string, string> guiVariables = new Dictionary<string, string>()
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
        internal override Dictionary<string, string> GetDefaultShaderVariables()
        {
            return guiVariables;
        }

        internal override IEnumerable<string> GetSpecificIncludes()
        {
            return new string[] { "#include \"Packages/com.unity.visualeffectgraph/Shaders/RenderPipeline/HDRP/VFXDefines.hlsl\"" };
        }

        internal override IEnumerable<string> GetPerPassSpecificIncludes()
        {
            return new string[] { @"#define VFX_VARYING_PS_INPUTS VaryingsMeshToDS",
                                        @"#define VFX_VARYING_POSCS positionRWS",
                                        @"#include ""Packages/com.unity.visualeffectgraph/Shaders/RenderPipeline/HDRP/VFXCommon.cginc""",
                                        @"#include ""Packages/com.unity.visualeffectgraph/Shaders/VFXCommon.cginc"""
                };
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

        internal override string vertexReturnType { get => "PackedVaryingsType"; }
        internal override string vertexInputType { get => "AttributesMesh"; }

        internal override Dictionary<Type, MasterNodeInfo> masterNodes => s_MasterNodeInfos;

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

        private static void PrepareHairMasterNode(Graph graph, Dictionary<string, string> guiVariables, Dictionary<string, int> defines)
        {
            var masterNode = graph.graphData.outputNode as HairMasterNode;

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
                            {
                                guiVariables["_AlphaSrcBlend"] = "One";
                                guiVariables["_AlphaDstBlend"] = "One";
                            }
                            break;
                    }
                }
            }
        }

        private static void PrepareFabricMasterNode(Graph graph, Dictionary<string, string> guiVariables, Dictionary<string, int> defines)
        {
            var masterNode = graph.graphData.outputNode as FabricMasterNode;

            if (masterNode != null)
            {

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

        static readonly Dictionary<System.Type, MasterNodeInfo> s_MasterNodeInfos = new Dictionary<Type, MasterNodeInfo>
        {
            {typeof(HDLitMasterNode), new MasterNodeInfo(HDlitPassInfos,PrepareHDLitMasterNode) },
            {typeof(HDUnlitMasterNode), new MasterNodeInfo(HDunlitPassInfos,PrepareHDUnlitMasterNode) },
            {typeof(FabricMasterNode), new MasterNodeInfo(HDfabricPassInfos,PrepareFabricMasterNode) },
            {typeof(HairMasterNode), new MasterNodeInfo(HDhairPassInfos,PrepareHairMasterNode) },
            {typeof(UnlitMasterNode), new MasterNodeInfo(unlitPassInfo,null) },
        };

        internal override bool ModifyPass(PassPart pass, ref VFXInfos vfxInfos, List<VFXSGShaderGenerator.VaryingAttribute> varyingAttributes, GraphData graphData)
        {

            // Pass particleID to pixel shader function : SurfaceDescriptionFunction
            int functionIndex;
            List<string> functionFragInputsToSurfaceDescriptionInputs = pass.ExtractFunction("SurfaceDescriptionInputs", "FragInputsToSurfaceDescriptionInputs", out functionIndex, "FragInputs", "input", "float3", "viewWS");

            if (functionFragInputsToSurfaceDescriptionInputs != null)
            {
                for (int i = 0; i < functionFragInputsToSurfaceDescriptionInputs.Count - 2; ++i)
                {
                    pass.InsertShaderLine(i + functionIndex, functionFragInputsToSurfaceDescriptionInputs[i]);
                }
                pass.InsertShaderLine(functionIndex + functionFragInputsToSurfaceDescriptionInputs.Count - 2, "                output.particleID = input.particleID;");
                for (int i = functionFragInputsToSurfaceDescriptionInputs.Count - 2; i < functionFragInputsToSurfaceDescriptionInputs.Count; ++i)
                {
                    pass.InsertShaderLine(i + functionIndex + 1, functionFragInputsToSurfaceDescriptionInputs[i]);
                }
            }
            ReplaceCBuffer(pass, ref vfxInfos);
            // pass VParticle varyings as additionnal parameter to SurfaceDescriptionFunction
            int surfaceDescCall = pass.IndexOfLineMatching(@"SurfaceDescription\s+surfaceDescription\s*=\s*SurfaceDescriptionFunction\s*\(\s*surfaceDescriptionInputs\s*\)\;");
            if (surfaceDescCall != -1)
            {
                pass.shaderCode[surfaceDescCall] = @"SurfaceDescription surfaceDescription = SurfaceDescriptionFunction(surfaceDescriptionInputs,fragInputs.vparticle);";
            }

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

                // override attribute load with value from varyings in case of attriibute values modified in output context
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
            }

            return true;
        }
}

    [InitializeOnLoad]
    public static class VFXSGHDRPShaderGenerator
    {
        static VFXSGHDRPShaderGenerator()
        {
            VFXSGShaderGenerator.RegisterPipeline(typeof(HDRenderPipelineAsset), new HDRPPipelineInfo());
        }
    }
}
