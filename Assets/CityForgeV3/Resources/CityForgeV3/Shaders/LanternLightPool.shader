Shader "CityForgeV3/LanternLightPool"
{
    Properties
    {
        _Color ("Warm Light", Color) = (1, 0.55, 0.20, 0.22)
    }

    SubShader
    {
        Tags { "Queue"="Transparent+20" "RenderType"="Transparent" }
        Pass
        {
            Blend SrcAlpha One
            Cull Off
            ZWrite Off
            ZTest LEqual

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct AppData { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; };
            fixed4 _Color;

            Varyings vert(AppData input)
            {
                Varyings output;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.uv = input.uv;
                return output;
            }

            fixed4 frag(Varyings input) : SV_Target
            {
                float2 centered = input.uv * 2.0 - 1.0;
                float radius = length(centered);
                clip(1.0 - radius);
                float falloff = smoothstep(1.0, 0.0, radius);
                falloff *= falloff;
                return fixed4(_Color.rgb, _Color.a * falloff);
            }
            ENDCG
        }
    }
}
