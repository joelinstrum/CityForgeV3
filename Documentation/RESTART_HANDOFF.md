# Restart handoff — September 15, 2026

Workspace: `/Users/joelinstrum/dev/CityForge - V3`

Branch: `feature/regions-rivers-mountains-hills`

The current work is saved on disk but is not committed. Preserve all working-tree changes, including the earlier map-layer and pencil-tool work.

## Latest completed work

- Region map layer dropdown: towns/cities, district names/borders (off by default), rivers, topography, transportation.
- Terrain → Roads → Create a national pike: pencil drag, release to name, save or cancel.
- Generated straight, curved, T, and cross dirt-road artwork. The user approved the Unity preview as “fantastic.”
- New district Dirt Road placement and pencil-created national pikes use the new dirt package: 8m roadway on a 10m tile.
- Pencil routes create connected district road tiles, repair junctions, preserve existing road materials, and cross district boundaries.
- River crossings leave gaps and resume on the opposite bank. No bridges or traffic connections span water.
- Road save failure restores the prior district road lists.

## Evidence and implementation notes

- 34 targeted Unity EditMode tests passed.
- Live pencil/name/save/reload check created 397 district road tiles and verified none overlapped river water.
- Preview: `QA/NationalPike/dirt-pieces.png` (QA directory is ignored by Git).
- Runtime images: `Assets/CityForgeV3/Resources/CityForgeV3/Roads/NationalPikeDirtV1/`.
- Generated PNG originals are preserved. `NationalPikeDirt.cginc` calibrates their geometry and shared edge material; use the Unity preview to judge assembled pieces.
- Detailed behavior: `Documentation/NATIONAL_PIKE_DRAWING.md`.
- Exact selected generation prompts and lineage: `Documentation/NATIONAL_PIKE_DIRT_ART.md`.
- Tests: `Assets/Tests/EditMode/NationalPikePlacementTests.cs`.
- Placement model: `Assets/CityForgeV3/Runtime/World/NationalPikePlacement.cs`.
- Unity was left out of Play Mode after restoring the user's original screen from the isolated QA fixture.

## Scope remaining for future requests

Surface policy is dirt in Founders/Industrial, concrete in Discovery, blacktop in Modern. Dedicated later-era concrete/highway artwork, highway geometry, and era advancement UI remain future work. Existing map-only pikes are not automatically converted into district tiles. Existing DirtRoadV1 tiles keep their original artwork.

No next task was requested; the user is restarting their computer. Resume from this state and ask what they want to work on next if needed. Do not repeat completed image generation or overwrite approved artwork.

## Subsequent orientation fix

The user reported rivers and roads switching diagonals between the region map and district. Fixed the region UI projection (local Y reflection, -45-degree rotation) to match the existing district camera; counter-reflected labels to keep text readable. Saved geometry was unchanged. 35 targeted tests now pass, including a comparison of the real UI transform and district camera, plus the live pencil save/reload check.

## Preferred shape restored

The user preferred the old region outline while retaining the corrected feature orientation. `RegionMapProjection` now applies reciprocal display scales to recover that outline without changing saved geometry. Labels cancel the scale. Non-square regions use cartographic proportions at region level; district view keeps physical proportions. All 35 tests and live pencil save/reload passed.

## Horizontal map labels

Town, district, and road labels now read horizontally left to right using screen-aligned anchors. Verified in Unity; 35 tests passed. Region shape and corrected terrain orientation remain intact.

## Drawn major and small rivers

Terrain → Rivers now replaces the dominant-direction selector with Create Major River and Create Small River. Generation uses default Varied flow. Both buttons close the modal and activate the shared pencil with a blue stroke; release saves directly, and Escape/Cancel discards the stroke. Major rivers are 64m wide with the existing deep/thin-bank profile; small rivers are 18m wide and shallow. Paths clip across district boundaries and persist through automatic regeneration. Drawing over roads removes covered road tiles, leaving gaps until bridges exist. Building conflicts reject the stroke; save failures restore the prior river and road lists.

Validation: all 37 targeted EditMode tests passed, plus live major/small button, pointer, release, save/reload checks and the national-pike drawing regression check. The isolated QA fixture was restored; Unity remains in Play Mode. Latest request is complete; changes remain uncommitted.

## Remove all region rivers

Added Remove Rivers beside Create Major River and Create Small River. Clears region paths and every district river list, saves immediately, invalidates district composition, and refreshes the map. Save failures roll back the river lists. Live isolated-fixture verification passed for generated, hand-drawn and district-local river removal via the actual button, followed by save/reload; transport routes remained unchanged. Restored the original screen afterward.

## Mississippi-style Major River

Added a third size: Major at 128m, twice the existing 64m river now labeled Large. Small remains 18m/shallow; Large and Major are deep with thin banks. Pencil preview and saved map lines distinguish Major at twice the Large width. Existing saves retain their stored widths. All 37 targeted tests passed, plus live drawing and save/reload checks for all three size buttons. Restored the original screen afterward.

## River ordering and stroke cleanup

