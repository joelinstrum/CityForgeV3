Shader "CityForgeV3/FrontPriorityProp"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo", 2D) = "white" {}
        _BumpMap ("Normal Map", 2D) = "bump" {}
        _BumpScale ("Normal Scale", Range(0,2)) = 1
        _MetallicGlossMap ("Metallic", 2D) = "white" {}
        _Metallic ("Metallic", Range(0,1)) = 0
        _GlossMapScale ("Smoothness", Range(0,1)) = 0.5
        _EmissionMap ("Emission", 2D) = "black" {}
        _EmissionColor ("Emission Color", Color) = (0,0,0,0)
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="AlphaTest-5" }
        LOD 300
        ZWrite Off
        ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            #include "Lighting.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            sampler2D _EmissionMap;
            fixed4 _Color;
            fixed4 _EmissionColor;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 worldNormal : TEXCOORD0;
                float2 uv : TEXCOORD1;
            };

            v2f vert(appdata input)
            {
                v2f output;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.worldNormal = UnityObjectToWorldNormal(input.normal);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                return output;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                fixed4 albedo = tex2D(_MainTex, input.uv) * _Color;
                half3 normal = normalize(input.worldNormal);
                half diffuse = saturate(dot(normalize(_WorldSpaceLightPos0.xyz), normal));
                half3 ambient = ShadeSH9(half4(normal, 1.0));
                ambient = max(ambient, half3(0.5, 0.5, 0.5));
                half3 lit = albedo.rgb *
                    (ambient + _LightColor0.rgb * diffuse * 0.8);
                lit += _LightColor0.rgb * diffuse * 0.08;
                lit += tex2D(_EmissionMap, input.uv).rgb * _EmissionColor.rgb;
                return fixed4(lit, albedo.a);
            }
            ENDCG
        }
    }
    FallBack "Standard"
}
