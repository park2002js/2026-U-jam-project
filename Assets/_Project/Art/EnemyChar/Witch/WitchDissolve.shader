Shader "UJam/Enemy/Witch Dissolve"
{
    Properties
    {
        [MainTexture] _BaseMap("Palette", 2D) = "white" {}
        [MainColor] _BaseColor("Color", Color) = (1,1,1,1)
        _Dissolve("Dissolve", Range(0,1)) = 0
        _DissolveOrigin("Dissolve Origin", Vector) = (0,0,0,0)
        _DissolveHeight("Dissolve Height", Float) = 4
        _NoiseScale("Ash Noise Scale", Float) = 8
        _EdgeColor("Ash Edge Color", Color) = (.55,.2,.7,1)
        _EdgeWidth("Ash Edge Width", Range(.001,.15)) = .045
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="AlphaTest" "RenderType"="TransparentCutout" }
        Cull Back
        ZWrite On

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        TEXTURE2D(_BaseMap);
        SAMPLER(sampler_BaseMap);
        CBUFFER_START(UnityPerMaterial)
        float4 _BaseMap_ST;
        half4 _BaseColor;
        half4 _EdgeColor;
        float4 _DissolveOrigin;
        float _Dissolve;
        float _DissolveHeight;
        float _NoiseScale;
        float _EdgeWidth;
        CBUFFER_END

        struct Attributes
        {
            float4 positionOS : POSITION;
            float3 normalOS : NORMAL;
            float2 uv : TEXCOORD0;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };

        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            float3 positionWS : TEXCOORD0;
            half3 normalWS : TEXCOORD1;
            float2 uv : TEXCOORD2;
            half fog : TEXCOORD3;
            UNITY_VERTEX_INPUT_INSTANCE_ID
            UNITY_VERTEX_OUTPUT_STEREO
        };

        Varyings Vert(Attributes input)
        {
            Varyings output = (Varyings)0;
            UNITY_SETUP_INSTANCE_ID(input);
            UNITY_TRANSFER_INSTANCE_ID(input, output);
            UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
            output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
            output.positionCS = TransformWorldToHClip(output.positionWS);
            output.normalWS = TransformObjectToWorldNormal(input.normalOS);
            output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
            output.fog = ComputeFogFactor(output.positionCS.z);
            return output;
        }

        float Hash(float3 value)
        {
            return frac(sin(dot(value, float3(127.1,311.7,74.7))) * 43758.5453);
        }

        float Noise(float3 position)
        {
            float3 cell = floor(position);
            float3 fraction = frac(position);
            fraction = fraction * fraction * (3 - 2 * fraction);
            return lerp(
                lerp(lerp(Hash(cell), Hash(cell + float3(1,0,0)), fraction.x),
                     lerp(Hash(cell + float3(0,1,0)), Hash(cell + float3(1,1,0)), fraction.x), fraction.y),
                lerp(lerp(Hash(cell + float3(0,0,1)), Hash(cell + float3(1,0,1)), fraction.x),
                     lerp(Hash(cell + float3(0,1,1)), Hash(cell + float3(1,1,1)), fraction.x), fraction.y), fraction.z);
        }

        float ClipDissolve(float3 positionWS)
        {
            float3 relative = positionWS - _DissolveOrigin.xyz;
            float height = saturate(relative.y / max(.01, _DissolveHeight));
            float noise = Noise(relative * _NoiseScale) * .7 + Noise(relative * _NoiseScale * 2.13) * .3;
            float field = height * .65 + noise * .35;
            float remaining = field - lerp(-.05, 1.05, saturate(_Dissolve));
            clip(remaining);
            return remaining;
        }

        half4 FragDepth(Varyings input) : SV_Target
        {
            UNITY_SETUP_INSTANCE_ID(input);
            ClipDissolve(input.positionWS);
            return 0;
        }
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile _ _ADDITIONAL_LIGHTS

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float remaining = ClipDissolve(input.positionWS);
                half3 normal = normalize(input.normalWS);
                half3 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).rgb * _BaseColor.rgb;
                Light main = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                half3 lighting = SampleSH(normal) + main.color * saturate(dot(normal, main.direction)) *
                    main.shadowAttenuation * main.distanceAttenuation;
                #if defined(_ADDITIONAL_LIGHTS)
                uint lightCount = GetAdditionalLightsCount();
                for (uint lightIndex = 0; lightIndex < lightCount; lightIndex++)
                {
                    Light light = GetAdditionalLight(lightIndex, input.positionWS);
                    lighting += light.color * saturate(dot(normal, light.direction)) * light.distanceAttenuation * light.shadowAttenuation;
                }
                #endif
                half edge = _Dissolve > 0 && _Dissolve < 1 ? 1 - smoothstep(0, _EdgeWidth, remaining) : 0;
                half3 color = albedo * lighting + _EdgeColor.rgb * edge;
                return half4(MixFog(color, input.fog), 1);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ColorMask 0
            ZTest LEqual
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex VertShadow
            #pragma fragment FragDepth
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            float3 _LightDirection;
            float3 _LightPosition;
            Varyings VertShadow(Attributes input)
            {
                Varyings output = Vert(input);
                #if defined(_CASTING_PUNCTUAL_LIGHT_SHADOW)
                float3 lightDirection = normalize(_LightPosition - output.positionWS);
                #else
                float3 lightDirection = _LightDirection;
                #endif
                output.positionCS = TransformWorldToHClip(ApplyShadowBias(output.positionWS, output.normalWS, lightDirection));
                output.positionCS = ApplyShadowClamping(output.positionCS);
                return output;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            ColorMask 0
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment FragDepth
            #pragma multi_compile_instancing
            ENDHLSL
        }
    }
    FallBack Off
}
