Shader "CityForgeV3/AlwaysVisibleBuildingProp"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo", 2D) = "white" {}
        _BumpMap ("Normal Map", 2D) = "bump" {}
        _MetallicGlossMap ("Metallic", 2D) = "black" {}
        _Metallic ("Metallic", Range(0,1)) = 0.35
        _Glossiness ("Smoothness", Range(0,1)) = 0.32
    }
    SubShader
    {
        Tags { "Queue"="AlphaTest+5" "RenderType"="Opaque" }
        Cull Off
        ZWrite On
        ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };
            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };
            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;

            v2f vert(appdata input)
            {
                v2f output;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                return output;
            }

            fixed4 frag(v2f input, fixed facing : VFACE) : SV_Target
            {
                // Imported sign lettering is authored on the front surface.
                // If that surface is viewed from behind, show opaque stained
                // wood rather than the mirrored front texture.
                if (facing < 0)
                    return fixed4(0.24, 0.13, 0.07, 1.0) * _Color;
                return tex2D(_MainTex, input.uv) * _Color;
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}
