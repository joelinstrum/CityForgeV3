Shader "CityForgeV3/MeadowGroundSurface"
{
    Properties
    {
        _Color ("Color", Color) = (1, 1, 1, 1)
        _GrassHueShift ("Meadow hue experiment",Range(0,.1)) = 0
        _MainTex ("Surface Texture", 2D) = "white" {}
        _DistrictMapTex ("District-wide grass color", 2D) = "white" {}
        _DistrictMapStrength ("District color influence", Range(0,1)) = 0
        _DistrictMapMipBias ("District color smoothing", Range(0,5)) = 2.5
        _TextureWorldSize ("Texture World Size (m)", Float) = 75
        _HillTex ("Legacy patch texture", 2D) = "white" {}
        _HillHeight ("Legacy hill height metres", Float) = 45
        _MeadowPatchStrength ("Meadow patch strength", Range(0,1)) = 0
        _DistantMeadow ("Distant meadow filtering", Range(0,1)) = 0
        _FarGrassNoise ("Far grass grain", Range(0,1)) = 0
        _FarGrassBrightness ("Far grass brightness", Range(0,1)) = 1
        _FarGrassGrainFrequency ("Far grass grain frequency", Range(.5,3)) = 1
        _GrassDetailMipScale ("Grass detail mip scale", Range(.25,1)) = 1
        _RollingHillDarkSlopeLift ("Darkest slope lift", Range(0,.75)) = .5
        _RollingHillDeepShadeLift ("Deepest slope lift", Range(0,.5)) = .225
        _SoilRevealStrength ("Close hill soil reveal", Range(0,1)) = 0
        _SoilTex ("Hill soil", 2D) = "white" {}
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
            #pragma multi_compile_local __ DISTRICT_GRASS_MAP
            #include "UnityCG.cginc"
            #include "Lighting.cginc"
            #include "AutoLight.cginc"
            #include "CityForgeWorldLighting.cginc"

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
                float2 hillVariation : TEXCOORD3;
                float2 meadowMetres : TEXCOORD4;
            };

            fixed4 _Color;
            sampler2D _MainTex, _HillTex, _DistrictMapTex, _SoilTex;
            float _MeadowPatchStrength;
            float _DistantMeadow;
            float _FarGrassNoise;
            float _FarGrassBrightness;
            float _FarGrassGrainFrequency;
            float _GrassDetailMipScale;
            float _GrassHueShift;
            float _RollingHillDarkSlopeLift;
            float _RollingHillDeepShadeLift;
            float _SoilRevealStrength;
            float _TextureWorldSize;
            float _DistrictMapStrength;
            float _DistrictMapMipBias;
            float4 _MainTex_ST;

            float MeadowNoise(float2 p);

            VertexToFragment vert(AppData input)
            {
                VertexToFragment output;
                output.pos = UnityObjectToClipPos(input.vertex);
                output.worldNormal = UnityObjectToWorldNormal(input.normal);
                // Terrain UVs cover the whole district once. The tiled grass
                // path below keeps its separate world-space coordinates.
                output.uv = input.uv;
                output.hillVariation=0;
                output.meadowMetres=mul(unity_ObjectToWorld,input.vertex).xz;
                #if defined(MEADOW_PATCHES)
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

            float MeadowNoise(float2 p)
            {
                float2 cell=floor(p), f=frac(p); f=f*f*(3-2*f);
                return lerp(lerp(MeadowOffset(cell).x,MeadowOffset(cell+float2(1,0)).x,f.x),
                    lerp(MeadowOffset(cell+float2(0,1)).x,MeadowOffset(cell+1).x,f.x),f.y);
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
                #if defined(HILL_MEADOW)
                // The district's small vertical relief needs a calibrated
                // normal response at its kilometre-wide presentation scale.
                // This changes directional light only, never the grass color.
                normal=normalize(fixed3(normal.x*3.0,normal.y,normal.z*3.0));
                #endif
                fixed3 illumination = CityForgeWorldLighting(normal, shadow);
                #if defined(HILL_MEADOW)
                // Lift only slopes substantially darker than level grass under
                // the same light and shadow. Neutral ground and highlights stay
                // at their existing values; no colour is painted onto the hill.
                fixed3 levelIllumination=CityForgeWorldLighting(fixed3(0,1,0),shadow);
                float levelValue=dot(levelIllumination,float3(.2126,.7152,.0722));
                float slopeValue=dot(illumination,float3(.2126,.7152,.0722));
                float darkness=saturate((levelValue-slopeValue)/max(levelValue,.001));
                illumination=lerp(illumination,levelIllumination,
                    _RollingHillDarkSlopeLift*smoothstep(.03,.12,darkness));
                // Trim only the deepest remaining shade; the midtone response
                // above and all light-facing slopes retain their prior lighting.
                illumination=lerp(illumination,levelIllumination,
                    _RollingHillDeepShadeLift*smoothstep(.20,.35,darkness));
                #endif
                // The authored macro grass is anchored directly in world space,
                // matching hosted lot receivers instead of restarting per lot.
                float2 surfaceUv=input.meadowMetres/max(.01,_TextureWorldSize);
                float2 surfaceDx, surfaceDy;
                MeadowGradients(surfaceUv,surfaceDx,surfaceDy);
                fixed4 surface=tex2Dgrad(_MainTex,surfaceUv,
                    surfaceDx*_GrassDetailMipScale,
                    surfaceDy*_GrassDetailMipScale);
                #if defined(HILL_MEADOW)
                // Blend another placement of the same approved grass. The
                // broad blend field is texture placement only: it never adds
                // a light/dark hill mask or changes the source grass palette.
                float2 shiftedUv=float2(-surfaceUv.y,surfaceUv.x)+float2(3.17,7.43);
                float2 shiftedDx=float2(-surfaceDx.y,surfaceDx.x);
                float2 shiftedDy=float2(-surfaceDy.y,surfaceDx.y);
                fixed3 alternate=tex2Dgrad(_MainTex,shiftedUv,
                    shiftedDx*_GrassDetailMipScale,shiftedDy*_GrassDetailMipScale).rgb;
                float blend=.24+.28*MeadowNoise(input.meadowMetres/240+float2(4.1,9.7));
                surface.rgb=lerp(surface.rgb,alternate,blend);
                #elif defined(MEADOW_PATCHES)
                // One extra sample on flat ground. Broad masking hides repetition;
                // mipmapped world-space detail never changes scale with camera zoom.
                float2 patchUV=input.meadowMetres/40.0;
                float2 patchDx, patchDy;
                MeadowGradients(patchUV, patchDx, patchDy);
                fixed3 thin=tex2Dgrad(_HillTex,patchUV,patchDx,patchDy).rgb;
                #endif
                if (_GrassHueShift > 0) surface.rgb=ShiftGrassHue(surface.rgb);
                if (_FarGrassNoise > 0)
                {
                    // Fine world-anchored stipple survives distant mip filtering.
                    // Keep the wider variation subtle so it does not read as
                    // soft, repeated patches at district scale.
                    float2 grainMetres=input.meadowMetres*_FarGrassGrainFrequency;
                    float fine=MeadowNoise(grainMetres/2.0+float2(17.3,41.7));
                    float broad=MeadowNoise(grainMetres/13.0+float2(63.1,9.4));
                    surface.rgb*=1+_FarGrassNoise*((fine-.5)*.34+(broad-.5)*.08);
                }
                surface.rgb*=_FarGrassBrightness;
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
                #if defined(DISTRICT_GRASS_MAP)
                // Keep the approved grass as the base. A smooth, low-strength
                // district color map adds broad variation at distant zooms.
                fixed3 districtColor=tex2Dbias(_DistrictMapTex,
                    float4(saturate(input.uv),0,_DistrictMapMipBias)).rgb;
                surface.rgb=lerp(surface.rgb,districtColor,_DistrictMapStrength);
                #endif
                #if defined(HILL_MEADOW)
                // Grass texture controls the fine edge of actual soil beneath
                // the meadow. The soil shares world coordinates across tiles.
                if (_SoilRevealStrength > 0)
                {
                    float3 terrainNormal=normalize(input.worldNormal);
                    float grade=length(terrainNormal.xz)/max(terrainNormal.y,.05);
                    float slopeMask=smoothstep(.10,.32,grade);
                    float opening=smoothstep(.77,.83,
                        surface.r/max(surface.g,.001));
                    fixed3 soil=tex2Dbias(_SoilTex,
                        float4(input.meadowMetres/18.0,0,2.0)).rgb;
                    // The shared mountain earth reads violet against this
                    // meadow. Shift it toward warm ochre without recoloring
                    // the grass or editing the mountain source texture.
                    soil=saturate(soil*fixed3(1.26,1.06,.65));
                    surface.rgb=lerp(surface.rgb,soil,
                        slopeMask*opening*_SoilRevealStrength);
                }
                #endif
                return fixed4(surface.rgb * _Color.rgb * illumination,
                    surface.a * _Color.a);
            }
            ENDCG
        }

        UsePass "Legacy Shaders/VertexLit/SHADOWCASTER"
    }
}
