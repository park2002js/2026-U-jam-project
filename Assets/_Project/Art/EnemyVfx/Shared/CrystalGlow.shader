Shader "UJam/Enemy/Crystal Glow"
{
    Properties
    {
        [HDR] _GlowColor("Crystal Glow", Color) = (.65,.2,1,1)
        _Billboard("Billboard Halo", Float) = 0
        _Visibility("Source Visibility", Range(0,1)) = 1
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent" }
        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            Blend One One
            ZWrite Off
            ZTest LEqual
            Cull Off
            Offset -1, -1
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _GlowColor;
                float _Billboard;
                float _Visibility;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.uv = input.uv;
                if (_Billboard > .5)
                {
                    float3 centerWS = TransformObjectToWorld(float3(0,0,0));
                    float3 centerVS = TransformWorldToView(centerWS);
                    float2 scale = float2(length(unity_ObjectToWorld._m00_m10_m20),
                        length(unity_ObjectToWorld._m01_m11_m21));
                    centerVS.xy += input.positionOS.xy * scale;
                    output.positionCS = mul(UNITY_MATRIX_P, float4(centerVS, 1));
                }
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half intensity;
                if (_Billboard > .5)
                {
                    float2 p = input.uv * 2 - 1;
                    half halo = pow(saturate(1 - dot(p,p)), 2) * .30;
                    float2 glint = p - float2(-.18,.28);
                    half horizontal = exp2(-abs(glint.x) * 16 - abs(glint.y) * 170);
                    half vertical = exp2(-abs(glint.x) * 170 - abs(glint.y) * 16);
                    half pulse = .9 + .1 * sin(_Time.y * 2.1);
                    intensity = (halo + (horizontal + vertical) * .42) * pulse;
                }
                else
                {
                    half3 view = normalize(GetWorldSpaceViewDir(input.positionWS));
                    half rim = pow(1 - saturate(abs(dot(normalize(input.normalWS), view))), 2.5);
                    intensity = .018 + rim * .5;
                }
                return half4(_GlowColor.rgb * intensity * _Visibility, 0);
            }
            ENDHLSL
        }
    }
}
