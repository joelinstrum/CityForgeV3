Shader "CityForgeV3/WorldColor"
{
    Properties
    {
        _Color ("Color", Color) = (1, 1, 1, 1)
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Geometry"
            "RenderType" = "Opaque"
        }

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite On

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "CityForgeWorldLighting.cginc"

            struct AppData
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct VertexToFragment
            {
                float4 vertex : SV_POSITION;
                float3 worldNormal : TEXCOORD0;
            };

            fixed4 _Color;

            VertexToFragment vert(AppData input)
            {
                VertexToFragment output;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.worldNormal = UnityObjectToWorldNormal(input.normal);
                return output;
            }

            fixed4 frag(VertexToFragment input) : SV_Target
            {
                return fixed4(_Color.rgb * CityForgeWorldLighting(
                    input.worldNormal, 1.0h), _Color.a);
            }
            ENDCG
        }

        UsePass "Legacy Shaders/VertexLit/SHADOWCASTER"
    }
}
