Shader "CityForgeV3/RiverWaterSurface"
{
    Properties
    {
        _MainTex ("Water Texture", 2D) = "white" {}
        _WhitecapTex ("Whitecap Texture", 2D) = "black" {}
        _Color ("Water Tint", Color) = (0.94,1.02,1.06,1)
        _Brightness ("Brightness", Range(0.1,2)) = 1.08
        _Smoothness ("Smoothness", Range(0,1)) = 0.62
        _CenterOpacity ("Center Opacity", Range(0,1)) = 0.72
        _EdgeOpacity ("Edge Opacity", Range(0,1)) = 0.08
        _DeepWaterStart ("Deep Water Start", Range(0,1)) = 0.24
        _DeepWaterStrength ("Deep Water Strength", Range(0,1)) = 0.58
        _DepthBlendSoftness ("Depth Blend Softness", Range(0.02,1)) = 0.56
        _FlowSpeed ("Flow Speed", Range(-0.1,0.1)) = 0.06
        _WaveDistortion ("Wave Distortion", Range(0,0.08)) = 0.025
        _WaveScale ("Wave Scale", Range(0.1,4)) = 0.75
        _WaveSpeed ("Wave Speed", Range(0,2)) = 0.28
        _ReflectionStrength ("Reflection Strength", Range(0,1)) = 0.18
        _ShimmerStrength ("Shimmer Strength", Range(0,1)) = 0.24
        _ShimmerSpeed ("Shimmer Speed", Range(0,2)) = 0.35
        _WhitecapStrength ("Whitecap Strength", Range(0,1)) = 0.32
        _WhitecapCoverage ("Whitecap Coverage", Range(0.05,1)) = 0.55
        _WhitecapTiling ("Whitecap Tiling", Range(0.1,4)) = 0.85
        _WhitecapSpeed ("Whitecap Speed", Range(0.1,3)) = 1.35
        _WhitecapPulseSpeed ("Whitecap Pulse Speed", Range(0,2)) = 0.12
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

            struct AppData
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            struct Varyings
            {
                float4 position : SV_POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
                float3 worldPosition : TEXCOORD1;
                float3 worldNormal : TEXCOORD2;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            sampler2D _WhitecapTex;
            fixed4 _Color;
            float _Brightness;
            float _Smoothness;
            float _CenterOpacity;
            float _EdgeOpacity;
            float _DeepWaterStart;
            float _DeepWaterStrength;
            float _DepthBlendSoftness;
            float _FlowSpeed;
            float _WaveDistortion;
            float _WaveScale;
            float _WaveSpeed;
            float _ReflectionStrength;
            float _ShimmerStrength;
            float _ShimmerSpeed;
            float _WhitecapStrength;
            float _WhitecapCoverage;
            float _WhitecapTiling;
            float _WhitecapSpeed;
            float _WhitecapPulseSpeed;

            Varyings vert(AppData input)
            {
                Varyings output;
                output.position = UnityObjectToClipPos(input.vertex);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.color = input.color;
                output.worldPosition = mul(unity_ObjectToWorld, input.vertex).xyz;
                output.worldNormal = UnityObjectToWorldNormal(input.normal);
                return output;
            }

            fixed4 frag(Varyings input) : SV_Target
            {
                // U is authored along the procedural centerline. Subtracting
                // time moves the visible texture in the +U/downstream
                // direction while every curved segment retains its local
                // tangent direction.
                float2 flowingUv = input.uv;
                flowingUv.x -= _Time.y * _FlowSpeed;
                // Two low-amplitude, differently phased UV bends keep the
                // river-relative downstream motion but prevent the artwork
                // from reading as one rigid sheet. This affects texture
                // sampling only; the flat water mesh remains unchanged.
                float waveTime = _Time.y * _WaveSpeed;
                float crossWarp = sin((input.uv.x * _WaveScale +
                    input.uv.y * 0.31) * 6.2831853 + waveTime);
                float alongWarp = sin((input.uv.y * (_WaveScale * 1.37) -
                    input.uv.x * 0.19) * 6.2831853 - waveTime * 0.73);
                flowingUv.y += crossWarp * _WaveDistortion;
                flowingUv.x += alongWarp * _WaveDistortion * 0.45;
                fixed4 water = tex2D(_MainTex, flowingUv) * _Color;
                float depthCoordinate = saturate(input.color.r);
                float halfSoftness = max(0.01, _DepthBlendSoftness * 0.5);
                float depthBlend = smoothstep(
                    _DeepWaterStart - halfSoftness,
                    _DeepWaterStart + halfSoftness,
                    depthCoordinate);
                float3 normal = normalize(input.worldNormal);
                float3 lightDirection = normalize(UnityWorldSpaceLightDir(
                    input.worldPosition));
                float3 viewDirection = normalize(UnityWorldSpaceViewDir(
                    input.worldPosition));
                float3 halfDirection = normalize(lightDirection + viewDirection);
                float diffuse = saturate(dot(normal, lightDirection));
                float specularPower = lerp(8.0, 128.0, _Smoothness);
                float specular = pow(saturate(dot(normal, halfDirection)),
                    specularPower) * lerp(0.08, 0.55, _Smoothness);
                // Preserve the supplied photographic water as the base color.
                // Lighting only nudges it slightly; the earlier full lighting
                // multiplication crushed the image into the dark riverbed.
                float lightResponse = lerp(0.86, 1.04, diffuse);
                water.rgb = water.rgb * _Brightness * lightResponse +
                            _LightColor0.rgb * specular;
                // Lightweight sky reflection keeps the supplied texture
                // visible while inheriting the current time-of-day lighting.
                float fresnel = pow(1.0 - saturate(dot(normal,
                    viewDirection)), 4.0);
                water.rgb += unity_AmbientSky.rgb * fresnel *
                    _ReflectionStrength;

                // Pull moving glints from bright detail already present in
                // the water photograph. This adds no geometry or flow map.
                float luminance = dot(water.rgb,
                    float3(0.2126, 0.7152, 0.0722));
                float waveHighlight = smoothstep(0.34, 0.76, luminance);
                float shimmerPulse = 0.72 + 0.28 * sin(
                    (flowingUv.x + flowingUv.y) * 28.0 +
                    _Time.y * _ShimmerSpeed);
                float lightGlance = lerp(0.22, 1.0, diffuse) *
                    lerp(0.35, 1.0, saturate(dot(normal, halfDirection)));
                water.rgb += _LightColor0.rgb * waveHighlight *
                    shimmerPulse * lightGlance * _ShimmerStrength;
                // Banks stay slightly lighter and transparent. The center
                // darkens and gains opacity through one broad smoothstep.
                float deepDarkening = lerp(1.0, 0.62, _DeepWaterStrength);
                water.rgb *= lerp(1.08, deepDarkening, depthBlend);

                // The authored transparent crest artwork is a second,
                // independently moving surface detail. It follows the same
                // river-relative U axis, with a slightly different speed and
                // scale so it does not lock to the base photograph. A spatial
                // pulse lets individual crests form and dissolve, while the
                // depth mask keeps whitecaps away from quiet bank water.
                float2 whitecapUv = input.uv * _WhitecapTiling;
                whitecapUv.x -= _Time.y * _FlowSpeed * _WhitecapSpeed;
                whitecapUv.y += crossWarp * _WaveDistortion * 1.15;
                whitecapUv.x += alongWarp * _WaveDistortion * 0.62;
                fixed4 whitecap = tex2D(_WhitecapTex, whitecapUv);
                float authoredCrest = whitecap.a * dot(whitecap.rgb,
                    float3(0.2126, 0.7152, 0.0722));
                // Each part of the authored foam has a complete lifetime:
                // fade in, remain briefly visible, then fade fully away. The
                // UV-derived phase prevents the entire river from blinking in
                // unison, while expressing speed as cycles per second makes
                // the control predictable.
                float crestCycle = 0.5 + 0.5 * sin(
                    (whitecapUv.x * 2.1 + whitecapUv.y * 3.7) * 6.2831853 +
                    _Time.y * _WhitecapPulseSpeed * 6.2831853);
                float crestLife = smoothstep(0.04, 0.34, crestCycle) *
                    (1.0 - smoothstep(0.68, 0.97, crestCycle));
                float crestThreshold = 1.0 - _WhitecapCoverage;
                float crestMask = smoothstep(crestThreshold, 1.0,
                    authoredCrest);
                crestMask *= crestLife;
                crestMask *= smoothstep(0.10, 0.48, depthCoordinate);
                float3 crestColor = lerp(float3(0.76, 0.84, 0.90),
                    float3(1.0, 0.98, 0.92), diffuse);
                water.rgb = lerp(water.rgb, crestColor,
                    crestMask * _WhitecapStrength);
                float deepOpacity = lerp(_CenterOpacity, 1.0,
                    _DeepWaterStrength * 0.12);
                water.a *= lerp(_EdgeOpacity, deepOpacity, depthBlend);
                return water;
            }
            ENDCG
        }
    }
}
