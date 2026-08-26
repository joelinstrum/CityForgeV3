Shader "CityForgeV3/WetStreetReflection"
{
    Properties
    {
        _MainTex ("Building Artwork", 2D) = "white" {}
        _Wetness ("Wetness", Range(0,1)) = 0
        _RainActive ("Rain Active", Range(0,1)) = 0
    }
    SubShader
    {
        Tags { "Queue"="Geometry+10" "RenderType"="Transparent" }
        Cull Off
        ZWrite Off
        ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha
        // Require the road bit while rejecting any nearer building host ID.
        // Bit 1 is reserved for projected-shadow de-duplication.
        Stencil { Ref 1 ReadMask 253 WriteMask 0 Comp Equal Pass Keep }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct AppData { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };
            sampler2D _MainTex;
            float _Wetness;
            float _RainActive;
            Varyings vert(AppData input)
            {
                Varyings output;
                output.pos = UnityObjectToClipPos(input.vertex);
                output.uv = input.uv;
                return output;
            }
            fixed4 frag(Varyings input) : SV_Target
            {
                float ripple = sin(input.uv.y * 145.0 + _Time.y * 3.4) *
                    0.0022 * _RainActive;
                fixed4 reflected = tex2D(_MainTex,
                    input.uv + float2(ripple, 0));
                fixed luminance = dot(reflected.rgb,
                    fixed3(0.299, 0.587, 0.114));
                reflected.rgb = lerp(luminance, reflected.rgb, 0.38) *
                    fixed3(0.48, 0.52, 0.55);
                float reflectionOpacity = lerp(0.18, 0.036, _RainActive);
                reflected.a *= _Wetness * reflectionOpacity;
                return reflected;
            }
            ENDCG
        }
    }
    Fallback Off
}
