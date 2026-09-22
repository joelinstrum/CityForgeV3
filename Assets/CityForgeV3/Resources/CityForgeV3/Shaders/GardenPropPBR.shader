Shader "CityForgeV3/GardenPropPBR"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo", 2D) = "white" {}
        [Normal] _BumpMap ("Normal Map", 2D) = "bump" {}
        _BumpScale ("Normal Scale", Range(0,2)) = 1
        _Metallic ("Metallic", Range(0,1)) = 0
        _Glossiness ("Smoothness", Range(0,1)) = 0.1
        [HideInInspector] _Mode ("Rendering Mode", Float) = 0
        [HideInInspector] _SrcBlend ("Source Blend", Float) = 1
        [HideInInspector] _DstBlend ("Destination Blend", Float) = 0
        [HideInInspector] _ZWrite ("Z Write", Float) = 1
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 250
        Blend [_SrcBlend] [_DstBlend]
        ZWrite [_ZWrite]

        CGPROGRAM
        #pragma surface surf StandardGarden fullforwardshadows keepalpha
        #pragma target 3.0
        #include "UnityPBSLighting.cginc"

        sampler2D _MainTex;
        sampler2D _BumpMap;
        fixed4 _Color;
        half _BumpScale;
        half _Metallic;
        half _Glossiness;
        half _CFNativeSurfaceIndirectScale;
        half _CFGardenSurfaceExposure;
        half _CFWorldWhitePoint;

        struct Input
        {
            float2 uv_MainTex;
            float2 uv_BumpMap;
        };

        half4 LightingStandardGarden(SurfaceOutputStandard surface,
            half3 viewDirection, UnityGI lighting)
        {
            half4 lit = LightingStandard(surface, viewDirection, lighting);
            half exposure = max(1.0h, _CFGardenSurfaceExposure);
            half3 exposed = max(0, lit.rgb * exposure);
            half peak = max(exposed.r, max(exposed.g, exposed.b));
            half whitePoint = _CFWorldWhitePoint > 0.01h
                ? _CFWorldWhitePoint : 0.98h;
            lit.rgb = exposed * min(1.0h,
                whitePoint / max(peak, 0.0001h));
            return lit;
        }

        void LightingStandardGarden_GI(SurfaceOutputStandard surface,
            UnityGIInput input, inout UnityGI lighting)
        {
            LightingStandard_GI(surface, input, lighting);
            lighting.indirect.diffuse *= max(1.0h,
                _CFNativeSurfaceIndirectScale);
        }

        void surf(Input input, inout SurfaceOutputStandard output)
        {
            fixed4 albedo = tex2D(_MainTex, input.uv_MainTex) * _Color;
            output.Albedo = albedo.rgb;
            output.Normal = UnpackScaleNormal(
                tex2D(_BumpMap, input.uv_BumpMap), _BumpScale);
            output.Metallic = _Metallic;
            output.Smoothness = _Glossiness;
            output.Occlusion = 1;
            output.Emission = 0;
            output.Alpha = albedo.a;
        }
        ENDCG
    }

    Fallback "Standard"
}
