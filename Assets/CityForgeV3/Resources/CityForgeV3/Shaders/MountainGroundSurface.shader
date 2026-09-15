Shader "CityForgeV3/MountainGroundSurface"
{
    Properties
    {
        _Color ("Color", Color) = (1, 1, 1, 1)
        _MainTex ("Surface Texture", 2D) = "white" {}
        _BedrockTex ("Bedrock", 2D) = "white" {}
        _ShaleTex ("Shale", 2D) = "white" {}
        _RockEnabled ("Rock Enabled", Float) = 1
        _AmbientFloor ("Ground Ambient Floor", Range(0, 1)) = 0.52
        _TerrainSunDirection ("Terrain Sun Direction", Vector) = (0, 1, 0, 0)
        _TerrainReliefStrength ("Terrain Relief Strength", Range(0, 2)) = 1.8
    }

    SubShader
    {
        Tags { "Queue" = "Geometry-1" "RenderType" = "Opaque" }
        // This shader is also used by horizontal overlay quads. Unity's Quad
        // primitive winding can face downward after its ground rotation, so
        // back-face culling made painted brick/concrete tiles disappear.
        Cull Off

        Pass
        {
            Tags { "LightMode" = "ForwardBase" }
            // The lot is a background receiver. Writing its depth clips the
            // below-pivot pixels of camera-facing architecture at ground level.
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase
            #include "UnityCG.cginc"
            #include "Lighting.cginc"
            #include "AutoLight.cginc"

            struct AppData
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct VertexToFragment
            {
                float4 pos : SV_POSITION;
                float3 worldNormal : TEXCOORD0;
                SHADOW_COORDS(1)
                float2 uv : TEXCOORD2;
                float3 worldPosition : TEXCOORD3;
            };

            fixed4 _Color;
            sampler2D _MainTex;
            float4 _MainTex_ST;
            sampler2D _BedrockTex, _ShaleTex;
            float _RockEnabled;
            float _AmbientFloor;
            float4 _TerrainSunDirection;
            float _TerrainReliefStrength;

            VertexToFragment vert(AppData input)
            {
                VertexToFragment output;
                output.pos = UnityObjectToClipPos(input.vertex);
                output.worldPosition = mul(unity_ObjectToWorld, input.vertex).xyz;
                output.worldNormal = UnityObjectToWorldNormal(input.normal);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                TRANSFER_SHADOW(output);
                return output;
            }

            fixed3 TriplanarRock(sampler2D map, float3 p, float3 n)
            {
                float3 weights = pow(abs(n), 4.0);
                weights /= max(dot(weights, float3(1,1,1)), 0.0001);
                // Mirrored wrapping avoids a hard source-edge seam without modifying artwork.
                return tex2D(map, p.zy / 12.0).rgb * weights.x
                    + tex2D(map, p.xz / 12.0).rgb * weights.y
                    + tex2D(map, p.xy / 12.0).rgb * weights.z;
            }

            fixed4 frag(VertexToFragment input) : SV_Target
            {
                fixed shadow = SHADOW_ATTENUATION(input);
                fixed3 normal = normalize(input.worldNormal);
                fixed3 lightDirection = normalize(_TerrainSunDirection.xyz);
                fixed diffuse = saturate(dot(normal, lightDirection));
                // The former max(floor, diffuse * shadow) made low-angle sun
                // shadows impossible: Morning/Afternoon diffuse was below the
                // floor for both lit and shadowed pixels. Treat ambient as the
                // stable minimum and let directional light supply the range
                // above it. A shadow now removes that directional contribution
                // without crushing the receiver below its ambient floor.
                fixed illumination = lerp(
                    _AmbientFloor, 1.0h, diffuse * shadow);
                // Broad sculpted hills have shallow normals, so Lambert alone
                // barely separates their two shoulders after the authored
                // ambient floor is applied. Reinforce only the horizontal
                // relief component: the slope facing the active world sun is
                // lifted, while the opposite slope receives a restrained
                // directional shade. Flat ground remains unchanged.
                fixed2 horizontalSun = normalize(lightDirection.xz + fixed2(0.0001h, 0.0001h));
                fixed reliefFacing = dot(normal.xz, horizontalSun);
                fixed relief = clamp(reliefFacing * _TerrainReliefStrength,
                    -0.42h, 0.28h);
                illumination = saturate(illumination * (1.0h + relief));
                fixed4 surface = tex2D(_MainTex, input.uv);
                float slope = length(normal.xz) / max(normal.y, 0.05);
                float3 p = input.worldPosition;
                // Broad, quiet variation breaks up contour-like material borders.
                float variation = sin(p.x * 0.071 + sin(p.z * 0.049))
                    * sin(p.z * 0.063 + p.y * 0.035) * 0.09;
                float exposure = slope + variation;
                float rockMask = smoothstep(0.32, 0.85, exposure) * saturate(_RockEnabled);
                float bedrockMask = smoothstep(0.70, 1.30, exposure);
                fixed3 shale = TriplanarRock(_ShaleTex, p, normal);
                fixed3 bedrock = TriplanarRock(_BedrockTex, p, normal);
                surface.rgb = lerp(surface.rgb, lerp(shale, bedrock, bedrockMask), rockMask);
                return fixed4(surface.rgb * _Color.rgb * illumination,
                    surface.a * _Color.a);
            }
            ENDCG
        }

        UsePass "Legacy Shaders/VertexLit/SHADOWCASTER"
    }
}
