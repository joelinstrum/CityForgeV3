# Projected tree clump shadow experiment

September 26, 2026. Baseline commit: `04facdf` on
`feature/time-and-light-adjustments`.

The normal district view now enables projected ground shadows for tree clumps
through the existing `ForestClusterShadows` geometry and `DistrictFloraBatches`
spatial batching. Each clump gets one source mesh with separate crown and
contact footprints for its member trees. Its source renderer is suppressed
after batching, as with individual trees. Clump shadows use the current
morning, noon, and afternoon direction and length settings; they hide along
with individual shadows at LOD3–LOD5 and during Night. The app's single
`DistrictClumpShadowExperiment` constant turns the study off again.

## Dense comparison

One isolated Unity 6000.1.12f1 EditMode fixture placed 5,368 individual
trees and 1,593 compact/large clumps from four families across a 4×4 district.
The two measurements were sequential headless runs in the same process, so
timings are approximate and may reflect asset caching. The general district
shadow switch was off in both runs, matching normal gameplay.

| Measure | Clump shadows off | Clump shadows on |
| --- | ---: | ---: |
| Flora records | 6,961 | 6,961 |
| Shadow sources | 5,368 | 6,961 |
| Shadow batches | 500 | 600 |
| Shadow batch vertices | 819,454 | 1,257,454 |
| District rebuild | 2,669 ms | 2,852 ms |
| Afternoon shadow update | 837 ms / 1,271 calls | 1,283 ms / 1,471 calls |
| Largest bounded call | 12.2 ms | 13.5 ms |

The additional time-of-day work is spread across eight source trees per frame
and one spatial batch rebuild per frame. Night prepares 32 shadow sources per
frame. The extra 100 shadow batches are a draw-count proxy when close zoom
shadows are visible. GPU frame time, actual draw calls, visual appearance, and
long-duration play still need review in the open editor. At low frame rates,
Night preparation can continue briefly after sunrise; the complete shadow
batch remains hidden until ready.

Validation: 41/41 `DistrictFloraBatchesTests` passed, including compact fir
and large deciduous clump footprints, batching, and far-zoom visibility.
`FarForestCanopyTests` passed 26/27; the remaining
`FirIndividualTrunksAlignWithTheirAtlasFootMargins` assertion still expects
an obsolete fir atlas slot, as it did before this change.
