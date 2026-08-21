Shader "CityForge/SnowAccumulation"
{
    Properties
    {
        _MainTex ("Snow Coverage", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Accumulation ("Accumulation", Range(0,1)) = 0.01
    }

    SubShader
    {
        Tags
        {
            "Queue"="Geometry+1"
            "RenderType"="Transparent"
            "IgnoreProjector"="True"
        }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

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
            float _Accumulation;

            v2f vert(appdata input)
            {
                v2f output;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                return output;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                fixed4 snow = tex2D(_MainTex, input.uv) * _Color;
                snow.a *= saturate(_Accumulation);
                return snow;
            }
            ENDCG
        }
    }
}
