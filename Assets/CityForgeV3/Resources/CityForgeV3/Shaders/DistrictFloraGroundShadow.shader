Shader "CityForgeV3/DistrictFloraGroundShadow"
{
    Properties
    {
        [PerRendererData] _MainTex ("Tree Silhouette", 2D) = "white" {}
        [PerRendererData] _Color ("Shadow Color", Color) = (0.018, 0.022, 0.026, 0.2)
        _Cutoff ("Alpha Cutoff", Range(0, 1)) = 0.02
    }
    SubShader
    {
        // Road artwork establishes its receiver stencil at Geometry+2. Draw
        // projections afterward so a second, road-only pass can restore the
        // same silhouette over the opaque brick pixels.
        Tags { "Queue"="Transparent+10" "RenderType"="Transparent" "CanUseSpriteAtlas"="True" }

        CGINCLUDE
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            fixed4 _Color;
            half _Cutoff;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                half heightRatio : TEXCOORD1;
            };

            v2f vert(appdata input)
            {
                v2f output;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.uv = input.uv;
                output.heightRatio = input.color.a;
                return output;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                half alpha = tex2D(_MainTex, input.uv).a;
                clip(alpha - _Cutoff);
                // Preserve trunk contact while gently losing density toward
                // the far canopy so the projection does not look stamped on.
                half distanceFade = lerp(1.0h, 0.62h,
                    smoothstep(0.48h, 1.0h, input.heightRatio));
                return fixed4(_Color.rgb,
                    alpha * _Color.a * distanceFade);
            }
        ENDCG

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Cull Off
            Offset -1, -1

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            ENDCG
        }

        // Brick roads write stencil bit 1 while drawing their visible pixels.
        // Reapply only there with an unconditional depth test: the road sits
        // slightly above the lot plane and would otherwise occlude a valid
        // ground projection. Buildings and other foreground art are excluded
        // because they do not carry this stencil bit.
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest Always
            Cull Off
            Stencil
            {
                Ref 1
                ReadMask 1
                Comp Equal
            }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            ENDCG
        }
    }
}
