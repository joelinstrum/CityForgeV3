Shader "CityForgeV3/RiverBankSurface"
{
    Properties
    {
        _MainTex ("Grass and pebble bank", 2D) = "white" {}
        _GravelTex ("Submerged fine gravel", 2D) = "white" {}
        _EarthTex ("Alternate shoreline composition", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _DistrictHalfSize ("District half size", Vector) = (100000,100000,0,0)
        _RiverWaterLevel ("Water level", Float) = -0.1
        _BankTop ("Top of bank", Float) = 0.186
        _DetailMeters ("Material repeat metres", Float) = 48
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
                SHADOW_COORDS(4)
            };
            sampler2D _MainTex, _GravelTex, _EarthTex;
            fixed4 _Color;
            float4 _DistrictHalfSize;
            float _RiverWaterLevel, _BankTop, _DetailMeters;
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
                float variant = clamp(.78 + .22 * ReachNoise(along * .73 + 2.3) + bend * .06, .7, 1);
                fixed3 albedo = lerp(Strip(_MainTex, float2(along, v)),
                    Strip(_EarthTex, float2(along + .31, v)), variant);
                // The submerged floor continues in metres rather than clamping
                // the last image row, which would extrude pixels into streaks.
                float2 bedUv = input.localPosition.xz / 9;
                bedUv.y = .025 + frac(bedUv.y) * .15;
                fixed3 bed = Strip(_GravelTex, bedUv) * .8;
                albedo = lerp(bed, albedo, smoothstep(.015, .13, across));
                float3 normal = normalize(input.worldNormal);
                fixed3 lighting = CityForgeWorldLighting(normal,
                    SHADOW_ATTENUATION(input));
                albedo *= lighting * _Color.rgb;
                // Reveal the real terrain at the grass boundary rather than
                // ending on a hard strip of differently coloured baked grass.
                float fadeStart = .72 + .025 * (ReachNoise(along * 2.3) - .5);
                float alpha = 1 - smoothstep(fadeStart, .833, across);
                return fixed4(albedo, alpha * input.color.a * _Color.a);
            }
            ENDCG
        }
    }
}