Buttons now read Major, Large, Small, Remove Rivers. River-only input filtering ignores small jitter, turns over 70 degrees between samples, and crossings/near returns to older parts of the same stroke. Width-relative spacing plus two endpoint-preserving corner-cutting passes provide a smoother fixed-width channel. Preview and commit share the smoothed geometry. Separate river strokes can still overlap/join; existing rivers are not migrated. All 39 targeted tests passed, including broad bends, jitter, backtracking, self-crossing and preview/save geometry agreement. Live river and road drawing/save checks passed; QA fixture restored.

## Region/district shape mismatch corrected

City 051 is physically 4-by-2 in a 28-by-20 region. The prior reciprocal map scaling made it look nearly square. Removed that distortion and matched region vertical projection to the district camera elevation (sin 20 degrees). This supersedes the earlier preferred-silhouette compromise: both views now preserve identical relative geometry, including river points. Saved dimensions and content are unchanged. All 39 tests passed with a strengthened actual-camera projection comparison, plus live river and road pointer/save checks. Original screen restored afterward.

## Persistent river pencil and connections

River drawing stays active after saving a stroke, preserving the visible blue pencil until Escape/Cancel. Both endpoints snap onto nearby saved river centerlines, including district-local rivers. Snap tolerance is the sum of half-widths plus 16m. Preview and saved geometry share the same snapping/smoothing; widths remain selected per stroke. All 40 targeted tests passed; live consecutive-stroke drawing verified the retained tool, connection to the previous saved endpoint and save/reload. QA fixture restored.

## District river sculpting and flush border rendering

Water → River Tools now offers Shape River, Soften River and Erase River with a 40–400m radius. District generation is removed from the menu. Drag previews use pooled lines and a brush ring; release commits/saves once and refreshes river terrain, preserving other presentations. Escape cancels; district undo works. Shape can snap interior endpoints to another channel; border crossings/tangents stay anchored. Erasing splits a river into retained sections. RiversEditedLocally persists district overrides so regeneration cannot resurrect removed water; Remove Rivers clears overrides. Building conflicts reject shape changes.

River centerlines extend beyond district crossings, and water/bank triangles are clipped to the district rectangle after junction merging, removing angled cutoffs/overhangs. All 45 targeted tests passed. Live isolated checks passed actual Shape and Erase pointer gestures, save/reload, mesh bounds, and undo persistence. Menu and clipped water were visually inspected. Original screen restored.

## Map grass color matched to district texture

Grassland and the map base now use RGB (73,89,36), sampled from the default district grass texture mean (72.68,89.42,36.12). The flat map style and other biome colors are retained. Visually checked in Unity using the isolated biome fixture; restored the original screen.

## Visible Water menu tools

Replaced the ambiguous wave submenu button with directly labeled Select Water, Shape River, Soften River and Erase River buttons. Widened only the Water flyout and placed brush radius beneath the tools. Corrected obsolete generation tooltips. Verified the actual Shape button activates directly and visually checked the final layout; restored the QA fixture afterward.

## Fixed-width river redraw and lightweight dragging

Replaced the Shape brush with a selected-channel redraw: mouse-down picks a river and locks its width/depth, dragging traces a replacement reach, mouse-up splices the smoothed path between projected start/end positions on the source river. Unselected rivers and untouched reaches remain intact. Water UI shows locked width. All district river tools now postpone edit calculation and textured mesh/terrain rebuilding until release; dragging only updates a bounded path and pooled untextured outlines, with no terrain sampling. Click-only/perpendicular no-progress strokes do not commit.

46 tests passed; live Shape/Erase/undo checks passed and explicitly verified data/river mesh identity stayed unchanged during dragging. Original QA screen restored.

## Smaller brushes and river refresh performance

Halved adjustable district brush radius: default 80m, range 20–200m. The locked-width redraw still preserves the selected river's authored width. Profiled RefreshRivers: the fixture redraw spent 897ms in ground decoration versus 7ms river mesh work (904ms total). Decoration repeatedly queried every river segment. RuntimeRiverSurface now indexes segment bounds in 128m cells and precomputes lengths/along-distance; queries only consider nearby segments. RefreshRivers also suppresses the duplicate decal rebuild inside RefreshElevation. Editor logs report mesh/terrain/decal timings.

Same fixture redraw refresh: 234ms after change (7ms mesh, 227ms decals). Read-only copy of saved Test Region II City 042 (3 rivers, 280 points) refreshed in 392ms; original save untouched. All 47 targeted tests passed, including indexed/full-scan query equivalence and repeated query stability. Live redraw validated; QA fixture restored.

## Shared district surface caching

Added value snapshots, dirty chunk matrices and a reusable spatial index for river, road and lot surface edits. Height samples and grass/hill chunks update locally; road refreshes retain unchanged objects. Drag paths defer surface commits until release. Workers untouched. 50 targeted tests passed; live flat/hilly incremental geometry matches full rebuilds, and road checks confirm deferred commit and object retention. Flat river refresh 85–89ms (previously234ms); hilly erase 1,408ms (before constraint indexing3,099ms). Terrain mesh normals/collider/grid still refresh once when heights change. QA state restored. See [district surface cache](DISTRICT_SURFACE_CACHE.md) for API/lifecycle and remaining costs.

