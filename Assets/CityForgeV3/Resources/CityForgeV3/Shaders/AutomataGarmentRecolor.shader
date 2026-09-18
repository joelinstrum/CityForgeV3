Shader "CityForgeV3/AutomataGarmentRecolor"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _MaskTex ("Garment mask", 2D) = "black" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _RecolorOne ("First garment", Color) = (1,1,1,1)
        _RecolorTwo ("Second garment", Color) = (1,1,1,1)
        _RecolorOneMix ("First garment mix", Float) = 0
        _RecolorTwoMix ("Second garment mix", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent"
               "IgnoreProjector"="True" "CanUseSpriteAtlas"="True" }
        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };
            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
            };
            sampler2D _MainTex;
            sampler2D _MaskTex;
            fixed4 _Color;
            fixed4 _RecolorOne;
            fixed4 _RecolorTwo;
            float _RecolorOneMix;
            float _RecolorTwoMix;
            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color * _Color;
                return o;
            }
            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 original = tex2D(_MainTex, i.uv);
                fixed2 mask = tex2D(_MaskTex, i.uv).rg;
                float shade = dot(original.rgb, float3(.2126, .7152, .0722));
                // Normalize separately for the burgundy dress and charcoal
                // coat; the baked light/shadow variation stays in the sheet.
                float3 dress = saturate(_RecolorOne.rgb * (shade / .18));
                float3 coat = saturate(_RecolorTwo.rgb * (shade / .16));
                original.rgb = lerp(original.rgb, dress,
                    saturate(mask.r * _RecolorOneMix));
                original.rgb = lerp(original.rgb, coat,
                    saturate(mask.g * _RecolorTwoMix));
                return original * i.color;
            }
            ENDCG
        }
    }
}
