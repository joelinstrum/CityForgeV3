Shader "CityForgeV3/ShadowReceivingRoadOverlay"
{
    Properties
    {
        _DirtTopology ("Calibrated Dirt Topology", Float) = -1
        _DirtStraightTex ("Dirt Port Material", 2D) = "white" {}
        _MainTex ("Road Artwork", 2D) = "white" {}
        _RoadSurfaceTex ("Road Surface", 2D) = "gray" {}
        _SidewalkSurfaceTex ("Sidewalk Surface", 2D) = "gray" {}
        _UseMaterialZones ("Use Semantic Material Zones", Float) = 0
        _MaterialTiling ("Material Tiling", Float) = 5
        _RoadMaterialTiling ("Road Material Tiling", Float) = 5
        _SidewalkMaterialTiling ("Sidewalk Material Tiling", Float) = 5
        _HideCurbBorders ("Hide Curb Borders", Float) = 0
        _UseWorldUv ("Use World UV", Float) = 0
        _Color ("Tint", Color) = (1, 1, 1, 1)
        _ReceiveSunShadow ("Receive Sun Shadow", Range(0, 1)) = 1
    }

    SubShader
    {
        Tags { "Queue" = "Geometry+2" "RenderType" = "TransparentCutout" }
        Cull Off
        // The opaque road pixels are ground, not a floating UI layer. Writing
        // their depth gives vehicle shadows a stable receiver without needing
        // the selection highlight to establish one first.
        ZWrite On

        // Establish a screen-space mask for wet reflections. Semantic road
        // pixels write stencil 1; sidewalks and transparent artwork do not.
        Pass
        {
            ColorMask 0
            ZWrite Off
            Stencil
            {
                Ref 1
                ReadMask 1
                WriteMask 1
                Comp Always
                Pass Replace
            }
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct AppData { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };
            sampler2D _MainTex;
            #include "NationalPikeDirt.cginc"
            float4 _MainTex_ST;
            float _UseMaterialZones;
            float _UseWorldUv;
            float _MaterialTiling;
            Varyings vert(AppData input)
            {
                Varyings output;
                output.pos = UnityObjectToClipPos(input.vertex);
                float3 world = mul(unity_ObjectToWorld, input.vertex).xyz;
                output.uv = _UseWorldUv > 0.5
                    ? world.xz * (_MaterialTiling / 10.0)
                    : TRANSFORM_TEX(input.uv, _MainTex);
                return output;
            }
            fixed4 frag(Varyings input) : SV_Target
            {
                fixed4 artwork = RoadArtwork(input.uv);
                clip(artwork.a - 0.02);
                // Mask the complete visible road artwork, including sidewalks
                // and antialiased material-zone edges. Restricting this pass to
                // semantic asphalt left gaps where the museum's manual ground
                // projection looked baked into an otherwise live-shadowed tile.
                return 0;
            }
            ENDCG
        }

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
            #include "CityForgeWorldLighting.cginc"

            struct AppData { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 worldPosition : TEXCOORD2;
                SHADOW_COORDS(1)
            };

            sampler2D _MainTex;
            #include "NationalPikeDirt.cginc"
            sampler2D _RoadSurfaceTex;
            sampler2D _SidewalkSurfaceTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            float _UseMaterialZones;
            float _MaterialTiling;
            float _RoadMaterialTiling;
            float _SidewalkMaterialTiling;
            float _HideCurbBorders;
            float _UseWorldUv;
            float _ReceiveSunShadow;
            float4 _CFCloudShadowCenter;
            float4 _CFCloudShadowParams;

            Varyings vert(AppData input)
            {
                Varyings output;
                output.pos = UnityObjectToClipPos(input.vertex);
                output.worldPosition = mul(unity_ObjectToWorld,
                    input.vertex).xyz;
                output.uv = _UseWorldUv > 0.5
                    ? output.worldPosition.xz * (_MaterialTiling / 10.0)
                    : TRANSFORM_TEX(input.uv, _MainTex);
                TRANSFER_SHADOW(output);
                return output;
            }

            fixed4 frag(Varyings input) : SV_Target
            {
                fixed4 artwork = RoadArtwork(input.uv) * _Color;
                clip(artwork.a - 0.02);
                fixed3 authoredColor = artwork.rgb;
                #ifndef UNITY_COLORSPACE_GAMMA
                authoredColor = LinearToGammaSpace(authoredColor);
                #endif
                // The original colonial-brick sprites contain a pale authored
                // border between the transparent verge and the brick paving.
                // Remove that border only when explicitly requested; retain all
                // brick and mortar pixels inside the roadway.
                fixed legacyCurb = 1.0 - smoothstep(0.045, 0.12,
                    distance(authoredColor, fixed3(0.718, 0.690, 0.647)));
                if (_UseMaterialZones < 0.5 && _HideCurbBorders > 0.5)
                    clip(0.48 - legacyCurb);
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
                    // Some semantic road templates include a narrow white curb
                    // separator. Historic all-brick roads do not need that
                    // modern-looking stripe, so allow it to inherit the adjacent
                    // brick paving without changing the topology artwork.
                    // The template's curb is warm off-white (232, 223, 207),
                    // so a neutral-white test leaves the stripe visible.
                    const fixed3 curbColor = fixed3(232.0 / 255.0,
                        223.0 / 255.0, 207.0 / 255.0);
                    fixed curbMask = 1.0 - smoothstep(0.025, 0.28,
                        min(distance(semantic, curbColor),
                            distance(semanticConverted, curbColor)));
                    artwork.rgb = lerp(artwork.rgb, sidewalkSurface,
                        curbMask * saturate(_HideCurbBorders));

                }
                fixed shadow = SHADOW_ATTENUATION(input);
                fixed3 illumination = CityForgeWorldLighting(
                    fixed3(0, 1, 0), shadow);
                illumination = lerp(CityForgeWorldLighting(
                    fixed3(0, 1, 0), 1.0), illumination,
                    _ReceiveSunShadow);
                fixed cloudDistance = distance(input.worldPosition.xz,
                    _CFCloudShadowCenter.xy);
                fixed cloudMask = (1.0h - smoothstep(
                    _CFCloudShadowParams.x * (1.0h - _CFCloudShadowParams.z),
                    _CFCloudShadowParams.x, cloudDistance)) *
                    _CFCloudShadowParams.w;
                illumination *= 1.0h - cloudMask * _CFCloudShadowParams.y;
                // The road now receives the native shadow map itself. The old
                // 90% opacity workaround exposed the already-shadowed ground
                // below it and doubled/dirtied shadows across road tiles.
                return fixed4(artwork.rgb * illumination,
                    1.0);
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
            #include "NationalPikeDirt.cginc"
            float4 _MainTex_ST;
            fixed4 _Color;
            float _UseWorldUv;
            float _MaterialTiling;

            Varyings vert(AppData v)
            {
                Varyings output;
                output.pos = UnityObjectToClipPos(v.vertex);
                output.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                output.uv = _UseWorldUv > 0.5
                    ? output.worldPos.xz * (_MaterialTiling / 10.0)
                    : TRANSFORM_TEX(v.uv, _MainTex);
                TRANSFER_VERTEX_TO_FRAGMENT(output);
                return output;
            }

            fixed4 frag(Varyings input) : SV_Target
            {
                fixed4 artwork = RoadArtwork(input.uv) * _Color;
                clip(artwork.a - 0.02);
                fixed attenuation = LIGHT_ATTENUATION(input);
                fixed3 beam = artwork.rgb * _LightColor0.rgb *
                    attenuation * 0.12;
                return fixed4(beam, 0);
            }
            ENDCG
        }
    }
    Fallback Off
}
