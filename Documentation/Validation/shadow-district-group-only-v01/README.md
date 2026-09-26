# Group-only shadow study

September 25, 2026. The opt-in soft shadow prototype remains enabled only in
`Shadow DIstrict`. It now creates shadows only for `ForestClusterCatalog`
cluster records. Individual trees, including standalone firs, have no shadow
meshes or shadow batch entries. Legacy district shadows are unchanged when
`ShowDistrictShadows` is enabled.

The saved `Shadow DIstrict` was copied into an isolated Unity 6000.1.12f1
project. The open editor and original save were untouched. The temporary
EditMode benchmark loaded its 5,353 flora records and changed time from the
saved preset to morning. Results are a single headless run per version:

| Measure | Previous prototype | Group-only prototype |
| --- | ---: | ---: |
| Source tree shadows | 5,353 | 2,103 |
| Individual tree shadows | 3,250 | 0 |
| Flora shadow batch renderers | 392 | 174 |
| `RebuildEntireDistrict` CPU wall time | 2,987 ms | 2,687 ms |
| `SetTimeOfDay` CPU wall time | 0.60 ms | 0.58 ms |
| Staged presentation sync CPU wall time | 590 ms | 420 ms |
| Staged sync calls | 1,062 | 655 |

The time-change path now copies only tracked shadow-bearing renderers into its
staged queue; the membership is maintained on add, harvest replacement, and
district rebuild. It still updates all 2,103 group shadows, then schedules a
rebuild across all flora batch cells. That is a costly existing bulk path at
each time preset change, and the reduced 655-step rollout remains visible.
Incremental updates to shadow-bearing cells, or fixed shadow geometry with a
shared material change, would be the next performance options if the visual
study warrants shadows.

The focused `FarForestCanopyTests` suite passed 27/27 in the isolated project,
including a 70-individual-tree plus one-group time-change case. The dense-save
benchmark passed its completion check. These timings do not measure Game view
frame time, GPU work, or draw calls. Batch renderer counts are only a proxy
for potential draw count. Managed allocation counters returned zero in this
Unity EditMode run, so no allocation improvement is claimed. The earlier broad
EditMode run had 92 failures and is not described as green.
