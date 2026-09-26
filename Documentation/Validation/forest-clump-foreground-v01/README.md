# Fir clumps and foreground trees

September 26, 2026. Mountain clumps now use three of four narrow firs in compact
layouts and six of seven in large layouts. One deciduous tree remains behind
them for variety. Deciduous clumps retain their fir accents. The district flora
picker exposes compact and large clumps in each family. A manually placed clump
gets one separate foreground tree when the camera-facing ground is clear.

Region or district coverage generation also adds one deterministic foreground
tree per new clump where the local planting mask permits it. The tree is an
ordinary flora record, so it uses existing selection, seasonal presentation,
batching, and close-zoom projected shadows. Clump shadow meshes remain absent.
Existing saved forests are untouched until coverage is regenerated; no player
progress is saved automatically.

## Dense saved-district comparison

The saved `Shadow DIstrict` was copied to an isolated Unity 6000.1.12f1 test
project. Generating Heavy coverage with its seed and family mix produced 1,593
clumps and 1,593 foreground trees. A temporary benchmark rebuilt the district
with and without only those foreground records. Values are one headless EditMode
run per configuration; CPU wall times are approximate.

| Measure | Without foreground | With foreground |
| --- | ---: | ---: |
| Flora records | 5,368 | 6,961 |
| District rebuild | 2,806 ms | 3,150 ms |
| Individual shadow sources | 3,775 | 5,368 |
| Flora shadow batches | 343 | 381 |
| Time-of-day presentation | 732 ms over 895 calls | 1,035 ms over 1,131 calls |
| Largest budgeted call | 12.8 ms | 13.4 ms |

The extra work occurs on explicit coverage generation, district load, local
clump placement, and time-preset changes. Ordinary zoom and camera movement do
not regenerate clumps or foreground trees. The 38 additional shadow batches
are a draw-count proxy; GPU frame time, actual draw calls, and visual quality
still need interactive review. The existing far-zoom tree shadow gate applies
to the new foreground singles.

Validation: 36/36 `DistrictFloraBatchesTests` passed. The generation suite
passed 13/14; its unrelated retired-artwork assertion still fails for
`angel-oak-spanish-moss`. The forest suite passed 26/27; its existing narrow-fir
foot-margin assertion still expects an obsolete atlas slot. The new clump
composition and foreground-generation assertions passed.
