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
        _NoiseScale("Noise Scale", Float) = 3.0
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
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            TEXTURE2D(_CausticMap);
            SAMPLER(sampler_CausticMap);

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                float4 _BaseMap_ST;
                float _SurfaceLevel;
                float _DepthVerticalScale;
                half4 _ShallowColor;
                half4 _DeepColor;
                float _RefractionStrength;
                float4 _CausticMap_ST;
                half4 _CausticColor;
                float _NoiseScale;
                float _CausticScale;
                float _CausticSwaySpeed;
                float _CausticSwayMagnitude;
                float _CausticStrength;
                float _CausticSharpness;
                float _CausticPixelation;
                half4 _ShadowColor;
                float _ShadowStrength;
            CBUFFER_END

            // Helper functions for Perlin-style Gradient Noise
            float2 hash(float2 p)
            {
                p = float2(dot(p, float2(127.1, 311.7)), dot(p, float2(269.5, 183.3)));
                return -1.0 + 2.0 * frac(sin(p) * 43758.5453123);
            }

            float noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(dot(hash(i + float2(0.0, 0.0)), f - float2(0.0, 0.0)),
                                 dot(hash(i + float2(1.0, 0.0)), f - float2(1.0, 0.0)), u.x),
                            lerp(dot(hash(i + float2(0.0, 1.0)), f - float2(0.0, 1.0)),
                                 dot(hash(i + float2(1.0, 1.0)), f - float2(1.0, 1.0)), u.x), u.y);
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // 0. Depth Gradient Calculation
                float depthDiff = _SurfaceLevel - IN.positionWS.y;
                float normalizedDist = saturate(depthDiff / max(0.001, abs(_DepthVerticalScale)));
                
                // Handle the "reverse" logic: if scale is negative, flip the gradient direction
                float depthFactor = (_DepthVerticalScale < 0) ? (1.0 - normalizedDist) : normalizedDist;
                half4 depthGradient = lerp(_ShallowColor, _DeepColor, depthFactor);

                // 1. Light Attenuation (Fade)
                // Light always fades as it moves away from the surface level, 
                // regardless of which color is being used for the gradient.
                float causticFade = saturate(1.0 - normalizedDist);

                // 2. 2D Top-Down Projection (XY Plane)
                // Using IN.positionWS.xy ensures the caustics move with the water tiles in 2D space.
                // Pixelation: Multiply by resolution, floor it, then divide back to snap to a grid.
                float2 waterUV = floor(IN.positionWS.xy * _CausticPixelation) / _CausticPixelation;

                // 1. Organic Noise-based Sway
                // Replaces the circular sway with an organic wiggle based on position and time
                float2 noiseUV = waterUV * _NoiseScale + _Time.y * _CausticSwaySpeed;
                float2 sway = float2(noise(noiseUV), noise(noiseUV + float2(7.0, 11.0))) * _CausticSwayMagnitude;
                
                float2 causticUV1 = waterUV * (_CausticScale * _CausticMap_ST.xy) + sway;
                float2 causticUV2 = waterUV * (_CausticScale * 0.8 * _CausticMap_ST.xy) - (sway * 1.2);
                
                // Use sampler_CausticMap specifically to ensure Wrap Mode "Repeat" is respected
                half3 caustic1 = SAMPLE_TEXTURE2D(_CausticMap, sampler_CausticMap, causticUV1).rgb;
                half3 caustic2 = SAMPLE_TEXTURE2D(_CausticMap, sampler_CausticMap, causticUV2).rgb;
                
                // Use min() for interference and pow() to sharpen the "webbing"
                half3 finalCaustic = min(caustic1, caustic2);
                
                // 2. Surface Shadows (derived from Caustics)
                // We use the soft (un-sharpened) caustic pattern to create subtle depth/ambient shadows
                // Fade shadows out as water gets deeper
                float shadowFactor = saturate(1.0 - finalCaustic.r) * _ShadowStrength * causticFade;

                // Sharpen the "webbing" for the light pass
                // Blur the caustics by reducing sharpness as depth increases
                float dynamicSharpness = lerp(1.0, _CausticSharpness, causticFade);
                finalCaustic = pow(max(finalCaustic, 0.0), dynamicSharpness) * (_CausticStrength * causticFade);

                // 3. Refraction logic
                float2 refractedUV = IN.uv + (sway * _RefractionStrength);
                half4 texColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, refractedUV) * _BaseColor;
                
                // 4. Combine All Layers
                // Apply the depth gradient to the base texture color
                half4 finalColor = texColor * depthGradient;
                finalColor.rgb = lerp(finalColor.rgb, finalColor.rgb * _ShadowColor.rgb, shadowFactor);
                
                finalColor.rgb += finalCaustic * _CausticColor.rgb;

                return finalColor;
            }
            ENDHLSL
        }
    }
}
