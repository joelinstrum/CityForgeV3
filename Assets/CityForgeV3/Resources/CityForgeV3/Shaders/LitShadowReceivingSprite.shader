Shader "CityForgeV3/LitShadowReceivingSprite"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)
        _Cutoff ("Alpha Cutoff", Range(0, 1)) = 0.02
        _ShadowFloor ("Shadow Floor", Range(0, 1)) = 0.38
    }

    SubShader
    {
        Tags { "Queue"="AlphaTest" "RenderType"="TransparentCutout" "CanUseSpriteAtlas"="True" }
        Cull Off

        CGPROGRAM
        #pragma surface surf ShadowOnly alphatest:_Cutoff fullforwardshadows
        #pragma target 3.0

        sampler2D _MainTex;
        fixed4 _Color;
        half _ShadowFloor;

        struct Input
        {
            float2 uv_MainTex;
            fixed4 color : COLOR;
        };

        void surf(Input input, inout SurfaceOutput output)
        {
            fixed4 artwork = tex2D(_MainTex, input.uv_MainTex) * input.color * _Color;
            output.Albedo = artwork.rgb;
            output.Alpha = artwork.a;
        }

        half4 LightingShadowOnly(SurfaceOutput surface, half3 lightDirection, half attenuation)
        {
            half illumination = lerp(_ShadowFloor, 1.0h, attenuation);
            return half4(surface.Albedo * illumination, surface.Alpha);
        }
        ENDCG
    }
    Fallback "Transparent/Cutout/VertexLit"
}
