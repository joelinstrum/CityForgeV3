Shader "CityForgeV3/BuildingDepthOccluder"
{
    Properties
    {
        [IntRange] _BuildingHostStencilRef
            ("Building Host Stencil Ref", Range(0,252)) = 0
        [IntRange] _BuildingHostStencilWriteMask
            ("Building Host Stencil Write Mask", Range(0,252)) = 0
    }
    SubShader
    {
        // Draw after opaque lot/road surfaces have written their color, but
        // immediately before alpha-tested flora. The pass then contributes
        // only building depth and cannot punch clear-color holes in the lot.
        Tags { "Queue"="AlphaTest-10" "RenderType"="Opaque" }
        Pass
        {
            ColorMask 0
            ZWrite On
            ZTest LEqual
            Cull Back
            Stencil
            {
                Ref [_BuildingHostStencilRef]
                WriteMask [_BuildingHostStencilWriteMask]
                Comp Always
                Pass Replace
            }
        }
    }
}
