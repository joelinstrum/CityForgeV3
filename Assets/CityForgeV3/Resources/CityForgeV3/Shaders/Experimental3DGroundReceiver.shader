Shader "CityForgeV3/Experimental3DGroundReceiver"
{
    Properties
    {
        _Color ("Color", Color) = (1, 1, 1, 1)
        _MainTex ("Surface Texture", 2D) = "white" {}
        _DisplayMatch ("Chooser Display Match", Color) = (0.75, 0.80, 0.75, 1)
    }

    SubShader
    {
        Tags { "Queue" = "Geometry" "RenderType" = "Opaque" }
        Pass
        {
            ZWrite On
            Cull Back
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            fixed4 _DisplayMatch;
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; };
            v2f vert(appdata input)
            {
                v2f output;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                return output;
            }
            fixed4 frag(v2f input) : SV_Target
            {
                fixed4 artwork = tex2D(_MainTex, input.uv) * _Color;
                artwork.rgb *= _DisplayMatch.rgb;
                artwork.a = 1.0;
                return artwork;
            }
            ENDCG
        }
    }

    FallBack Off
}
