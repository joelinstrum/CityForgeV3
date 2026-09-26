# Rolling terrain vertical relief — September 25, 2026

Temporary QA screenshots and Unity test-result XML were removed from the repository; the measurements and findings remain.

## Height trace

The Relief modal reads an `IntegerField`. Previously, Apply clamped non-mountain
height to 60 m; `DistrictElevation` independently clamped it to 60 m. Thus a
typed 80 m became 60 m before terrain construction. The current rolling-hill
field is a sum of broad elliptical `strength × (1 − distance²)³` forms, passed
through `1 − exp(−1.7 × field)`. A `SmoothStep` clearance factor then levels
district edges and road/lot/river corridors. These factors further reduce
peak Y. `CreateMesh` assigns each sampled `Heights[i]` directly to vertex Y;
there is no later mesh-height multiplier or transform scale.
`DistrictSurfaceCache` also has a 60 m clamp, but it bounds the horizontal
clearance invalidation radius, not vertex Y. The `_HillHeight` material value is
likewise clamped to 60 m, but the rolling grass shader no longer reads it.

The isolated 4×4, seed 1209, 40% coverage baseline measured **min Y 0.000 m,
max Y 54.489 m, relief 54.489 m** for an entered 80 m. A 2×2 district gave
54.488 m. The prior 60 m cap and the field/clearance response explain the
shortfall.

## Correction

The UI and `DistrictElevation` now accept a base height up to 240 m. After the
existing shape and edge clearance are sampled, one construction-time pass
calibrates vertical Y so an unobstructed district reaches the requested height
times `Vertical Relief Scale` (default 1x, UI range 0.25–4x). The calibration
uses the unconstrained edge-faded peak. Road/lot/river clearance can therefore
lower the measured peak without rescaling unrelated ground. Local surface edits
reuse the cached factor; reloads produce the same heights. Missing scale values
in older saves resolve to 1x.

The rolling forms, positions, coverage, seed, horizontal radii, grass shader,
lighting, camera, and cloud code were not changed. Above 60 m, clearance width
stays at its former 60 m-cap value of 240 m; raising only vertical amplitude
cannot widen the horizontal flattening around roads, rivers, or district edges.

On initial district construction and explicit full terrain rebuilds, the Unity
Editor Console logs:

```text
Requested relief height: 80.000 m
Vertical Relief Scale: 1.000x
Generated terrain min Y: 0.000
Generated terrain max Y: 80.000
Generated vertical relief: 80.000 m
```

These values come from the actual generated mesh bounds. No routine frame,
simulation tick, or local surface edit logs or scans the mesh for diagnostics.

## Isolated validation

- Relief tests: 14/14 passed. At 80 m and 40% coverage,
  both 2×2 and 4×4 meshes measure exactly 80.000 m. At 1.5x, they measure
  120.000 m. Sampled X/Z vertices and the normalized height profile remain
  unchanged between 60 and 80 m. Older save data defaults to 1x. Incremental
  river/road updates at 80 m match full rebuilds. The actual district build
  logs the measured bounds above.
- Regression tests: 17/17 passed for region map,
  mountain/quarry, and grass zoom behavior.
- A read-only copy of saved City 062 with 80 m/40% produces **0.000–80.000 m**.
  A read-only copy of river-constrained `Shadow DIstrict` produces
  **0.000–68.991 m** because the river levels part of the high ground. The
  diagnostic reports this actual result; no player save was written.
- Same-camera, same-lighting synthetic Afternoon previews:
  before and after. These show
  only the vertical amplitude correction and are not full-district Game views.

The extra calibration pass runs only during full terrain construction. In the
read-only dense 4×4 fixture (5,353 flora records, one river, 263,169 terrain
samples), eight warm constructions averaged **158.89 ms before** and
**163.59 ms after**; retained managed memory was about 1.27 MB per build in
both. This single short CPU measurement does not measure GPU time, draw calls,
frame spikes, or long-duration stability. The live Unity editor and saved
region were left alone.
