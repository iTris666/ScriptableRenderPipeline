using System.Collections.Generic;
using System.Linq;
using UnityEditor.ShaderGraph;
using UnityEngine;
using System.Text;

namespace UnityEditor.VFX
{

    class VFXShaderGraphInfo
    {
        public class Pass
        {
            public string name;
            public Function[] functions = new Function[2];

        }

        public struct Property
        {
            public string name;
            public string type;
        }

        public class Function
        {
            public ShaderGraphRequirements requirements;
            public Property[] properties;
            public string definition;
        }

        public static VFXShaderGraphInfo GetDefaultInfos()
        {
            VFXShaderGraphInfo info = new VFXShaderGraphInfo();


            var pixelForwardProperties = new Property[]
            {
                new Property(){name = "color", type="Float3"},
                new Property(){name = "firstColor", type="Float3"},
                new Property(){name = "secondColor", type="Float3"},
            };

            var vertexProperty = new Property[]
            {
                new Property(){name = "offset",type = "Vector3" }
            };

            var vertexRequirements = new ShaderGraphRequirements()
            {
                requiresVertexColor = true,
                requiresPosition = NeededCoordinateSpace.Object,
            };

            var pixelDepthRequirements = new ShaderGraphRequirements()
            {
                requiresPosition = NeededCoordinateSpace.Object | NeededCoordinateSpace.World,
                requiresMeshUVs = new List<UVChannel>() { UVChannel.UV0 }
            };

            var pixelForwardRequirements = new ShaderGraphRequirements()
            {
                requiresNormal = NeededCoordinateSpace.Object,
                requiresTangent = NeededCoordinateSpace.Object,
                requiresMeshUVs = new List<UVChannel>() { UVChannel.UV1 },
            }.Union(pixelDepthRequirements);



            info.passes = new Pass[]
            {
                new Pass()
                {
                    name="DepthOnly",
                    functions = new Function[]{ new Function{requirements = vertexRequirements, properties = vertexProperty},
                                                new Function{requirements = pixelDepthRequirements, properties = new Property[]{ },definition = @"
float4 GetSurfaceFunction(FragInput input,Properties properties)
{
return float4(1,0,0,1);
}" }
                    },
                },
                new Pass()
                {
                    name="Forward",
                    functions = new Function[]{ new Function{requirements = vertexRequirements, properties = vertexProperty},
                                                new Function{requirements = pixelForwardRequirements, properties = pixelForwardProperties,definition = @"
float4 GetSurfaceFunction(FragInput input,Properties properties)
{
return float4(1,0,0,1);
}" }
                    }
                }
            };

            return info;
        }

        public Pass[] passes;
    }


    class VFXShaderGraphParticleOutput : VFXAbstractParticleOutput
    {
        public Shader shaderGraph;

        VFXShaderGraphInfo GetShaderGraphInfos()
        {
            return VFXShaderGraphInfo.GetDefaultInfos();
        }

        class AdditionnalICompileInfo
        {
            public List<string> additionalDefines;
            public List<string> additionalDataHeaders;
            public List<VFXMapping> additionalMappings;
            public List<VFXAttributeInfo> attributes;
            public Dictionary<string,string> additionalMacros;
        }

        AdditionnalICompileInfo m_Infos;

        public override void BeginCompile()
        {
            base.BeginCompile();
            var infos = GetShaderGraphInfos();

            if (infos != null)
            {

                m_Infos = new AdditionnalICompileInfo();

                m_Infos.additionalDefines = new List<string>();
                m_Infos.additionalDefines.Add("UNITY_VFX_SG");

                foreach (var pass in infos.passes)
                {
                    for (int i = 0; i < 2; ++i)
                    {
                        AddFunction(pass.name, i, pass.functions[i]);
                    }

                    StringBuilder attributeStruct = new StringBuilder("struct ParticleAttributes\n{\n");

                    foreach( var attr in pass.functions[1].properties)
                    {
                        attributeStruct.AppendFormat("\tnointerpolation {0} {1};\n",attr.type,attr.name);
                    }

                    attributeStruct.AppendLine("}");


                }
            }
        }


        void AddFunction(string passName,int functionIndex, VFXShaderGraphInfo.Function function)
        {
            string format = string.Format("VFX_SG_{0}_REQUIRE_{{0}}_{1}",functionIndex == 0 ? "VERTEX":"PIXEL",passName.ToUpper());

            if(function.requirements.requiresVertexColor)
                m_Infos.additionalDefines.Add(string.Format(format,"COLOR"));

            for(UVChannel i = UVChannel.UV0; i <= UVChannel.UV3; ++i)
            {
                if(function.requirements.requiresMeshUVs.Contains(i))
                    m_Infos.additionalDefines.Add(string.Format(format, "TEXCOORD" +(int)i));
            }

            if (function.requirements.requiresNormal != 0)
            {
                m_Infos.additionalDefines.Add(string.Format(format, "NORMAL"));
            }

            if (function.requirements.requiresTangent != 0)
            {
                m_Infos.additionalDefines.Add(string.Format(format, "TANGENT"));
            }

            if(function.definition != null)
            {
                string definition = string.Format(functionIndex == 0 ? "GetSurfaceFuntionDefinition_{0}" : "ApplyVertexModificationDefinition_{0}", passName);

                m_Infos.additionalMacros.Add(definition, function.definition);
            }
        }

        public override IEnumerable<VFXAttributeInfo> attributes { get { return base.attributes.Concat(m_Infos.attributes); } }
        public override IEnumerable<VFXMapping> additionalMappings { get { return base.additionalMappings.Concat(m_Infos.additionalMappings); } }
        public override IEnumerable<string> additionalDataHeaders { get { return base.additionalDataHeaders.Concat(m_Infos.additionalDataHeaders); } }
        public override IEnumerable<string> additionalDefines { get { return base.additionalDefines.Concat(m_Infos.additionalDefines); } }

        public override void EndCompile()
        {
            m_Infos = null;

            base.EndCompile();
        }
    }

    [VFXInfo]
    class VFXBasicCubeOutput : VFXShaderGraphParticleOutput
    {
        public override string name { get { return "Cube Output"; } }
        public override string codeGeneratorTemplate { get { return RenderPipeTemplate("VFXParticleBasicCube"); } }
        public override VFXTaskType taskType { get { return VFXTaskType.ParticleHexahedronOutput; } }

        public override bool supportsUV { get { return true; } }
        public override bool implementsMotionVector { get { return true; } }

        public override CullMode defaultCullMode { get { return CullMode.Back; } }

        public override IEnumerable<VFXAttributeInfo> attributes
        {
            get
            {
                yield return new VFXAttributeInfo(VFXAttribute.Position, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.Color, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.Alpha, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.Alive, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.AxisX, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.AxisY, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.AxisZ, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.AngleX, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.AngleY, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.AngleZ, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.PivotX, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.PivotY, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.PivotZ, VFXAttributeMode.Read);

                yield return new VFXAttributeInfo(VFXAttribute.Size, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.ScaleX, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.ScaleY, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.ScaleZ, VFXAttributeMode.Read);

                if (usesFlipbook)
                    yield return new VFXAttributeInfo(VFXAttribute.TexIndex, VFXAttributeMode.Read);
            }
        }

        protected override IEnumerable<VFXNamedExpression> CollectGPUExpressions(IEnumerable<VFXNamedExpression> slotExpressions)
        {
            foreach (var exp in base.CollectGPUExpressions(slotExpressions))
                yield return exp;

            yield return slotExpressions.First(o => o.name == "mainTexture");
        }

        public class InputProperties
        {
            public Texture2D mainTexture = VFXResources.defaultResources.particleTexture;
        }
    }
}
