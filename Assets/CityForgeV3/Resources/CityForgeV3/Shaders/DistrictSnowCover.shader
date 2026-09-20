Shader "CityForgeV3/DistrictSnowCover"
{
 Properties { _Accumulation ("Temporary snow",Range(0,1))=0 }
 SubShader
 {
  Tags { "Queue"="Geometry+460" "RenderType"="Transparent" }
  Cull Off ZWrite Off ZTest LEqual Offset -1,-1
  Blend SrcAlpha OneMinusSrcAlpha
  Pass
  {
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #pragma target 3.0
   #include "UnityCG.cginc"
   #include "CityForgeWorldLighting.cginc"
   float _Accumulation;
   struct appdata { float4 vertex:POSITION; float3 normal:NORMAL; };
   struct v2f { float4 pos:SV_POSITION;float3 normal:TEXCOORD0;float2 ground:TEXCOORD1; };
   v2f vert(appdata v)
   {
    v2f o;v.vertex.y+=.035;o.pos=UnityObjectToClipPos(v.vertex);
    o.normal=UnityObjectToWorldNormal(v.normal);o.ground=v.vertex.xz;return o;
   }
   fixed4 frag(v2f i):SV_Target
   {
    float3 n=normalize(i.normal);
    float mottling=.5+.25*sin(i.ground.x*.31+sin(i.ground.y*.23))+.25*sin(i.ground.y*.47+i.ground.x*.17);
    float cover=smoothstep(mottling*.45,mottling*.45+.5,_Accumulation);
    float slope=smoothstep(.25,.85,n.y);
    fixed3 light=CityForgeWorldLighting(n,1.0h);
    return fixed4(float3(.91,.95,1)*light,cover*slope*.96);
   }
   ENDCG
  }
 }
}
