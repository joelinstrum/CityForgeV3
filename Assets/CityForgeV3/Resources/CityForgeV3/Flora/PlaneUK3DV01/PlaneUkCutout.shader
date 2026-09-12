Shader "CityForgeV3/Flora/Plane UK Cutout"
{
 Properties {
  _MainTex("Original color and opacity",2D)="white"{}
  _BumpMap("Original normals",2D)="bump"{}
  _Color("Tint",Color)=(1,1,1,1)
  _Cutoff("Leaf alpha cutoff",Range(0,1))=.35
 }
 SubShader {
  Tags {"RenderType"="TransparentCutout" "Queue"="AlphaTest"}
  Cull Off
  CGPROGRAM
  #pragma surface surf Standard fullforwardshadows addshadow alphatest:_Cutoff
  #pragma target 3.0
  #pragma multi_compile_instancing
  #define _SPECULARHIGHLIGHTS_OFF 1
  #define _GLOSSYREFLECTIONS_OFF 1
  sampler2D _MainTex,_BumpMap;
  fixed4 _Color;
  struct Input {float2 uv_MainTex;float2 uv_BumpMap;float facing:VFACE;};
  void surf(Input IN,inout SurfaceOutputStandard o){
   fixed4 c=tex2D(_MainTex,IN.uv_MainTex)*_Color;
   o.Albedo=c.rgb;o.Alpha=c.a;o.Metallic=0;o.Smoothness=.1;
   o.Normal=UnpackNormal(tex2D(_BumpMap,IN.uv_BumpMap));
   o.Normal*=IN.facing>=0?1:-1;
  }
  ENDCG
 }
 FallBack "Transparent/Cutout/Diffuse"
}
