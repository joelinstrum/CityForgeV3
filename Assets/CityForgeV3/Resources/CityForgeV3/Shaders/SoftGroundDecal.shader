Shader "CityForgeV3/SoftGroundDecal"
{
    Properties
    {
        _MainTex ("Decal", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _EdgeFade ("Edge Fade", Range(0.01, 0.45)) = 0.18
    }
    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "IgnoreProjector"="True"
        }
        Cull Off
        ZWrite Off
        ZTest LEqual
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 position : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            float _EdgeFade;
            int _EraseMarkCount;
            float4 _EraseMarks[32];

            v2f vert(appdata input)
            {
                v2f output;
                output.position = UnityObjectToClipPos(input.vertex);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                return output;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                fixed4 decal = tex2D(_MainTex, input.uv) * _Color;
                float2 distanceToEdge = min(input.uv, 1.0 - input.uv);
                float nearestEdge = min(distanceToEdge.x, distanceToEdge.y);
                decal.a *= smoothstep(0.0, _EdgeFade, nearestEdge);
                for (int markIndex = 0; markIndex < 32; markIndex++)
                {
                    if (markIndex >= _EraseMarkCount) break;
                    float2 radii = max(_EraseMarks[markIndex].zw,
                        float2(0.0001, 0.0001));
                    float eraseDistance = length(
                        (input.uv - _EraseMarks[markIndex].xy) / radii);
                    decal.a *= smoothstep(0.72, 1.0, eraseDistance);
                }
                return decal;
            }
            ENDCG
        }
    }
}
