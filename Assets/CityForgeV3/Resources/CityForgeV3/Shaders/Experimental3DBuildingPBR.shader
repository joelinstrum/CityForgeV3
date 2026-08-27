Shader "CityForgeV3/Experimental3DBuildingPBR"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo", 2D) = "white" {}
        [Normal] _BumpMap ("Normal Map", 2D) = "bump" {}
        _BumpScale ("Normal Scale", Range(0,2)) = 1
        [NoScaleOffset] _MetallicGlossMap ("Metallic (R) Smoothness (A)", 2D) = "black" {}
        _Metallic ("Metallic", Range(0,1)) = 0
        _GlossMapScale ("Smoothness", Range(0,1)) = 0.28
        _Contrast ("Tripo Contrast", Range(0.8,2)) = 1.42
        _Saturation ("Tripo Saturation", Range(0,2)) = 1.34
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 300

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0

        sampler2D _MainTex;
        sampler2D _BumpMap;
        sampler2D _MetallicGlossMap;
        fixed4 _Color;
        half _BumpScale;
        half _Metallic;
        half _GlossMapScale;
        half _Contrast;
        half _Saturation;

        struct Input
        {
            float2 uv_MainTex;
            float2 uv_BumpMap;
        };

        fixed3 PreserveSourceColor(fixed3 source)
        {
            // CityForge currently renders in Gamma space. Preserve the source
            // hue separation locally without lowering the scene exposure or
            // changing the project's established billboard presentation.
            // Pivot around middle gray: roofs/cornices return to charcoal,
            // while the already-correct brownstone midtones do not become
            // uniformly darker as they did with the former power curve.
            fixed3 contrasted = saturate(
                (source - fixed3(0.5, 0.5, 0.5)) * _Contrast +
                fixed3(0.5, 0.5, 0.5));
            fixed luminance = dot(contrasted, fixed3(0.2126, 0.7152, 0.0722));
            return saturate(lerp(luminance.xxx, contrasted, _Saturation));
        }

        void surf(Input input, inout SurfaceOutputStandard output)
        {
            fixed4 albedo = tex2D(_MainTex, input.uv_MainTex) * _Color;
            output.Albedo = PreserveSourceColor(albedo.rgb);
            output.Alpha = albedo.a;
            output.Normal = UnpackScaleNormal(
                tex2D(_BumpMap, input.uv_BumpMap), _BumpScale);
            fixed4 metalSmooth = tex2D(_MetallicGlossMap, input.uv_MainTex);
            output.Metallic = saturate(metalSmooth.r * _Metallic);
            output.Smoothness = saturate(metalSmooth.a * _GlossMapScale);
        }
        ENDCG
    }

    FallBack "Standard"
}
