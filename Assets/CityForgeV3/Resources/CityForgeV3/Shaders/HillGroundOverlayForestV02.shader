Shader "CityForgeV3/HillGroundOverlayForestV02"
{
    Properties
    {
        _Color ("Color", Color) = (1, 1, 1, 1)
        _MainTex ("Surface Texture", 2D) = "white" {}
        _AmbientFloor ("Ground Ambient Floor", Range(0, 1)) = 0.52
        _TerrainSunDirection ("Terrain Sun Direction", Vector) = (0, 1, 0, 0)
        _TerrainReliefStrength ("Terrain Relief Strength", Range(0, 2)) = 1.8
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" }
        // This shader is also used by horizontal overlay quads. Unity's Quad
        // primitive winding can face downward after its ground rotation, so
        // back-face culling made painted brick/concrete tiles disappear.
        Cull Off

        Pass
        {
            Tags { "LightMode" = "ForwardBase" }
            // The lot is a background receiver. Writing its depth clips the
            // below-pivot pixels of camera-facing architecture at ground level.
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase
            #include "UnityCG.cginc"
            #include "Lighting.cginc"
            #include "AutoLight.cginc"

            struct AppData
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            struct VertexToFragment
            {
                float4 pos : SV_POSITION;
                float3 worldNormal : TEXCOORD0;
                SHADOW_COORDS(1)
                float2 uv : TEXCOORD2;
                fixed opacity : TEXCOORD3;
                float2 groundPosition : TEXCOORD4;
            };

            fixed4 _Color;
            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _AmbientFloor;
            float4 _TerrainSunDirection;
            float _TerrainReliefStrength;

            VertexToFragment vert(AppData input)
            {
                VertexToFragment output;
                output.pos = UnityObjectToClipPos(input.vertex);
                output.opacity = input.color.a;
                output.groundPosition = mul(unity_ObjectToWorld,input.vertex).xz;
                output.worldNormal = UnityObjectToWorldNormal(input.normal);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                TRANSFER_SHADOW(output);
                return output;
            }

            fixed4 frag(VertexToFragment input) : SV_Target
            {
                fixed shadow = SHADOW_ATTENUATION(input);
                fixed3 normal = normalize(input.worldNormal);
                fixed3 lightDirection = normalize(_TerrainSunDirection.xyz);
                fixed diffuse = saturate(dot(normal, lightDirection));
                // The former max(floor, diffuse * shadow) made low-angle sun
                // shadows impossible: Morning/Afternoon diffuse was below the
                // floor for both lit and shadowed pixels. Treat ambient as the
                // stable minimum and let directional light supply the range
                // above it. A shadow now removes that directional contribution
                // without crushing the receiver below its ambient floor.
                fixed illumination = lerp(
                    _AmbientFloor, 1.0h, diffuse * shadow);
                // Broad sculpted hills have shallow normals, so Lambert alone
                // barely separates their two shoulders after the authored
                // ambient floor is applied. Reinforce only the horizontal
                // relief component: the slope facing the active world sun is
                // lifted, while the opposite slope receives a restrained
                // directional shade. Flat ground remains unchanged.
                fixed2 horizontalSun = normalize(lightDirection.xz + fixed2(0.0001h, 0.0001h));
                fixed reliefFacing = dot(normal.xz, horizontalSun);
                fixed relief = clamp(reliefFacing * _TerrainReliefStrength,
                    -0.42h, 0.28h);
                illumination = saturate(illumination * (1.0h + relief));
                // Mirror an interior texture region at the original fine physical scale.
                // Coverage is independent, so enlarging a swath never enlarges grass blades.
                float2 detail = abs(frac(input.groundPosition / 10.0) * 2.0 - 1.0);
                fixed4 surface = tex2D(_MainTex, 0.25 + detail * 0.5);
                // Deep forest vegetation palette; preserve source fine detail and alpha.
                float detailLuma = dot(surface.rgb,float3(.2126,.7152,.0722));
                surface.rgb = detailLuma * float3(.38,.70,.30);
                float2 p = input.uv * 2.0 - 1.0;
                float angle = atan2(p.y,p.x);
                float boundary = 0.79 + 0.10*sin(angle*3.0+1.1)
                    + 0.06*sin(angle*5.0-0.7) + 0.04*sin(angle*7.0);
                fixed fade = 1.0-smoothstep(boundary-0.30,boundary,length(p));
                return fixed4(surface.rgb * _Color.rgb * illumination,
                    surface.a * _Color.a * input.opacity * fade * (1.0-smoothstep(.22,.45,length(normal.xz)/max(normal.y,.05))));
            }
            ENDCG
        }


    }
}
