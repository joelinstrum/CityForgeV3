# Macro grass V04 validation

The runtime texture is a 4096 × 4096 RGB albedo representing 75 m × 75 m.
Opposite one-pixel borders compare at RMSE `0` horizontally and vertically.
The 3 × 3 `tiled-preview.png` was inspected without a visible edge seam.
SHA-256: `264946b843134bab002cf6cafe2b1d6d74706a08967d9ee8c51c430bcb694e73`.

The separate district leaf-litter and hill-dressing presentation now defaults
off, preserving the earlier request to judge the base terrain in isolation and
preventing its orange flecks from being mistaken for texture content after an
editor restart.

Lighting diagnosis: at Noon/Afternoon the district material tint is white, and
`MeadowGroundSurface` interpolates illumination from the ambient floor to 1.0;
it does not brighten the albedo above the source. The large source/game
difference in V03 came from its 82% production blend toward a flat olive field,
not from a >1 lighting multiplier. Global lighting is unchanged in V04.

The focused macro-grass resource/shader test and decal/hill-overlay visibility
test both passed on September 20, 2026. Unity reported no C# or shader errors.
