// Qorxulu yazı (TextMesh üçün): tünd qan-qırmızı, kənarları cızılmış/yeyilmiş, yüngül titrəyən.
// İşıqdan və dumandan asılı deyil ki, qaranlıq otaqda və kamerada görünsün. _Fade ilə yavaş-yavaş görünür.
Shader "Custom/ScaryText"
{
    Properties
    {
        _MainTex ("Font Texture", 2D) = "white" {}
        _Color ("Color", Color) = (0.75, 0.04, 0.03, 1)
        _Rough ("Roughness", Range(0, 1)) = 0.6
        _Fade ("Fade", Range(0, 1)) = 1
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent" "RenderType" = "Transparent" }
        ZWrite Off
        Cull Off
        Offset -1, -1 // döşəmə ilə titrəşməsin
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4 _Color;
                float _Rough, _Fade;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; float3 positionWS : TEXCOORD1; };

            float Hash(float2 p) { return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453); }
            float Noise(float2 p)
            {
                float2 i = floor(p), f = frac(p);
                f = f * f * (3 - 2 * f);
                return lerp(lerp(Hash(i), Hash(i + float2(1, 0)), f.x), lerp(Hash(i + float2(0, 1)), Hash(i + 1), f.x), f.y);
            }

            Varyings Vert(Attributes i)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(i.positionOS.xyz);
                o.positionWS = TransformObjectToWorld(i.positionOS.xyz);
                o.uv = i.uv;
                return o;
            }

            half4 Frag(Varyings i) : SV_Target
            {
                float2 p = i.positionWS.xz;
                // titrək əl yazısı: atlasda kiçik sürüşmə
                float2 wobble = (float2(Noise(p * 3), Noise(p * 3 + 7)) - 0.5) * 0.004 * _Rough;
                float a = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv + wobble).a;
                // yeyilmiş, kələ-kötür kənarlar
                float grunge = Noise(p * 18) * 0.6 + Noise(p * 45) * 0.4;
                a = saturate((a - 0.5 + (grunge - 0.5) * _Rough) * 6);
                float flicker = 0.85 + 0.15 * sin(_Time.y * 17) * sin(_Time.y * 5.3);
                half3 c = _Color.rgb * (0.7 + 0.6 * grunge) * flicker;
                return half4(c, a * _Fade * _Color.a);
            }
            ENDHLSL
        }
    }
}
