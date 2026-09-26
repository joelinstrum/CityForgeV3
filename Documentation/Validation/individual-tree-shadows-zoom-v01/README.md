# Individual tree shadow zoom pass

September 26, 2026. The normal district view now uses the existing projected
ground silhouettes for individual trees. Forest clumps have no shadow meshes.
The district's realtime sun shadows remain off; building and cloud shadows are
outside this pass. Cached tree shadow batches render at LOD0–LOD2 and are hidden
at LOD3–LOD5. A zoom change only toggles batch renderers.

## Dense saved district

The saved `Shadow DIstrict` was copied into an isolated Unity 6000.1.12f1
project for read-only EditMode measurements. It contains 5,353 flora records.
These are single headless runs of `RebuildEntireDistrict`, followed by 40 zoom
calls and one time-of-day change; timings are approximate CPU wall time.

| Measure | Shadows off | Individual shadows on |
| --- | ---: | ---: |
| District rebuild | 2,342 ms | 2,666 ms |
| Source tree shadow meshes | 0 | 3,250 |
| Flora shadow batches | 0 | 304 |
| 40 zoom calls | 0.2 ms | 2.4 ms |
| Time-of-day presentation | 0.3 ms | 658 ms over 799 budgeted calls |
| Largest budgeted call | 0 ms | 11.4 ms |

The time-of-day path still updates the source shadows at far zoom and stages
their batch refresh. That work is a justified remaining cost for keeping
close-view shadows ready after a lighting change. It touches the 3,250 shadow
sources and their affected spatial batches only on a time preset change, not
on ordinary camera movement. Deferring those updates would require hiding
shadows during a later close-zoom catch-up or accepting stale projections.

The 36-test `DistrictFloraBatchesTests` EditMode suite passed, including the
new case: an individual fir casts a projected shadow, a clump does not,
close/far zoom reuses its batch and selectable tree, and zooming in at night
does not restore a shadow. GPU frame time, draw calls,
visual quality, and sustained play were not measured in headless Unity; the
open interactive editor remains the visual review path.
