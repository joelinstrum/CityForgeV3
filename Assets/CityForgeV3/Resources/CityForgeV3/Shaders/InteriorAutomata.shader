Shader "CityForge/Interior Automata"
{
    Properties
    {
        [PerRendererData] _MainTex ("Shared frames", 2D) = "white" {}
        _EmissionColor ("Night room illumination", Color) = (0,0,0,0)
        _RoomMin ("Room minimum", Vector) = (-4.7,4.13,-3.95,0)
        _RoomMax ("Room maximum", Vector) = (5.4,6.71,2.12,0)
    }
    SubShader
    {
        Tags { "Queue"="AlphaTest" "RenderType"="TransparentCutout" "CanUseSpriteAtlas"="True" }
        Cull Off ZWrite On ZTest LEqual
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "CityForgeWorldLighting.cginc"
            sampler2D _MainTex;
            fixed4 _EmissionColor;
            float4x4 _WorldToRoom;
            float4 _RoomMin, _RoomMax;
            struct input { float4 vertex:POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; };
            struct output { float4 vertex:SV_POSITION; float2 uv:TEXCOORD0; float3 room:TEXCOORD1; fixed4 color:COLOR; };
            output vert(input v) { output o; o.vertex=UnityObjectToClipPos(v.vertex); o.uv=v.uv; o.color=v.color; o.room=mul(_WorldToRoom,mul(unity_ObjectToWorld,v.vertex)).xyz; return o; }
            fixed4 frag(output i):SV_Target
            {
                fixed4 c=tex2D(_MainTex,i.uv)*i.color;
                c.rgb *= max(0, _CFWorldAmbientColor.rgb +
                    _CFWorldSunColor.rgb);
                clip(c.a-.2);
                // Keep the camera-facing card wholly inside the upper room,
                // including oblique views and the ends of the walking loop.
                clip(i.room-_RoomMin.xyz);
                clip(_RoomMax.xyz-i.room);
                // Source atlas already contains character shading. Warm room
                // fill lifts it at night without illuminating outside walls.
                c.rgb *= fixed3(.48,.48,.48) + _EmissionColor.rgb;
                return fixed4(c.rgb,1);
            }
            ENDCG
        }
    }
}
