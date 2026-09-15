Shader "CityForgeV3/CragSurfaceV01"
{
 Properties { _MainTex("Rock",2D)="white"{} _Color("Tint",Color)=(1,1,1,1) _TerrainSunDirection("Sun",Vector)=(0,1,0,0) _AmbientFloor("Ambient",Float)=.42 }
 SubShader {
 Tags{"Queue"="Geometry-2" "RenderType"="Opaque"}
 Pass {
 Tags{"LightMode"="ForwardBase"} ZWrite On Cull Back
 CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #pragma multi_compile_fwdbase
 #include "UnityCG.cginc"
 #include "AutoLight.cginc"
 struct appdata {float4 vertex:POSITION;float3 normal:NORMAL;};
 struct v2f {float4 pos:SV_POSITION;float3 p:TEXCOORD0;float3 n:TEXCOORD1;SHADOW_COORDS(2)};
 sampler2D _MainTex;fixed4 _Color;float4 _TerrainSunDirection;float _AmbientFloor;
 v2f vert(appdata v){v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.p=mul(unity_ObjectToWorld,v.vertex).xyz;o.n=UnityObjectToWorldNormal(v.normal);TRANSFER_SHADOW(o);return o;}
 fixed4 frag(v2f i):SV_Target{
 float3 n=normalize(i.n),w=pow(abs(n),4);w/=max(dot(w,float3(1,1,1)),.001);
 float3 p=i.p/11;
 fixed3 stone=tex2Dbias(_MainTex,float4(p.zy,0,2)).rgb*w.x+tex2Dbias(_MainTex,float4(p.xz,0,2)).rgb*w.y+tex2Dbias(_MainTex,float4(p.xy,0,2)).rgb*w.z;
 stone=saturate((stone-.35)*.65+.36);
 float light=lerp(_AmbientFloor,1,saturate(dot(n,normalize(_TerrainSunDirection.xyz)))*SHADOW_ATTENUATION(i));
 return fixed4(stone*_Color.rgb*light,1);
 }
 ENDCG
 }
 UsePass "Legacy Shaders/VertexLit/SHADOWCASTER"
 }
}
