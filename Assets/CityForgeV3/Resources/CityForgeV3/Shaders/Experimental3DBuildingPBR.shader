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
        _Saturation ("Tripo Saturation", Range(0,4)) = 1.34
        _Vibrance ("Sunlit Vibrance", Range(0,1)) = 0
        _AmbientFill ("Local Ambient Fill", Range(0,1)) = 0
        _AlbedoBoost ("Local Albedo Lift", Range(0.5,3)) = 1
        _EnvironmentDim ("Time Of Day Brightness", Range(0,1)) = 1
        _DirectionalContrast ("Directional Light Contrast", Range(0,1)) = 0
        [HideInInspector] _DirectionalLightDirection ("Directional Light Direction", Vector) = (0,1,0,0)
        _SunIntensityScale ("Sun Intensity Scale", Range(0,3)) = 1
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
        #pragma surface surf Standard fullforwardshadows vertex:vert
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
        half _Vibrance;
        half _AmbientFill;
        half _AlbedoBoost;
        half _EnvironmentDim;
        half _DirectionalContrast;
        float4 _DirectionalLightDirection;
        half _SunIntensityScale;
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

        void surf(Input input, inout SurfaceOutputStandard output)
        {
            clip(_ConstructionRevealHeight - input.worldPos.y);
            fixed4 albedo = tex2D(_MainTex, input.uv_MainTex) * _Color;
            fixed3 preserved = saturate(
                PreserveSourceColor(albedo.rgb) * _AlbedoBoost);
            output.Normal = UnpackScaleNormal(
                tex2D(_BumpMap, input.uv_BumpMap), _BumpScale);
            // Render the authored atlas directly, then apply stable
            // CityForge directional form in the emissive path. The standard
            // surface-lighting path crushes these Tripo atlases nearly black
            // in the project's gamma/exposure configuration.
            // Classify the whole facade from the mesh normal. The imported
            // normal map remains active for timber detail, but must not make
            // neighboring texels disagree about which side of the building
            // faces the afternoon sun.
            half3 worldSurfaceNormal = normalize(input.geometricWorldNormal);
            half facingLight = saturate(dot(worldSurfaceNormal,
                normalize(_DirectionalLightDirection.xyz)));
            // Afternoon uses a much wider neutral value range so façades
            // facing away from the western sun read as genuinely shaded.
            // This remains local to building materials: it neither tints nor
            // changes the exposure of grass, roads, overlays, or props.
            // Shade by scaling the complete RGB triplet uniformly. This
            // lowers value without blending toward gray or reducing the
            // authored timber saturation.
            half shadowFloor = lerp(0.62h, 0.16h, _DirectionalContrast);
            // Let the lighting-lab sun control lift the directly illuminated
            // face without raising the shaded face or the environment.
            half directSunBoost = 1.0h +
                max(0.0h, _SunIntensityScale - 1.0h) * 0.38h;
            half sunlightCeiling = lerp(1.20h,
                1.36h * directSunBoost, _DirectionalContrast);
            half directionalShape = lerp(facingLight,
                smoothstep(0.08h, 0.58h, facingLight),
                _DirectionalContrast);
            fixed luminance = dot(preserved,
                fixed3(0.2126, 0.7152, 0.0722));
            preserved = saturate(lerp(luminance.xxx, preserved,
                _Saturation));
            half chroma = max(preserved.r, max(preserved.g, preserved.b)) -
                min(preserved.r, min(preserved.g, preserved.b));
            // Color grading belongs to the authored surface, not its current
            // normal-to-sun angle. The old directional weighting effectively
            // disabled Afternoon saturation on most façade pixels while Noon
            // received the full grade. Keep directionality in localLighting.
            half vibrance = _Vibrance * (1.0h - chroma);
            preserved = saturate(lerp(luminance.xxx, preserved,
                1.0h + vibrance));
            half localLighting = lerp(
                shadowFloor, sunlightCeiling, directionalShape);
            output.Albedo = fixed3(0, 0, 0);
            fixed mask = tex2D(_NightEmissionMask, input.uv_MainTex).r;
            fixed3 nightEmission = albedo.rgb * _NightEmissionColor.rgb *
                (mask * _NightEmissionIntensity);
            // Use the color that actually passed through CityForge's
            // contrast, saturation, and vibrance grade. The former albedo.rgb
            // reference silently discarded every building color control and
            // made the warm sun act as the only visible source of color.
            // Darkness and colorfulness are separate controls. Reducing the
            // full RGB triplet equally is numerically saturation-preserving,
            // but the chroma becomes visually weak at low values. Darken the
            // luminance fully while retaining more of the authored chroma so
            // shaded timber reads as dark brown, not neutral gray.
            fixed3 chromaComponent = preserved - luminance.xxx;
            fixed3 directionallyShaded =
                luminance.xxx * localLighting +
                chromaComponent * sqrt(max(localLighting, 0.001h));
            output.Emission = saturate(directionallyShaded) *
                _EnvironmentDim + nightEmission;
            output.Alpha = albedo.a;
            fixed4 metalSmooth = tex2D(_MetallicGlossMap, input.uv_MainTex);
            output.Metallic = saturate(metalSmooth.r * _Metallic);
            output.Smoothness = saturate(metalSmooth.a * _GlossMapScale);
        }
        ENDCG
    }

    FallBack "Standard"
}
