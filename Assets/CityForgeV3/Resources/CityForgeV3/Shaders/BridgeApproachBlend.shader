Shader "CityForgeV3/BridgeApproachBlend"
{
    Properties
    {
        _MainTex ("Road Artwork", 2D) = "white" {}
        _MaterialTiling ("Material Tiling", Float) = 5
        _TimeTint ("Time of Day Tint", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "Queue"="Geometry+4" "RenderType"="Transparent" }
        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            Tags { "LightMode"="ForwardBase" }
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase
            #include "UnityCG.cginc"
            #include "Lighting.cginc"
            #include "AutoLight.cginc"

            struct AppData
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };
            struct Varyings
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
                float3 worldPosition : TEXCOORD2;
                SHADOW_COORDS(1)
            };
            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _MaterialTiling;
            fixed4 _TimeTint;

            Varyings vert(AppData v)
            {
                Varyings output;
                output.pos=UnityObjectToClipPos(v.vertex);
                output.worldPosition=mul(unity_ObjectToWorld,v.vertex).xyz;
                output.uv=v.uv;
                output.color=v.color;
                TRANSFER_SHADOW(output);
                return output;
            }
            fixed4 frag(Varyings input) : SV_Target
            {
                fixed4 artwork=tex2D(_MainTex,input.uv);
                fixed alpha=artwork.a*input.color.a;
                clip(alpha-.02);
                fixed shadow=SHADOW_ATTENUATION(input);
                fixed illumination=lerp(.42,1.0,shadow);
                return fixed4(artwork.rgb*illumination*_TimeTint.rgb,alpha);
            }
            ENDCG
        }
    }
    Fallback Off
}
