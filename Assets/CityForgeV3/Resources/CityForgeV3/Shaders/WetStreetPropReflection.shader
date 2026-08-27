Shader "CityForgeV3/WetStreetPropReflection"
{
    Properties
    {
        _MainTex ("Prop Texture", 2D) = "white" {}
        _SourceTint ("Source Material Tint", Color) = (1,1,1,1)
        _Wetness ("Wetness", Range(0,1)) = 0
        _RainActive ("Rain Active", Range(0,1)) = 0
        _ReflectionDirection ("World Reflection Direction", Vector) = (0,0,-1,0)
        _GroundY ("Road Height", Float) = 0.061
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
            fixed4 _SourceTint;
            float4 _ReflectionDirection;
            float _GroundY;
            Varyings vert(AppData input)
            {
                Varyings output;
                float3 world = mul(unity_ObjectToWorld, input.vertex).xyz;
                float3 origin = mul(unity_ObjectToWorld,
                    float4(0.0, 0.0, 0.0, 1.0)).xyz;
                float height = max(0.0, world.y - _GroundY);
                float2 direction = normalize(_ReflectionDirection.xz);
                float2 side = float2(-direction.y, direction.x);
                float sideways = dot(world.xz - origin.xz, side);
                // Keep recognizable width without letting detailed prop mesh
                // branches produce a fan of competing diagonal reflections.
                world.xz -= side * sideways * 0.62;
                world.xz += direction * height;
                world.y = _GroundY;
                output.pos = UnityWorldToClipPos(world);
                output.uv = input.uv;
                return output;
            }
            fixed4 frag(Varyings input) : SV_Target
            {
                float ripple = sin(input.uv.y * 145.0 + _Time.y * 3.4) *
                    0.0022 * _RainActive;
                fixed4 reflected = tex2D(_MainTex, input.uv + float2(ripple, 0));
                reflected *= _SourceTint;
                fixed luminance = dot(reflected.rgb, fixed3(0.299, 0.587, 0.114));
                reflected.rgb = lerp(luminance, reflected.rgb, 0.30) *
                    fixed3(0.44, 0.48, 0.51);
                reflected.a *= _Wetness * lerp(0.15, 0.03, _RainActive);
                return reflected;
            }
            ENDCG
        }
    }
    Fallback Off
}
