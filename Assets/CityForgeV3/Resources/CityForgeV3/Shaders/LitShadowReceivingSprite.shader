Shader "CityForgeV3/LitShadowReceivingSprite"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        [PerRendererData] _FloraOpacity ("Tree Opacity", Float) = 1
        [PerRendererData] _FloraSaturation ("Tree Saturation", Float) = 1
        [PerRendererData] _FloraBaseEllipse ("Trunk Base Ellipse", Vector) = (0,0,0,0)
        [PerRendererData] _ForestPalette ("District Forest Palette", Float) = 0
        _Color ("Tint", Color) = (1, 1, 1, 1)
        _Cutoff ("Alpha Cutoff", Range(0, 1)) = 0.02
        _ShadowFloor ("Shadow Floor", Range(0, 1)) = 0.38
        [PerRendererData] _GroundFadeEnabled ("Ground Fade Enabled", Float) = 0
        [PerRendererData] _GroundY ("Ground Height", Float) = 0.02
        [PerRendererData] _GroundFadeWidth ("Ground Fade Width", Float) = 0.42
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest ("Depth Test", Float) = 4
    }

    SubShader
    {
        Tags { "Queue"="AlphaTest" "RenderType"="TransparentCutout" "CanUseSpriteAtlas"="True" }
        Cull Off
        ZTest [_ZTest]
        // Keep the established depth-writing cutout contract, but blend the
        // antialiased edge pixels that survive clipping. Without blending,
        // low-alpha RGB is written as fully opaque and reads as a dark stroke.
        ZWrite On
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Tags { "LightMode"="ForwardBase" }
            // A billboard pixel that wins ordinary depth becomes the nearest
            // flora surface at that pixel. Clear only the building-host bits
            // there so a later front-recovery pass cannot draw a farther tree
            // through this one. Road and projected-shadow bits are preserved.
            Stencil
            {
                Ref 0
                WriteMask 252
                Comp Always
                Pass Replace
                ZFail Keep
            }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile_fwdbase

            #include "UnityCG.cginc"
            #include "AutoLight.cginc"

            sampler2D _MainTex;
            fixed4 _Color;
            half _FloraSaturation;
            half _ForestPalette;
            float4 _FloraBaseEllipse;
            half _FloraOpacity;
            half _Cutoff;
            half _ShadowFloor;
            half _GroundFadeEnabled;
            float _GroundY;
            float _GroundFadeWidth;
            float4 _CFCloudShadowCenter;
            float4 _CFCloudShadowParams;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
                float3 worldPosition : TEXCOORD2;
                SHADOW_COORDS(1)
            };

            v2f vert(appdata input)
            {
                v2f output;
                output.pos = UnityObjectToClipPos(input.vertex);
                output.uv = input.uv;
                output.color = input.color;
                output.worldPosition = mul(unity_ObjectToWorld, input.vertex).xyz;
                TRANSFER_SHADOW(output);
                return output;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                fixed4 artwork = tex2D(_MainTex, input.uv) * input.color * _Color;
                if (_ForestPalette > .5h)
                {
                    // Continuous world-space patches survive batching, reload,
                    // zoom and regeneration; no per-frame CPU work or new textures.
                    float2 p = input.worldPosition.xz;
                    half patch = saturate(.5h + .28h * sin(p.x * .041 + p.y * .027)
                        + .22h * sin(p.x * -.019 + p.y * .063));
                    half leaf = smoothstep(.015h, .11h, min(artwork.g - artwork.r * .85h, artwork.g - artwork.b));
                    half3 tint = lerp(half3(.82h, .94h, .78h), half3(.79h, 1.02h, 1.10h), patch);
                    tint = lerp(tint, half3(1.02h, 1.02h, .90h), smoothstep(.76h, 1.0h, patch) * .45h);
                    if (_ForestPalette > 1.5h)
                        artwork.rgb = lerp(artwork.rgb, artwork.rgb * half3(1.12h, 1.17h, 1.10h) + .018h, leaf);
                    else artwork.rgb *= lerp(half3(1,1,1), tint, leaf);
                }
                half luminance = dot(artwork.rgb, half3(0.2126h,0.7152h,0.0722h));
                artwork.rgb = max(0, lerp(luminance.xxx, artwork.rgb, _FloraSaturation));
                if (_FloraBaseEllipse.w > 0 && input.uv.y < _FloraBaseEllipse.y + _FloraBaseEllipse.w)
                {
                    float x = (input.uv.x - _FloraBaseEllipse.x) / _FloraBaseEllipse.z;
                    float edge = _FloraBaseEllipse.y + _FloraBaseEllipse.w *
                        (1 - sqrt(saturate(1 - x*x)));
                    artwork.a *= smoothstep(edge - 0.00065, edge + 0.00065, input.uv.y);
                }
                artwork.a = saturate(artwork.a * _FloraOpacity);
                half groundFade = smoothstep(_GroundY - _GroundFadeWidth,
                    _GroundY + _GroundFadeWidth, input.worldPosition.y);
                artwork.a *= lerp(1.0h, groundFade, _GroundFadeEnabled);
                clip(artwork.a - _Cutoff);
                half shadowAttenuation = SHADOW_ATTENUATION(input);
                half illumination = lerp(_ShadowFloor, 1.0h, shadowAttenuation);
                half cloudDistance = distance(input.worldPosition.xz,
                    _CFCloudShadowCenter.xy);
                half cloudMask = (1.0h - smoothstep(
                    _CFCloudShadowParams.x * (1.0h - _CFCloudShadowParams.z),
                    _CFCloudShadowParams.x, cloudDistance)) *
                    _CFCloudShadowParams.w;
                illumination *= 1.0h - cloudMask * _CFCloudShadowParams.y;
                return fixed4(artwork.rgb * illumination, artwork.a);
            }
            ENDCG
        }
    }
    Fallback "Transparent/Cutout/VertexLit"
}
