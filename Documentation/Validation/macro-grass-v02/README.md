# Macro grass V02 validation

The runtime texture is 4096 × 4096 RGB with no alpha. Opposite one-pixel borders compare at RMSE `0` horizontally and vertically. A 3 × 3 tiled preview was inspected after the localized edge blend and final luminance-only variation pass; V02 does not use V01's mirrored-quadrant construction.

The focused Unity 6000.1.12f1 EditMode test `CityForgeV3.Tests.UiFoundationTests.DistrictDefaultGrassUsesBroadWorldSpaceTextureDensity` passed on September 20, 2026. It loaded the V02 runtime texture and verified its dimensions, mip chain, Repeat wrap mode, 75 m scale at every district zoom level, default-lot world-space UV configuration, and affected shader compilation.

The user-requested district decal presentation is disabled during visual review so the terrain albedo can be judged without secondary dressing. The toggle now propagates to both the small leaf decals and the separate dry-grass hill surface overlay; the focused EditMode test `DistrictDecalVisibilityAlsoControlsHillSurfaceDetail` passed on September 20, 2026. Final visual acceptance remains an in-editor review step.
