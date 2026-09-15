Shader "CityForgeV3/MountainGroundSurfaceV09"
{
    Properties
    {
        _Color ("Color", Color) = (1, 1, 1, 1)
        _MainTex ("Surface Texture", 2D) = "white" {}
        _BedrockTex ("Bedrock", 2D) = "white" {}
        _ShaleTex ("Shale", 2D) = "white" {}
        _BrownTex ("Brown Earth and Scree", 2D) = "white" {}
        _BrownStrength ("Brown Coverage", Range(0,1)) = 1
        _TransitionStrength ("Natural Transitions", Range(0,1)) = 1
        _GrassNeutralStrength ("Neutral Grass Experiment", Range(0,1)) = 1
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

        // Only elevated terrain owns depth. Flat river corridors retain the
        // existing below-ground bed/water composition without blocking it.
        Pass
        {
            ZWrite On
            ZTest LEqual
            ColorMask 0
            CGPROGRAM
            #pragma vertex depthVert
            #pragma fragment depthFrag
            #include "UnityCG.cginc"
            struct DepthOutput { float4 pos:SV_POSITION; float height:TEXCOORD0; };
            DepthOutput depthVert(float4 vertex:POSITION)
            {
                DepthOutput o;o.pos=UnityObjectToClipPos(vertex);o.height=vertex.y;return o;
            }
            fixed4 depthFrag(DepthOutput i):SV_Target { clip(i.height-.25);return 0; }
            ENDCG
        }

        Pass
        {
            Tags { "LightMode" = "ForwardBase" }
            // Opaque mountain terrain must resolve its own overlapping slopes and occlude background objects.
            ZWrite Off
            Blend Off

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
            sampler2D _BedrockTex, _ShaleTex, _BrownTex;
            float _BrownStrength;
            float _RockEnabled;
            float _GrassNeutralStrength;
            float _TransitionStrength;
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

            fixed3 QuietBedrock(float3 p, float3 n)
            {
                // Irregular sampling bends the mirrored seam axes so cracks no longer
                // form repeated bilateral emblems. Grass retains its existing mapping.
                p += 1.8 * float3(sin(p.z*.071+p.y*.037),
                    sin(p.x*.059+p.z*.031),sin(p.y*.067+p.x*.043));
                float3 q=p*(12.0/18.0);
                fixed3 detail=TriplanarRock(_BedrockTex,q,n);
                float3 w=pow(abs(n),4.0);w/=max(dot(w,float3(1,1,1)),.0001);
                // Blend with a coarser mip of the SAME stone: quieter fissures,
                // preserving local silver-gray color instead of tinting the material.
                fixed3 broad=tex2Dbias(_BedrockTex,float4(q.zy/12.0,0,4.0)).rgb*w.x
                    +tex2Dbias(_BedrockTex,float4(q.xz/12.0,0,4.0)).rgb*w.y
                    +tex2Dbias(_BedrockTex,float4(q.xy/12.0,0,4.0)).rgb*w.z;
                return lerp(broad,detail,.38);
            }

            fixed3 NeutralGrass(fixed3 color)
            {
                // Grass-only, luminance-preserving correction of the olive source.
                float3 candidate = color * float3(.86, 1.0, .78);
                float3 luminance = float3(.2126, .7152, .0722);
                candidate *= dot(color, luminance) / max(dot(candidate, luminance), .0001);
                return lerp(color, saturate(candidate), _GrassNeutralStrength);
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
                surface.rgb = NeutralGrass(surface.rgb);
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
                // Vegetation collects on shelves; exposed steep faces stay mostly bare.
                float shelf = 1.0 - smoothstep(.45, 1.5, slope);
                float shelteredGreen = smoothstep(.30, .70, patch) * lerp(.09, .88, shelf);
                green = lerp(green, shelteredGreen, _TransitionStrength);
                float shalePocket = smoothstep(0.44, 0.68,
                    RockNoise(p / 19.0 + float3(41, 7, 11)));
                float bedrockMask = smoothstep(0.45, 0.95, exposure) * (1.0 - shalePocket * 0.28);
                fixed3 shale = TriplanarRock(_ShaleTex, p * (12.0 / 8.0), normal);
                fixed3 bedrock = QuietBedrock(p, normal);
                shale = saturate((shale - .35) * 1.18 + .29);
                bedrock = saturate((bedrock - .35) * 1.18 + .29);
                fixed3 grassSource = TriplanarRock(_MainTex, p * (12.0 / 5.0), normal);
                fixed3 grass = dot(grassSource,float3(.2126,.7152,.0722)) * float3(.38,.70,.30);
                fixed3 variedRock = lerp(shale, bedrock, bedrockMask);
                float brownPatch = .75 * RockNoise(p / float3(43,29,43) + float3(19,73,47))
                    + .25 * RockNoise(p / 13.0 + float3(67,31,9));
                float brownMask = smoothstep(.42,.66,brownPatch) * .18 * _BrownStrength;
                fixed3 brown = TriplanarRock(_BrownTex,p,normal);
                variedRock = lerp(variedRock,brown,brownMask);
                variedRock = lerp(variedRock, grass, green);
                // Broad, restrained grass variation avoids a uniform carpet.
                float meadow = RockNoise(p / float3(87,51,87) + float3(11,3,29));
                float dry = smoothstep(.46,.73,meadow);
                float3 grassVariation = lerp(float3(.94,1.015,.90),float3(.85,.97,.77),dry);
                surface.rgb *= lerp(float3(1,1,1),grassVariation,_TransitionStrength);
                // Loose scree on lower inclined shoulders; flat river corridor remains clear.
                float lowSlope = smoothstep(.10,.32,slope) * (1.0-smoothstep(.65,1.15,slope));
                float footHeight = smoothstep(.7,4.0,p.y) * (1.0-smoothstep(22.0,65.0,p.y));
                float debrisPatch = smoothstep(.32,.68,RockNoise(p/float3(29,17,29)+float3(31,9,71)));
                float debris = lowSlope * footHeight * debrisPatch * .72 * _TransitionStrength * saturate(_RockEnabled);
                fixed3 scree = lerp(shale,brown,.10);
                surface.rgb = lerp(surface.rgb,scree,debris);
                surface.rgb = lerp(surface.rgb, variedRock, rockMask);
                return fixed4(surface.rgb * _Color.rgb * illumination,
                    1.0);
            }
            ENDCG
        }

        UsePass "Legacy Shaders/VertexLit/SHADOWCASTER"
    }
}
