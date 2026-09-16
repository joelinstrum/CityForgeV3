Shader "CityForgeV3/RiverBedSurface"
{
    Properties
    {
        _MainTex ("Riverbed Artwork", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _DistrictHalfSize ("District half size", Vector) = (100000,100000,0,0)
        _RiverWaterLevel ("Water level", Float) = -0.1
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Cull Off
        ZWrite Off
        ZTest LEqual
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase
            #include "UnityCG.cginc"
            #include "Lighting.cginc"

            struct AppData
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            struct Varyings
            {
                float4 position : SV_POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
                float3 worldNormal : TEXCOORD1;
                float3 localPosition : TEXCOORD2;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            float4 _DistrictHalfSize;
            float _RiverWaterLevel;

            Varyings vert(AppData input)
            {
                Varyings output;
                output.position = UnityObjectToClipPos(input.vertex);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.color = input.color;
                output.localPosition = input.vertex.xyz;
                output.worldNormal = UnityObjectToWorldNormal(input.normal);
                return output;
            }

            fixed4 frag(Varyings input) : SV_Target
            {
                // At an open district crossing the lowered bed projects beyond
                // the water plane. Clip that exposed underside, not the side banks.
                float3 towardCamera = mul((float3x3)unity_WorldToObject,
                    UNITY_MATRIX_V[2].xyz);
                float depth = _RiverWaterLevel - input.localPosition.y;
                if (depth > 0 && towardCamera.y > 0.001)
                {
                    float2 atWater = input.localPosition.xz +
                        towardCamera.xz * (depth / towardCamera.y);
                    clip(_DistrictHalfSize.xy - abs(atWater));
                }
                fixed4 albedo = tex2D(_MainTex, input.uv) * _Color * input.color;
                float3 normal = normalize(input.worldNormal);
                float3 lightDirection = normalize(UnityWorldSpaceLightDir(0));
                float diffuse = saturate(dot(normal, lightDirection));
                fixed3 ambient = ShadeSH9(float4(normal, 1.0));
                fixed3 lighting = max(ambient + _LightColor0.rgb * diffuse,
                    0.64);
                // Retain useful relief without turning the sloped bank into a
                // dark marker-like outline at the normal isometric camera.
                albedo.rgb *= lerp(fixed3(1.0, 1.0, 1.0), lighting, 0.42);
                return albedo;
            }
            ENDCG
        }
    }
}
