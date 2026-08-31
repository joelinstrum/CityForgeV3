Shader "CityForgeV3/WindowLightGlow"
{
    Properties
    {
        _Color ("Light Color", Color) = (1, 0.64, 0.28, 0.75)
        _EmissionStrength ("Emission Strength", Range(0, 8)) = 2.5
    }

    SubShader
    {
        Tags { "Queue"="Transparent+120" "RenderType"="Transparent" }
        Cull Off
        ZWrite Off
        Blend SrcAlpha One

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct AppData
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct VertexToFragment
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            fixed4 _Color;
            float _EmissionStrength;

            VertexToFragment vert(AppData input)
            {
                VertexToFragment output;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.uv = input.uv;
                return output;
            }

            fixed4 frag(VertexToFragment input) : SV_Target
            {
                // Feather all four edges and gently vary the interior so this
                // reads as light passing through glass, not a solid card.
                float2 edgeDistance = min(input.uv, 1.0 - input.uv);
                float edge = smoothstep(0.0, 0.24, min(edgeDistance.x,
                    edgeDistance.y));
                float2 centered = input.uv - 0.5;
                float centerGlow = 1.0 - saturate(dot(centered, centered) * 1.1);
                float glassVariation = 0.86 +
                    0.08 * sin(input.uv.x * 19.0 + input.uv.y * 7.0) +
                    0.06 * sin(input.uv.y * 31.0);
                float alpha = _Color.a * edge * centerGlow * glassVariation;
                return fixed4(_Color.rgb * _EmissionStrength, alpha);
            }
            ENDCG
        }
    }
}
