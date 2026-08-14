Shader "CityForgeV3/VehicleContactShadow"
{
    Properties { _Color ("Shadow", Color) = (0.008, 0.012, 0.016, 0.42) }
    SubShader
    {
        Tags { "Queue"="Geometry+4" "RenderType"="Transparent" }
        Cull Off
        ZWrite Off
        // Explicit road decal ordering is intentional: ordinary road artwork
        // otherwise rejects this plane, while the selected-road quad does not.
        // Visible vehicle materials draw afterward and cover the decal.
        ZTest Always
        Offset -8, -8
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct AppData { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };
            fixed4 _Color;
            Varyings vert(AppData input)
            {
                Varyings output;
                output.pos = UnityObjectToClipPos(input.vertex);
                output.uv = input.uv;
                return output;
            }
            fixed4 frag(Varyings input) : SV_Target
            {
                float2 centered = (input.uv - 0.5) * 2.0;
                float radius = dot(centered, centered);
                fixed alpha = _Color.a * saturate(1.0 - smoothstep(0.18, 1.0, radius));
                return fixed4(_Color.rgb, alpha);
            }
            ENDCG
        }
    }
    Fallback Off
}
