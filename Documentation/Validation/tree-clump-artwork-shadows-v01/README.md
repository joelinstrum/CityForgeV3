# Artwork-based tree clump shadows

September 26, 2026. This follows the oval-footprint experiment in
`tree-clump-shadows-v01`. The normal district view now projects the actual
alpha cutout of each member tree in a clump. A mixed fir/deciduous clump keeps
its two source atlases through the existing spatial shadow batch and samples
the correct one in the ground-shadow shader. Older composed clump sprites
project their complete artwork cutout. The projected geometry samples ground
height on a grid, so it follows district hills. Individual tree shadows and
sun lighting are unchanged.

`DistrictClumpArtworkShadowExperiment` in `CityForgeApp.RegionEditor.cs` selects
artwork (`true`) or the earlier oval footprints (`false`). Both modes retain
the same clump-shadow visibility and spatial batching.

## Dense A/B timing and geometry

An isolated Unity 6000.1.12f1 EditMode fixture placed 5,368 individual trees
and 1,593 compact/large clumps from four families across a 4×4 district.
The runs were sequential in one headless process: oval, artwork, then oval
again. Asset caches and editor GC can affect wall-clock times; these are
single-run comparisons, not sustained frame-time results.

| Measure | Oval first | Artwork | Oval repeat |
| --- | ---: | ---: | ---: |
| Shadow batches | 600 | 600 | 600 |
| Shadow vertices | 1,257,454 | 1,038,454 | 1,257,454 |
| Shadow triangles | 1,249,742 | 934,382 | 1,249,742 |
| District rebuild | 3,285 ms | 2,777 ms | 2,951 ms |
| Afternoon update | 1,341 ms | 1,227 ms | 1,389 ms |
| Bounded update calls | 1,471 | 1,471 | 1,471 |
| Largest bounded call | 12.2 ms | 12.2 ms | 13.8 ms |

The artwork grid uses 25 vertices per member tree, versus 50 for each oval
crown/contact pair. It kept the same shadow batch count in this fixture and
did not show a CPU regression. GPU frame time, actual draw calls, alpha-sample
cost, and appearance still require review in the open Unity editor.

Validation: 45/45 `DistrictFloraBatchesTests` passed, including mixed-atlas
selection, legacy clump artwork, and sloped receiver geometry.
`FarForestCanopyTests` passed 26/27; its existing
`FirIndividualTrunksAlignWithTheirAtlasFootMargins` assertion still expects
an obsolete fir atlas slot.
