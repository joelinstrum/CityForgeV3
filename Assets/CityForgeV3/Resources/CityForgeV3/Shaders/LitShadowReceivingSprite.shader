Shader "CityForgeV3/LitShadowReceivingSprite"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)
        _Cutoff ("Alpha Cutoff", Range(0, 1)) = 0.02
        _ShadowFloor ("Shadow Floor", Range(0, 1)) = 0.38
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
            half _Cutoff;
            half _ShadowFloor;

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
                SHADOW_COORDS(1)
            };

            v2f vert(appdata input)
            {
                v2f output;
                output.pos = UnityObjectToClipPos(input.vertex);
                output.uv = input.uv;
                output.color = input.color;
                TRANSFER_SHADOW(output);
                return output;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                fixed4 artwork = tex2D(_MainTex, input.uv) * input.color * _Color;
                clip(artwork.a - _Cutoff);
                half shadowAttenuation = SHADOW_ATTENUATION(input);
                half illumination = lerp(_ShadowFloor, 1.0h, shadowAttenuation);
                return fixed4(artwork.rgb * illumination, artwork.a);
            }
            ENDCG
        }
    }
    Fallback "Transparent/Cutout/VertexLit"
}
