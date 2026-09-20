Shader "CityForgeV3/Experimental3DGroundReceiver"
{
    Properties
    {
        _Color ("Color", Color) = (1, 1, 1, 1)
        _MainTex ("Surface Texture", 2D) = "white" {}
        [Toggle] _UseWorldSpaceUV ("Use World Space UV", Float) = 0
        _TextureWorldSize ("Texture World Size (m)", Float) = 5
        _DisplayMatch ("Chooser Display Match", Color) = (0.75, 0.80, 0.75, 1)
    }

    SubShader
    {
        Tags { "Queue" = "Geometry" "RenderType" = "Opaque" }
        Pass
        {
            Tags { "LightMode" = "ForwardBase" }
            ZWrite On
            Cull Back
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase
            #include "UnityCG.cginc"
            #include "Lighting.cginc"
            #include "AutoLight.cginc"
            #include "CityForgeWorldLighting.cginc"
            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _UseWorldSpaceUV;
            float _TextureWorldSize;
            fixed4 _Color;
            fixed4 _DisplayMatch;
            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };
            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldNormal : TEXCOORD0;
                SHADOW_COORDS(1)
                float2 uv : TEXCOORD2;
                float3 worldPosition : TEXCOORD3;
            };
            v2f vert(appdata input)
            {
                v2f output;
                output.pos = UnityObjectToClipPos(input.vertex);
                output.worldNormal = UnityObjectToWorldNormal(input.normal);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.worldPosition = mul(unity_ObjectToWorld, input.vertex).xyz;
                TRANSFER_SHADOW(output);
                return output;
            }
            fixed4 frag(v2f input) : SV_Target
            {
                float2 worldUv = input.worldPosition.xz /
                    max(0.01, _TextureWorldSize);
                float2 surfaceUv = lerp(input.uv, worldUv,
                    step(0.5, _UseWorldSpaceUV));
                fixed4 artwork = tex2D(_MainTex, surfaceUv) * _Color;
                fixed shadow = SHADOW_ATTENUATION(input);
                fixed3 normal = normalize(input.worldNormal);
                fixed3 illumination = CityForgeWorldLighting(normal, shadow);
                artwork.rgb *= _DisplayMatch.rgb * illumination;
                artwork.a = 1.0;
                return artwork;
            }
            ENDCG
        }

        UsePass "Legacy Shaders/VertexLit/SHADOWCASTER"
    }

    FallBack Off
}
