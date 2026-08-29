Shader "CityForgeV3/ProjectedBuildingMeshShadow"
{
    Properties
    {
        _Color ("Shadow Color", Color) = (0.035, 0.042, 0.05, 0.16)
        _ShadowDisplacement ("World Shadow Displacement", Vector) = (1, 0, 1, 0)
        _GroundY ("World Ground Height", Float) = 0.062
        _ReferenceHeight ("Reference Building Height", Float) = 10.0
        _LotHalfExtents ("Lot Half Extents XZ", Vector) = (40, 40, 0, 0)
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
            Stencil
            {
                // Bit 0 is written by semantic road pixels. Manual projection
                // is only needed by the unlit experimental grass receiver;
                // roads receive the live Unity shadow map and must not get this
                // second, baked-looking copy. Bit 1 still prevents overlapping
                // projected mesh fragments from darkening each other.
                Ref 0
                ReadMask 3
                WriteMask 2
                Comp Equal
                Pass Invert
            }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _Color;
            float4 _ShadowDisplacement;
            float _GroundY;
            float _ReferenceHeight;
            float4 _LotHalfExtents;

            struct appdata { float4 vertex : POSITION; };
            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 worldXZ : TEXCOORD0;
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
                output.worldXZ = world.xz;
                return output;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                clip(_LotHalfExtents.x - abs(input.worldXZ.x));
                clip(_LotHalfExtents.y - abs(input.worldXZ.y));
                return _Color;
            }
            ENDCG
        }
    }
}
