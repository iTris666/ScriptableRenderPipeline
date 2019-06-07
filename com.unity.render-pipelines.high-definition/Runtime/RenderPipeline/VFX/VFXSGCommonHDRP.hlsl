#include "Packages/com.unity.shadergraph/ShaderGraphLibrary/Functions.hlsl"

ByteAddressBuffer attributeBuffer;


struct VertInputForSG
{
    float4 posCS; // In case depth offset is use, positionRWS.w is equal to depth offset
    float3 posWD; // Relative camera space position
    float2 uv0;
    float2 uv1;
    float2 uv2;
    float2 uv3;
    float4 color; // vertex color

    float3 ObjectSpaceNormal;
    float3 ViewSpaceNormal;
    float3 WorldSpaceNormal;
    float3 TangentSpaceNormal;

    float3 ObjectSpaceTangent;
    float3 ViewSpaceTangent;
    float3 WorldSpaceTangent;
    float3 TangentSpaceTangent;

    float3 ObjectSpaceBiTangent;
    float3 ViewSpaceBiTangent;
    float3 WorldSpaceBiTangent;
    float3 TangentSpaceBiTangent;

    float3 ObjectSpaceViewDirection;
    float3 ViewSpaceViewDirection;
    float3 WorldSpaceViewDirection;
    float3 TangentSpaceViewDirection;

    float3 ObjectSpacePosition;
    float3 ViewSpacePosition;
    float3 WorldSpacePosition;
    float3 TangentSpacePosition;
};

VertInputForSG InitializeVertStructs(AttributesMesh input, float4x4 elementToVFX, out float3 particlePos)
{
    VertInputForSG fisg = (VertInputForSG)0;

#ifdef ATTRIBUTES_NEED_TEXCOORD0
    fisg.uv0 = input.uv0;
#endif
#ifdef ATTRIBUTES_NEED_TEXCOORD1
    fisg.uv1 = input.uv1;
#endif
#ifdef ATTRIBUTES_NEED_TEXCOORD2
    fisg.uv2 = input.uv2;
#endif
#ifdef ATTRIBUTES_NEED_TEXCOORD3
    fisg.uv2 = input.uv2;
#endif
#ifdef ATTRIBUTES_NEED_COLOR
    fisg.color = input.color;
#endif
#ifdef ATTRIBUTES_NEED_NORMAL
    float3 particleSpaceNormal = mul(elementToVFX, float4(input.normalOS, 0)).xyz;

    fisg.WorldSpaceNormal = TransformObjectToWorldNormal(particleSpaceNormal);
    fisg.ObjectSpaceNormal = input.normalOS;
    fisg.ViewSpaceNormal = mul(fisg.WorldSpaceNormal, (float3x3) UNITY_MATRIX_I_V);         // transposed multiplication by inverse matrix to handle normal scale
    fisg.TangentSpaceNormal = float3(0.0f, 0.0f, 1.0f);
#endif

#ifdef ATTRIBUTES_NEED_TANGENT
    float3 particleSpaceTangent = mul(elementToVFX, float4(input.tangentOS.xyz, 0)).xyz;

    fisg.WorldSpaceTangent = TransformObjectToWorldDir(particleSpaceTangent);
    fisg.ObjectSpaceTangent = input.tangentOS.xyz;
    fisg.ViewSpaceTangent = TransformWorldToViewDir(fisg.WorldSpaceTangent);
    fisg.TangentSpaceTangent = float3(1.0f, 0.0f, 0.0f);
#endif


#if defined(ATTRIBUTES_NEED_TANGENT) && defined(ATTRIBUTES_NEED_NORMAL)
    float3 objectSpaceBiTangent = cross(particleSpaceNormal, particleSpaceTangent);
    float3 particleSpaceBiTangent = mul(elementToVFX, float4(objectSpaceBiTangent, 0)).xyz;

    fisg.WorldSpaceBiTangent = TransformObjectToWorldDir(particleSpaceBiTangent);
    fisg.ObjectSpaceBiTangent = objectSpaceBiTangent;
    fisg.ViewSpaceBiTangent = TransformWorldToViewDir(fisg.WorldSpaceBiTangent);
    fisg.TangentSpaceBiTangent = float3(0.0f, 1.0f, 0.0f);

#endif

    particlePos = mul(elementToVFX, float4(input.positionOS, 1)).xyz;

    fisg.WorldSpacePosition = TransformObjectToWorld(particlePos);
    fisg.ObjectSpacePosition = input.positionOS;
    fisg.ViewSpacePosition = TransformWorldToView(particlePos);
    fisg.TangentSpacePosition = float3(0.0f, 0.0f, 0.0f);

    fisg.WorldSpaceViewDirection = GetWorldSpaceNormalizeViewDir(fisg.WorldSpacePosition);
    fisg.ObjectSpaceViewDirection = TransformWorldToObjectDir(fisg.WorldSpaceViewDirection);
    fisg.ViewSpaceViewDirection = TransformWorldToViewDir(fisg.WorldSpaceViewDirection);
    float3x3 tangentSpaceTransform = float3x3(fisg.WorldSpaceTangent, fisg.WorldSpaceBiTangent, fisg.WorldSpaceNormal);
    fisg.TangentSpaceViewDirection = mul(tangentSpaceTransform, fisg.WorldSpaceViewDirection);

    return fisg;
}
struct FragInputForSG
{
    float4 posCS; // In case depth offset is use, positionRWS.w is equal to depth offset
    float3 posWD; // Relative camera space position
    float4 uv0;
    float4 uv1;
    float4 uv2;
    float4 uv3;
    float4 VertexColor; // vertex color

