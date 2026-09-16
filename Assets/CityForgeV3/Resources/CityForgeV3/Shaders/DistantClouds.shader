Shader "CityForgeV3/DistantClouds"
{
 Properties
 {
  _MainTex ("Cumulus artwork",2D)="white"{}
  _AlternateTex ("Wispy cloud artwork",2D)="white"{}
  _Color ("Cloud light",Color)=(1,1,1,1)
  _Shadow ("Ground projection",Float)=0
  _ShadowStrength ("Shadow strength",Float)=.13
  _DistrictSize ("District metres",Vector)=(640,640,0,0)
 }
 SubShader
 {
  Tags { "Queue"="Transparent+100" "RenderType"="Transparent" }
  Cull Off ZWrite Off ZTest LEqual
  Blend SrcAlpha OneMinusSrcAlpha
  Pass
  {
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #pragma target 3.0
   #include "UnityCG.cginc"
   sampler2D _MainTex, _AlternateTex;
   float4 _Color, _DistrictSize, _Clouds[2], _CloudMotion[2];
   float _Shadow, _ShadowStrength;
   struct appdata { float4 vertex:POSITION;float2 uv:TEXCOORD0;float2 center:TEXCOORD1;float4 color:COLOR; };
   struct v2f { float4 pos:SV_POSITION;float2 uv:TEXCOORD0;float2 local:TEXCOORD1;float fade:TEXCOORD2;float variant:TEXCOORD3; };
   v2f vert(appdata v)
   {
    v2f o;
    float4 motion=_CloudMotion[v.color.r<.5 ? 0 : 1];
    float2 moved=motion.xy;
    if(_Shadow<.5) v.vertex.xz+=moved-v.center;
    else v.vertex.y+=.21;
    o.pos=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.local=v.vertex.xz;
    o.fade=motion.z;o.variant=v.color.r;return o;
   }
   fixed4 frag(v2f i):SV_Target
   {
    if(_Shadow>.5)
    {
     float density=0;
     [unroll]for(int c=0;c<2;c++)
     {
      float4 motion=_CloudMotion[c];
      float2 center=motion.xy;
      float2 uv=(i.local-center-float2(35,20))/_Clouds[c].zw+.5;
      if(c==1) uv.x=1-uv.x;
      float inside=step(0,uv.x)*step(uv.x,1)*step(0,uv.y)*step(uv.y,1);
      float a = c%2 == 0 ? tex2Dlod(_MainTex,float4(saturate(uv),0,4)).a : tex2Dlod(_AlternateTex,float4(saturate(uv),0,4)).a;
      density+=a*inside*motion.z;
     }
     return fixed4(.20,.24,.30,saturate(density)*_ShadowStrength);
    }
    fixed4 cloud=i.variant<.5 ? tex2D(_MainTex,i.uv) : tex2D(_AlternateTex,i.uv);
    // Coarse coverage feathers the silhouette while preserving opaque interior detail.
    float coverage=i.variant<.5 ? tex2Dlod(_MainTex,float4(i.uv,0,5)).a : tex2Dlod(_AlternateTex,float4(i.uv,0,5)).a;
    float alpha=cloud.a*smoothstep(.06,.65,coverage)*i.fade*.78;
    // Neutralize colour fringes in very thin generated wisps.
    float grey=dot(cloud.rgb,float3(.299,.587,.114));
    cloud.rgb=lerp(grey.xxx,cloud.rgb,.55);
    return fixed4(cloud.rgb*_Color.rgb,alpha*_Color.a);
   }
   ENDCG
  }
 }
}
