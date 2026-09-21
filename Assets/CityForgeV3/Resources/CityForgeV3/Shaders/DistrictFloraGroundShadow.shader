Shader "CityForgeV3/DistrictFloraGroundShadow"
{
    Properties
    {
        [PerRendererData] _MainTex ("Tree Silhouette", 2D) = "white" {}
        [PerRendererData] _Color ("Shadow Color", Color) = (0.018, 0.022, 0.026, 0.2)
        _DistrictHalfSize ("District Shadow Bounds", Vector) = (100000,100000,0,0)
        _Cutoff ("Alpha Cutoff", Range(0, 1)) = 0.12
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
            float4 _DistrictHalfSize;
            float4x4 _DistrictWorldToLocal;

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
                half edgeOpacity : TEXCOORD2;
                float2 receiverPosition : TEXCOORD3;
            };

            v2f vert(appdata input)
            {
                v2f output;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.uv = input.uv;
                output.receiverPosition = mul(_DistrictWorldToLocal, mul(unity_ObjectToWorld, input.vertex)).xz;
                output.heightRatio = input.color.a;
                output.edgeOpacity = input.color.r;
                return output;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                clip(_DistrictHalfSize.xy - abs(input.receiverPosition));
                // A ground-projected billboard is viewed at a steep angle.
                // Implicit sampling otherwise selects a coarse mip where the
                // tree's alpha averages across its transparent source card,
                // producing the detached black rectangles seen at close zoom.
                // Retain mipmaps, but bias this one shared silhouette sample
                // toward its authored cutout detail.
                half alpha = tex2Dbias(_MainTex,
                    float4(input.uv, 0, -1.5)).a;
                clip(alpha - _Cutoff);
                // Preserve trunk contact while gently losing density toward
                // the far canopy so the projection does not look stamped on.
                half distanceFade = lerp(1.0h, 0.62h,
                    smoothstep(0.48h, 1.0h, input.heightRatio));
                return fixed4(_Color.rgb,
                    alpha * _Color.a * distanceFade * input.edgeOpacity);
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
