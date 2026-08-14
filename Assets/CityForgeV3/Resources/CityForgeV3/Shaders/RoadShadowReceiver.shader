Shader "CityForgeV3/RoadShadowReceiver"
{
    SubShader
    {
        Tags { "Queue"="Geometry+3" "RenderType"="Transparent" }
        Cull Off
        ZWrite Off
        ZTest LEqual
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            Tags { "LightMode"="ForwardBase" }
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase
            #include "UnityCG.cginc"
            #include "AutoLight.cginc"
            struct AppData { float4 vertex : POSITION; };
            struct Varyings { float4 pos : SV_POSITION; SHADOW_COORDS(0) };
            Varyings vert(AppData input)
            {
                Varyings output;
                output.pos = UnityObjectToClipPos(input.vertex);
                TRANSFER_SHADOW(output);
                return output;
            }
            fixed4 frag(Varyings input) : SV_Target
            {
                fixed shadow = SHADOW_ATTENUATION(input);
                return fixed4(0.012, 0.014, 0.016, (1.0 - shadow) * 0.62);
            }
            ENDCG
        }
    }
    Fallback Off
}
