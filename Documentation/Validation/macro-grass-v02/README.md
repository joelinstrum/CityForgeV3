# Macro grass V02 validation

The runtime texture is 4096 × 4096 RGB with no alpha. Opposite one-pixel borders compare at RMSE `0` horizontally and vertically. A 3 × 3 tiled preview was inspected after the localized edge blend; V02 does not use V01's mirrored-quadrant construction.

The focused Unity 6000.1.12f1 EditMode test `CityForgeV3.Tests.UiFoundationTests.DistrictDefaultGrassUsesBroadWorldSpaceTextureDensity` passed on September 20, 2026. It loaded the V02 runtime texture and verified its dimensions, mip chain, Repeat wrap mode, 75 m scale at every district zoom level, default-lot world-space UV configuration, and affected shader compilation.

The user-requested district decal presentation was disabled during visual review so the terrain albedo could be judged without that secondary presentation layer. Final visual acceptance remains an in-editor review step.
