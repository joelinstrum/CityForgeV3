Shader "CityForgeV3/VehiclePaint"
{
    Properties
    {
        _MainTex ("Albedo", 2D) = "white" {}
        _BumpMap ("Normal", 2D) = "bump" {}
        _MetallicGlossMap ("Metallic", 2D) = "black" {}
        _PaintColor ("Paint Color", Color) = (0.1, 0.2, 0.1, 1)
        _BlackRoof ("Black Roof", Range(0,1)) = 0
        _WorldMinY ("World Min Y", Float) = -1
        _WorldMaxY ("World Max Y", Float) = 1
        _Glossiness ("Smoothness", Range(0,1)) = 0.38
        _Metallic ("Metallic", Range(0,1)) = 0.22
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 300

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows vertex:vert addshadow
        #pragma target 3.0

        sampler2D _MainTex;
        sampler2D _BumpMap;
        sampler2D _MetallicGlossMap;
        fixed4 _PaintColor;
        half _BlackRoof;
        half _Glossiness;
        half _Metallic;
        float _WorldMinY;
        float _WorldMaxY;

        struct Input
        {
            float2 uv_MainTex;
            float2 uv_BumpMap;
            float3 worldPos;
        };

        void vert(inout appdata_full vertex, out Input output)
        {
            UNITY_INITIALIZE_OUTPUT(Input, output);
        }

        void surf(Input input, inout SurfaceOutputStandard output)
        {
            fixed4 source = tex2D(_MainTex, input.uv_MainTex);
            half maximum = max(source.r, max(source.g, source.b));
            half minimum = min(source.r, min(source.g, source.b));
            half saturation = maximum - minimum;
            half luminance = dot(source.rgb, half3(0.2126, 0.7152, 0.0722));
            half normalizedHeight = saturate((input.worldPos.y - _WorldMinY) /
                max(0.001, _WorldMaxY - _WorldMinY));

            // The Meshy atlas contains every surface in one texture. Restrict
            // paint to neutral dark body-panel pixels in the central body band;
            // this preserves skin, brass, windows, black roof, chassis and trim.
            half neutral = 1.0 - smoothstep(0.10, 0.24, saturation);
            half paintValue = smoothstep(0.075, 0.14, luminance) *
                (1.0 - smoothstep(0.34, 0.48, luminance));
            half bodyBand = smoothstep(0.12, 0.24, normalizedHeight) *
                (1.0 - smoothstep(0.72, 0.84, normalizedHeight));
            half roofExclusion = smoothstep(0.56, 0.70, normalizedHeight);
            half mask = saturate(neutral * paintValue * bodyBand * (1.0 - roofExclusion));
            half detail = saturate(0.35 + luminance * 2.2);
            half3 painted = _PaintColor.rgb * detail;
            half3 coloredBody = lerp(source.rgb, painted, mask);
            half roofMask = _BlackRoof * smoothstep(0.58, 0.72, normalizedHeight) *
                neutral * smoothstep(0.05, 0.18, luminance);
            half3 blackFabric = source.rgb * 0.08;
            half interiorHeight = smoothstep(0.30, 0.44, normalizedHeight) *
                (1.0 - smoothstep(0.66, 0.76, normalizedHeight));
            half interiorMask = interiorHeight * neutral *
                (1.0 - smoothstep(0.08, 0.16, luminance));

            output.Albedo = lerp(lerp(coloredBody, blackFabric, roofMask),
                source.rgb * 0.16, interiorMask);
            output.Normal = UnpackNormal(tex2D(_BumpMap, input.uv_BumpMap));
            fixed4 metal = tex2D(_MetallicGlossMap, input.uv_MainTex);
            output.Metallic = max(_Metallic * mask, metal.r * (1.0 - mask));
            output.Smoothness = lerp(_Glossiness, 0.44, mask);
            output.Alpha = source.a;
        }
        ENDCG
    }
    FallBack "Standard"
}
