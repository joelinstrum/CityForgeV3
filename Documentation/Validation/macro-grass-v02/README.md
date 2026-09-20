# Macro grass V02 validation

The runtime texture is 4096 × 4096 RGB with no alpha. Opposite one-pixel borders compare at RMSE `0` horizontally and vertically. A 3 × 3 tiled preview was inspected after the localized edge blend and final broad luminance-only variation pass; V02 does not use V01's mirrored-quadrant construction.

The focused Unity 6000.1.12f1 EditMode test `CityForgeV3.Tests.UiFoundationTests.DistrictDefaultGrassUsesBroadWorldSpaceTextureDensity` passed on September 20, 2026. It loaded the V02 runtime texture and verified its dimensions, mip chain, Repeat wrap mode, 75 m scale at every district zoom level, default-lot world-space UV configuration, and affected shader compilation.

The user-requested district decal presentation is disabled during visual review so the terrain albedo can be judged without secondary dressing. The toggle now propagates to both the small leaf decals and the separate dry-grass hill surface overlay; the focused EditMode test `DistrictDecalVisibilityAlsoControlsHillSurfaceDetail` passed on September 20, 2026. Final visual acceptance remains an in-editor review step.

Close-view follow-up leaves the V02 bitmap unchanged. Player-facing Zoom 1
(`LOD0`) and Zoom 2 (`LOD1`) receive neutral, world-anchored shader grain at
decreasing strengths; Zoom 3 and farther receive none. The terrain grid is
shown at the three close working views, Zooms 1–3 (`LOD0`–`LOD2`), and hidden
at Zooms 4–5. The initial meter-scale detail trial looked too smooth and
liquid-like at close range, so the final shader uses only 0.19 m and 0.42 m
grain. The focused zoom/grid and macro
grass shader tests each passed after this follow-up, and Unity compiled the
updated shader without errors.
