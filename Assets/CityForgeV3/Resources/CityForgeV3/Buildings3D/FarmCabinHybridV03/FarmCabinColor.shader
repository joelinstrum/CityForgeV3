Shader "CityForgeV3/Buildings/Selective Matte Color"
{
    Properties
    {
        _MainTex ("Original color texture", 2D) = "white" {}
        _Color ("Original tint", Color) = (1,1,1,1)
        _BumpMap ("Original normal texture", 2D) = "bump" {}
        _BumpScale ("Normal strength", Float) = 1
        _EmissionMap ("Emission", 2D) = "white" {}
        _EmissionColor ("Emission color", Color) = (0,0,0,0)
        _Glossiness ("Smoothness", Range(0,1)) = 0.1
        _Metallic ("Metallic", Range(0,1)) = 0
        _SpecularHighlights ("Highlights", Float) = 0
        _GlossyReflections ("Reflections", Float) = 0
        _GradeStrength ("Color adjustment", Range(0,1)) = 1
        _RoofDarkening ("Roof darkening", Range(0,0.5)) = 0.25
        _GreenDarkening ("Green darkening", Range(0,0.5)) = 0.18
        _Contrast ("Midtone contrast", Range(1,1.4)) = 1.10
        _Saturation ("Color saturation", Range(1,1.3)) = 1.08
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 300
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows addshadow
        #pragma target 3.0
        #pragma shader_feature_local _NORMALMAP
        #pragma shader_feature_local _EMISSION
        #define _SPECULARHIGHLIGHTS_OFF 1
        #define _GLOSSYREFLECTIONS_OFF 1
        #include "UnityStandardUtils.cginc"
        sampler2D _MainTex, _BumpMap, _EmissionMap;
        fixed4 _Color, _EmissionColor;
        half _BumpScale, _Glossiness, _GradeStrength, _RoofDarkening, _GreenDarkening, _Contrast, _Saturation;
        struct Input { float2 uv_MainTex; float2 uv_BumpMap; float2 uv_EmissionMap; float3 worldNormal; INTERNAL_DATA };
        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            fixed4 source = tex2D(_MainTex, IN.uv_MainTex) * _Color;
            float3 c = source.rgb;
            #ifndef UNITY_COLORSPACE_GAMMA
            c = LinearToGammaSpace(c);
            #endif
            float luma = dot(c, float3(0.2126,0.7152,0.0722));
            // Protect cream siding and white window/porch detail.
            float protectLight = 1 - smoothstep(0.58,0.85,luma);
            float3 graded = lerp(c, saturate((c - 0.5) * _Contrast + 0.5), protectLight);
            graded = lerp(luma.xxx, graded, _Saturation);
            // Green/olive paint only: warm wood and terracotta fail this mask.
            float green = smoothstep(0.005,0.055,c.g-c.b) * (1-smoothstep(0.015,0.075,c.r-c.g));
            float3 surfaceNormal = normalize(WorldNormalVector(IN,float3(0,0,1)));
            float roof = smoothstep(0.35,0.65,surfaceNormal.y) * (1-smoothstep(0.45,0.7,luma));
            graded *= 1 - _GreenDarkening * green * protectLight;
            graded *= 1 - _RoofDarkening * roof;
            c = lerp(c,saturate(graded),_GradeStrength);
            #ifndef UNITY_COLORSPACE_GAMMA
            c = GammaToLinearSpace(c);
            #endif
            o.Albedo = c;
            o.Metallic = 0;
            o.Smoothness = _Glossiness;
            o.Occlusion = 1;
            o.Alpha = source.a;
            #ifdef _NORMALMAP
            o.Normal = UnpackScaleNormal(tex2D(_BumpMap, IN.uv_BumpMap),_BumpScale);
            #endif
            #ifdef _EMISSION
            o.Emission = tex2D(_EmissionMap, IN.uv_EmissionMap).rgb * _EmissionColor.rgb;
            #endif
        }
        ENDCG
    }
    FallBack "Diffuse"
}
