Shader "UJam/PortalInterior"
{
    Properties
    {
        _CoreColor("Core Color", Color) = (0.028,0.015,0.065,1)
        _RimColor("Rim Color", Color) = (0.11,0.055,0.22,1)
        _Radius("Aperture Radius", Range(0.3,0.5)) = 0.48
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        CBUFFER_START(UnityPerMaterial)
        half4 _CoreColor, _RimColor;
        float _Radius;
        CBUFFER_END
        struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
        struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };
        Varyings Vert(Attributes v)
        {
            Varyings o;
            o.positionCS=TransformObjectToHClip(v.positionOS.xyz);
            o.uv=v.uv;
            return o;
        }
        ENDHLSL
        Pass
        {
            Name "PortalInterior"
            Tags { "LightMode"="UniversalForwardOnly" }
            Cull Off
            ZWrite On
            ZTest LEqual
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            half4 Frag(Varyings i) : SV_Target
            {
                float radius=length(i.uv-0.5);
                clip(_Radius-radius);
                float rim=smoothstep(0.18,_Radius,radius);
                return half4(lerp(_CoreColor.rgb,_RimColor.rgb,rim),1);
            }
            ENDHLSL
        }
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            Cull Off
            ZWrite On
            ColorMask R
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragDepth
            half4 FragDepth(Varyings i) : SV_Target
            {
                clip(_Radius-length(i.uv-0.5));
                return 0;
            }
            ENDHLSL
        }
    }
}
