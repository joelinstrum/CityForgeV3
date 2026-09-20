Shader "CityForgeV3/MeadowGroundSurface"
{
    Properties
    {
        _Color ("Color", Color) = (1, 1, 1, 1)
        _GrassHueShift ("Meadow hue experiment",Range(0,.1)) = 0
        _MainTex ("Surface Texture", 2D) = "white" {}
        _TextureWorldSize ("Texture World Size (m)", Float) = 75
        _HillTex ("Thin crest meadow", 2D) = "white" {}
        _HillHeight ("Hill height metres", Float) = 45
        _MeadowPatchStrength ("Meadow patch strength", Range(0,1)) = 0
        _DistantMeadow ("Distant meadow filtering", Range(0,1)) = 0
        _NearDetailStrength ("Near grass detail strength", Range(0,.2)) = 0
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
            #pragma multi_compile_local __ MEADOW_PATCHES
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
                float2 meadowMetres : TEXCOORD5;
            };

            fixed4 _Color;
            sampler2D _MainTex, _HillTex;
            float _HillHeight;
            float _MeadowPatchStrength;
            float _DistantMeadow;
            float _NearDetailStrength;
            float _GrassHueShift;
            float _TextureWorldSize;
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
                output.meadowMetres=mul(unity_ObjectToWorld,input.vertex).xz;
                #if defined(HILL_MEADOW) || defined(MEADOW_PATCHES)
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
            void MeadowGradients(float2 uv, out float2 dx, out float2 dy)
            {
                dx = ddx(uv); dy = ddy(uv);
                // Smooth in both texture directions: the shallow camera angle
                // otherwise preserves grain along the anisotropic short axis.
                // Coarse mip filtering retains broad colour without extra samples.
                float footprint = max(.125, max(length(dx), length(dy)));
                dx = lerp(dx, float2(footprint, 0), _DistantMeadow);
                dy = lerp(dy, float2(0, footprint), _DistantMeadow);
            }

            fixed4 Meadow(sampler2D meadowSampler, float2 uv)
            {
                // Neighbouring compositions receive stable offsets and overlap
                // smoothly. This remains useful for the separate hill artwork.
                float2 cell = floor(uv);
                float2 weight = smoothstep(.15, .85, frac(uv));
                float2 dx, dy;
                MeadowGradients(uv, dx, dy);
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
            fixed3 HillMeadow(float2 uv, float elevation, float3 normal, fixed3 meadow, float2 variation, fixed3 thin)
            {
                float broad=variation.x, patches=variation.y;
                float hill=smoothstep(.5,4,elevation);
                float height=saturate(elevation/max(1,_HillHeight));
                float slope=length(normal.xz);
                // Broken transitions: no contour bands or lot-sized material stamps.
                float crest=smoothstep(.12,.60,height+(broad-.5)*.26);
                float wear=smoothstep(.06,.35,slope)*smoothstep(.36,.72,patches)*.65;
                float mixWeight=saturate(crest*.68+wear)*hill;
                fixed3 color=lerp(meadow,thin,mixWeight);
                // Preserve the approved meadow palette: vary brightness, never tint hills green.
                // The lighter crest artwork supplies its own natural colour variation.
                color*=lerp(1.0,lerp(.96,1.055,crest),hill);
                color*=1+(broad-.5)*.22*hill+(patches-.5)*.10*hill;
                return color;
            }

            float3 ShiftGrassHue(float3 rgb)
            {
                // HSV rotation retains the source value/saturation and all fine artwork.
                float4 K=float4(0,-1.0/3,2.0/3,-1);
                float4 p=lerp(float4(rgb.bg,K.wz),float4(rgb.gb,K.xy),step(rgb.b,rgb.g));
                float4 q=lerp(float4(p.xyw,rgb.r),float4(rgb.r,p.yzx),step(p.x,rgb.r));
                float d=q.x-min(q.w,q.y), e=1e-6;
                float3 hsv=float3(abs(q.z+(q.w-q.y)/(6*d+e)),d/(q.x+e),q.x);
                // Keep brown dirt and neutral pebbles from picking up the green tint.
                float grass=smoothstep(.10,.17,hsv.x)*(1-smoothstep(.40,.47,hsv.x))*smoothstep(.12,.28,hsv.y);
                hsv.x+=_GrassHueShift*grass;
                float3 hue=abs(frac(hsv.xxx+float3(0,2.0/3,1.0/3))*6-3);
                return hsv.z*lerp(float3(1,1,1),saturate(hue-1),hsv.y);
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
                // The authored macro grass is anchored directly in world space,
                // matching hosted lot receivers instead of restarting per lot.
                float2 surfaceUv=input.meadowMetres/max(.01,_TextureWorldSize);
                float2 surfaceDx, surfaceDy;
                MeadowGradients(surfaceUv,surfaceDx,surfaceDy);
                fixed4 surface=tex2Dgrad(_MainTex,surfaceUv,surfaceDx,surfaceDy);
                #if defined(HILL_MEADOW)
                fixed3 thin=Meadow(_HillTex,input.meadowMetres/40.0).rgb;
                surface.rgb=HillMeadow(input.uv,input.elevation,normal,surface.rgb,input.hillVariation,thin);
                #elif defined(MEADOW_PATCHES)
                // One extra sample on flat ground. Broad masking hides repetition;
                // mipmapped world-space detail never changes scale with camera zoom.
                float2 patchUV=input.meadowMetres/40.0;
                float2 patchDx, patchDy;
                MeadowGradients(patchUV, patchDx, patchDy);
                fixed3 thin=tex2Dgrad(_HillTex,patchUV,patchDx,patchDy).rgb;
                #endif
                if (_GrassHueShift > 0) surface.rgb=ShiftGrassHue(surface.rgb);
                #if defined(MEADOW_PATCHES)
                // Soft 28–110m fields span many tiles. No camera/time/lot input,
                // no extra mesh or material layer over roads and riverbanks.
                float field=input.hillVariation.x*.72+input.hillVariation.y*.28;
                float dryMask=smoothstep(.51,.77,field);
                // Lift straw centers without widening patches or hardening their edges.
                float dry=(.52*dryMask+.10*dryMask*dryMask*dryMask)*_MeadowPatchStrength;
                float lush=(1-smoothstep(.23,.49,field))*.30*_MeadowPatchStrength;
                surface.rgb*=lerp(fixed3(1,1,1),fixed3(.92,1.025,.91),lush);
                // Apply after the hue study so straw retains its warm colour.
                surface.rgb=lerp(surface.rgb,thin*fixed3(1.045,1.0,.94),dry);
                #endif
                // Close inspection needs physical-scale texture that the broad
                // 75 m color field intentionally omits. Neutral modulation adds
                // irregular 0.55-2.2 m grain without changing hue or affecting
                // the approved player Zoom 3+ presentation.
                float broadGrain=MeadowNoise(input.meadowMetres/2.2+19.7);
                float mediumGrain=MeadowNoise(input.meadowMetres/.95+41.3);
                float fineGrain=MeadowNoise(input.meadowMetres/.55+73.1);
                float nearGrain=(broadGrain-.5)*.44+
                    (mediumGrain-.5)*.34+(fineGrain-.5)*.22;
                surface.rgb*=1+nearGrain*_NearDetailStrength;
                return fixed4(surface.rgb * _Color.rgb * illumination,
                    surface.a * _Color.a);
            }
            ENDCG
        }

        UsePass "Legacy Shaders/VertexLit/SHADOWCASTER"
    }
}
