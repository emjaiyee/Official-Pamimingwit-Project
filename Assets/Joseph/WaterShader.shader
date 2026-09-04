Shader "Custom/NewUnlitUniversalRenderPipelineShader"
{
    Properties
    {
        [MainColor] _BaseColor("Base Color (Transparency in Alpha)", Color) = (1, 1, 1, 0.5)
        [MainTexture] _BaseMap("Base Map", 2D) = "white" {}

        [Header(Depth Gradient)]
        _SurfaceLevel("Surface Y Level", Float) = 0.0
        _DepthVerticalScale("Depth Distance", Float) = 5.0
        _ShallowColor("Shallow Color", Color) = (1, 1, 1, 1)
        _DeepColor("Deep Color", Color) = (0.2, 0.4, 0.8, 1)
        _RefractionStrength("Refraction Strength", Range(0, 0.2)) = 0.02
        
        [Header(Caustics)]
        _CausticMap("Caustic Texture", 2D) = "white" {}
        _CausticColor("Caustic Color", Color) = (1, 1, 1, 1)
        _CausticScale("Caustic Scale", Float) = 0.5
        _CausticSwaySpeed("Caustic Sway Speed", Float) = 1.0
        _CausticSwayMagnitude("Caustic Sway Magnitude", Float) = 0.05
        _CausticStrength("Caustic Strength", Float) = 0.5
        _CausticSharpness("Caustic Sharpness", Range(1, 20)) = 3.0
        _CausticPixelation("Caustic Pixelation", Float) = 64.0

        [Header(Shadows)]
        _ShadowColor("Shadow Color Tint", Color) = (0.1, 0.2, 0.3, 1)
        _ShadowStrength("Shadow Strength", Range(0, 1)) = 0.4
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off

        Pass
        {
            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float4 uv_wsXY : TEXCOORD0; // xy = Base UV, zw = positionWS.xy
                float  positionWS_Y : TEXCOORD1;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            TEXTURE2D(_CausticMap);
            SAMPLER(sampler_CausticMap);

            // SRP Batcher Memory Alignment (Grouped by type to enforce 16-byte boundaries)
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _CausticMap_ST;
                half4 _BaseColor;
                half4 _ShallowColor;
                half4 _DeepColor;
                half4 _CausticColor;
                half4 _ShadowColor;
                float _SurfaceLevel;
                float _DepthVerticalScale;
                float _RefractionStrength;
                float _CausticScale;
                float _CausticSwaySpeed;
                float _CausticSwayMagnitude;
                float _CausticStrength;
                float _CausticSharpness;
                float _CausticPixelation;
                float _ShadowStrength;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                float3 positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                
                OUT.uv_wsXY.xy = TRANSFORM_TEX(IN.uv, _BaseMap);
                OUT.uv_wsXY.zw = positionWS.xy;
                OUT.positionWS_Y = positionWS.y;
                
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // 0. Depth Gradient Calculation
                float depthDiff = _SurfaceLevel - IN.positionWS_Y;
                float normalizedDist = saturate(depthDiff / max(0.001, abs(_DepthVerticalScale)));
                float depthFactor = (_DepthVerticalScale < 0.0) ? (1.0 - normalizedDist) : normalizedDist;
                half4 depthGradient = lerp(_ShallowColor, _DeepColor, (half)depthFactor);

                // 1. Light Attenuation
                half causticFade = (half)saturate(1.0 - normalizedDist);

                // 2. Pixelated Top-Down Projection
                float2 waterUV = floor(IN.uv_wsXY.zw * _CausticPixelation) / _CausticPixelation;

                // 3. Optimized Trigonometric Sway (Replaces Procedural Perlin Loop)
                float time = _Time.y * _CausticSwaySpeed;
                float2 sway = float2(
                    sin(waterUV.y * 3.0 + time),
                    cos(waterUV.x * 3.0 + time)
                ) * _CausticSwayMagnitude;

                // 4. Dual Caustic Texture Sampling
                float2 causticUV1 = waterUV * (_CausticScale * _CausticMap_ST.xy) + sway;
                float2 causticUV2 = waterUV * (_CausticScale * 0.8 * _CausticMap_ST.xy) - (sway * 1.2);

                half3 caustic1 = SAMPLE_TEXTURE2D(_CausticMap, sampler_CausticMap, causticUV1).rgb;
                half3 caustic2 = SAMPLE_TEXTURE2D(_CausticMap, sampler_CausticMap, causticUV2).rgb;
                half3 finalCaustic = min(caustic1, caustic2);

                // 5. Surface Shadows
                half shadowFactor = saturate(1.0 - finalCaustic.r) * (half)_ShadowStrength * causticFade;

                // 6. Dynamic Sharpness & Webbing
                half dynamicSharpness = lerp(1.0h, (half)_CausticSharpness, causticFade);
                finalCaustic = pow(max(finalCaustic, 0.0h), dynamicSharpness) * ((half)_CausticStrength * causticFade);

                // 7. Base Refraction Sampling
                float2 refractedUV = IN.uv_wsXY.xy + (sway * _RefractionStrength);
                half4 texColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, refractedUV) * _BaseColor;

                // 8. Output Pass Combination
                half4 finalColor = texColor * depthGradient;
                finalColor.rgb = lerp(finalColor.rgb, finalColor.rgb * _ShadowColor.rgb, shadowFactor);
                finalColor.rgb += finalCaustic * _CausticColor.rgb;

                return finalColor;
            }
            ENDHLSL
        }
    }
}