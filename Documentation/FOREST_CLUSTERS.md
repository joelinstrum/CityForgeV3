# Mixed forest placements

Forest → Generate Tree Coverage now uses five mixed-tree cluster variants in
Temperate and Mediterranean districts. A candidate has an 80% chance of being a
five-tree scenery cluster and a 20% chance of being an individual Cilician fir.
Clearance rejection changes the final ratio, particularly near development.
The existing fir is the lumber tree: its ID, yield, animation, harvest state,
and worker targeting are unchanged. Firs depicted inside cluster artwork are
scenery and cannot be harvested separately.

Wooded candidate spacing is 48m and Sparse spacing is 128m in those climates,
twice the old spacing on each axis. Tropical retains individual tropical trees;
Desert still disallows generated forest. Original saved forests are not migrated
on load. Explicit regeneration replaces generated standing flora, retains manual
plantings and harvested trees, and supports existing undo. Only Save persists.

Each cluster uses one existing PlacedDistrictFlora record, selection handle, and
shared cached sprite. All five variants participate in the existing spatial
render batches; no new per-frame work or district scans were added. Generation
uses bounded occupancy-cell footprint checks at 16m times scale. Retained
clusters reserve the same larger footprint. Generation/RefreshFlora remain
explicit bulk operations; they can still stall for hundreds of milliseconds.

## Artwork and rendering

Runtime PNGs: Assets/CityForgeV3/Resources/CityForgeV3/Flora/ForestClustersV01.
The approved ForestClustersV03 summer art was edited using built-in image_gen
to remove baked preview ground shadows. Exact prompts/source lineage:
[runtime art prompts](ArtStudies/ForestClustersRuntimeV01/prompts.json).
Original art studies are untouched. Each image is 1254px square with real alpha,
50 pixels per metre, pivot (.5,.065), mipmaps and clamp wrapping. Shadows use
the existing sun-driven ground projection. Multi-tree silhouettes use a shared
anchor and approximate projection; steep slopes can expose billboard/footing
limitations. This is not a set of five independently grounded 3D meshes.

District flora currently renders summer. Autumn/winter studies remain available
under ArtStudies but are not connected to a season controller; the shared
resource resolver deliberately returns ready summer art for every season until
the seasonal cutouts are production-ready. No winter snow layer added here.

RefreshFlora no longer nulls the unrelated cloud-layer reference. This preserves
cloud zoom controls after regenerating forest; clouds themselves are not rebuilt.

## Validation

Targeted EditMode coverage includes determinism, all five variants, climate
restrictions, expanded road/water/building clearance, separate fir harvesting,
one-record cluster removal, retained plantings/harvests, save/reload, undo,
failure rollback, and shared batches. See XML in Validation/forest-clusters-v01.

Live QA uses an isolated copy of saved Little River Bend (1280 x 1280m, originally
2947 flora). For a comparable coverage profile, its standing scenery is marked
generated in the copy, and old/new generators run with seed193 over the same
roads, river, lot, mountains, and retained harvested trees. A frozen editor-only
HEAD generator is retained solely for repeating this comparison. No user save
was written. The fixture and previous screen were restored.

| Measure | Old single-tree generation | Mixed clusters |
| --- | ---: | ---: |
| Flora records | 1973 | 477 |
| Harvestable firs | 590 | 100 |
| Cluster records | 0 | 333 |
| Generation CPU | 9.23ms | 4.47ms |
| Bulk flora refresh CPU | 441.87ms | 389.72ms |
| Median frame (180-frame Editor sample) | 17.84ms | 15.82ms |
| p95 frame | 41.70ms | 33.59ms |
| Maximum sampled frame | 58.32ms | 52.73ms |
| Editor-reported draw calls | 855 | 724 |

This is 75.8% fewer saved placements, not 75.8% less rendering cost or equal
lumber supply. The new grove artwork also changes visible coverage. Timings and
draws include Editor views; short samples do not establish long-run performance.
GC.GetAllocatedBytesForCurrentThread returned zero for known allocating bulk
paths on this Editor, so allocation measurement is unavailable, not zero-cost.
Live checks confirmed cluster batching, unchanged terrain-cache revision, a fir
falling/producing wood, and retention of unrelated cluster objects during harvest.
Before/after and closer captures were visually reviewed and stored alongside
the raw report. No worker/labor code was changed.

Final checks: forest EditMode suite 24/24 passed (2026-09-17 01:01:40–01:01:43 UTC);
regional regression suite 83/83 passed (01:09:54–01:10:00 UTC). Fresh XML reports
are saved in Documentation/Validation/forest-clusters-v01/.

## September 16 follow-up: coverage and rooted cluster shadows

The shared district/region coverage chooser now offers **Heavy**, **Medium**, and
**Light**. Medium preserves the former Wooded generator exactly; Light preserves
Sparse. Their serialized enum values/names remain unchanged for compatibility.
Heavy adds value3 and divides spacing by sqrt(3), giving approximately three times
Medium's placements on plantable land. Roads, river buffers, buildings and retained
plantings still reserve land, so this is not a promise to cover every district pixel.
Regenerate explicitly to apply density; Save and Undo retain their existing behavior.

Cluster shadows now use five authored trunk/canopy proxies matched to each summer
cutout, with separate ground contacts, fir versus broadleaf outlines and feathered
edges. The camera ray through each pictured foot finds its terrain contact; sun
projection follows the existing time-of-day lighting. These are approximate canopy
shapes, not recovered 3D geometry or exact leaf silhouettes. Individual fir shadows
and harvesting are unchanged. No runtime art was overwritten.

All five proxies remain in one shadow mesh per cluster and join existing shared
batches. The bounded five terrain raycasts per cluster run only on creation and
existing shadow/terrain updates, not every frame. Explicit generation still uses
the existing bulk flora refresh; it is profiled on the saved 1280m district copy.
Fresh tests and reviewed captures: Validation/forest-coverage-v02.

Final dense-copy profile (2026-09-17 01:31 UTC): Medium433 new records +44 retained,
Heavy1316 +44 retained (3.04x new records); 333→1007 clusters, 100→309 harvestable
firs. Generation4.71→6.31ms; explicit bulk refresh393.36→526.11ms. Median frame
8.12→12.76ms, p95 18.13→28.46ms, max26.74→34.77ms; draws740→945. Increased density
has a measurable rendering cost. These are 180-frame Editor samples, not a
long-duration performance guarantee. Allocation API still returns zero for known
allocating operations, so no reliable allocation result is claimed. Live harvesting,
shared batching, terrain-cache retention and fixture restoration passed. Steep
mountain projection remains an approximation; proxy canopies can stretch across
abrupt slopes. No user save was written. Forest suite30/30 passed, fresh01:30:31 UTC.
