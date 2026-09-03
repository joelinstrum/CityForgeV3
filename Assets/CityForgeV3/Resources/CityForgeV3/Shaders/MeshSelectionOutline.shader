Shader "CityForgeV3/MeshSelectionOutline"
{
    Properties
    {
        _Color ("Outline Color", Color) = (0.3, 0.82, 1, 0.95)
        _OutlineWidth ("Outline Width Metres", Float) = 0.035
    }
    SubShader
    {
        Tags { "Queue"="Geometry+60" "RenderType"="Opaque" }
        Pass
        {
            Cull Front
            ZWrite Off
            ZTest LEqual
            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float4 _Color;
            float _OutlineWidth;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 position : SV_POSITION;
            };

            v2f vert(appdata input)
            {
                v2f output;
                float3 worldPosition = mul(unity_ObjectToWorld,
                    input.vertex).xyz;
                float3 worldNormal = UnityObjectToWorldNormal(input.normal);
                output.position = UnityWorldToClipPos(worldPosition +
                    worldNormal * _OutlineWidth);
                return output;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                return _Color;
            }
            ENDCG
        }
    }
}
