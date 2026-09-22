#ifndef CITY_FORGE_WORLD_LIGHTING_INCLUDED
#define CITY_FORGE_WORLD_LIGHTING_INCLUDED

// Set once by the active world owner. Custom artwork shaders consume the same
// environment as Unity-lit geometry instead of inventing local brightness.
fixed4 _CFWorldAmbientColor;
fixed4 _CFWorldSunColor;
float4 _CFWorldLightDirection;
half _CFWorldWhitePoint;

inline fixed3 CityForgeBoundWorldIllumination(fixed3 illumination)
{
    // Preserve hue and sub-white contrast. Only scale values whose brightest
    // channel would exceed the world's display-white contract.
    half peak = max(illumination.r, max(illumination.g, illumination.b));
    half whitePoint = _CFWorldWhitePoint > 0.01h
        ? _CFWorldWhitePoint : 0.98h;
    half scale = min(1.0h, whitePoint / max(peak, 0.0001h));
    return max(0, illumination * scale);
}

inline fixed3 CityForgeWorldLighting(fixed3 worldNormal, fixed shadow)
{
    fixed diffuse = saturate(dot(normalize(worldNormal),
        normalize(_CFWorldLightDirection.xyz)));
    return CityForgeBoundWorldIllumination(_CFWorldAmbientColor.rgb +
        _CFWorldSunColor.rgb * diffuse * shadow);
}

// Camera-facing artwork cannot use its quad normal as a physical surface
// normal. It still receives the world's intensity, color and shadow state.
inline fixed3 CityForgeArtworkLighting(fixed shadow)
{
    return CityForgeBoundWorldIllumination(_CFWorldAmbientColor.rgb +
        _CFWorldSunColor.rgb * shadow);
}

#endif
