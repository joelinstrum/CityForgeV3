Shader "CityForgeV3/BuildingWindowEmission"
{
    Properties
    {
        _MainTex ("Preserved Window Artwork", 2D) = "white" {}
        [HDR] _EmissionColor ("Warm Window Light", Color) = (1.0, 0.45, 0.12, 1)
        _EmissionStrength ("Emission Strength", Range(0, 8)) = 2.5
        _ArtworkInfluence ("Artwork Detail", Range(0, 1)) = 0.82
        _WarmPixelThreshold ("Window Pixel Threshold", Range(0, 1)) = 0.08
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry+20" }
        Cull Off
        ZWrite On

        CGPROGRAM
        #pragma surface surf Lambert noforwardadd
        #pragma target 3.0

        fixed4 _EmissionColor;
        sampler2D _MainTex;
        half _EmissionStrength;
        half _ArtworkInfluence;
        half _WarmPixelThreshold;

        struct Input { float2 uv_MainTex; };

        void surf(Input input, inout SurfaceOutput output)
        {
            fixed4 artwork = tex2D(_MainTex, input.uv_MainTex);
            half luminance = dot(artwork.rgb, half3(0.299, 0.587, 0.114));
            half warmBias = saturate((artwork.r - artwork.b * 0.82) * 3.2);
            // The extracted mesh follows the source triangulation, so some
            // triangles extend beyond the glass. Gate emission per pixel using
            // both brightness and warmth: window interiors survive, while the
            // slate roof, stone trim, mullions, and curtains remain unlit.
            half brightnessMask = smoothstep(_WarmPixelThreshold,
                _WarmPixelThreshold + 0.28, luminance);
            half colorMask = smoothstep(0.10, 0.42, warmBias);
            half lightMask = brightnessMask * colorMask;
            clip(lightMask - 0.025);
            half3 preserved = lerp(half3(1, 1, 1), artwork.rgb,
                _ArtworkInfluence);
            output.Albedo = artwork.rgb * 0.10;
            output.Emission = preserved * _EmissionColor.rgb *
                (_EmissionStrength * lightMask);
            output.Alpha = artwork.a;
        }
        ENDCG
    }
    FallBack "Unlit/Color"
}
