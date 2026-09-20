#ifndef CITY_FORGE_WORLD_LIGHTING_INCLUDED
#define CITY_FORGE_WORLD_LIGHTING_INCLUDED

// Set once by the active world owner. Custom artwork shaders consume the same
// environment as Unity-lit geometry instead of inventing local brightness.
fixed4 _CFWorldAmbientColor;
fixed4 _CFWorldSunColor;
float4 _CFWorldLightDirection;

inline fixed3 CityForgeWorldLighting(fixed3 worldNormal, fixed shadow)
{
    fixed diffuse = saturate(dot(normalize(worldNormal),
        normalize(_CFWorldLightDirection.xyz)));
    return max(0, _CFWorldAmbientColor.rgb +
        _CFWorldSunColor.rgb * diffuse * shadow);
}

// Camera-facing artwork cannot use its quad normal as a physical surface
// normal. It still receives the world's intensity, color and shadow state.
inline fixed3 CityForgeArtworkLighting(fixed shadow)
{
    return max(0, _CFWorldAmbientColor.rgb +
        _CFWorldSunColor.rgb * shadow);
}

#endif
