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
        _SubmergedOpacity ("Near Submerged Opacity", Range(0,1)) = 0.60
        _SubmergedFadeStart ("Submerged Fade Start", Range(0,4)) = 0.12
        _SubmergedFadeEnd ("Submerged Fade End", Range(0.2,12)) = 2.2
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
            #include "CityForgeWorldLighting.cginc"

            struct AppData
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
                float2 flow : TEXCOORD1;
                fixed4 color : COLOR;
            };

            struct Varyings
            {
                float4 position : SV_POSITION;
                float2 uv : TEXCOORD0;
                float2 flow : TEXCOORD3;
                fixed4 color : COLOR;
                float3 worldPosition : TEXCOORD1;
                float3 worldNormal : TEXCOORD2;
                float4 screenPosition : TEXCOORD4;
                float eyeDepth : TEXCOORD5;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            sampler2D _WhitecapTex;
            UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);
            fixed4 _Color;
            float _Brightness;
            float _Smoothness;
            float _CenterOpacity;
            float _EdgeOpacity;
            float _DeepWaterStart;
            float _DeepWaterStrength;
            float _DepthBlendSoftness;
            float _SubmergedOpacity;
            float _SubmergedFadeStart;
            float _SubmergedFadeEnd;
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
                output.flow = input.flow;
                output.worldPosition = mul(unity_ObjectToWorld, input.vertex).xyz;
                output.worldNormal = UnityObjectToWorldNormal(input.normal);
                output.screenPosition = ComputeScreenPos(output.position);
                output.eyeDepth = -UnityObjectToViewPos(input.vertex).z;
                return output;
            }

            fixed4 frag(Varyings input) : SV_Target
            {
                // Keep metre-scaled district UVs; advect along the local river
                // tangent. Two fading phases prevent unlimited bend distortion.
                float2 flow = input.flow / max(length(input.flow), 0.0001);
                float phase = frac(_Time.y * _FlowSpeed * 0.25);
                float phaseB = frac(phase + 0.5);
                float blend = abs(phase * 2.0 - 1.0);
                float2 flowingUv = input.uv - flow * phase * 4.0;
                float2 flowingUvB = input.uv - flow * phaseB * 4.0;
                float waveTime = _Time.y * _WaveSpeed;
                float alongWarp = sin(dot(input.uv, flow) * _WaveScale * 6.2831853 - waveTime);
                flowingUv += flow * alongWarp * _WaveDistortion;
                flowingUvB += flow * alongWarp * _WaveDistortion;
                fixed4 water = lerp(tex2D(_MainTex, flowingUv),
                    tex2D(_MainTex, flowingUvB), blend) * _Color;
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
                fixed3 worldLighting = CityForgeWorldLighting(normal, 1.0);
                water.rgb = water.rgb * _Brightness * worldLighting +
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
                float crestPhase = frac(_Time.y * _FlowSpeed * _WhitecapSpeed * 0.25);
                float crestPhaseB = frac(crestPhase + 0.5);
                float2 whitecapUv = input.uv * _WhitecapTiling - flow * crestPhase * 4.0;
                float2 whitecapUvB = input.uv * _WhitecapTiling - flow * crestPhaseB * 4.0;
                fixed4 whitecap = lerp(tex2D(_WhitecapTex, whitecapUv),
                    tex2D(_WhitecapTex, whitecapUvB), abs(crestPhase * 2.0 - 1.0));
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
                // Fade by the actual distance from this water pixel to the
                // opaque surface behind it. Stone immediately under the
                // surface remains visible; deeper riverbed and foundations
                // progressively regain the authored water opacity.
                float sceneEyeDepth = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE_PROJ(
                    _CameraDepthTexture, UNITY_PROJ_COORD(input.screenPosition)));
                float submergedDepth = max(0.0, sceneEyeDepth - input.eyeDepth);
                float submergedFade = smoothstep(_SubmergedFadeStart,
                    max(_SubmergedFadeStart + 0.01, _SubmergedFadeEnd),
                    submergedDepth);
                water.a *= lerp(_SubmergedOpacity, 1.0, submergedFade);
                // Junction processing writes a longitudinal fade into vertex
                // alpha so tributaries dissolve cleanly into wider rivers.
                // Red remains reserved for the cross-channel depth profile.
                water.a *= input.color.a;
                return water;
            }
            ENDCG
        }
    }
}
