Shader "UJam/Enemy/Sniper Current"
{
    Properties
    {
        [HDR] _BaseColor("Current Color", Color) = (.85,.6,1,1)
        _Intensity("Energy", Float) = 1
        _Pulse("Flash Intensity", Range(0,1)) = 1
        [Enum(Current,0,TargetGlow,1,PolyCore,2)] _Shape("Geometry Profile", Float) = 0
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

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                float _Intensity, _Pulse, _Shape;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
                half3 normalWS : TEXCOORD1;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.color = input.color;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                if (_Shape > .5 && _Shape < 1.5)
                {
                    float3 centerVS = TransformWorldToView(TransformObjectToWorld(float3(0,0,0)));
                    float2 scale = float2(length(unity_ObjectToWorld._m00_m10_m20), length(unity_ObjectToWorld._m01_m11_m21));
                    centerVS.xy += input.positionOS.xy * scale;
                    output.positionCS = mul(UNITY_MATRIX_P, float4(centerVS, 1));
                }
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half profile;
                half3 vertexColor = 1;
                if (_Shape < .5)
                {
                    float across = abs(input.uv.y * 2 - 1);
                    // Each ribbon contains only one narrow current and its feathered local glow.
                    profile = exp2(-across * across * 55) + exp2(-across * across * 8) * .14;
                    vertexColor = input.color.rgb * input.color.a;
                }
                else if (_Shape < 1.5)
                {
                    float2 p = input.uv * 2 - 1;
                    float radiusSquared = dot(p, p);
                    profile = pow(saturate(1 - radiusSquared), 3) * .65 + exp2(-radiusSquared * 35) * .35;
                }
                else
                {
                    profile = .55 + .45 * abs(dot(normalize(input.normalWS), normalize(float3(.4,.8,.3))));
                }
                return half4(_BaseColor.rgb * vertexColor * profile * _Intensity * _Pulse * _BaseColor.a, 0);
            }
            ENDHLSL
        }
    }
}