## Consistent reshaped river rendering

Replaced spacing-biased bank normals with bounded offset-line joins shared by water and bank geometry. Unevenly spaced stroke points now preserve perpendicular bank width. Water UVs use district-space metre scaling so bends and separate reaches cannot stretch/reset the photograph. Grass banks use the same texture throughout instead of changing variants at every short segment. Stored width/depth and deferred commit/caching remain unchanged. 52 targeted tests passed, including uneven-spacing width and bounded degenerate-join checks. Live reshape passed and close-up Unity inspection showed continuous water/banks; refresh89ms. QA fixture restored. Very tight hairpins remain bounded by a miter limit rather than receiving a general polygon-union remesh.

## Click-to-repair river tool

Added Water → Repair River. Click a river to collapse short duplicate/backtracking spikes, resample by physical arc length and relax corners whose radius is too small for the channel width. Retains endpoints, identity, width, depth and unrelated rivers; healthy straight channels are no-ops. Bounded160-pass repair runs once on click, with existing building-conflict validation, save rollback, district-local override, incremental refresh and undo. Does not merge distinct rivers or rebuild their topology. 54 targeted tests passed; regression covers128m-wide reversal/tight bend, bank-safe turning radius, immutable source and preserved endpoints/settings. Live actual button/pointer, save/reload, one-cache-commit and undo passed; inspected repaired S bend at close zoom with continuous banks. Refresh127ms in isolated fixture. Original QA state restored.

## Shape River width slider restored

Shape River now shows a20–200m width slider and numeric entry. Before adjustment it inherits the clicked river width; after adjustment the chosen width locks for each stroke. Redraw applies that width to the selected river, keeps depth, and uses existing undo/save/cache completion. Soften/Erase keep their radius controls. 55 targeted tests passed including selected-only width override/source immutability. Unity menu visually checked; entered100m and live sculpt/save check passed. QA state restored.

## One-click district river rebuilding

Repair River now executes directly from the Water button for all district rivers; no map selection click. Adds three physical-scale smoothing passes along the general course before bank-radius cleanup, addressing gentle jaggedness as well as short spikes. Endpoint samples and adjacent approach samples remain anchored during smoothing. Width/depth/identity stay fixed. Uses one combined conflict check/save/cache refresh/undo snapshot. 56 targeted tests passed, including district-wide gentle-jagged cleanup and entry/exit tangent preservation.

## Repair replaces channel from centerline guides

RepairAll now consolidates equal-width/equal-depth continuations at nearby interior endpoints and discards pieces entirely inside another channel while retaining distinct border crossings and different depths. Then discards the old point spacing, samples sparse centerline guides at1.25 widths, smooths and builds a fresh corner-cut curve before the bank-radius safeguard. RefreshRivers already deactivates and destroys the complete previous river root before creating all new water/bank meshes. Saved Test Region II City042 contained three separate128m pieces: a main reach, border continuation and interior remnant (within49.2m of main centerline). Prior separate-piece smoothing left internal banks. 58 tests passed including consolidation, border/depth separation and original-state preservation.

Live validation on an isolated copy of saved Test Region II City042: three pieces became one272-point rebuilt channel; visual inspection showed continuous banks. Actual Repair button, save/reload, single cache commit and undo passed. Full repair refresh227ms; subsequent unchanged river refresh22ms. Original save and screen restored.

## City051 disconnected Shape/Repair regression

Saved City051 had two128m-wide reaches with interior endpoints164m apart: previous128m join cutoff left both capped remnants. Repair now joins matching-width/depth interior endpoints within two channel widths. If the reaches point past each other, trims one width of obsolete terminal geometry from each before rebuilding the combined centerline; border endpoints retained. Added a City051 coordinate regression with repeated Shape→Repair;59 tests passed. Live isolated saved City051 copy reproduced the two capped ends before repair and one continuous channel afterward. Actual button, save/reload, single-cache-commit and undo passed. Repair refresh120ms. Original save untouched and QA view restored.

## Open river crossings without brown underside

RiverBedSurface now clips submerged fragments whose camera projection onto the water plane lies outside the district rectangle. This removes the brown exposed bed strip at open border crossings while retaining side banks. Materials receive district bounds and current water elevation. No save/geometry changes. Compiled and visually checked in Unity on a saved-region copy; original view restored.

## Waves follow channel direction

Water mesh UV2 stores the normalized local centerline tangent. Border clipping and junction subtraction preserve/interpolate UV2. RiverWaterSurface advects district-space texture coordinates along that vector for water and whitecaps using two fading bounded phases, preventing unbounded texture distortion through bends. Wave UV perturbation is longitudinal too.60 targeted tests passed including interpolation through clip/subtract; shader loaded in Unity and live repair/save check passed. QA state restored. Flow follows authored point order (no new watershed/downhill simulation).
