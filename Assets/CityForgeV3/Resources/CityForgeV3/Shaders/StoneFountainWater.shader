Shader "CityForgeV3/StoneFountainWater"
{
    Properties
    {
        _Color ("Water Color", Color) = (0.34, 0.47, 0.48, 0.75)
        _Opacity ("Placement Opacity", Range(0, 1)) = 1
        _Mode ("0 Pool, 1 Stream, 2 Splash", Float) = 0
        _Phase ("Motion Phase Override", Float) = -1
    }
    SubShader
    {
        Tags { "Queue"="Transparent+2" "RenderType"="Transparent" }
        Cull Off
        ZWrite Off
        ZTest LEqual
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct AppData
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };
            struct Varyings
            {
                float4 position : SV_POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            fixed4 _Color;
            float _Opacity;
            float _Mode;
            float _Phase;

            Varyings vert(AppData input)
            {
                Varyings output;
                output.position = UnityObjectToClipPos(input.vertex);
                output.uv = input.uv;
                output.color = input.color;
                return output;
            }

            fixed4 frag(Varyings input) : SV_Target
            {
                float time = _Phase < 0.0 ? _Time.y : _Phase;
                fixed3 rgb = _Color.rgb;
                float alpha = _Color.a * _Opacity * input.color.a;
                if (_Mode < 0.5)
                {
                    float radius = length(input.uv * 2.0 - 1.0);
                    float ripple = sin(radius * 39.0 - time * 5.0);
                    float fine = sin(input.uv.x * 27.0 + input.uv.y * 19.0
                        + time * 2.1);
                    rgb *= 0.92 + 0.06 * ripple + 0.035 * fine;
                    alpha *= 1.0 - 0.22 * smoothstep(0.78, 1.0, radius);
                }
                else if (_Mode < 1.5)
                {
                    float widthFade = 1.0 - abs(input.uv.x * 2.0 - 1.0);
                    // Stream UV.y grows from the upper lip (0) to the lower
                    // bowl (1). Subtracting time moves highlights toward 1.
                    float flow = sin(input.uv.y * 38.0 - time * 13.0);
                    rgb *= 0.91 + 0.15 * flow;
                    alpha *= (0.36 + 0.64 * widthFade)
                        * (0.82 + 0.18 * flow);
                }
                else
                {
                    float radius = length(input.uv * 2.0 - 1.0);
                    alpha *= pow(saturate(1.0 - radius), 1.5);
                }
                return fixed4(rgb * input.color.rgb, saturate(alpha));
            }
            ENDCG
        }
    }
}
