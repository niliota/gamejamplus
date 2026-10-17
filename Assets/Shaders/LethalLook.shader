// Lethal Company üslubunda tam ekran effekti (URP Full Screen Pass Renderer Feature üçün).
// Aşağı rezolyusiya (iri piksel) + dərinlikdən qara kontur + az rəng (posterize) + Bayer dither + dən + vinyet.
Shader "Custom/LethalLook"
{
    Properties
    {
        _PixelHeight ("Pixel Height (şaquli piksel sayı)", Float) = 300
        _Levels ("Color Levels (hər kanalda)", Float) = 20
        _DitherStrength ("Dither", Range(0, 1)) = 0.8
        _OutlineThreshold ("Outline Threshold", Range(0.001, 0.5)) = 0.06
        _OutlineStrength ("Outline Strength", Range(0, 1)) = 0.9
        _Saturation ("Saturation", Range(0, 2)) = 0.8
        _Contrast ("Contrast", Range(0.5, 2)) = 1.15
        _Grain ("Grain", Range(0, 0.3)) = 0.06
        _Vignette ("Vignette", Range(0, 1)) = 0.5
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off ZTest Always Cull Off Blend Off

        Pass
        {
            Name "LethalLook"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            float _PixelHeight, _Levels, _DitherStrength, _OutlineThreshold, _OutlineStrength;
            float _Saturation, _Contrast, _Grain, _Vignette;
            // Qlobal (Properties-də yoxdur): 0 = tam effekt, 1 = piksel/dither/kontur/dən/vinyet yox (məs. CCTV monitoruna baxanda).
            // Kontrast və doyğunluq dəyişmir ki, görüntü qaralmasın.
            float _LethalLookWeaken;

            static const float Bayer[16] = { 0, 8, 2, 10, 12, 4, 14, 6, 3, 11, 1, 9, 15, 7, 13, 5 };

            // 1/dərinlik ekranda müstəvilər üçün xəttidir: düz səthdə Laplacian ~0, kənarlarda böyükdür.
            float InvDepth(float2 uv) { return 1.0 / LinearEyeDepth(SampleSceneDepth(uv), _ZBufferParams); }

            half4 Frag(Varyings input) : SV_Target
            {
                float k = 1 - saturate(_LethalLookWeaken); // effektin gücü
                float pixelHeight = lerp(_ScreenParams.y, _PixelHeight, k);
                float levels = lerp(64, _Levels, k);

                float2 res = float2(pixelHeight * _ScreenParams.x / _ScreenParams.y, pixelHeight);
                float2 cell = floor(input.texcoord * res);
                float2 uv = (cell + 0.5) / res;
                float2 px = 1.0 / res;

                half3 c = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_PointClamp, uv).rgb;

                // Kontur
                float w = InvDepth(uv);
                float lap = InvDepth(uv + float2(px.x, 0)) + InvDepth(uv - float2(px.x, 0))
                          + InvDepth(uv + float2(0, px.y)) + InvDepth(uv - float2(0, px.y)) - 4 * w;
                float edge = saturate((abs(lap) / max(w, 1e-5) - _OutlineThreshold) * 8);
                c *= 1 - edge * _OutlineStrength * k;

                // Rəng: sRGB-də işlənir ki, qaranlıq tonlar düzgün pillələnsin
                c = LinearToSRGB(saturate(c));
                float lum = dot(c, float3(0.299, 0.587, 0.114));
                c = lerp(lum.xxx, c, _Saturation);
                c = (c - 0.5) * _Contrast + 0.5;

                float noise = frac(sin(dot(cell + floor(_Time.y * 24) * 17.0, float2(12.9898, 78.233))) * 43758.5453) - 0.5;
                c += noise * _Grain * k;

                uint2 p = (uint2)cell;
                float dither = Bayer[(p.x & 3) + (p.y & 3) * 4] / 16.0 - 0.5;
                c = floor(saturate(c) * levels + 0.5 + dither * _DitherStrength * k) / levels;

                float2 v = input.texcoord - 0.5;
                c *= saturate(1 - dot(v, v) * _Vignette * k * 2.5);

                return half4(SRGBToLinear(saturate(c)), 1);
            }
            ENDHLSL
        }
    }
}
