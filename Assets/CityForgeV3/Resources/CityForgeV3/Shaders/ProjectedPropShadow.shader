Shader "CityForgeV3/ProjectedPropShadow"
{
    Properties
    {
        _Color ("Shadow Color", Color) = (0.035, 0.042, 0.05, 0.2)
        _ShadowDisplacement ("World Shadow Displacement", Vector) = (1, 0, 1, 0)
        _GroundY ("World Ground Height", Float) = 0.062
        _ReferenceHeight ("Reference Prop Height", Float) = 1.5
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            Offset -1, -1

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _Color;
            float4 _ShadowDisplacement;
            float _GroundY;
            float _ReferenceHeight;

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
            };

            v2f vert(appdata input)
            {
                v2f output;
                float3 world = mul(unity_ObjectToWorld, input.vertex).xyz;
                float heightRatio = saturate(
                    (world.y - _GroundY) / max(0.01, _ReferenceHeight));
                world.xz += _ShadowDisplacement.xz * heightRatio;
                world.y = _GroundY;
                output.vertex = UnityWorldToClipPos(world);
                return output;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                return _Color;
            }
            ENDCG
        }
    }
}
