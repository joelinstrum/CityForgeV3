# Macro grass V05 validation

The runtime texture is a 4096 × 4096 RGB albedo representing 75 m × 75 m.
Opposite one-pixel borders remain identical after the color transform: RMSE
`0` horizontally and vertically. The 3 × 3 `tiled-preview.png` retains the V04
grain without a visible edge seam. SHA-256:
`8cb93263fa46e5cb995ce0b637a2c4d5a33d52be2dc298e6cceb5bf0a533f7d7`.

Only the source color changes. World-space mapping, close-view sampling,
distant smoothing, terrain lighting, grid visibility, and the default-off
secondary ground decals remain unchanged.

The focused `DistrictDefaultGrassUsesBroadWorldSpaceTextureDensity` EditMode
test passed on September 20, 2026. Unity reported no C# or shader errors.
