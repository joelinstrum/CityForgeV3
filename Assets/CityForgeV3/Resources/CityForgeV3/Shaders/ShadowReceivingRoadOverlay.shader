Shader "CityForgeV3/ShadowReceivingRoadOverlay"
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
        Tags { "Queue" = "Geometry+2" "RenderType" = "TransparentCutout" }
        Cull Off
        // The opaque road pixels are ground, not a floating UI layer. Writing
        // their depth gives vehicle shadows a stable receiver without needing
        // the selection highlight to establish one first.
        ZWrite On
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Tags { "LightMode" = "ForwardBase" }
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase
            #include "UnityCG.cginc"
            #include "Lighting.cginc"
            #include "AutoLight.cginc"

            struct AppData { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                SHADOW_COORDS(1)
            };

            sampler2D _MainTex;
            sampler2D _RoadSurfaceTex;
            sampler2D _SidewalkSurfaceTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            fixed4 _TimeTint;
            float _UseMaterialZones;
            float _MaterialTiling;
            float _RoadMaterialTiling;
            float _SidewalkMaterialTiling;

            Varyings vert(AppData input)
            {
                Varyings output;
                output.pos = UnityObjectToClipPos(input.vertex);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                TRANSFER_SHADOW(output);
                return output;
            }

            fixed4 frag(Varyings input) : SV_Target
            {
                fixed4 artwork = tex2D(_MainTex, input.uv) * _Color;
                clip(artwork.a - 0.02);
                if (_UseMaterialZones > 0.5)
                {
                    // Imported semantic artwork is authored in sRGB. tex2D only
                    // returns linear values when the project uses Linear color
                    // space; applying this conversion in Gamma projects moves
                    // every zone away from its authored matching color.
                    fixed3 semantic = artwork.rgb;
                    #ifndef UNITY_COLORSPACE_GAMMA
                    semantic = LinearToGammaSpace(semantic);
                    #endif
                    fixed3 semanticConverted = LinearToGammaSpace(artwork.rgb);
                    fixed roadMaskRaw = 1.0 - smoothstep(0.035, 0.075,
                        min(distance(semantic, fixed3(0.349, 0.388, 0.420)),
                            distance(semantic, fixed3(0.788, 0.537, 0.447))));
                    fixed roadMaskConverted = 1.0 - smoothstep(0.035, 0.075,
                        min(distance(semanticConverted, fixed3(0.349, 0.388, 0.420)),
                            distance(semanticConverted, fixed3(0.788, 0.537, 0.447))));
                    fixed roadMask = max(roadMaskRaw, roadMaskConverted);
                    fixed sidewalkMaskRaw = 1.0 - smoothstep(0.035, 0.075,
                        min(distance(semantic, fixed3(0.831, 0.651, 0.435)),
                            distance(semantic, fixed3(0.604, 0.518, 0.404))));
                    fixed sidewalkMaskConverted = 1.0 - smoothstep(0.035, 0.075,
                        min(distance(semanticConverted, fixed3(0.831, 0.651, 0.435)),
                            distance(semanticConverted, fixed3(0.604, 0.518, 0.404))));
                    fixed sidewalkMask = max(sidewalkMaskRaw, sidewalkMaskConverted);
                    fixed3 roadSurface = tex2D(_RoadSurfaceTex,
                        input.uv * _RoadMaterialTiling).rgb;
                    fixed3 sidewalkSurface = tex2D(_SidewalkSurfaceTex,
                        input.uv * _SidewalkMaterialTiling).rgb;
                    artwork.rgb = lerp(artwork.rgb, roadSurface, roadMask);
                    artwork.rgb = lerp(artwork.rgb, sidewalkSurface, sidewalkMask);
                }
                fixed shadow = SHADOW_ATTENUATION(input);
                // Road art remains legible at night while still showing the
                // silhouettes cast by fully 3D vehicles.
                fixed illumination = lerp(0.42, 1.0, shadow);
                // Leave a small amount of the already shadowed lot surface
                // visible beneath the road. This is the same reason the
                // translucent yellow selection tile shows vehicle shadows
                // correctly; a fully opaque road hid the ground's shadow map.
                // 0.90 preserves the authored material while exposing the
                // vehicle silhouette consistently on selected and plain tiles.
                return fixed4(artwork.rgb * illumination * _TimeTint.rgb,
                    artwork.a * _TimeTint.a * 0.90);
            }
            ENDCG
        }

        Pass
        {
            Tags { "LightMode" = "ForwardAdd" }
            Blend One One
            ZWrite Off
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdadd_fullshadows
            #include "UnityCG.cginc"
            #include "Lighting.cginc"
            #include "AutoLight.cginc"

            struct AppData { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
                LIGHTING_COORDS(2, 3)
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            fixed4 _TimeTint;

            Varyings vert(AppData v)
            {
                Varyings output;
                output.pos = UnityObjectToClipPos(v.vertex);
                output.uv = TRANSFORM_TEX(v.uv, _MainTex);
                output.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                TRANSFER_VERTEX_TO_FRAGMENT(output);
                return output;
            }

            fixed4 frag(Varyings input) : SV_Target
            {
                fixed4 artwork = tex2D(_MainTex, input.uv) * _Color;
                clip(artwork.a - 0.02);
                fixed attenuation = LIGHT_ATTENUATION(input);
                fixed3 beam = artwork.rgb * _TimeTint.rgb *
                    _LightColor0.rgb * attenuation * 0.12;
                return fixed4(beam, 0);
            }
            ENDCG
        }
    }
    Fallback Off
}
