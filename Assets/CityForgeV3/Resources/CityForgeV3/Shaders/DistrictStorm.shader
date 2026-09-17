Shader "CityForgeV3/DistrictStorm"
{
 Properties { _RainPass ("Rain pass", Float)=0 _CloudTex ("Cloud detail",2D)="white"{} }
 SubShader
 {
  Tags { "Queue"="Transparent+120" "RenderType"="Transparent" }
  Cull Off ZWrite Off ZTest Always
  Blend SrcAlpha OneMinusSrcAlpha
  Pass
  {
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #pragma target 3.0
   #include "UnityCG.cginc"
   float _RainPass, _MistIntensity, _Snowfall;
   sampler2D _CloudTex;
   float4 _Eye, _Right, _Up, _Forward, _District, _Weather;
   struct v2f { float4 pos:SV_POSITION; float2 screen:TEXCOORD0; };
   v2f vert(float4 vertex:POSITION)
   {
    v2f o; o.pos=float4(vertex.x,vertex.y*_ProjectionParams.x,0,1); o.screen=vertex.xy; return o;
   }
   float hash(float2 p) { return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453); }
   float noise(float2 p)
   {
    float2 c=floor(p),f=frac(p);f=f*f*(3-2*f);
    return lerp(lerp(hash(c),hash(c+float2(1,0)),f.x),lerp(hash(c+float2(0,1)),hash(c+1),f.x),f.y);
   }
   float drops(float2 p, float time)
   {
    p.x+=p.y*.16; p.y+=time*13;
    float2 c=floor(p), f=frac(p);
    float rnd=hash(c);
    float x=.15+.7*hash(float2(c.x,17));
    float streak=1-smoothstep(.015,.055,abs(f.x-x));
    return streak*sin(f.y*3.14159)*step(.72,rnd);
   }
   float flakes(float2 p,float time)
   {
    p.y+=time*1.6;
    p.x+=sin(p.y*.6+time*.5)*.25;
    float2 cell=floor(p), f=frac(p);
    float2 center=float2(.15+.7*hash(cell),.15+.7*hash(cell+17));
    return 1-smoothstep(.025,.085,length(f-center));
   }
   fixed4 frag(v2f i):SV_Target
   {
    float3 origin=_Eye.xyz+i.screen.x*_Right.xyz+i.screen.y*_Up.xyz;
    float height=_RainPass>.5 ? 0 : _District.z;
    float distance=(height-origin.y)/min(-.001,_Forward.y);
    float2 world=(origin+_Forward.xyz*distance).xz;
    float2 edge=_District.xy*(_RainPass>.5 ? .5 : .75)-abs(world);
    float inside=saturate(min(edge.x,edge.y)/max(1,min(_District.x,_District.y)*.008));
    clip(inside-.001);
    if(_RainPass>.5)
    {
     float2 uv=(i.screen*.5+.5)*float2(_Weather.w,1);
     float rain=saturate(drops(uv*float2(115,48),_Weather.z)+drops(uv*float2(83,35)+7.2,_Weather.z*.83)*.55);
     if(_Snowfall>.5)
        rain=saturate(flakes(uv*float2(65,35),_Weather.z)+flakes(uv*float2(95,50)+13,_Weather.z*.7)*.6);
     float alpha=(.16*_Weather.x+rain*lerp(.56,.85,_Snowfall)*_Weather.y)*inside;
     float3 rainColor=lerp(float3(.12,.15,.18),lerp(float3(.78,.83,.86),float3(.97,.98,1),_Snowfall),saturate(rain*_Weather.y*3));
     // The lot editor's neutral rain fog, composited behind the visible streaks.
     // It has its own envelope so it lingers after emission stops.
     float mist=.28*_MistIntensity*inside;
     float combined=alpha+mist*(1-alpha);
     float3 color=(rainColor*alpha+float3(.72,.73,.72)*mist*(1-alpha))/max(.0001,combined);
     return fixed4(color,combined);
    }
    float2 p=world/max(50,min(_District.x,_District.y)*.13)+float2(_Weather.z*.025,0);
    float n=noise(p)*.55+noise(p*2.13+8.4)*.28+noise(p*4.37)*.12+noise(p*8.51)*.05;
    // Continuous deck: even the thinnest areas close before rainfall begins.
    float cover=smoothstep(n*.65,n*.65+.35,_Weather.x);
    // A ragged, feathered margin extends beyond the district, avoiding a rectangular lid.
    inside=smoothstep(0,min(_District.x,_District.y)*(.1+n*.12),min(edge.x,edge.y));
    float shade=.38+n*.32;
    float span=max(120,min(_District.x,_District.y)*.48);
    // Compensate for the shallow camera angle rather than flattening billboard art into strips.
    float2 cloudUv=float2(dot(world,normalize(_Right.xz))/span,
        dot(world,normalize(_Forward.xz))/(span*2.4));
    cloudUv+=float2(n*.22,_Weather.z*.003);
    float2 dx=ddx(cloudUv),dy=ddy(cloudUv);
    fixed4 billow=tex2Dgrad(_CloudTex,frac(cloudUv),dx,dy);
    fixed4 other=tex2Dgrad(_CloudTex,frac(cloudUv+float2(.47,.51)),dx,dy);
    // Reuse the approved cumulus highlights over a gap-free overcast body.
    shade=lerp(shade,dot(billow.rgb,float3(.299,.587,.114))*.85,billow.a*.85);
    shade=lerp(shade,dot(other.rgb,float3(.299,.587,.114))*.85,other.a*.7);
    return fixed4(shade*.96,shade*.99,shade*1.04,cover*inside*.94);
   }
   ENDCG
  }
 }
}
