Shader "CityForgeV3/KingKongEnclosurePBR"
{
    Properties
    {
        _Color ("Timber Tint", Color) = (0.94,0.86,0.72,1)
        _MainTex ("Albedo", 2D) = "white" {}
        [Normal] _BumpMap ("Normal", 2D) = "bump" {}
        _BumpScale ("Normal Strength", Range(0,2)) = 0.85
        [NoScaleOffset] _RoughnessMap ("Roughness", 2D) = "white" {}
        _Contrast ("Albedo Contrast", Range(0.5,2)) = 1.24
        _Saturation ("Albedo Saturation", Range(0,2)) = 1.25
        _Brightness ("Albedo Brightness", Range(0.25,1.5)) = 0.82
        _SmoothnessScale ("Smoothness Scale", Range(0,0.5)) = 0.16
        _CavityStrength ("Texture Cavity Strength", Range(0,1)) = 0.38
        [HideInInspector] _ConstructionRevealHeight ("Construction Reveal Height", Float) = 100000
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
        sampler2D _RoughnessMap;
        fixed4 _Color;
        half _BumpScale;
        half _Contrast;
        half _Saturation;
        half _Brightness;
        half _SmoothnessScale;
        half _CavityStrength;
        float _ConstructionRevealHeight;

        struct Input
        {
            float2 uv_MainTex;
            float2 uv_BumpMap;
            float3 worldPos;
        };

        void surf(Input input, inout SurfaceOutputStandard output)
        {
            clip(_ConstructionRevealHeight - input.worldPos.y);
            fixed4 source = tex2D(_MainTex, input.uv_MainTex) * _Color;
            fixed3 color = saturate((source.rgb - 0.5) * _Contrast + 0.5);
            fixed luminance = dot(color, fixed3(0.2126, 0.7152, 0.0722));
            color = saturate(lerp(luminance.xxx, color, _Saturation));
            output.Albedo = color * _Brightness;
            output.Normal = UnpackScaleNormal(
                tex2D(_BumpMap, input.uv_BumpMap), _BumpScale);

            fixed roughness = tex2D(_RoughnessMap, input.uv_MainTex).r;
            output.Metallic = 0;
            output.Smoothness = saturate((1.0 - roughness) *
                _SmoothnessScale);

            // The export does not contain a dedicated AO texture. Preserve
            // the atlas's authored creases as conservative local occlusion so
            // beam joints and recesses retain the depth visible in Tripo.
            fixed cavity = smoothstep(0.12, 0.62, luminance);
            output.Occlusion = lerp(1.0, cavity, _CavityStrength);
            output.Alpha = source.a;
        }
        ENDCG
    }

    FallBack "Standard"
}
