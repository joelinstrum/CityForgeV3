# Player-facing Zoom 3 grass correction

September 24, 2026. Joe's Chambersburg screenshot showed a solid-color
meadow at player-facing Zoom 3. The earlier grass pass changed `LOD3`, which
is actually player-facing Zoom 4. `DistrictScale` maps Zooms 1–3 to
`LOD0`–`LOD2`; `LOD2` still applied full distant filtering and zero procedural
grain. This explains the smooth view.

`LOD2` now uses the visible far-grass treatment: 0.15 distant filtering,
full grain, 0.89 brightness, and 1.82 grain frequency. `LOD3` was restored to
its earlier Zoom 4 settings (0.3 filtering, 0.6 grain, 0.94 brightness,
unscaled grain frequency). The 75 m artwork registration and the farther LOD
settings are unchanged. No reference pixels or assets were copied from SimCity 4.

[`grass-player-zoom3.png`](grass-player-zoom3.png) is a 2048 × 1096 offscreen
capture of a flat 4 × 4 district at the actual `LOD2` camera distance. It
shows visible fine variation rather than the uniform ground in Joe's
Chambersburg screenshot. This fixture has no trees or buildings and has a
construction grid, so Joe's open editor remains the final visual check.

## Validation

- The isolated Unity fixture passed the grass material contract and large
  district capture tests: 2/2. The render profile test passed separately:
  1/1. The open Unity editor was not driven or restarted.
- A short 4 × 4 district fixture with 1,777 trees rendered 30 synchronous
  2048 × 1096 frames per pass. Before medians were 0.321 and 0.308 ms;
  after medians were 0.340 and 0.311 ms. Per-render managed allocations
  were zero in both passes. This measures CPU render submission only.
  `UnityStats` reported zero draw calls and batches in the offscreen batch
  fixture, so draw calls and GPU cost are unmeasured. The short test does
  not establish long-duration frame stability.
- The earlier focused run passed 52/52; its broader EditMode run had 92
  failures. Neither was rerun as a full suite for this correction.
