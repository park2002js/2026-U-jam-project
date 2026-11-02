Shader "UJam/BoundaryFog"
{
    Properties
    {
        _FogColor("Fog Color", Color) = (0.55,0.65,0.67,1)
        _Density("Density", Range(0,2)) = 1.1
        _HeightFade("Height Fade", Float) = 12
        _EdgeFade("Edge Fade", Float) = 12
        _NoiseScale("Noise Scale", Float) = 0.08
        _Speed("Animation Speed", Float) = 0.15
        _Opacity("Opacity", Range(0,1)) = 1
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent+20" "RenderType"="Transparent" }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest Always
            Cull Front
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            CBUFFER_START(UnityPerMaterial)
            half4 _FogColor;
            float _Density, _HeightFade, _EdgeFade, _NoiseScale, _Speed, _Opacity;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD0; };
            Varyings Vert(Attributes v)
            {
                Varyings o;
                o.positionWS = TransformObjectToWorld(v.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(o.positionWS);
                return o;
            }
            float Noise(float3 p)
            {
                return 0.78 + 0.12 * sin(p.x + sin(p.z * 0.8)) * sin(p.z * 1.3 + p.y) + 0.1 * sin(p.x * 0.43 + p.z * 0.72);
            }
            half4 Frag(Varyings i) : SV_Target
            {
                float2 uv = GetNormalizedScreenSpaceUV(i.positionCS);
                float depth = SampleSceneDepth(uv);
                #if !UNITY_REVERSED_Z
                    depth = lerp(UNITY_NEAR_CLIP_VALUE, 1, depth);
                #endif
                float3 scene = ComputeWorldSpacePosition(uv, depth, UNITY_MATRIX_I_VP);
                float3 origin = GetCameraPositionWS();
                float3 direction = normalize(i.positionWS - origin);
                float3 ro = TransformWorldToObject(origin);
                float3 rd = mul((float3x3)unity_WorldToObject, direction);
                rd = float3(abs(rd.x) < 0.000001 ? 0.000001 : rd.x, abs(rd.y) < 0.000001 ? 0.000001 : rd.y, abs(rd.z) < 0.000001 ? 0.000001 : rd.z);
                float3 a = (-0.5 - ro) / rd;
                float3 b = (0.5 - ro) / rd;
                float3 nearT = min(a,b), farT = max(a,b);
                float entry = max(0, max(nearT.x,max(nearT.y,nearT.z)));
                float exit = min(min(farT.x,min(farT.y,farT.z)), dot(scene-origin,direction));
                float stepSize = max(0,exit-entry) / 24;
                float opticalDepth = 0;
                float3 size = float3(length(unity_ObjectToWorld._m00_m10_m20),length(unity_ObjectToWorld._m01_m11_m21),length(unity_ObjectToWorld._m02_m12_m22));
                [unroll] for (int s=0;s<24;s++)
                {
                    float3 p = origin + direction * (entry + (s+0.5) * stepSize);
                    float3 q = TransformWorldToObject(p);
                    float side = smoothstep(0,_EdgeFade,(0.5-abs(q.x))*size.x);
                    float front = smoothstep(0,_EdgeFade,(0.5-q.z)*size.z);
                    float back = smoothstep(0,_EdgeFade, (q.z+0.5)*size.z);
                    float height = smoothstep(0,_HeightFade,(0.5-q.y)*size.y);
                    float bottom = smoothstep(0,2,(q.y+0.5)*size.y);
                    opticalDepth += side*front*back*height*bottom*Noise(p*_NoiseScale + float3(_Time.y*_Speed,0,_Time.y*_Speed*0.37))*stepSize*_Density;
                }
                float alpha = (1-exp(-opticalDepth))*_Opacity;
                return half4(_FogColor.rgb,alpha);
            }
            ENDHLSL
        }
    }
}
