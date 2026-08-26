Shader "CityForgeV3/FrontFacadeLitShadowReceivingSprite"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)
        _Cutoff ("Alpha Cutoff", Range(0, 1)) = 0.02
        _ShadowFloor ("Shadow Floor", Range(0, 1)) = 0.38
        [Enum(UnityEngine.Rendering.CullMode)] _Cull
            ("Cull Mode", Float) = 0
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest
            ("Depth Test", Float) = 4
        [IntRange] _BuildingHostStencilRef
            ("Host Building Stencil Ref", Range(0,252)) = 0
        [IntRange] _BuildingHostStencilRef2
            ("Host Building Stencil Ref 2", Range(0,252)) = 0
        [IntRange] _BuildingHostStencilRef3
            ("Host Building Stencil Ref 3", Range(0,252)) = 0
        [IntRange] _BuildingHostStencilRef4
            ("Host Building Stencil Ref 4", Range(0,252)) = 0
    }

    SubShader
    {
        Tags { "Queue"="AlphaTest" "RenderType"="TransparentCutout" "CanUseSpriteAtlas"="True" }
        Cull [_Cull]

        CGINCLUDE
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

        // Ordinary physical-depth pass. This preserves every approved side,
        // back, ground, prop, and unrelated-building occlusion.
        Pass
        {
            Tags { "LightMode"="ForwardBase" }
            ZTest [_ZTest]
            ZWrite On
            Blend SrcAlpha OneMinusSrcAlpha
            // Clear a host ID only where this flora pixel actually passes
            // ordinary depth. Pixels hidden by the host retain its ID for the
            // recovery pass below, while nearer flora continues to occlude
            // farther flora normally.
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
            ENDCG
        }

        Pass
        {
            Tags { "LightMode"="Always" }
            ZTest Greater
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha
            Stencil
            {
                Ref [_BuildingHostStencilRef2]
                ReadMask 252
                WriteMask 0
                Comp Equal
                Pass Keep
            }
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile_fwdbase
            ENDCG
        }

        Pass
        {
            Tags { "LightMode"="Always" }
            ZTest Greater
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha
            Stencil
            {
                Ref [_BuildingHostStencilRef3]
                ReadMask 252
                WriteMask 0
                Comp Equal
                Pass Keep
            }
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile_fwdbase
            ENDCG
        }

        Pass
        {
            Tags { "LightMode"="Always" }
            ZTest Greater
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha
            Stencil
            {
                Ref [_BuildingHostStencilRef4]
                ReadMask 252
                WriteMask 0
                Comp Equal
                Pass Keep
            }
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile_fwdbase
            ENDCG
        }

        // Recover only the pixels that failed ordinary depth against this
        // flora item's authored host. The nearest semantic wall/roof proxy
        // owns the upper stencil bits, so unrelated nearer geometry still wins.
        Pass
        {
            Tags { "LightMode"="Always" }
            ZTest Greater
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha
            Stencil
            {
                Ref [_BuildingHostStencilRef]
                ReadMask 252
                WriteMask 0
                Comp Equal
                Pass Keep
            }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile_fwdbase
            ENDCG
        }
    }
    Fallback "Transparent/Cutout/VertexLit"
}
