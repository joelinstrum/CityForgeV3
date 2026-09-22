Shader "CityForgeV3/Experimental3DBuildingPBR"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _PaintColor ("Paint Color", Color) = (1,1,1,1)
        _PaintEnabled ("Paint Enabled", Float) = 0
        _MainTex ("Albedo", 2D) = "white" {}
        [Normal] _BumpMap ("Normal Map", 2D) = "bump" {}
        _BumpScale ("Normal Scale", Range(0,2)) = 1
        [NoScaleOffset] _MetallicGlossMap ("Metallic (R) Smoothness (A)", 2D) = "black" {}
        _Metallic ("Metallic", Range(0,1)) = 0
        _GlossMapScale ("Smoothness", Range(0,1)) = 0.28
        _Contrast ("Tripo Contrast", Range(0.8,2)) = 1.42
        _Saturation ("Tripo Saturation", Range(0,4)) = 1.34
        _Vibrance ("Sunlit Vibrance", Range(0,1)) = 0
        _AlbedoBoost ("Local Albedo Lift", Range(0.5,3)) = 1
        [NoScaleOffset] _NightEmissionMask ("Night Emission Mask", 2D) = "black" {}
        [HDR] _NightEmissionColor ("Night Emission Color", Color) = (1,0.55,0.22,1)
        _NightEmissionIntensity ("Night Emission Intensity", Range(0,8)) = 0
        [HideInInspector] _ConstructionRevealHeight ("Construction Reveal Height", Float) = 100000
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 300

        CGPROGRAM
        #pragma surface surf StandardBuilding fullforwardshadows vertex:vert
        #pragma target 3.0
        #include "UnityPBSLighting.cginc"

        sampler2D _MainTex;
        sampler2D _BumpMap;
        sampler2D _MetallicGlossMap;
        fixed4 _Color;
        fixed4 _PaintColor;
        half _PaintEnabled;
        half _BumpScale;
        half _Metallic;
        half _GlossMapScale;
        half _Contrast;
        half _Saturation;
        half _Vibrance;
        half _AlbedoBoost;
        half _CFNativeSurfaceIndirectScale;
        half _CFNativeBuildingNightDimming;
        sampler2D _NightEmissionMask;
        fixed4 _NightEmissionColor;
        half _NightEmissionIntensity;
        float _ConstructionRevealHeight;

        struct Input
        {
            float2 uv_MainTex;
            float2 uv_BumpMap;
            float3 worldNormal;
            float3 geometricWorldNormal;
            float3 worldPos;
            INTERNAL_DATA
        };

        void vert(inout appdata_full vertex, out Input output)
        {
            UNITY_INITIALIZE_OUTPUT(Input, output);
            output.geometricWorldNormal = UnityObjectToWorldNormal(vertex.normal);
        }

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
            return contrasted;
        }

        half4 LightingStandardBuilding(SurfaceOutputStandard surface,
            half3 viewDirection, UnityGI lighting)
        {
            return LightingStandard(surface, viewDirection, lighting);
        }

        void LightingStandardBuilding_GI(SurfaceOutputStandard surface,
            UnityGIInput input, inout UnityGI lighting)
        {
            LightingStandard_GI(surface, input, lighting);
            // Keep an uninitialized preview on the neutral path until its
            // environment owner publishes the shared daylight scale.
            lighting.indirect.diffuse *= max(1.0h,
                _CFNativeSurfaceIndirectScale);
        }

        void surf(Input input, inout SurfaceOutputStandard output)
        {
            clip(_ConstructionRevealHeight - input.worldPos.y);
            fixed4 albedo = tex2D(_MainTex, input.uv_MainTex) * _Color;
            // The supplied atlases combine white clapboard with dark roof,
            // stone and brick. Recolor bright, low-chroma paint while keeping
            // source texture shading and all non-paint surfaces intact.
            half high = max(albedo.r, max(albedo.g, albedo.b));
            half low = min(albedo.r, min(albedo.g, albedo.b));
            half neutral = 1.0h - smoothstep(0.10h, 0.23h, high - low);
            half paintMask = _PaintEnabled * neutral *
                smoothstep(0.43h, 0.73h, high);
            albedo.rgb = lerp(albedo.rgb,
                albedo.rgb * _PaintColor.rgb, paintMask);
            fixed3 preserved = saturate(
                PreserveSourceColor(albedo.rgb) * _AlbedoBoost);
            output.Normal = UnpackScaleNormal(
                tex2D(_BumpMap, input.uv_BumpMap), _BumpScale);
            fixed luminance = dot(preserved,
                fixed3(0.2126, 0.7152, 0.0722));
            preserved = saturate(lerp(luminance.xxx, preserved,
                _Saturation));
            half chroma = max(preserved.r, max(preserved.g, preserved.b)) -
                min(preserved.r, min(preserved.g, preserved.b));
            half vibrance = _Vibrance * (1.0h - chroma);
            preserved = saturate(lerp(luminance.xxx, preserved,
                1.0h + vibrance));
            // Ordinary building surfaces use Unity's Standard lighting path.
            // Only the authored night mask is emissive.
            output.Albedo = preserved * (1.0h - _CFNativeBuildingNightDimming);
            fixed mask = tex2D(_NightEmissionMask, input.uv_MainTex).r;
            fixed3 nightEmission = albedo.rgb * _NightEmissionColor.rgb *
                (mask * _NightEmissionIntensity);
            output.Emission = nightEmission;
            output.Alpha = albedo.a;
            fixed4 metalSmooth = tex2D(_MetallicGlossMap, input.uv_MainTex);
            output.Metallic = saturate(metalSmooth.r * _Metallic);
            output.Smoothness = saturate(metalSmooth.a * _GlossMapScale);
        }
        ENDCG
    }

    FallBack "Standard"
}
