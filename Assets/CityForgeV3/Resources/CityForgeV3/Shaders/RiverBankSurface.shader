Shader "CityForgeV3/RiverBankSurface"
{
    Properties
    {
        _MainTex ("Grass and pebble bank", 2D) = "white" {}
        _GravelTex ("Submerged fine gravel", 2D) = "white" {}
        _EarthTex ("Alternate shoreline composition", 2D) = "white" {}
        _BankTex2 ("Third shoreline composition", 2D) = "white" {}
        _BankTex3 ("Fourth shoreline composition", 2D) = "white" {}
        _BankVariantCount ("Shoreline variant count", Float) = 2
        _BankPatternOffset ("Stable shoreline pattern offset", Float) = 0
        _TerrainTex ("Matching terrain grass", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _DistrictHalfSize ("District half size", Vector) = (100000,100000,0,0)
        _RiverWaterLevel ("Water level", Float) = -0.1
        _BankTop ("Top of bank", Float) = 0.186
        _DetailMeters ("Material repeat metres", Float) = 48
        _OuterFadeEnd ("Outer terrain fade end", Float) = 0.833333
        _OuterFadeNoise ("Outer fade irregularity", Float) = 0
        _TerrainBlendStrength ("Terrain match strength", Float) = 0
        _TerrainWorldSize ("Terrain texture metres", Float) = 75
        _SubmergedBedBrightness ("Submerged bed brightness", Range(0,2)) = 0.8
        _SubmergedBlendStart ("Submerged blend start", Range(-1,1)) = 0.015
        _SubmergedBlendEnd ("Submerged blend end", Range(-1,1)) = 0.13
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Cull Off
        ZWrite Off
        ZTest LEqual
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase
            #include "UnityCG.cginc"
            #include "Lighting.cginc"
            #include "AutoLight.cginc"
            #include "CityForgeWorldLighting.cginc"
            struct AppData
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 bank : TEXCOORD1;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };
            struct Varyings
            {
                float4 position : SV_POSITION;
                float2 bank : TEXCOORD1;
                float across : TEXCOORD0;
                float3 localPosition : TEXCOORD2;
                float3 worldNormal : TEXCOORD3;
                fixed4 color : COLOR;
                float2 worldMetres : TEXCOORD5;
                SHADOW_COORDS(4)
            };
            sampler2D _MainTex, _GravelTex, _EarthTex, _BankTex2,
                _BankTex3, _TerrainTex;
            fixed4 _Color;
            float4 _DistrictHalfSize;
            float _RiverWaterLevel, _BankTop, _DetailMeters, _OuterFadeEnd;
            float _OuterFadeNoise, _TerrainBlendStrength, _TerrainWorldSize;
            float _SubmergedBedBrightness, _SubmergedBlendStart,
                _SubmergedBlendEnd;
            float _BankVariantCount;
            float _BankPatternOffset;
            Varyings vert(AppData input)
            {
                Varyings output;
                output.position = UnityObjectToClipPos(input.vertex);
                output.bank = input.bank;
                output.across = input.uv.y;
                output.localPosition = input.vertex.xyz;
                // Clipping can reverse the mirrored bank's calculated normals.
                input.normal.y = abs(input.normal.y);
                output.worldNormal = UnityObjectToWorldNormal(input.normal);
                output.color = input.color;
                output.worldMetres =
                    mul(unity_ObjectToWorld, input.vertex).xz;
                TRANSFER_SHADOW(output);
                return output;
            }
            // Crossfade offset copies only near the artwork's wrap boundary.
            // Both copies have zero weight at their own seam, including mip levels.
            fixed3 Strip(sampler2D artwork, float2 uv)
            {
                float seam = smoothstep(.22, .46, abs(frac(uv.x) - .5));
                return lerp(tex2D(artwork, uv).rgb,
                    tex2D(artwork, uv + float2(.5, 0)).rgb, seam);
            }
            // Stable irregular reach lengths rather than a repeating sine rhythm.
            float ReachNoise(float x)
            {
                float cell = floor(x), t = frac(x);
                t = t * t * (3 - 2 * t);
                float a = frac(sin(cell * 127.1 + 311.7) * 43758.5453);
                float b = frac(sin((cell + 1) * 127.1 + 311.7) * 43758.5453);
                return lerp(a, b, t);
            }
            float ReachHash(float cell)
            {
                return frac(sin(cell * 127.1 + 71.9) * 43758.5453);
            }
            float EdgeNoise(float2 p)
            {
                float2 cell = floor(p), t = frac(p);
                t = t * t * (3 - 2 * t);
                float4 corners = frac(sin(float4(
                    dot(cell, float2(127.1, 311.7)),
                    dot(cell + float2(1, 0), float2(127.1, 311.7)),
                    dot(cell + float2(0, 1), float2(127.1, 311.7)),
                    dot(cell + 1, float2(127.1, 311.7)))) * 43758.5453);
                return lerp(lerp(corners.x, corners.y, t.x),
                    lerp(corners.z, corners.w, t.x), t.y);
            }
            fixed4 frag(Varyings input) : SV_Target
            {
                // Keep the open-border underside clipping used by the riverbed.
                float3 towardCamera = mul((float3x3)unity_WorldToObject, UNITY_MATRIX_V[2].xyz);
                float depth = _RiverWaterLevel - input.localPosition.y;
                if (depth > 0 && towardCamera.y > .001)
                {
                    float2 atWater = input.localPosition.xz + towardCamera.xz * (depth / towardCamera.y);
                    clip(_DistrictHalfSize.xy - abs(atWater));
                }
                // Keep the entire artwork: wet bed, irregular stones and grass
                // share one continuous coordinate frame along the river.
                float across = input.across;
                float along = input.bank.y;
                float bend = input.bank.x;
                float macro = (ReachNoise(along * .81 + 1.7) - .5) * .05;
                float v = saturate(across + macro * sin(saturate(across) * 3.14159)
                    + bend * .035);
                fixed3 bank0 = Strip(_MainTex, float2(along, v));
                fixed3 bank1 = Strip(_EarthTex,
                    float2(-along * 1.03 + .31, v));
                fixed3 albedo;
                if (_BankVariantCount > 2.5)
                {
                    fixed3 bank2 = Strip(_BankTex2,
                        float2(along * .94 + .57, v));
                    fixed3 bank3 = Strip(_BankTex3,
                        float2(-along * 1.07 + .83, v));
                    float reach = along * .69 + 4.7 + _BankPatternOffset;
                    float cell = floor(reach);
                    float blend = smoothstep(.12, .88, frac(reach));
                    float first = floor(ReachHash(cell) * 4);
                    float second = floor(ReachHash(cell + 1) * 4);
                    float4 indices = float4(0, 1, 2, 3);
                    float4 firstWeight = 1 - saturate(abs(indices - first));
                    float4 secondWeight = 1 - saturate(abs(indices - second));
                    fixed3 firstBank = bank0 * firstWeight.x +
                        bank1 * firstWeight.y + bank2 * firstWeight.z +
                        bank3 * firstWeight.w;
                    fixed3 secondBank = bank0 * secondWeight.x +
                        bank1 * secondWeight.y + bank2 * secondWeight.z +
                        bank3 * secondWeight.w;
                    albedo = lerp(firstBank, secondBank, blend);
                }
                else
                {
                    float variant = clamp(.78 + .22 *
                        ReachNoise(along * .73 + 2.3) + bend * .06, .7, 1);
                    albedo = lerp(bank0, bank1, variant);
                }
                // The submerged floor continues in metres rather than clamping
                // the last image row, which would extrude pixels into streaks.
                float2 bedUv = input.localPosition.xz / 9;
                bedUv.y = .025 + frac(bedUv.y) * .15;
                fixed3 bed = Strip(_GravelTex, bedUv) *
                    _SubmergedBedBrightness;
                albedo = lerp(bed, albedo, smoothstep(
                    _SubmergedBlendStart, _SubmergedBlendEnd, across));
                float3 normal = normalize(input.worldNormal);
                fixed3 lighting = CityForgeWorldLighting(normal,
                    SHADOW_ATTENUATION(input));
                float broadNoise = EdgeNoise(input.worldMetres / 11);
                float fineNoise = EdgeNoise(input.worldMetres / 3.7 + 17.3);
                float edgeNoise = broadNoise * .68 + fineNoise * .32 - .5;
                float jitter = edgeNoise * _OuterFadeNoise;
                // Match the actual ground artwork before transparency reaches
                // zero, rather than ending on differently tinted baked grass.
                fixed3 terrain = tex2D(_TerrainTex,
                    input.worldMetres / max(.01, _TerrainWorldSize)).rgb;
                float terrainBlend = smoothstep(.68 + jitter * .25,
                    _OuterFadeEnd - .18 + jitter * .5, across) *
                    _TerrainBlendStrength;
                albedo = lerp(albedo, terrain, terrainBlend);
                albedo *= lighting * _Color.rgb;
                // Multi-scale irregularity breaks the long parallel contour;
                // physical bank and construction boundaries stay unchanged.
                float fadeStart = .64 + jitter * .35;
                float alpha = 1 - smoothstep(fadeStart,
                    _OuterFadeEnd + jitter, across);
                return fixed4(albedo, alpha * input.color.a * _Color.a);
            }
            ENDCG
        }
    }
}
