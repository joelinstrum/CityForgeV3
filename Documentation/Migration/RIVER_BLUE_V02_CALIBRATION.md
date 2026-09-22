# River blue V02 material calibration

Date: September 21, 2026

This pass calibrates the existing `RiverBlueV01` artwork toward the river in
the user-supplied visual reference
`/Users/joelinstrum/Desktop/village-goal.png`: a clearly readable medium blue
body with larger directional ripples and persistent narrow highlights.

No source or runtime texture was replaced. The V01 base and transparent crest
atlas, their prompts, their documented processing, and the prior unversioned
river textures all remain intact. This is a reversible runtime-material change.

## Calibration

- Blue tint: `(0.82, 1.04, 1.18)` to `(0.86, 1.03, 1.28)`.
- Base brightness: `1.08` to `1.16`.
- Texture world size: `18m` to `30m`, enlarging the authored ripple chains.
- Deep-water strength: `0.58` to `0.42`.
- Removed the additional runtime remap that forced the effective deep-water
  strength to approximately `0.91`; the authored value now reaches the shader
  unchanged.
- Shimmer strength: `0.24` to `0.32`.
- Crest strength: `0.32` to `0.44`.
- Crest coverage: `0.55` to `0.72`.
- Crest tiling: `0.85` to `0.95`.
- Crest pulse speed: `0.12` to `0.08`, keeping highlights readable longer.

River geometry, cardinal flow vectors, animation method, opacity profile,
lighting shader, draw calls, shoreline artwork, and river generation are
unchanged. North-to-south and west-to-east rivers continue to share the same
base and crest resources and differ only in their downstream flow vector.
