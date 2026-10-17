// Anomaliya ətrafında bulanıq, titrəşən hava (isti hava dalğası kimi).
// Arxadakı görüntünü (_CameraOpaqueTexture) titrədərək və bulandıraraq çəkir; kənarlara doğru yumşaq sönür.
// URP asset-də "Opaque Texture" açıq olmalıdır.
Shader "Custom/AnomalyDistortion"
{
    Properties
    {
        _Strength ("Distortion", Range(0, 0.06)) = 0.02
        _Blur ("Blur", Range(0, 0.03)) = 0.008
        _Speed ("Speed", Float) = 4
        _Tint ("Tint", Color) = (0.85, 0.95, 1, 1)
        _Fade ("Fade", Range(0, 1)) = 1
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent" "RenderType" = "Transparent" }
        ZWrite Off
        Cull Back
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _Strength, _Blur, _Speed, _Fade;
                half4 _Tint;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 screenPos : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
            };

            Varyings Vert(Attributes i)
            {
                Varyings o;
                VertexPositionInputs p = GetVertexPositionInputs(i.positionOS.xyz);
                o.positionCS = p.positionCS;
                o.screenPos = ComputeScreenPos(p.positionCS);
                o.normalWS = TransformObjectToWorldNormal(i.normalOS);
                o.positionWS = p.positionWS;
                return o;
            }

            half4 Frag(Varyings i) : SV_Target
            {
                float2 uv = i.screenPos.xy / i.screenPos.w;
                float3 viewDir = normalize(GetWorldSpaceViewDir(i.positionWS));
                float rim = saturate(dot(normalize(i.normalWS), viewDir)); // mərkəzdə 1, kənarda 0
                float t = _Time.y * _Speed;
                float3 w = i.positionWS * 10;
                float2 wobble = float2(sin(w.y + t) + sin(w.x * 0.9 - t * 1.3), cos(w.x * 1.1 + t * 0.8) + cos(w.z + t)) * 0.5;
                float2 offset = wobble * _Strength * rim * _Fade;

                half3 c = 0;
                [unroll] for (int k = 0; k < 8; k++)
                {
                    float a = k * 0.785398;
                    c += SampleSceneColor(uv + offset + float2(cos(a), sin(a)) * _Blur * rim * _Fade);
                }
                c = c / 8 * lerp(1, _Tint.rgb, _Fade);
                return half4(c, smoothstep(0, 0.6, rim) * _Fade);
            }
            ENDHLSL
        }
    }
}
