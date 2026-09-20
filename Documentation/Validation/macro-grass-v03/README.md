# Macro grass V03 validation

The runtime texture is a 4096 × 4096 RGB albedo representing 75 m × 75 m.
Opposite one-pixel borders compare at RMSE `0` horizontally and vertically.
The 3 × 3 `tiled-preview.png` was inspected without a visible edge seam or
repeated colored cluster. SHA-256:
`70625083992a84c328d62eec9a3674f10db98bc580cdbd5711e0569eb247011d`.

V03 carries its close texture in the bitmap. The rejected close-zoom shader
modulation was removed, preventing a second smooth noise field from producing
liquid-like mottling. Unity retains the established 75 m world-space mapping
and mip/smooth-filter transition beginning at player-facing Zoom 3. The focused
`DistrictDefaultGrassUsesBroadWorldSpaceTextureDensity` EditMode test passed on
September 20, 2026, verifying the V03 resource, 4096-pixel dimensions, mipmaps,
Repeat wrap mode, 75 m mapping, lot catalog integration, and shader compilation.
