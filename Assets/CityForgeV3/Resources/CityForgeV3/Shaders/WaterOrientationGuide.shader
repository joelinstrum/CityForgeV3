Shader "CityForgeV3/WaterOrientationGuide" {
Properties { _Color ("Color", Color) = (0.15,0.65,1,0.95) }
SubShader { Tags { "Queue"="Overlay" "RenderType"="Transparent" }
Pass { ZTest Always ZWrite Off Cull Off Blend SrcAlpha OneMinusSrcAlpha
CGPROGRAM
#pragma vertex vert
#pragma fragment frag
#include "UnityCG.cginc"
fixed4 _Color;
float4 vert(float4 vertex : POSITION) : SV_POSITION { return UnityObjectToClipPos(vertex); }
fixed4 frag() : SV_Target { return _Color; }
ENDCG
} } }
