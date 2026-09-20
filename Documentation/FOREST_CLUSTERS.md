# Mixed forest placements

## September 19 — weighted families and terrain-sized clumps

Follow-up: multi-tree artwork is now limited to terrain where its shared root
line can remain credible. Five local elevation samples choose large groups on
flat ground (≤0.4m spread), compact groups on gentle slopes (≤0.9m), and one
family-matched, individually grounded tree on steeper terrain. This prevents a
single five-tree baseline from appearing skewed across a hill. Steep candidates
remain one placement and one renderer; the fix does not multiply records,
objects, draw batches, or per-frame work. Existing generated records change only
after explicit Tree Coverage regeneration; no district is rewritten or saved
automatically.

Tree Coverage now stores three relative weights: Deciduous, Fir & Mountain,
and Tropical. The default is 33 / 33 / 33; totals do not need to equal 100.
These weights select a billboard's **dominant** family, not an exclusive stand.
Deciduous compositions contain one fir and Fir & Mountain compositions contain
one deciduous tree. Tropical compositions mix fan palms, date palms and tropical
broadleaf trees. Existing saved districts receive the default mix and are not
regenerated until the player explicitly uses Generate Tree Coverage.

Each candidate samples the existing deterministic elevation field at its center
and four points 12m away. A maximum height spread of 1.25m selects a broad
nine-tree billboard with a 23m clearance; gentle terrain selects a compact
five-tree billboard with a 16m clearance; steep terrain selects one rooted tree.
If a broad footprint conflicts with a road, river, lot or district edge, the
same candidate may fall back to compact.
This is five bounded samples and one occupancy-grid query per candidate during
explicit generation, never a routine district scan or per-frame calculation.

All six family/size compositions are one `PlacedDistrictFlora`, one
`SpriteRenderer`, and one spatially batched presentation. Large art uses 36
pixels/metre and compact art uses 50 pixels/metre. Summer/autumn/winter variants
are provided for deciduous and mountain compositions; tropical compositions use
their summer art in every season. Shadows stay one mesh per billboard, using five
or nine approximate grounded proxies. Separately placed harvestable Cilician firs
remain at the existing one-in-five candidate rate, preserving lumber yields,
worker selection, carts and mill routing. Trees painted inside a cluster remain
non-harvestable scenery.

The district and regional Flora panels expose the same mix controls. Applying a
district mix changes only that district; regional generation copies the mix to
each district. Generation and Undo remain in memory. Only the existing explicit
Save action writes district or region state to disk.

Runtime art and lineage are recorded in
`Documentation/Migration/FOREST_FAMILY_MIX_V01.md`. Validation evidence is in
`Documentation/Validation/forest-family-mix-v01/`.

## September 19 — interactive groups and bounded presentation updates

The interactive **Paint Family Groups** tool now includes a harvestable
Cilician fir at a one-in-five rate when the district climate allows that tree.
This affects newly painted groups only and does not rewrite existing flora.
For the **Fir and Mountain** family, the first successfully placed member is
always that harvestable Cilician fir, so a completed group cannot miss the
lumber tree through random selection. Remaining members keep the varied
fir/spruce mix and the existing one-in-five harvestable chance.

Interactive group painting now adds only the new flora presentations and
rebuilds their affected spatial batch cells. It no longer destroys and recreates
every district tree, recalculates every tree shadow, or rebuilds all flora
batches after each pointer update. Whole-coverage generation, load, Undo, and
explicit bulk refresh retain the full-refresh path because those operations
replace or restore the complete flora set.

An isolated dense fixture with 1,600 existing flora records and a 12-tree group
measured 77.554ms for incremental insertion versus 1,555.714ms for the former
full refresh on the same 1,612-record state (20.06x faster). This synchronous
EditMode CPU comparison on one machine does not establish live frame time, draw
calls, allocations, or long-duration stability. A presentation-identity test
confirms unrelated trees survive insertion.

## Current runtime — realistic seasonal clusters (September 17)

Supersedes the artwork and summer-only rendering described below. Runtime uses
`Flora/ForestClustersRealisticV01`: six original RGBA images copied without pixel
changes from the approved realistic art studies (two palettes × summer/autumn/winter).
The five existing saved IDs remain intact: 01/03/05 use palette A, 02/04 use B.
These are two color variants of one arrangement, not five distinct silhouettes.
No regeneration or save migration is needed for existing **cluster** records;
old individual-tree forests are not automatically converted.

Each texture is 1254 square, 50 pixels/metre, pivot (.5,.027), sRGB, clamp,
trilinear filtering, mipmaps with alpha coverage at .02 (winter .12). Winter
renderers use the matching .12 alpha cutoff to reject faint residual canopy
pixels without editing the source PNGs. The original images and
previous V01 runtime artwork are preserved. Existing cluster clearance, density,
selection, batching and separate harvestable firs are unchanged. The standing
Cilician fir now uses `Flora/CilicianFirRealisticV01`: a versioned realistic
evergreen cutout with identical seasonal copies. Its prior falling sheets and
stump, lumber yield, ID, worker targeting, and save representation are retained.
The previous
world-space summer palette multiplier is disabled on clusters so the approved
artwork colors remain visible.

Clusters read the district's existing Labor.SeasonIndex without modifying it:
0 summer, 1 autumn, 2 winter, 3 spring, repeating. Spring shares summer artwork.
No calendar skip or preview UI was added. This seasonal feature covers mixed
clusters, not a whole-world seasonal overhaul of terrain/buildings/other flora.
Winter deciduous shadows use open branch proxies and lighter contact shade;
the fir retains a canopy shadow. Five shadow contacts are recalibrated against
the new artwork. These remain approximate proxies on steep ground.

At load/bulk flora refresh, six shared sprites are warmed. A maintained registry
tracks only cluster renderers. Routine Update reads one season value. At a change,
one registry snapshot is processed at most 16 clusters per frame, reusing source
objects and rebuilding deduplicated affected batch cells. New flora uses the target
season; deletion skips inactive queued objects; a new season supersedes pending
work; load/Undo/refresh clears pending work. No full district/terrain rebuild or
worker algorithm change. The transition intentionally appears progressively.

Validation: `Validation/realistic-forest-v01/`. 36 forest and 83 regional tests
passed. Actual normal windowed Game-view summer/fall/winter close/far captures
inspected. Dense isolated Little River Bend copy: 4060 records, 849 clusters
(includes retained individual trees), 54 frames per transition. Back-to-back CPU
slice measurements: worst slice 35.24ms autumn, 31.72ms winter, 42.06ms spring;
total work .84–.96s. This replaces a .35–.41s single-frame bulk transition, but
increases aggregate CPU due to repeated local batch rebuilds. 10000 unchanged
season checks took .840ms. No whole-frame speedup, reliable allocations, or
long-run stability claimed. All three user save files remained byte-identical.


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
clusters reserve the same larger footprint. Generation and
`RebuildAllFloraPresentations` remain explicit bulk operations; they can still
stall for hundreds of milliseconds.

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

`RebuildAllFloraPresentations` does not null the unrelated cloud-layer reference.
This preserves cloud zoom controls after regenerating forest; clouds themselves
are not rebuilt.

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

## September 17 — visible shadows and restrained summer palette

Fixed sun-aligned shadow collapse with transverse canopy volume, added soft contact
shade, raised opacity, and clipped shadow geometry at district edges. District
forest foliage now has deterministic world-space summer green variation; individual
firs are slightly lighter. Source PNGs unchanged. 30 forest +83 regional tests
passed. See Validation/forest-shadow-palette-v03/README.md for visual evidence,
profiling limits and pending season-preview decision. No season UI added yet.
