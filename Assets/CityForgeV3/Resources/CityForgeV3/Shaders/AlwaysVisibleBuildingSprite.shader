Shader "CityForgeV3/AlwaysVisibleBuildingSprite"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
    }
    SubShader
    {
        // The solid proxy writes depth at AlphaTest-10. Draw the registered
        // building artwork next, then let alpha-tested flora draw over it only
        // where the proxy depth says the flora is actually camera-nearer.
        Tags { "Queue"="AlphaTest-5" "RenderType"="Transparent" "CanUseSpriteAtlas"="True" }
        Cull Off
        Lighting Off
        ZWrite Off
        ZTest Always
        Blend One OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex SpriteVert
            #pragma fragment SpriteFrag
            #pragma target 2.0
            #include "UnitySprites.cginc"
            ENDCG
        }
    }
}
