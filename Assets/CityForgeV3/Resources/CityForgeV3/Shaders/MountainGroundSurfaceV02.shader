Shader "CityForgeV3/MountainGroundSurfaceV02"
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

            float RockHash(float3 p)
            {
                p = frac(p * 0.1031);
                p += dot(p, p.yzx + 33.33);
                return frac((p.x + p.y) * p.z);
            }
            float RockNoise(float3 p)
            {
                float3 i=floor(p), f=frac(p);
                f=f*f*(3.0-2.0*f);
                return lerp(lerp(lerp(RockHash(i),RockHash(i+float3(1,0,0)),f.x),
                    lerp(RockHash(i+float3(0,1,0)),RockHash(i+float3(1,1,0)),f.x),f.y),
                    lerp(lerp(RockHash(i+float3(0,0,1)),RockHash(i+float3(1,0,1)),f.x),
                    lerp(RockHash(i+float3(0,1,1)),RockHash(i+float3(1,1,1)),f.x),f.y),f.z);
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
                // Separate coverage from fine texture detail: broad green pockets and shale seams.
                float broad = RockNoise(p / float3(31, 23, 31));
                float detail = RockNoise(p / 9.0 + float3(8, 13, 21));
                float patch = broad * 0.72 + detail * 0.28;
                float green = smoothstep(0.40, 0.65, patch);
                // Steeper faces retain more exposed stone, without eliminating greenery.
                green *= lerp(0.85, 0.55, smoothstep(0.7, 2.0, slope));
                float shalePocket = smoothstep(0.44, 0.68,
                    RockNoise(p / 19.0 + float3(41, 7, 11)));
                float bedrockMask = smoothstep(0.70, 1.30, exposure) * (1.0 - shalePocket * 0.75);
                fixed3 shale = TriplanarRock(_ShaleTex, p, normal);
                fixed3 bedrock = TriplanarRock(_BedrockTex, p, normal);
                fixed3 grass = TriplanarRock(_MainTex, p * (12.0 / 5.0), normal);
                fixed3 variedRock = lerp(shale, bedrock, bedrockMask);
                variedRock = lerp(variedRock, grass, green);
                surface.rgb = lerp(surface.rgb, variedRock, rockMask);
                return fixed4(surface.rgb * _Color.rgb * illumination,
                    surface.a * _Color.a);
            }
            ENDCG
        }

        UsePass "Legacy Shaders/VertexLit/SHADOWCASTER"
    }
}
