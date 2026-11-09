// Preserves Unity 2022.3.62f3's legacy particle blend, tint, alpha, and fog formulas.
// Source: Unity Technologies' MIT-licensed BuiltinShaders/DefaultResourcesExtra/Particle shaders.
/*
Copyright (c) 2016 Unity Technologies

Permission is hereby granted, free of charge, to any person obtaining a copy of
this software and associated documentation files (the "Software"), to deal in
the Software without restriction, including without limitation the rights to
use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies
of the Software, and to permit persons to whom the Software is furnished to do
so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/
Shader "PolygonParticles/Legacy URP"
{
    Properties
    {
        [MainTexture] _MainTex ("Particle Texture", 2D) = "white" {}
        _TintColor ("Tint Color", Color) = (0.5,0.5,0.5,0.5)
        _InvFade ("Soft Particles Factor", Range(0.01,3.0)) = 1
        [HideInInspector] _LegacyMode ("Legacy Mode", Float) = 2
        [Toggle(_SOFTPARTICLES_ON)] _SoftParticlesEnabled ("Soft Particles", Float) = 0
        [HideInInspector] _SrcBlend ("Source Blend", Float) = 1
        [HideInInspector] _DstBlend ("Destination Blend", Float) = 6
        [HideInInspector] _ColorMask ("Color Mask", Float) = 14
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "PreviewType"="Plane" }
        Pass
        {
            Tags { "LightMode"="UniversalForwardOnly" }
            Blend [_SrcBlend] [_DstBlend]
            ColorMask [_ColorMask]
            Cull Off
            ZWrite Off

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #pragma shader_feature_local _SOFTPARTICLES_ON
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #if defined(_SOFTPARTICLES_ON)
                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #endif

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4 _TintColor;
                float _InvFade;
                float _LegacyMode;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
                half fogFactor : TEXCOORD1;
                #if defined(_SOFTPARTICLES_ON)
                    float eyeDepth : TEXCOORD2;
                #endif
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                VertexPositionInputs position = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = position.positionCS;
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.color = input.color;
                output.fogFactor = ComputeFogFactor(position.positionCS.z);
                #if defined(_SOFTPARTICLES_ON)
                    output.eyeDepth = -position.positionVS.z;
                #endif
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                #if defined(_SOFTPARTICLES_ON)
                    float depth = SampleSceneDepth(GetNormalizedScreenSpaceUV(input.positionCS));
                    float sceneDepth = unity_OrthoParams.w == 0
                        ? LinearEyeDepth(depth, _ZBufferParams)
                        : LinearDepthToEyeDepth(depth);
                    input.color.a *= saturate(_InvFade * (sceneDepth - input.eyeDepth));
                #endif

                half4 color = input.color * SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                half3 fogColor = 0;
                if (_LegacyMode < 1.5)
                {
                    color *= 2 * _TintColor;
                    color.a = saturate(color.a);
                    if (_LegacyMode < 0.5)
                        fogColor = unity_FogColor.rgb;
                }
                else if (_LegacyMode < 2.5)
                {
                    color.rgb *= color.a;
                }
                else if (_LegacyMode < 3.5)
                {
                    color = lerp(half4(1,1,1,1), color, color.a);
                    fogColor = 1;
                }
                else
                {
                    color.rgb *= 2;
                    color = lerp(half4(0.5,0.5,0.5,0.5), color, color.a);
                    fogColor = 0.5;
                }
                color.rgb = MixFogColor(color.rgb, fogColor, input.fogFactor);
                return color;
            }
            ENDHLSL
        }
    }
}
