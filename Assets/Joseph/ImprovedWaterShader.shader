Shader "Custom/2DPixelWater"
{
    Properties
    {
        _MainTex ("Water Base Tex", 2D) = "white" {}
        _NoiseTex ("Noise Texture", 2D) = "white" {}
        _CausticTex ("Caustics Texture", 2D) = "white" {}

        _BaseMap ("Base Map", 2D) = "white" {}
        _CausticMap ("Caustic Map", 2D) = "white" {}
        _FoamMap ("Foam Map", 2D) = "white" {}
        _HeightMap ("Height Map", 2D) = "white" {}

        _BaseColor ("Base Color", Color) = (1, 1, 1, 1)
        _CausticColor ("Caustic Color", Color) = (0.3, 0.7, 0.9, 1)
        _FoamColor ("Foam Color", Color) = (1, 1, 1, 1)
        _ShallowColor ("Shallow Color", Color) = (0.2, 0.8, 0.9, 0.8)
        _DeepColor ("Deep Color", Color) = (0.05, 0.2, 0.5, 1.0)
        _SpecularColor ("Specular Color", Color) = (1, 1, 1, 1)

        _PixelDensity ("Pixel Density", Float) = 32.0
        _PixelsPerUnit ("Pixels Per Unit", Float) = 16.0
        _Speed ("Wave Speed", Float) = 0.5
        _Distortion ("Distortion Strength", Float) = 0.05
        _DistortionStrength ("Distortion Strength (Alt)", Float) = 0.08
        _CausticScale ("Caustic Scale", Float) = 1.0
        _CausticYSquash ("Caustic Y Squash", Float) = 0.8
        _SpecularScale ("Specular Scale", Float) = 12.0
        _SpecularSpeed ("Specular Speed", Float) = 0.2
        _SpecularThreshold ("Specular Threshold", Float) = 0.65
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 worldPos : TEXCOORD0;
            };

            sampler2D _MainTex, _NoiseTex, _CausticTex;
            sampler2D _BaseMap, _CausticMap, _FoamMap, _HeightMap;
            float4 _BaseColor, _CausticColor, _FoamColor, _ShallowColor, _DeepColor, _SpecularColor;
            float _PixelDensity, _PixelsPerUnit, _Speed, _Distortion, _DistortionStrength;
            float _CausticScale, _CausticYSquash, _SpecularScale, _SpecularSpeed, _SpecularThreshold;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xy;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float pixelDensity = (_PixelsPerUnit > 0.0) ? _PixelsPerUnit : _PixelDensity;
                float2 pixelatedUV = floor(i.worldPos * pixelDensity) / pixelDensity;

                float2 noiseUV = pixelatedUV * 0.22 + float2(_Time.y * _Speed * 0.8, _Time.y * _Speed * 0.45);
                float noiseA = tex2D(_NoiseTex, noiseUV).r;
                float noiseB = tex2D(_NoiseTex, noiseUV * 1.7 + 10.0).r;
                float waveNoise = (noiseA + noiseB) * 0.5;

                float distortion = max(_Distortion, _DistortionStrength);
                float2 distortedUV = pixelatedUV + (waveNoise - 0.5) * distortion;

                float2 waterTexUV = distortedUV * 2.5 + float2(_Time.y * _Speed * 0.2, _Time.y * _Speed * 0.15);
                float4 baseTex = tex2D(_MainTex, waterTexUV);
                float4 textureNoise = tex2D(_NoiseTex, waterTexUV * 1.8 + 5.0);

                float2 causticUV = distortedUV * 2.0 + float2(_Time.y * _Speed * 1.8, _Time.y * _Speed * 1.2);
                float causticsA = tex2D(_CausticTex, causticUV).r;
                float causticsB = tex2D(_CausticMap, causticUV * 1.4 + 2.0).r;
                float caustics = saturate((causticsA + causticsB) * 0.5 * _CausticScale);

                float oceanPattern = saturate((waveNoise * 1.2) + (baseTex.r * 0.6) + 0.15);
                float4 waterColor = lerp(_ShallowColor, _DeepColor, saturate(1.0 - oceanPattern * 0.75 + pixelatedUV.y * 0.15));

                waterColor.rgb = lerp(waterColor.rgb, baseTex.rgb * _BaseColor.rgb, 0.55);
                waterColor.rgb += (caustics * _CausticColor.rgb) * 0.75;
                waterColor.rgb += textureNoise.rgb * 0.08;
                waterColor.a = _ShallowColor.a;

                return waterColor;
            }
            ENDHLSL
        }
    }
}