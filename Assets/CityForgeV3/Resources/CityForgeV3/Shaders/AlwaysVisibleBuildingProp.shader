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
        _UseWoodBackface ("Use Wood Backface", Range(0,1)) = 0
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest ("Depth Test", Float) = 4
    }
    SubShader
    {
        Tags { "Queue"="AlphaTest+5" "RenderType"="Opaque" }
        Cull Off
        ZWrite On
        // Committed attachments respect the host primitive's depth silhouette.
        ZTest [_ZTest]

        Pass
        {
            Tags { "LightMode"="ForwardBase" }
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase
            #include "UnityCG.cginc"
            #include "AutoLight.cginc"
            #include "CityForgeWorldLighting.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };
            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
                SHADOW_COORDS(2)
            };
            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            float _UseWoodBackface;

            v2f vert(appdata input)
            {
                v2f output;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.worldNormal = UnityObjectToWorldNormal(input.normal);
                TRANSFER_SHADOW(output);
                return output;
            }

            fixed4 frag(v2f input, fixed facing : VFACE) : SV_Target
            {
                // Imported sign lettering is authored on the front surface.
                // If that surface is viewed from behind, show opaque stained
                // wood rather than the mirrored front texture.
                fixed4 surface = facing < 0 && _UseWoodBackface > 0.5
                    ? fixed4(0.24, 0.13, 0.07, 1.0) * _Color
                    : tex2D(_MainTex, input.uv) * _Color;
                fixed shadow = SHADOW_ATTENUATION(input);
                surface.rgb *= CityForgeWorldLighting(
                    facing < 0 ? -input.worldNormal : input.worldNormal,
                    shadow);
                return surface;
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}
