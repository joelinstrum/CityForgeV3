Shader "CityForgeV3/SoftCloud"
{
    Properties
    {
        _Color ("Cloud Color", Color) = (0.94, 0.97, 1, 0.30)
        _EdgeSoftness ("Edge Softness", Range(0.05, 0.8)) = 0.22
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Cull Back
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _Color;
            half _EdgeSoftness;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldNormal : TEXCOORD0;
                float3 worldPosition : TEXCOORD1;
            };

            v2f vert(appdata input)
            {
                v2f output;
                output.pos = UnityObjectToClipPos(input.vertex);
                output.worldNormal = UnityObjectToWorldNormal(input.normal);
                output.worldPosition = mul(unity_ObjectToWorld,
                    input.vertex).xyz;
                return output;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                half3 normal = normalize(input.worldNormal);
                half3 viewDirection = normalize(_WorldSpaceCameraPos.xyz -
                    input.worldPosition);
                half facing = saturate(dot(normal, viewDirection));
                half feather = smoothstep(0.0h, _EdgeSoftness, facing);
                half softLight = lerp(0.78h, 1.0h,
                    saturate(normal.y * 0.5h + 0.5h));
                return fixed4(_Color.rgb * softLight,
                    _Color.a * feather);
            }
            ENDCG
        }
    }
    Fallback Off
}
