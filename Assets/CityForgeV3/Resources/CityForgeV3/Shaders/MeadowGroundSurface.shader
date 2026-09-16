Shader "CityForgeV3/MeadowGroundSurface"
{
    Properties
    {
        _Color ("Color", Color) = (1, 1, 1, 1)
        _MainTex ("Surface Texture", 2D) = "white" {}
        _HillTex ("Thin crest meadow", 2D) = "white" {}
        _HillHeight ("Hill height metres", Float) = 45
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
            #pragma target 3.5
            #pragma multi_compile_fwdbase
            #pragma multi_compile_local __ HILL_MEADOW
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
                float elevation : TEXCOORD3;
                float2 hillVariation : TEXCOORD4;
            };

            fixed4 _Color;
            sampler2D _MainTex, _HillTex;
            float _HillHeight;
            float4 _MainTex_ST;
            float _AmbientFloor;
            float4 _TerrainSunDirection;
            float _TerrainReliefStrength;

            float MeadowNoise(float2 p);

            VertexToFragment vert(AppData input)
            {
                VertexToFragment output;
                output.pos = UnityObjectToClipPos(input.vertex);
                output.worldNormal = UnityObjectToWorldNormal(input.normal);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.elevation = input.vertex.y;
                output.hillVariation=0;
                #if defined(HILL_MEADOW)
                // Broad hill colour stays anchored when near-zoom grass detail changes.
                float2 metres=input.vertex.xz;
                output.hillVariation=float2(MeadowNoise(metres/110),MeadowNoise(metres/28+7.3));
                #endif
                TRANSFER_SHADOW(output);
                return output;
            }

            float2 MeadowOffset(float2 cell)
            {
                // Integer hashing stays identical across neighbouring pixels;
                // large sine hashes can acquire visible precision noise on Metal.
                uint h = (uint)(int)cell.x * 73856093u ^ (uint)(int)cell.y * 19349663u;
                h ^= h >> 16; h *= 2246822519u; h ^= h >> 13;
                uint k = h * 3266489917u; k ^= k >> 16;
                return float2(h & 65535u, k & 65535u) / 65536.0;
            }
            fixed4 Meadow(sampler2D meadowSampler, float2 uv)
            {
                // The source covers 40m. Neighbouring compositions receive
                // stable offsets and overlap smoothly, independent of lots.
                float2 cell = floor(uv);
                float2 weight = smoothstep(.15, .85, frac(uv));
                float2 dx = ddx(uv), dy = ddy(uv);
                fixed4 a = tex2Dgrad(meadowSampler, uv + MeadowOffset(cell), dx, dy);
                fixed4 b = tex2Dgrad(meadowSampler, uv + MeadowOffset(cell + float2(1,0)), dx, dy);
                fixed4 c = tex2Dgrad(meadowSampler, uv + MeadowOffset(cell + float2(0,1)), dx, dy);
                fixed4 d = tex2Dgrad(meadowSampler, uv + MeadowOffset(cell + 1), dx, dy);
                return lerp(lerp(a,b,weight.x),lerp(c,d,weight.x),weight.y);
            }

            float MeadowNoise(float2 p)
            {
                float2 cell=floor(p), f=frac(p); f=f*f*(3-2*f);
                return lerp(lerp(MeadowOffset(cell).x,MeadowOffset(cell+float2(1,0)).x,f.x),
                    lerp(MeadowOffset(cell+float2(0,1)).x,MeadowOffset(cell+1).x,f.x),f.y);
            }
            fixed3 HillMeadow(float2 uv, float elevation, float3 normal, fixed3 meadow, float2 variation)
            {
                float broad=variation.x, patches=variation.y;
                float hill=smoothstep(.5,4,elevation);
                float height=saturate(elevation/max(1,_HillHeight));
                float slope=length(normal.xz);
                // Broken transitions: no contour bands or lot-sized material stamps.
                float crest=smoothstep(.12,.60,height+(broad-.5)*.26);
                float wear=smoothstep(.06,.35,slope)*smoothstep(.36,.72,patches)*.65;
                float mixWeight=saturate(crest*.68+wear)*hill;
                fixed3 thin=Meadow(_HillTex,uv*.8).rgb;
                fixed3 color=lerp(meadow,thin,mixWeight);
                // Preserve the approved meadow palette: vary brightness, never tint hills green.
                // The lighter crest artwork supplies its own natural colour variation.
                color*=lerp(1.0,lerp(.96,1.055,crest),hill);
                color*=1+(broad-.5)*.22*hill+(patches-.5)*.10*hill;
                return color;
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
                fixed4 surface = Meadow(_MainTex,input.uv);
                #if defined(HILL_MEADOW)
                surface.rgb=HillMeadow(input.uv,input.elevation,normal,surface.rgb,input.hillVariation);
                #endif
                return fixed4(surface.rgb * _Color.rgb * illumination,
                    surface.a * _Color.a);
            }
            ENDCG
        }

        UsePass "Legacy Shaders/VertexLit/SHADOWCASTER"
    }
}