    float3 ObjectSpaceNormal;
    float3 ViewSpaceNormal;
    float3 WorldSpaceNormal;
    float3 TangentSpaceNormal;

    float3 ObjectSpaceTangent;
    float3 ViewSpaceTangent;
    float3 WorldSpaceTangent;
    float3 TangentSpaceTangent;

    float3 ObjectSpaceBiTangent;
    float3 ViewSpaceBiTangent;
    float3 WorldSpaceBiTangent;
    float3 TangentSpaceBiTangent;

    float3 ObjectSpaceViewDirection;
    float3 ViewSpaceViewDirection;
    float3 WorldSpaceViewDirection;
    float3 TangentSpaceViewDirection;

    float3 ObjectSpacePosition;
    float3 ViewSpacePosition;
    float3 WorldSpacePosition;
    float3 TangentSpacePosition;
};

#define VFX_VARYING_PS_INPUTS FragInputForSG
#define VFX_VARYING_POSCS posCS
#include "Packages/com.unity.visualeffectgraph/Shaders/RenderPipeline/HDRP/VFXCommon.cginc"
#include "Packages/com.unity.visualeffectgraph/Shaders/VFXCommon.cginc"

FragInputForSG InitializeFragStructsHDRP(inout FragInputs input, PositionInputs posInput, float3 V, out SurfaceData surfaceData, out BuiltinData builtinData)
{
    FragInputForSG fisg;
    fisg.posCS = input.positionSS;
    fisg.posWD = input.positionRWS;
    fisg.uv0 = input.texCoord0;
    fisg.uv1 = input.texCoord1;
    fisg.uv2 = input.texCoord2;
    fisg.uv3 = input.texCoord3;
    fisg.VertexColor = input.color;

    fisg.WorldSpaceNormal = normalize(input.worldToTangent[2].xyz);
    fisg.ObjectSpaceNormal = mul(fisg.WorldSpaceNormal, (float3x3) UNITY_MATRIX_M);           // transposed multiplication by inverse matrix to handle normal scale
    fisg.ViewSpaceNormal = mul(fisg.WorldSpaceNormal, (float3x3) UNITY_MATRIX_I_V);         // transposed multiplication by inverse matrix to handle normal scale
    fisg.TangentSpaceNormal = float3(0.0f, 0.0f, 1.0f);
    fisg.WorldSpaceTangent = input.worldToTangent[0].xyz;
    fisg.ObjectSpaceTangent = TransformWorldToObjectDir(fisg.WorldSpaceTangent);
    fisg.ViewSpaceTangent = TransformWorldToViewDir(fisg.WorldSpaceTangent);
    fisg.TangentSpaceTangent = float3(1.0f, 0.0f, 0.0f);
    fisg.WorldSpaceBiTangent = input.worldToTangent[1].xyz;
    fisg.ObjectSpaceBiTangent = TransformWorldToObjectDir(fisg.WorldSpaceBiTangent);
    fisg.ViewSpaceBiTangent = TransformWorldToViewDir(fisg.WorldSpaceBiTangent);
    fisg.TangentSpaceBiTangent = float3(0.0f, 1.0f, 0.0f);
    fisg.WorldSpaceViewDirection = normalize(V);
    fisg.ObjectSpaceViewDirection = TransformWorldToObjectDir(fisg.WorldSpaceViewDirection);
    fisg.ViewSpaceViewDirection = TransformWorldToViewDir(fisg.WorldSpaceViewDirection);
    float3x3 tangentSpaceTransform = float3x3(fisg.WorldSpaceTangent, fisg.WorldSpaceBiTangent, fisg.WorldSpaceNormal);
    fisg.TangentSpaceViewDirection = mul(tangentSpaceTransform, fisg.WorldSpaceViewDirection);
    fisg.WorldSpacePosition = GetAbsolutePositionWS(input.positionRWS);
    fisg.ObjectSpacePosition = TransformWorldToObject(input.positionRWS);
    fisg.ViewSpacePosition = TransformWorldToView(input.positionRWS);
    fisg.TangentSpacePosition = float3(0.0f, 0.0f, 0.0f);
    fisg.VertexColor = input.color;

    surfaceData = (SurfaceData)0;
    builtinData = (BuiltinData)0;

    //Setup default value in case sg does not set them


    return fisg;
}
