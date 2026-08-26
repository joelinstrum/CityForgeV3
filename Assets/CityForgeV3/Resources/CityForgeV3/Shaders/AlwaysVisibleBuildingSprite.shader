Shader "CityForgeV3/AlwaysVisibleBuildingSprite"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Cutoff ("Alpha Cutoff", Range(0,1)) = 0.02
        [IntRange] _BuildingHostStencilRef
            ("Host Building Stencil Ref", Range(0,252)) = 0
    }
    SubShader
    {
        // The solid proxy writes depth and its building ID at AlphaTest-10.
        // First draw artwork that passes ordinary scene depth, then recover
        // only pixels hidden by this artwork's own semantic proxy.
        Tags { "Queue"="AlphaTest-5" "RenderType"="Transparent" "CanUseSpriteAtlas"="True" }
        Cull Off
        Lighting Off

        CGINCLUDE
        #include "UnitySprites.cginc"
        half _Cutoff;
        half _BuildingHostStencilRef;

        fixed4 BuildingSpriteFrag(v2f input) : SV_Target
        {
            fixed4 color = SampleSpriteTexture(input.texcoord) * input.color;
            clip(color.a - _Cutoff);
            color.rgb *= color.a;
            return color;
        }

        fixed4 BuildingSpriteRecoveryFrag(v2f input) : SV_Target
        {
            // An unbound presentation must never recover through stencil 0.
            clip(_BuildingHostStencilRef - 0.5h);
            return BuildingSpriteFrag(input);
        }
        ENDCG

        // Respect roads, props, vehicles, and every unrelated building.
        Pass
        {
            ZWrite Off
            ZTest LEqual
            Blend One OneMinusSrcAlpha

            CGPROGRAM
            #pragma vertex SpriteVert
            #pragma fragment BuildingSpriteFrag
            #pragma target 2.0
            ENDCG
        }

        // The billboard is decorative skin for its own semantic massing. Let
        // it show through that proxy, but never through another building ID.
        Pass
        {
            ZWrite Off
            ZTest Greater
            Blend One OneMinusSrcAlpha
            Stencil
            {
                Ref [_BuildingHostStencilRef]
                ReadMask 252
                WriteMask 0
                Comp Equal
                Pass Keep
            }

            CGPROGRAM
            #pragma vertex SpriteVert
            #pragma fragment BuildingSpriteRecoveryFrag
            #pragma target 2.0
            ENDCG
        }
    }
}
