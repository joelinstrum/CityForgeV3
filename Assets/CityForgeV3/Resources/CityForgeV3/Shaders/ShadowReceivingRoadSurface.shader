Shader "CityForgeV3/ShadowReceivingRoadSurface"
{
    Properties
    {
        _MainTex ("Road Artwork", 2D) = "white" {}
        _RoadSurfaceTex ("Road Surface", 2D) = "gray" {}
        _SidewalkSurfaceTex ("Sidewalk Surface", 2D) = "gray" {}
        _UseMaterialZones ("Use Semantic Material Zones", Float) = 0
        _MaterialTiling ("Material Tiling", Float) = 5
        _RoadMaterialTiling ("Road Material Tiling", Float) = 5
        _SidewalkMaterialTiling ("Sidewalk Material Tiling", Float) = 5
        _Color ("Tint", Color) = (1, 1, 1, 1)
        _TimeTint ("Time of Day Tint", Color) = (1, 1, 1, 1)
    }

    SubShader
    {
        Tags { "Queue"="Geometry+2" "RenderType"="Opaque" }
        Cull Off
        ZWrite On

        CGPROGRAM
        // Let Unity generate the same proven shadow-receiver plumbing used by
        // the selected-road Standard material. The previous hand-written pass
        // sampled as fully lit on ordinary road tiles.
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0 // Built-in forward renderer

        sampler2D _MainTex;
        sampler2D _RoadSurfaceTex;
        sampler2D _SidewalkSurfaceTex;
        fixed4 _Color;
        fixed4 _TimeTint;
        float _UseMaterialZones;
        float _RoadMaterialTiling;
        float _SidewalkMaterialTiling;

        struct Input { float2 uv_MainTex; };

        void surf(Input input, inout SurfaceOutputStandard output)
        {
            fixed4 artwork = tex2D(_MainTex, input.uv_MainTex) * _Color;
            clip(artwork.a - 0.02);
            if (_UseMaterialZones > 0.5)
            {
                fixed3 semantic = artwork.rgb;
                #ifndef UNITY_COLORSPACE_GAMMA
                semantic = LinearToGammaSpace(semantic);
                #endif
                fixed3 converted = LinearToGammaSpace(artwork.rgb);
                fixed roadMask = max(
                    1.0 - smoothstep(0.035, 0.075, min(
                        distance(semantic, fixed3(0.349, 0.388, 0.420)),
                        distance(semantic, fixed3(0.788, 0.537, 0.447)))),
                    1.0 - smoothstep(0.035, 0.075, min(
                        distance(converted, fixed3(0.349, 0.388, 0.420)),
                        distance(converted, fixed3(0.788, 0.537, 0.447)))));
                fixed sidewalkMask = max(
                    1.0 - smoothstep(0.035, 0.075, min(
                        distance(semantic, fixed3(0.831, 0.651, 0.435)),
                        distance(semantic, fixed3(0.604, 0.518, 0.404)))),
                    1.0 - smoothstep(0.035, 0.075, min(
                        distance(converted, fixed3(0.831, 0.651, 0.435)),
                        distance(converted, fixed3(0.604, 0.518, 0.404)))));
                fixed3 road = tex2D(_RoadSurfaceTex,
                    input.uv_MainTex * _RoadMaterialTiling).rgb;
                fixed3 sidewalk = tex2D(_SidewalkSurfaceTex,
                    input.uv_MainTex * _SidewalkMaterialTiling).rgb;
                artwork.rgb = lerp(artwork.rgb, road, roadMask);
                artwork.rgb = lerp(artwork.rgb, sidewalk, sidewalkMask);
            }
            output.Albedo = artwork.rgb * _TimeTint.rgb;
            output.Alpha = artwork.a * _TimeTint.a;
            output.Metallic = 0.0;
            output.Smoothness = 0.05;
        }
        ENDCG
    }
    Fallback Off
}
