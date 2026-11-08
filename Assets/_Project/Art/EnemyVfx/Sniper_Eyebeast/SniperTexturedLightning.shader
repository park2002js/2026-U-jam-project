Shader "UJam/Enemy/Sniper Textured Lightning"
{
    Properties
    {
        [MainTexture] _BaseMap("Branched Lightning RGBA", 2D) = "black" {}
        [MainColor] _BaseColor("Filament Tint", Color) = (1,1,1,1)
        _Intensity("Energy", Float) = 1.2
        _Pulse("Flash Intensity", Range(0,1)) = 1
        _EdgeFade("Endpoint Fade", Range(0.001,0.1)) = 0.03
        _Distortion("Filament Motion", Range(0,0.02)) = 0.004
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent" }
        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            Blend One One
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                float _Intensity, _Pulse, _EdgeFade, _Distortion;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv * _BaseMap_ST.xy + _BaseMap_ST.zw;
                output.color = input.color;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv;
                uv.y += sin(uv.x * 24.0 + _Time.y * 20.0) * _Distortion * sin(uv.x * PI);
                half4 source = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv);
                half ends = smoothstep(0.0, _EdgeFade, input.uv.x) * smoothstep(0.0, _EdgeFade, 1.0 - input.uv.x);
                half alpha = source.a * input.color.a * _BaseColor.a * ends * _Pulse;
                half energy = _Intensity * (0.97 + 0.03 * sin(_Time.y * 28.0));
                // Premultiply source alpha once; additive blending never fills transparent filament gaps.
                return half4(source.rgb * input.color.rgb * _BaseColor.rgb * alpha * energy, 0.0);
            }
            ENDHLSL
        }
    }
}
