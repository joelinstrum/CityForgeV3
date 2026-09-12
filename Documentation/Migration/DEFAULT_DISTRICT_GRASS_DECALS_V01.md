# Default district grass decals — experiment v01, 2026-09-12

Requested by Joe: try the Lot editor grass decals as automatic district terrain dressing.
Implemented on `experiment/district-grass-decals`; Joe accepted the appearance and density: “That looks fantastic”.

`DistrictGroundDecals` uses the original `Decals/Grass/leaves-01..03` PNGs and
`SoftGroundDecal` shader. A stable tile-ID/cell hash chooses texture, quarter-turn,
position jitter, and 5–8 m patch size on an 8 m grid. No global random state is
consumed. It builds automatically for existing and new districts through the normal
river/ground initialization path; no decal records or migrations are written to saves.

Chunks span 128 m and share three materials. Generated meshes/materials are disposed
on rebuild/destruction. Detail fades from orthographic size 80 to 180 m and is disabled
at overview scale. The layer uses the Lot decal queue 3003, above district road artwork
at 3002, at elevation .17 m. River-channel/bank patches are excluded by sampling the
center and corners; river edits regenerate the dressing. Very narrow crossings between
samples are a remaining edge case. Hosted lot terrain with varying heights is not
conformed by this district layer; this experiment targets the flat district surface.

Flora art, materials, shadows, and camera initialization order are untouched. No tree
brightness/exposure overrides were added. The original default grass remains unchanged.

## Verification

- Unity compiled after adding the runtime layer and editor QA tools.
- Loaded the existing saved region through RegionSaveStore.Load, selecting city-033
  (2,418 flora entries, including stones; one river; no district roads).
- Reloaded that district from disk, rebuilt the world, restored close zoom/pan:
  `DISTRICT SAVED RELOAD PASS decals=21768 alignedTrees=2082`.
- Normal docked Unity Game view: compared decals OFF/ON at LOD1 and close LOD0.
  Leaf litter appears on the grass, river remains clear, and loaded tree cards retain
  their orientation without diagonal slicing.
- Saved-region file SHA-256 hashes are unchanged before/after the checks.
- `git diff --check` passed. Full project tests and large-district performance benchmarks
  were not run. Road layering uses the existing render contract but was not visually
  checked in this saved fixture because it contains no district roads.

Evidence: CityForgeMCP/artifacts/district-grass-decals-v01/.
Joe accepted this density and appearance on 2026-09-12: “That looks fantastic”. Preserve this as the default district decal baseline; the verification limits above still apply.

## Review controls

City Forge > Flora > Default District Decals On / Off changes only live visibility.
Load Saved District Decal Preview requires a fresh splash and refuses to replace live
work. Check Saved District Decal Reload refuses when the current tile differs from its
saved data. It checks a real disk load, repeat patch count, and non-empty tree alignment.
The preview helper initially needed explicit world creation before the asynchronous
preloader; that helper timing error was corrected before the successful run above.
