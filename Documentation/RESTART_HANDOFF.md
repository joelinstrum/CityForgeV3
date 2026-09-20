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


## September 16 — Riverbank material pass V1

Verified the prior handoff commit `2fa4b57` was pushed and merged via PR #13; `origin/main` was `380c54c`. New local branch: `feature/riverbank-materials`, based on that merge. Work below is uncommitted and not pushed.

Added three versioned generated bank textures, a dedicated bank shader and distance-based signed bend weights. Ordinary reaches use grass/pebbles, inside bends blend toward gravel, outside bends toward exposed earth. The shader blends fixed material samples by the actual waterline and fades into terrain, with district-space detail to avoid stretching through bends/steep slopes. All cross-section bands share the treatment; existing vertices/elevations, width/depth, water animation, border/junction clipping and surface cache lifecycle remain intact. No workers/labor changes. The first artwork direction was approved; final in-game visual acceptance is pending.

66 targeted EditMode tests passed at 16:30:45 UTC (fresh XML). Actual normal windowed Game-view inspection covered detail/close/far, S-bend, shallow tributary/junction, borders and isolated copies of saved City 042/051. Both saved copies passed two Shape → Repair cycles, save/reload and repair undo. Synthetic Shape/Erase UI Toolkit pointer, deferred cache, retained meshes and undo checks passed after waiting for the district preloader. No fresh physical OS-mouse gesture pass is claimed; the computer-use tool selected the separate Review process after V3 restarted, so visual inspection used the dedicated bridge's actual Game-view captures with a non-maximized guard. No substitute camera. Three user save JSON hashes are unchanged. QA fixture restored; V3 is in Play Mode at the title screen, Review remains untouched.

See `Documentation/RIVER_BANKS.md` for runtime/material contracts, exact prompts, lineage and limits; reports in `Documentation/Validation/river-banks-v01/`. Final local previews: `QA/RiverBanks/final-bend.png` and `final-closeup.png`. QA helper `/tmp/map-layers-command.py` now supports `windowed-game`, and after `prepare`, `bank-fixture`, `bank-view`, `bank-bend`, `bank-detail`, `bank-wide`, `bank-border`, `bank-check`, `bank-repeat`, `bank-capture`. Existing saved-copy commands `river-perf-prepare` (City 042) and `river-gap-prepare` (City 051) remain useful. Wait for scene loading before arm/gesture commands; always `restore` afterward. Current V3 log is `/tmp/cityforge-river-bank-editor.log`.

## September 16 — V1 rejected; continuous shoreline artwork V2

The user explicitly rejected V1 as far from the reference and challenged the tile-by-tile approach. V2 now uses whole 48m shoreline compositions along river arc length (16m cross-bank coordinate scale), independent of the lot grid. Two new wide paintings preserve coherent rock clusters, grassy fingers and sedge patches; slow variant blending and curvature vary their appearance. No narrow mirrored strips. Fixed submerged pixel streaks by blending to fine gravel beneath the shoreline composition. Existing river geometry, editing, flow, saves and surface cache remain unchanged. V1 artwork is retained; V2 assets in Water/River/BanksV2. This is still textured ground, not new 3D props or physical erosion. Do not describe it as matching/approved by the user.

Fresh 66/66 targeted EditMode tests passed at 16:49:07 UTC, including both V2 asset imports and longitudinal-coordinate interpolation through clipping. Live isolated copies of City042 and City051 each passed two Shape→Repair cycles, save/reload, repair undo, deterministic bank meshes and unchanged cache on no-op rebuild. Surface cache check passed. Actual non-maximized Game-view screenshots inspected at closeup, bend, border and saved layouts; no alternate camera or new physical mouse test. User save hashes unchanged; fixture restored. V3 remains in Play at title, separate Review editor untouched. Current previews: QA/RiverBanks/v02/closeup.png, bend.png, city-042.png, city-051.png. Evidence and exact built-in imagegen prompts: Documentation/Validation/river-banks-v02. See current section of RIVER_BANKS.md. Changes remain local and uncommitted on feature/riverbank-materials.

## September 16 — V3 fewer grassy fingers

User endorsed V2 as much better, requested less repeating grass creeping into rocks. Added versioned BanksV3/open-gravel.png, a built-in imagegen edit of V2 shoreline-gravel with most descending grass and prominent sedge tufts removed. Retained V2 sources. Open gravel now dominates the blend; smooth deterministic irregular reach noise replaces sine-driven material/fade variation. Full 48m artwork mapping retained. No geometry, water, simulation, or cache changes.

66/66 targeted tests passed fresh at 16:55:53 UTC, including V3 texture import and shader compile. Live isolated bank-check passed; inspected actual windowed Game-view closeup and bend, with longer uninterrupted gravel stretches and sparse grass intrusions. QA fixture restored. Previews QA/RiverBanks/v03/closeup.png and bend.png; before.png is same-camera V2. Exact prompt/provenance and evidence in Documentation/Validation/river-banks-v03. Still local/uncommitted.

Save-audit caveat: the user had opened a real district between turns. Restoring that screen before stopping Play triggered existing DistrictLabor.OnApplicationQuit/PersistDistrictRegion at 09:54:55 local. One user save hash changed (497dfb8d8f264e89b7e67d2bd7ec23de.json); two stayed unchanged. Only pre-test hashes were captured, so individual changed fields cannot be verified. No manual user-save edits or rollback. Future QA should snapshot full bytes and avoid restoring a live district immediately before Play stop; do not alter labor production code for this. User was informed of normal save-on-exit.

## September 16 — Default district meadow across 4 × 4 lots

User endorsed V3 banks, requested richer default grass matching supplied reference using one 4×4 composition. New versioned Terrain/MeadowV01/meadow-4x4.png covers40×40m (four10m lots each axis). DistrictGrassResource/scale select it. New district-only MeadowGroundSurface retains existing shadow/time-of-day/relief behavior and blends four deterministically offset texture samples to suppress repeated patch grids. Integer hashing fixes a visible Metal precision artefact seen with a sine hash; shader target3.5. Existing canonical grass/bank art preserved. Ordinary flat/hilly districts use meadow; mountains explicitly retain calibrated old5m grass/triplanar contract. Shared ground cache, roads, worker/labor code unchanged.

66 tests passed at17:23:56 UTC; final shader hash refinement compiled/live-checked afterward. Actual windowed Game-view closeup, bend, hills inspected. Flat/hilly surface-cache equivalence and live bank-check passed. Saves backed up fully before QA; all3 hashes unchanged afterward. Fixture restored at title. Final previews QA/MeadowV01/closeup.png, bend.png, hills.png. Details/prompt/provenance: Documentation/MEADOW_GROUND.md and Documentation/Validation/meadow-v01. Local/uncommitted on feature/riverbank-materials.

## September 16 — Visual approval and commit handoff

User approved the final meadow and riverbank appearance ("substantially better", "looks fantastic") and explicitly requested an immediate commit to prepare a PR. This commit packages the complete riverbank/material work, versioned artwork, meadow ground, regression tests, QA helpers and validation documentation on feature/riverbank-materials. The other session is preparing a separate PR with performance improvements; those changes have not been integrated here. Workers/labor optimization remains untouched. Latest verification is documented above:66 targeted tests passed, followed by final shader compile/live checks and flat/hilly surface-cache checks. User saves unchanged during the meadow pass.

## September16 — Clouds at the farthest district zoom

New branch feature/distant-clouds based on main7eeb795. Added six batched distant cloud billboards and a shared-terrain projected shadow pass, visible only at LOD5Billboard; all closer stops deactivate both. Two generated transparent cloud assets, slow shader-driven drift, soft edge/wrap fade and existing time-of-day tint. Built-in pipeline retained: this is not HDRP volumetric clouds. Two renderers, no per-frame district scans/rebuilds. Cloud geometry/materials have explicit cleanup; shadow mesh follows terrain mesh replacement by reference. No worker/labor changes or persistence writes.

73targeted tests passed at18:35:04 UTC. Live all-zoom/mesh-identity/unchanged-model checks passed; windowed actual Game view inspected at LOD5 and LOD4 on flat and isolated saved Little River Bend (2,947flora). Final120-frame profile medians3.35ms off/3.67ms on, p954.02/4.48ms; editor observation only. Fixture restored. Documentation/DISTANT_CLOUDS.md contains contracts, limitations, artwork lineage and exact prompt links; evidence in Documentation/Validation/distant-clouds-v01, previews QA/CloudsV01. Changes uncommitted.

Important tooling correction: both Unity checkouts shared the old global QA command path; an incoming test request reached Review. This checkout's bridge now uses project-specific filenames in OS temp. Use python Tools/Unity/map-layers-command.py, not the old /tmp/map-layers-command.py. Tests now write a project-specific XML in OS temp. Existing older river QA scripts still assume automatic saving; main switched to manual Save, so use explicit scratch Save actions when reusing persistence checks.

### 2026-09-16 — sparse clouds and persistent shadows

Supersedes earlier cloud visibility/count behavior: at most two independently randomized cloud slots with soft arrivals/departures and rest periods; bodies at LOD4 and LOD5, shadows at all six levels. Coarse alpha coverage feathers cloud edges. Shared shader timing keeps hidden bodies and shadows synchronized without CPU animation or district scans. Private random seed resets only with layer construction. 73 fresh EditMode tests pass (19:06:12 UTC); isolated saved 2,947-flora district live checks and far/near profiles recorded in Documentation/Validation/distant-clouds-v02. Fixture restored, no Save. Details and performance limits in DISTANT_CLOUDS.md. Work remains uncommitted on feature/distant-clouds.

### 2026-09-16 — higher clouds and closer far zooms

Raised cloud center altitude from terrain height + 80m + 0.18×cloud size to terrain height + 140m + 0.25×cloud size. LOD5 orthographic framing now equals former LOD4 (fullFit); LOD4 is 0.75×fullFit, a 25% smaller view span. Camera radii now 2400m/1800m respectively, retaining mountain near-plane safeguards. LOD0–3 unchanged; bodies still LOD4/5 and shadows all levels. Live dense-copy visibility/mesh/model check passed; flat actual Game-view captures inspected at both far stops. QA fixture restored without Save.

Fresh targeted EditMode run: 73/73 passed at 19:28:14 UTC; evidence in Documentation/Validation/distant-clouds-v03.

### 2026-09-16 — closer LOD3

User requested LOD3 40% closer. Its orthographic half-span is now fullFit × 0.42 (formerly × 0.70); other zoom settings and cloud behavior unchanged. Unity recompiled and live six-stop cloud visibility/mesh/model checks passed on the isolated dense saved district; fixture restored without Save.

### 2026-09-16 — final 10% adjustment to LOD3/4

Reduced LOD3/4 orthographic view spans another 10%: fullFit multipliers now 0.378 and 0.675 respectively. Other stops unchanged. Existing cloud drift remains 1.1 m/s on X and 0.35 m/s on Z (~1.15 m/s total), shared by bodies and shadows. Shader uses scaled Unity time, so simulation Pause also pauses clouds; Go resumes them. Unity compiled and live six-stop visibility/mesh/model check passed on isolated dense copy.

### 2026-09-16 — 50% faster cloud drift

Increased shared body/shadow drift vector from (1.1, 0.35) to (1.65, 0.525) metres per second. Appearance/rest timing and fade durations remain unchanged. Unity shader refreshed; live cloud shader/zoom/mesh/model checks passed on an isolated dense saved district. Fixture restored without Save.

### 2026-09-16 — verified cloud motion correction

User correctly reported clouds fading without moving. Two real Game-view captures 10.867 seconds apart on an isolated saved Little River Bend showed exactly zero displacement of the same cloud (normalized image correlation 1.0). Earlier visibility/compilation tests did not establish motion, and the earlier separated images could include cloud respawns.

Replaced the shader built-in `_Time.y` dependency with `_CloudTime`, explicitly written to both cached materials each LateUpdate from elapsed `Time.realtimeSinceStartupAsDouble`. Two constant-time uniform writes, no mesh updates or district scans. Cosmetic weather now continues through simulation pause; zoom does not reset it. Wind remains (1.65,0.525) m/s. The exact underlying built-in shader-time failure was not isolated; do not claim a specific Unity/Metal defect.

Same-camera, same-silhouette capture comparison after correction: 14 pixels right and 9 pixels upward in 13.298 seconds, normalized correlation 0.99942. Both material clocks advanced together from 0.8805783 to 14.17375. The before/after measurements distinguish translation from fading or respawning. Live visibility/mesh/model/shader checks passed; scratch fixture restored without Save. Short 2,947-flora district profile: off/on median 7.71/8.27ms, p95 17.97/20.87ms, editor-wide and noisy. Unchanged zoom calls: 10,000 in 0.211ms, zero reported bytes. Evidence in Validation/distant-clouds-motion.

### 2026-09-16 — CPU positions and district-scaled motion

User again reports stationary clouds after the clock fix; exact viewed district/zoom still unknown (asked asynchronously). Do not claim the reported view has been reproduced/resolved. Computer Use selected the separate Review editor, which has no cloud source; no Review edits.

Replaced shader weather calculation with CPU EvaluateMotion for exactly two slots. Cached two-element Vector4 position/opacity array is uploaded to body and shadow materials each LateUpdate. No allocations, object scans or mesh uploads in that path. Integer hash replaces GPU sine hash; double elapsed time preserves scheduling. Base drift multiplied by max(1,min(width,depth)/640) so large-map movement remains perceptible. Real-time motion continues while simulation is paused.

76 tests passed fresh at 19:56:18 UTC, including same-lifetime continuous movement at 640,2560,7680m. Live isolated saved Little River Bend checks pass; both material motion arrays equal. Actual windowed Game-view pair 195658667 / 195707905 shows same cloud translated (19,-13) pixels in 9.238s, normalized correlation 0.99808 despite fading. Original images under QA/RiverBanks. Dense 2947-flora profile off/on median7.89/8.16ms p9520.18/20.59ms; editor-wide short sample only. 10000 unchanged zoom calls0.790ms/0reported bytes. Fixture restored without Save. Evidence in Documentation/Validation/distant-clouds-cpu-motion. Changes remain uncommitted.

### September16 — hill meadow material V1

User confirmed clouds moving, then requested hills matching the supplied natural-variation reference. Added HillsV01/crest-meadow-4x4 generated art, height/slope/irregular-field blending in MeadowGroundSurface, configured only for ordinary hills. Broad colour fields on existing vertices, four offset samples per source to reduce repeats. Flat meadow and mountain shader contracts unchanged; no geometry/save/worker changes. Detail pebbles are texture artwork. User visual acceptance pending. Details, prompt/lineage, performance limits in Documentation/HILL_MEADOW.md; previews QA/HillsV01. Dense profile shows ~4.5ms median increase, no extra draws/geometry. Live shader/cache checks pass; fixture restored without Save. All work still uncommitted on feature/distant-clouds. QA play now clears editor pause; resume command added after a shader error paused live QA.

Fresh targeted EditMode results: 77/77 passed at 2026-09-16 21:35:28Z.

### September16 — preserve meadow hue on hills

User likes the lighter crest art but questioned the different green. Ordinary hills already inherit MeadowV01 as their base; the HillMeadow fragment function additionally multiplied RGB by (.91,1.005,.95) in low areas, introducing a green bias. Replaced both low/crest colour multipliers with neutral scalar brightness (.96 to1.055), retaining the lighter crest texture, height/slope blending, and broad irregular variation. No texture replacements or hill-overlay changes. Live shader/material and surface-cache checks passed; windowed Game-view capture inspected at QA/RiverBanks/Pine-Ridge-214720651.png. Fixture restored without Save.

## 2026-09-16 — Main grass update merged into cloud/hill branch

- Preserved cloud/hill work in checkpoint `6a175fc`, then merged `origin/main` at `3350d59` into `feature/distant-clouds`.
- Retained both cloud visibility and incoming grass texture scale updates in SetZoom. Main also includes building assets, lot fixes and performance improvements.
- Hill broad colour noise now uses terrain-local metres, so near-zoom grass UV scaling does not shift hill wear/colour regions. Crest texture detail inherits the same zoom scaling as default meadow; lighter artwork and neutral brightness treatment remain.
- Fresh Unity EditMode run: 78 passed, 0 failed, finished 2026-09-16 22:14:58 UTC. Live isolated hills passed all six grass scales, retained hill artwork/mesh identity and district data; bank shader, cloud visibility, and surface cache checks passed. Actual Game view captures reviewed. Fixture state restored; no progress saved.
- Validation: `Documentation/Validation/main-grass-cloud-hill-merge/`. No new performance benchmark for this uniform/vertex-coordinate integration; prior hill shader cost remains documented in HILL_MEADOW.md.

### September 16 — stronger cloud shadows and far meadow filtering

Cloud daytime shadow strength .13 -> .34, night .035 -> .065, darker neutral shadow RGB (.055,.075,.10). Alpha mask uses mip6 instead of mip4 for broader soft edges. Two cloud motion slots, all-zoom shadows and cloud visibility unchanged.

Farthest zoom LOD5Billboard alone sets _DistantMeadow=1 on ordinary meadow. Existing four texture samples use a minimum 1/8-source gradient footprint to average artwork before the offset-cell blend and suppress its distant diamond lattice. Closer zoom texture scale/detail and mountain materials remain unchanged. No added texture fetches, draws, meshes or district scans; no new performance benchmark.

78 targeted tests passed at 22:22:07 UTC. After final shadow feathering, live shader and all-six-zoom cloud checks passed. Actual windowed Game-view far capture QA/RiverBanks/Pine-Ridge-222516695.png shows soft pronounced shadows and reduced meadow repetition. Also reviewed isolated saved dense district; temporary state restored without saving. Changes uncommitted for user visual review.

### September 16 — district rain sequence

Environment cloud icon now offers Rain and Clear Skies, replacing nonfunctional
Clouds/Mist previews. Rain gathers a continuous overcast for 5 real-time seconds,
rains for 10 seconds only at full coverage, then clears for 4 seconds. Repeat
Rain restarts; Clear Skies and leaving the district cancel. No persistence or
regional-climate changes. Overcast visible at the two farthest zooms; rain and
shading at all zooms. Uses a projected feathered canopy, existing cumulus art,
and procedural pale streaks, two fixed renderers sharing one quad. No lot rain
collision/reflection scans, terrain rebuilds or worker changes.

80 targeted tests passed fresh 22:45:09 UTC; live paused dense saved copy checked
5.01s rain start, 15.01s end, automatic clear and unchanged district JSON. Final
far/close Game-view captures inspected. Fixture restored without saving. Cost
and rendering limitations: Documentation/DISTRICT_RAIN.md; evidence under
Documentation/Validation/district-rain-v01. Still uncommitted with previous
stronger-shadow/distant-grass changes. QA commands bank-storm-start,
bank-storm-clear, bank-storm-review; the latter runs a ~23s coroutine, restores
simulation pause in finally, and writes /tmp/cityforge-storm-check.txt. Wait for
DONE before changing its fixture.

### September 16 — rain mist

User approved the district rain and requested lot-style mist. Added a separate
MistIntensity envelope: builds during the first 2s of rain, holds through the
last drops, fades during the 4s clearing phase. Existing rain shader composites
lot fog RGB (.72,.73,.72), max opacity .28, behind streaks; no extra draw or
texture sample. Clear Skies/scene exit retain immediate cancellation.

81 tests passed at 22:52:12 UTC. Live close-zoom dense saved-copy cycle confirmed
mist after rain stops, zero mist on completion and unchanged district JSON.
Actual Game-view captures 225325833 (rain/mist) and 225335836 (mist fading, no
drops) under QA/RiverBanks/Little-River-Bend-*.png inspected. Short editor sample
clear/rain medians 4.91/5.69ms, p95 12.28/12.26ms; this measures the whole storm,
not isolated incremental mist cost. Fixture restored without Save. Evidence:
Documentation/Validation/district-rain-mist-v01. Changes remain uncommitted.

### September 16 — temporary district snow test

Added Snow to Environment. Same 5s cloud gathering, then exactly 25s from
first snowfall: 10s falling/accumulating, 10s settled, 5s melting. Snowfall uses
round drifting flakes in the existing storm pass. Snow cover uses one extra
renderer referencing the existing terrain mesh, with slope-aware material and
no terrain/data edits. Mist clears after snowfall; settled ground snow persists
independently. Weather switching, Clear Skies and scene exit reset all cover.
This is explicitly temporary test functionality; no roof/tree snow added.

83 tests passed fresh 23:01:49 UTC. Full live cycle on isolated dense save copy
verified cover gating, accumulation/hold/melt/cleanup and unchanged JSON. Three
Game-view captures inspected; fixture restored without Save. Profile and
limitations in Documentation/DISTRICT_RAIN.md; results under
Documentation/Validation/district-snow-v01. All weather work still uncommitted.

### September 16 — reversible meadow hue study

User loves default texture but wants a few shades toward forest green, hue only.
Added _GrassHueShift to MeadowGroundSurface, default0 so other shader consumers
retain their palette. Ordinary district ground opts into .035 turns (~12.6deg
maximum), weighted to yellow/green pixels; brown soil and neutral pebbles largely
excluded. HSV value/saturation stay unchanged. Applies after hill artwork blend
to keep hills consistent. Original PNG, bank materials and mountain material
unchanged. Set value0 to revert, .055 is the stronger comparison. No extra
texture sample or draw; added shader arithmetic not separately benchmarked.

Actual same-camera Game-view original/.035/.055 captures inspected and copied to
QA/GrassHueStudy/{original,forest,stronger}.png. Live shader/bank checks passed;
isolated hill fixture checked and restored without Save. Current .035 preview
is enabled for user review, uncommitted. No additional tests for this reversible
visual adjustment. QA bank-grass-hue-{original,forest,deep} switches material.

### September 16 — mixed forest cluster integration

Forest coverage now places five summer mixed-cluster variants in Temperate and
Mediterranean climates, with a 20% candidate chance of a separate harvestable
Cilician fir. One cluster is one saved/selectable flora record; cluster artwork
firs are scenery. Wooded spacing48m, Sparse128m; bounded 16m-times-scale cluster
clearance. Tropical individual trees and Desert restriction retained. Regenerate
explicitly to replace existing generated standing forests; manual and harvested
trees survive, Save remains manual, undo supported. No worker code changed.

Runtime art derived from approved V03 summer, with baked shadows removed using
image_gen. Sun-driven projected shadows and shared flora batches reused.
Seasonal studies are not yet wired into district seasons (summer still active).
RefreshFlora no longer loses the cloud-layer reference. Full detail, art lineage,
validation limits and benchmark: Documentation/FOREST_CLUSTERS.md. QA bridge
forest-tests; prepare → bank-cloud-dense → forest-review → wait DONE at
/tmp/cityforge-forest-clusters.txt → restore. Forest review restores original flora
itself; restore also returns the prior screen. Frozen editor-only generator is
for comparable before/after QA only. Same saved district layout/seed comparison:
1973→477 records, frame median17.84→15.82ms, draws855→724. User saves untouched.
All this work remains uncommitted on feature/distant-clouds.

Final checks: forest EditMode suite 24/24 passed (2026-09-17 01:01:40–01:01:43 UTC);
regional regression suite 83/83 passed (01:09:54–01:10:00 UTC). Fresh XML reports
are saved in Documentation/Validation/forest-clusters-v01/.

### September 16 — three coverage levels and cluster shadow contacts

Shared district/region UI now offers Heavy, Medium, Light. Existing enum Sparse=1
and Wooded=2 retained for saves (display Light/Medium); Heavy=3 uses spacing/sqrt3.
Medium preserves previous density, Heavy measured3.04x new placements in saved
Little River Bend copy. ForestClusterShadows gives five authored trunk/canopy
proxies per summer variant, ground contacts via five bounded terrain raycasts per
cluster at build/update only, shared batches retained. Canopy shapes approximate;
steep terrain can stretch projections. Individual fir harvesting unchanged.
30 forest tests passed01:30:31 UTC, live dense QA passed and fixture restored.
New validation evidence: Documentation/Validation/forest-coverage-v02; profile and
limits in FOREST_CLUSTERS.md. Explicit Heavy refresh526ms, median frame12.76ms
versus Medium393ms/8.12ms in short Editor sample. User saves untouched. Uncommitted.

Regional regression suite83/83 passed fresh 2026-09-17 01:33:22Z; XML archived beside forest30/30 results.

### September 17 — forest shadow visibility and summer palette

On ui-makeover, uncommitted: fix shadows collapsing when sun aligns with billboard
width. ForestClusterShadows now projects volumetric canopy footprints plus contact
shade; single fir silhouette faces across sunlight. Higher opacity and shader
district-bound clipping. Foliage-mask palette varies cluster greens in world-space
patches, modestly lifts standalone firs. No source-art, save, labor changes.
30 forest +83 regional tests passed; reviewed flat near/far Game views and copied
dense district, restored without Save. Evidence/limits in Validation/forest-shadow-palette-v03.
Season-preview versus calendar-advance question pending; currently summer-only
forest rendering, seasonal cutouts not production-ready. Preserve other-session
MapChrome/RegionEditor/USS edits. QA prepare → bank-fixture → wait loaded →
forest-style → bank-capture; forest-style sets Afternoon for visible shadows.
restore afterward. Shader diagnostics fully removed.

### September 17 — Clear Flora action

Clear Flora now immediately removes all non-stone district flora (manual and
generated trees/clusters, fallen trees/stumps) and sets TreeCoverage=None.
Stones and other district content remain. One explicit scan collects IDs,
DistrictHarvestIndex swap-removes each; RemoveFloraPresentations batches local
render removals. No full terrain/forest redraw or labor algorithm changes.
Flora nudge records and stale selection/paint state are cleared; one Undo restores
all removed content; disk persistence remains manual. Empty action is a no-op.
31 forest tests passed fresh17:57:27 UTC. Live isolated Little River Bend copy
2947 flora: cleared2842, retained105 stones, same terrain-cache revision; exact
JSON restored with Undo. Clear measured16.37ms in Editor (not a frame-time or
allocation benchmark). Fixture restored, no Save. Evidence under
Documentation/Validation/clear-flora-v01. QA prepare → bank-cloud-dense → wait
loaded → forest-clear-review → restore. Changes uncommitted with prior forest
shadow/palette work and concurrent UI edits; preserve all unrelated edits.


### September 17 — realistic seasonal forest clusters in-game

User approved both realistic summer palettes and the fall/winter derivatives,
then requested runtime integration. Added versioned ForestClustersRealisticV01
runtime art, copied unchanged from same-named ArtStudies. Saved cluster IDs01–05
map to two palette variants (odd A/even B); existing clusters change appearance
without regeneration or migration. Prior PNGs preserved. Same placement densities,
clearance, harvestable separate firs, shared batches, manual Save and Undo.

Clusters now follow existing district Labor.SeasonIndex; spring uses summer.
No season skip/preview UI. Calendar and worker/labor algorithms untouched.
New cluster-only registry and staged season swaps (16 per Update), shared sprites
warmed on flora load, retain tree objects/unrelated batches/terrain. Recalibrated
five shadow contacts; bare winter branch proxies, evergreen fir canopy. Winter
alpha cutoff/mipmap coverage .12 removes faint canopy residue. Disabled
old world-space palette multiplier on clusters to preserve approved art colors.

35 forest tests and83 regional tests passed. All three seasons inspected in actual
normal windowed Game view. Dense isolated Little River Bend:4060 records/849clusters,
54frames per season swap. Worst slice~32–42ms; aggregate~.84–.96s (more total CPU,
less single-frame work than initial .35–.41s transition). No measured frame-rate
improvement claimed. Clear/Undo retained105stones, same terrain cache, exact JSON.
Three user saves byte-identical. Evidence Documentation/Validation/realistic-forest-v01.

QA: prepare → bank-fixture (or bank-cloud-dense) → wait loaded → forest-style →
forest-realistic-check. forest-realistic-{summer,autumn,winter}-{close,far} selects
an isolated fixture season/camera; wait for staged transition before bank-capture.
forest-realistic-staged runs actual Update transitions; wait for DONE in
/tmp/cityforge-realistic-forest.txt before restore. Fixtures restored after QA.
Concurrent MapChrome/RegionEditor/USS and previous shadow/Clear Flora edits preserved.
All work remains uncommitted on ui-makeover; no commit or push performed.

### September 17 — realistic standing Cilician fir

Replaced the standing harvestable Cilician fir billboard with versioned
`CilicianFirRealisticV01` art: a more natural blue-green, irregular evergreen
cutout. The runtime has matching spring/summer/autumn/winter copies because the
fir is evergreen. Existing `CilicianHarvestV01` falling animation sheets and
stump, flora ID, lumber yield, worker targeting, save representation, Undo, and
manual saving are unchanged. Old TreeRepairsV01 art is retained.

New asset contract test passed in forest suite36/36 (fresh XML 20:24:05–20:24:07Z).
Live isolated dense `forest-review` passed: cluster batching retained; the
separate fir fell, yielded wood, and fixture flora restored. QA helper wait was
made time-based (animation duration plus two seconds) rather than 180 frames so
fast Editor frames cannot falsely fail the existing harvest check. Normal
windowed Game View capture inspected; no baked backdrop. Evidence:
Documentation/Validation/cilician-fir-realistic-v01. Fixture restored without
Save. Work remains uncommitted on ui-makeover; preserve concurrent UI edits.

### September 17 — London Plane A/B and measured Plane A root correction

Runtime LondonPlaneRealisticV01 contains A spring/summer/autumn/winter and B
spring/summer/autumn. B winter remains legacy. User approved both silhouettes;
B's accidental lamp was removed from its runtime images. Spring/autumn flora
color multipliers are now neutral in SeasonLighting; approved A art restored
after a mistaken attempt to fix lighting through image edits.

Latest Plane A anchor fix supersedes all guessed pivots/selection offsets:
alpha-measured bottom padding is 19px spring/summer, 20px autumn, 18px winter,
on a1536px canvas. FloraTreeRepairs uses those exact normalized pivots at96PPU.
Removed SelectionRootLocalY and restored standard ApplyFloraSelection logic;
tree/shadow/selector now share the trunk foot. B remains .065 pivot unchanged.
Actual normal Game View visually inspected beside B and bench; all four alpha
foot checks passed in live Unity. Evidence Validation/plane-a-anchor-v02.
No new full forest suite run this turn (previous38/38 did not prove visual
alignment); updated asset test now compares real alpha pixels to the pivot.
No stop Play, Save, PNG mutation, commit or unrelated UI edit this turn.
# 2026-09-17 — Plane C retirement and next species study

Removed London Plane C from lot and district planting catalogs. Existing saved `london-plane-c` records are retained and resolve to Plane B artwork/presentation/scale, with display name London Plane B. Old source textures remain. A/B anchoring and artwork are unchanged. No save or commit performed.

Read-only Unity bridge command `plane-retirement-review` checks all seasons and variation profiles, asset availability and scale parity; report under `Documentation/Validation/plane-c-retirement-v01/`. Red Maple summer study saved under `Documentation/ArtStudies/RedMapleRealisticV01/`; preview only, not installed. Continue seasonal/runtime work after visual feedback.


### September 17 — approved Red Maple seasonal set installed

Mature `vendor-red-maple` now resolves to RedMapleRealisticV01 in the shared lot/district resource resolver, all four seasons. Approved source studies preserved; runtime PNGs copied unchanged with the Plane A importer settings and new GUIDs. PPU96 gives approximately15.5m visible height. Measured alpha>128 trunk foot is39px spring/summer/winter and40px autumn on1536px canvas; standard shared shadow/selection anchor retained. Young Red Maple unchanged. No flora IDs, placements, save files or worker logic changed.

Unity refreshed/compiled and read-only `maple-review` passed all four resource, root and scale checks at22:10:11Z; evidence Validation/red-maple-realistic-v01/review.txt. No live Game-view visual check or full test suite this turn; did not stop Play or save. Existing displayed sprites may need a season change or lot reopen to refresh. All changes uncommitted.


### September 17 — photographic Silver Maple A installed

Approved photographic Silver Maple V02 seasonal studies were copied unchanged into `SilverMapleRealisticV01` with unique Unity GUIDs. Existing `silver-maple-a` ID now resolves there in shared lot/district resource path for spring/summer/autumn/winter. `silver-maple-b` remains legacy. PPU84 retains about the original17.5m visible height. Alpha>128 root-foot measurements on1536px images: spring40, summer42, autumn36, winter42; pivot matches each. Existing save records, selection/shadow handling, worker logic and manual saving unchanged.

Unity refreshed/compiled; read-only `silver-maple-review` passed at22:28:24Z, including all assets, foot pivots, scale and B preservation. Evidence `Documentation/Validation/silver-maple-realistic-v01/review.txt`. No Game-view appearance capture or full test suite this turn; did not stop Play, save or commit. Existing sprites may need a season change or lot reopen to refresh. Preserve unrelated UI and water edits.


### September 17 — Willow seasonal set installed

Approved Willow summer study V01 (rather than the denser V02 alternative) supplied spring/autumn/winter derivatives. Four originals retained under `ArtStudies/WillowRealisticV01`; copies are under `Resources/.../WillowRealisticV01`. Existing `vendor-willow` ID in shared lot/district resolver points to them. Legacy art retained; save records and manual saving untouched. New PPU104 keeps summer near former12.56m visible height. Alpha>128 visible foot on1536px images: spring64, summer105, autumn133, winter135; each pivot matched. Removed legacy1.4x Willow saturation boost for approved natural palette.

Unity refresh/compile and read-only `willow-review` passed all seasonal loads, foot anchors and scale at22:40:38Z; report `Validation/willow-realistic-v01/review.txt`. No Game-view visual check or full suite this turn; did not stop Play or save. Existing sprites may require season change/lot reopen to refresh. No commit; preserve unrelated edits.


### September 17 — Willow Lot ground contact corrected

User screenshot showed Willow roots hovering above terrain; selection square position was already correct. Willow PNG alpha foot pivots were correct, so do not change them. LotWorldController now applies a0.16m Willow-only presentation sink, a narrow ground fade, and matching shadow sink compensation; saved placement and selection ground point stay unchanged. Other flora and district behavior unchanged. Read-only `willow-ground-review` captured live Game view and confirmed lot JSON unchanged, shadow sprite shared, selector around rooted trunk. Evidence Validation/willow-ground-v02. No Save or commit.


### September 17 — approved dark Oak and two autonomous photographic species

User approved OakPhotorealV01 summer V02 and explicitly authorized making more trees and installing them without further approval while away. Created oak spring/autumn/winter from that source, plus American Elm and Shagbark Hickory in all four seasons. Elm has a high vase silhouette; Hickory a narrower crown and peeling bark. Built-in imagegen source studies and exact prompts are preserved under `Documentation/ArtStudies/{OakPhotorealV01,ElmPhotorealV01,HickoryPhotorealV01}`. Runtime copies are in new `PhotographicDeciduousV01`; no canonical PNG overwritten.

Added NEW lot/district planting IDs `mature-oak`, `american-elm`, `shagbark-hickory`, with display names and shared resource paths. Existing `ashe`, `oak`, `oak-b`, `vendor-hickory` remain unchanged. PPU:100,94,98 respectively. Measured alpha>128 foot pivots by spring/summer/autumn/winter: oak146/149/149/135, elm59/61/50/55, hickory29/33/33/31 pixels on1536px images. Forest generation still uses its five approved clusters; these are manual planting choices.

Unity refreshed/compiled and read-only `photo-trees-review` passed all12 seasonal resources, pivot/scale contracts and legacy source paths at23:22:32Z. A direct active Lot camera render used temporary scene objects for all four seasons and removed them immediately; lot JSON unchanged. Summer, autumn and winter captures visually inspected: oak appears upright at the lot camera angle; root positions and different silhouettes read clearly. All seasonal captures use current lot lighting and temp trees have no shadows, so this is not a complete seasonal lighting/shadow or dense performance test. Evidence `Documentation/Validation/photographic-deciduous-v01/`. No Play stop, Save, commit or push. Preserve concurrent UI, water, labor and prior forest edits.

### September 17 — bald cypress with Spanish moss

New `bald-cypress-moss` manual planting choice in Lot and District flora lists. New `BaldCypressMossV01` runtime PNGs for all four seasons, copied from versioned imagegen studies under `Documentation/ArtStudies/BaldCypressMossV01`; old trees and art remain untouched. Bald cypress has fine green summer needles, sparse fresh spring needles, copper/russet autumn needles, and bare winter branches, with pale Spanish moss retained throughout. PPU82 (~18m). Alpha-measured trunk foot/pivot spring25, summer27, autumn27, winter26px on1536px canvas. Shared resource resolver makes both editors use same art. Forest auto-generation remains unchanged.

Unity refreshed/compiled. `cypress-moss-review` passed all four seasonal resource, scale and ground-pivot checks at00:18:34Z. `cypress-moss-game` rendered all four via the active Lot camera with a temporary object, then removed it; Lot JSON unchanged. Summer and winter captures visually inspected; evidence `Documentation/Validation/bald-cypress-moss-v01/`. Captures use summer Lot lighting and omit actual placed-tree shadows, so live placement/seasonal lighting remains for user review. No Save, commit or push; preserve all concurrent UI/water/forest edits.

### September 17 — second bald cypress palette and silhouette

At user's request for multiple cypresses with color variation, added `bald-cypress-moss-b` as a separate manual Lot/District planting choice; original `bald-cypress-moss` and its artwork unchanged. B has broader/open crown, darker summer foliage and golden-yellow/amber autumn instead of A's copper/russet. Spring has yellow-green emerging needles; winter bare branches; Spanish moss persists in every season. New versioned source studies `Documentation/ArtStudies/BaldCypressMossV02`, runtime copies `Resources/.../BaldCypressMossV02`. PPU82; B measured central trunk feet/pivots17/17/17/18px spring/summer/autumn/winter.

Unity refreshed/compiled. `cypress-moss-pair-review` passed eight resources/pivots and `cypress-moss-pair-game` captured both side by side in all seasons with temporary objects removed and Lot JSON unchanged. Summer/autumn camera captures inspected; evidence `Documentation/Validation/bald-cypress-moss-v02/`. Preview lighting/shadows limit as above. No Save, commit or push; preserve all unrelated edits.

### September 17 — medium Balsam Fir, Fraser Fir and Blue Spruce

Added three NEW manual Lot/District planting IDs: `medium-balsam-fir`, `medium-fraser-fir`, `medium-blue-spruce`, all in Fir and Mountain. Each has two imagegen assets: snow-free for Spring/Summer/Autumn and branch-snow variant for Winter. Balsam is loose/deep green, Fraser compact/emerald with silver underside, Blue Spruce stiff/powdery steel blue. Source studies and generation/edit intent under `Documentation/ArtStudies/MediumConifersV01`; runtime copies under `Resources/.../MediumConifersV01`; old fir art/IDs unchanged. PPU128/128/120. Alpha-measured snow-free/snowy foot pixels on1536px: Balsam14/15, Fraser21/20, Blue Spruce6/7; matching pivots.

Unity refreshed/compiled. `medium-conifers-review` passed all three IDs, six textures, four-season routing, mountain family and pivots at00:56:59Z. `medium-conifers-game` rendered snow-free and snowy trio via active Lot camera with temporary objects; both captures visually inspected, objects removed, Lot JSON unchanged. Evidence `Documentation/Validation/medium-conifers-v01/`. Capture used current Lot lighting and no actual placed-tree shadows; no dense performance test. No Save, commit or push. Procedural forest species unchanged; preserve concurrent UI, water, labor and prior flora edits.

### September 17 — photographic date and Los Angeles palms

Four NEW manual planting IDs in Tropical Lot/District libraries: `date-palm-tall`, `date-palm-short`, `la-fan-palm-a`, `la-fan-palm-b`. Tall/short date palms have visibly substantial trunks and feather fronds; LA palms are distinct tall skinny Washingtonia robusta fan palms. Four built-in imagegen source studies under `Documentation/ArtStudies/PhotographicPalmsV01`, copied unchanged to versioned runtime `Resources/.../PhotographicPalmsV01`. Existing `date-palm` ID/art untouched. Evergreen resolver reuses one texture in all seasons. PPU120/180/70/80 yields ~12.7/8.3/21.6/18.8m visible height. Alpha-measured root pivots12/41/24/31px on1536px. `RegionClimateRules` now allows the two NEW date palms in Desert, as it already allows the legacy date palm; California/Mediterranean permits all four.

Unity refreshed/compiled. `photographic-palms-review` passed resource/season/climate/pivot/legacy path checks at01:32:18Z. `photographic-palms-game` rendered four palms via active Lot camera; final capture visually inspected with all four roots on Lot terrain. Temporary preview objects removed; Lot JSON unchanged. Evidence `Documentation/Validation/photographic-palms-v01/`. Preview uses current lighting and no placed-tree shadows; no dense performance test. No Save, commit or push. Preserve concurrent UI, water, labor and prior flora edits.

### September 17 — 75% LA fan palm heights

At user's request, added two NEW manual planting IDs `la-fan-palm-a-medium` and `la-fan-palm-b-medium` to Lot/District Tropical libraries. They share existing PhotographicPalmsV01 art with `la-fan-palm-a/b`; original IDs/scale and PNGs unchanged. New PPU=70*4/3 and80*4/3 gives exactly75% of the originals' visible height (~16.2m and14.1m). Shared source texture names preserve the measured ground pivot and selection/shadow anchor. California/Mediterranean climate eligibility retained. No additional image generation or duplicate textures.

Unity refreshed/compiled. `palm-heights-review` verified both all-season shared resource paths, family/climate and exact .75 height ratios at01:44:46Z. `palm-heights-game` captured tall/medium A/B comparison using temporary Lot SpriteRenderers; final Game-camera capture visually inspected and lot JSON unchanged. Evidence `Documentation/Validation/la-palm-heights-v01/`. No Save, commit or push; concurrent edits preserved.

### September 17 — retire legacy tree choices, preserve saved placements

Removed pre-regeneration tree choices from the Lot and District planting libraries, including the old 3D tree choices. The current photographic Plane, Maple, Willow, Oak, Elm, Hickory, Cypress, palm and medium conifer choices remain. The Cilician Fir ID remains plantable and is still the individually harvestable lumber tree; generated five-tree forest clusters and their harvest pattern remain unchanged. Tropical generation now selects only new photographic palms.

Saved Lot/District flora records are not rewritten or deleted. `LotWorldController.CurrentTreeArtwork` resolves retired IDs to approved new tree art for resource loading, presentation, pivots and sizing. Legacy PNGs remain in the project as canonical source/compatibility assets. The Lot and District sprite caches now key by presentation ID as well as resource path so the 75%-height palms can share texture without sharing sprite scale. No automatic save was added.

Unity refreshed/compiled. Fresh forest suite: 38 passed, 1 failed of 39. The new retired-ID test passed with all four seasonal resources and Cilician harvest. The remaining failure is `DistrictFloraBatchesTests.NearbyCopiesShareOneMeshAndRemainPickable`: expected `_FloraSaturation` 1.4, got 1.0; it concerns existing batching/material state outside this tree retirement. No Save, commit or push; concurrent UI/water/forest edits preserved.

### September 17 — open directly at the main menu

Removed the click-to-continue splash route from `CityForgeApp`: startup now calls `Show(AppScreen.MainMenu)` directly, and the splash enum case/composition is gone. Editor QA helpers that required a fresh splash now require a fresh main menu. The splash image source and unrelated UI styles remain untouched. Unity refreshed/compiled and entered Play mode through the project-scoped bridge; no new compile or startup exception appeared. No save, commit or push.

### September 17 — commit-ready validation

The forest batching saturation assertion was updated to the current neutral artwork value (1.0). Fresh project-scoped Unity EditMode runs passed: forest 39/39 at 02:26:01Z and regional 83/83 at 02:26:42Z. The earlier 38/39 result above is superseded. Startup was also refreshed and entered Play mode without a new startup exception. The branch remains `ui-makeover`; unrelated concurrent map chrome and water import changes are excluded from the forest/tree/startup commit.

### September 17 — Garden Lot Editor menu

Added Garden between Flora and Props, with a flower icon, tooltip, dedicated empty library and inspector entry. User approved the category for directional hedge sections, beds, borders and arrangements. No garden assets or rotation implementation yet; existing Flora/Props entries retained. Appended enum value preserves previous category values. Unity compilation and real-button UI Toolkit submit open/Done checks passed; normal windowed Game capture inspected. QA had no lot open, restored previous screen/category, no Save. No physical-mouse or populated-lot regression claim; prior 39/39 and 83/83 suites are not garden validation. Evidence: Documentation/Validation/garden-menu-v01. Worker/labor and persistence untouched.

### September 17 — first Georgian Garden border

Added `georgian-flower-thicket-border-v01` to the Lot Editor Garden library: a 4 × 1.5 m rectangular 18th-century-inspired flower row with rear thicket, slim stone rim, photographic upright cutouts and overhead planting layer. New source studies/prompts and unchanged versioned runtime copies live under `Documentation/ArtStudies/GeorgianGardenBorderV01` and `Resources/.../Garden/GeorgianBorderV01`. Uses existing placed-prop ID/rotation/selection/save representation; seasons alter visuals only, with a distinct woody winter thicket and no winter flowers. Garden UI exposes rotation and deletion. Workers/labor untouched; Save remains manual.

Unity compiled; isolated unsaved Lot fixture UI Toolkit catalog and preview check, one-record placement, 90° footprint swap, serialization, and summer/winter normal windowed Game-view inspection passed. Prior no-lot screen restored. A second isolated check found 307 pickable pixels and submitted the enabled Garden Rotate button, advancing the saved turn. Physical mouse QA and dense repeated-placement profiling remain open. Details/evidence: `Documentation/GARDEN_BORDER.md` and `Documentation/Validation/georgian-garden-border-v01`. The prior forest/regional test counts do not cover this garden piece.

### September 17 — complete Georgian mixed beds at Lot isometric angle

User clarified that Garden pieces should be whole square/rectangular plots with multiple shrubs and flower groups, not narrow border strips, and asked whether that plan can work at the Lot Editor's isometric angle. Added new Garden catalog IDs `georgian-mixed-garden-square-v01` (4 × 4 m) and `georgian-mixed-garden-rectangle-v01` (6 × 3 m). Each uses a photographic overhead planting plan, multiple upright crossed flower/shrub cards and a real low stone rim. The Lot camera projects the assembly at 45° azimuth/20° elevation and existing prop quarter-turns rotate the full piece; a single fixed-angle billboard is not used. The earlier `georgian-flower-thicket-border-v01` is kept loadable for saved placements but no longer offered in the Garden catalog. Existing save schema, ground anchor, selection, Undo and manual-only Save routes are reused; labor untouched.

Four original plan studies and separate winter edits plus a non-runtime isometric direction study are under `ArtStudies/GeorgianGardenBedsV01`; runtime plan copies under `Resources/.../Garden/GeorgianBedsV01` are byte-identical. Existing GeorgianBorderV01 cutouts are reused for upright depth. Spring has lighter/fewer flowers, summer full bloom, autumn warm-muted bloom, winter dormant plan art and woody shrubs without flower cards. Unity compiled; isolated temporary geometry captured summer square/rectangle and rectangle 90°, winter variants at actual Lot camera angles. The active `Untitled Lot` was left untouched after in-lot fixture refusal; the final isolated render verified unchanged lot JSON and 3 × 6 m rotated footprint. No Save. This is camera-angle QA, not full in-Lot mouse/Undo/save or dense performance QA. Evidence: `Documentation/GARDEN_BEDS.md` and `Documentation/Validation/georgian-garden-beds-v01`. Forest/regional 39/39 and 83/83 are not new garden tests.

### September 17 — Georgian beds checked in the actual Lot Game view

User requested seeing the complete mixed beds at the game's exact angle. Read-only inspection of the active clean `Untitled Lot` showed Detail zoom, Summer, enabled Lot Camera at **30° elevation and 45° azimuth**. This supersedes the assumption that all Lot camera views use the earlier 20° calibrated stage angle; 20° is used by some native 3D lot views. Two temporary `CreatePropPresentation` roots were shown over the current Lot ground and captured in the real Game view, then destroyed. A closer synchronous capture used the same camera with temporary orthographic size 6.5, restored afterward. The active lot JSON was identical before/after and no Save was called. The square and rectangle both read as contained planted plots; the rectangle also appears after 90° rotation. Evidence: `Documentation/Validation/georgian-garden-beds-v01/{summer-in-game.png,rectangle-90-in-game.png,garden-close-in-game.png,game-camera.txt}`. Physical placement, selection/Undo and saved-lot round-trip of these new IDs remain unverified; prior forest/regional suites do not cover garden.

### September 18 — clipped hedge plots and muted mixed-bed edges

Added two complete Garden assemblies: `georgian-clipped-hedge-square-v01` (4 × 4 m) and `georgian-clipped-hedge-rectangle-v01` (6 × 3 m). Each uses a single textured, beveled clipped-hedge mesh with perimeter and interior islands over dark earth. The new versioned photographic leaf source/runtime copy is under `GeorgianClippedHedgesV01`. The existing mixed beds now have a lower charcoal-brown physical rim plus a thin colored overlay that covers white edging baked into their plan art. Already-instantiated beds in a running lot may retain their old white rim until their presentations are rebuilt. Source plans and save records were not modified.

Unity refreshed and compiled. Temporary presentations were inspected with the actual enabled Lot Camera at Detail zoom, 30° elevation and 45° azimuth: square and rectangle, rectangle quarter turn, muted-edge flower bed and winter hedge tint. An isolated `LotEditorSession` JSON round trip retained both new prop IDs and quarter turn; the rotated rectangle footprint checked as 3 × 6 m. Active lot JSON was identical before/after; no Save was invoked. Temporary editor QA script was removed after capture. Evidence and limitations: `Documentation/GARDEN_HEDGES.md` and `Documentation/Validation/georgian-clipped-hedges-v01/`. Physical UI placement/selection/Undo and saved-file regression remain unverified. Forest 39/39 and regional 83/83 predate this Garden work and do not validate it. No commit or push; worker/labor code untouched.

### September 18 — Natural Grass Garden patches

Renamed the Lot base display label `Default Grass` to `Natural Grass` without changing the saved `default-grass` ID or source texture. Garden now offers four soft-edged grass props: 4 × 2 m, 6 × 3 m, 4 × 4 m and 6 × 6 m. They reuse the exact base grass texture at its five-metre density; a shadow-receiving material fades the outer 0.40 m with no raised border. New IDs use the existing `PlacedProp` rotation, selection, Undo and manual Save paths. Brick sidewalk overlays remain available separately for paths. Worker/labor code untouched.

Unity compiled and a Play-mode isolated check passed all four IDs, source/material contracts, rotated footprints and session JSON round trip. Temporary presentations over a dark comparison plane were captured in the normal windowed Game view and removed; active lot JSON was unchanged, camera restored, no Save. This was not a physical-mouse placement/selection/Undo or saved-file regression pass, nor a direct brick-overlay interaction check. New EditMode tests are written but were not run while the active editor was in Play mode. Evidence and limits: `Documentation/GARDEN_GRASS_PATCHES.md` and `Documentation/Validation/natural-grass-patches-v01/`. The forest 39/39 and regional 83/83 runs are unrelated.

### September 18 — muted grass borders and circular piece

The four existing Natural Grass Garden IDs now build a flat 0.12 m charcoal-brown perimeter matching the mixed beds' muted edge family. New `natural-grass-circle-v01` is a 4 m diameter piece with a radial fade and cached ring mesh. The original grass texture, texture density, saved IDs, selection anchor, Undo route and manual Save behavior remain unchanged. No worker/labor changes. Existing live presentations made before the code refresh can remain borderless until presentation rebuild; saved pieces require no migration.

Unity compiled. Isolated Play-mode checks passed all five IDs, border and circular material/mesh contracts, and a session JSON round trip. Temporary presentations were viewed in the normal windowed Game view on the current Lot base; the active lot JSON was identical before/after, previous presentations/preview restored, and no Save was called. Evidence: `Documentation/GARDEN_GRASS_PATCHES.md` and `Documentation/Validation/natural-grass-patches-v02/`. Physical placement/selection/Undo, disk save/reload, direct brick overlap and dense profiling remain open. The prior forest/regional test counts do not validate this Garden work.

### September 18 — Natural Grass fills its muted border

Joel flagged the visible pink strip between grass and border in the V02 view. Removed the 0.40 m perimeter alpha fade from bordered Natural Grass presentations. Rectangles are opaque to the quad edge; the circle is radially clipped only outside the rim, with a tiny antialias transition hidden beneath it. The grass texture, border geometry, saved IDs and manual Save route are unchanged. Unity refreshed/compiled. The active Lot Camera capture of square and circular pieces shows grass against the muted rims, with unchanged Lot session JSON and no Save. Evidence: `Documentation/Validation/natural-grass-patches-v03/`. The temporary Editor QA helper was removed. Physical UI and disk save/reload checks remain open; old forest/regional suites are unrelated.

### September 18 — supplied brick paving in Base and Overlays

Imported Joel's 1254 × 1254 `brick-texture-1.png` byte-for-byte into versioned `BrickPavingV01` Resources. Unity's initial non-power-of-two import reduced it to 1024, so the importer was set to preserve 1254. Added `brick-paving-v01` to both Lot Base and Overlays. A 10 m base repeat matches the existing 10 × 10 m overlay tile, while other bases remain at 5 m; Base UI now says surfaces rather than grass. The new overlay creates no pedestrian route. Source, saved IDs, Undo and manual-only Save remain intact. Unity compiled, an isolated base/overlay session JSON round trip passed, and a temporary tile was inspected in the active Lot Camera beside Natural Grass. The active Lot JSON was unchanged, no Save was called, and QA code was removed. Evidence and remaining physical UI/disk checks: `Documentation/BRICK_PAVING.md` and `Documentation/Validation/brick-paving-v01/`. Prior forest/regional runs do not cover this work.

### September 18 — base ends at the lot boundary; overlays may continue outside

Joel's Brick Paving screenshot showed the Base covering the two-metre terrain apron outside the yellow lot line. The visible heightfield mesh and collider now crop to exact lot bounds in standalone and district-hosted Lots; the stored terrain grid retains its apron and no save data migrates. Overlay painting now accepts one 10 m cell ring outside the lot so a driveway/path can continue from an edge cell toward a road; overlays and other props are not globally clipped. An isolated 30 × 30 m fixture captured the bounded base and a separate overlay beyond its east edge, with unchanged session JSON and no Save. Two focused EditMode tests passed (2/2), covering mesh bounds, outside-cell painting/round trip and previous base/overlay catalog persistence. Temporary QA code was removed. Evidence/remaining physical-pointer and live-reload checks: `Documentation/LOT_BASE_BOUNDARY.md` and `Documentation/Validation/lot-base-boundary-v01/`. Prior forest/regional suites remain unrelated.

### September 18 — dark cobblestone in Base and Overlays

Created a separate dark charcoal-slate texture using built-in imagegen with the approved gray road cobblestone as an edit reference. The existing road asset is unchanged. Source art and exact prompt are under `Documentation/ArtStudies/DarkCobblestoneV01`; the byte-identical runtime image is `LotTextures/DarkCobblestoneV01/dark-cobblestone.png`, imported at its full 1254 × 1254 resolution. Added `dark-cobblestone-v01` to both Base and Overlays, with a 10 m base repeat matching the 10 × 10 m tile. An isolated Lot fixture rendered both actual paths; the overlay's temporary selection highlight was cleared for review. Isolated session round trip passed, no Save, and fixture/Editor QA code removed. The focused EditMode test is written but not run because Unity returned to Play mode during this work; leave the user's session intact. Evidence and limits: `Documentation/DARK_COBBLESTONE.md` and `Documentation/Validation/dark-cobblestone-v01/`. Previous forest/regional suites are unrelated.

### September 18 — Joel's cobblestone replaces rejected dark generation; stone fountain joins Garden

Joel rejected the generated charcoal cobblestone and supplied a 1250 × 1250
`cobblestone-texture.png`. The active Base and Overlay choice now displays
**Cobblestone** and uses the supplied PNG unchanged. Retained saved ID
`dark-cobblestone-v01` makes earlier placements resolve to the new art without
schema migration; the generated source remains only as a rejected ArtStudy.
Added his archived stone fountain FBX and five texture maps unchanged under
versioned `Garden/StoneFountainV01/Source`. New Garden library prop
`stone-garden-fountain-v01` is a grounded, rotatable, 3 m stone basin with a
rendered card preview. It uses existing prop placement, selection, Undo and
manual-only Save routes; no worker/labor changes. Unity compiled. An isolated
capture and session JSON round trip passed, with no Save; temporary QA objects
were removed. Two focused EditMode tests are written but could not run while
the Editor remained in Play mode. Physical UI and disk reload still need review.
See `Documentation/STONE_FOUNTAIN_AND_COBBLESTONE.md` and
`Documentation/Validation/stone-fountain-and-cobblestone-v01/`. Prior
forest/regional suites are unrelated.

### September 18 — trees plant on the first click; seasons share lighting

An armed Flora tool now prioritizes planting on the first click and on
subsequent clicks even over existing tree canopies or a prior selection. Esc
disarms planting for direct selection/movement. Tree ground anchors accept
water, overlays, roads, props and other flora; only the lot bounds and building
footprints block new placement. Hybrid building footprints use package size
and rotation; 3D buildings use cached projected mesh contours with bounds
fallback, invalidated on presentation rebuild. Existing saved tree positions
are not migrated. Removed season-wide ground/flora/building lighting tints;
time-of-day and environment lighting remain, along with authored seasonal
artwork and weather. No save schema, manual Save or worker/labor changes.
Unity compiled and an isolated Play-mode fixture passed repeated first-click,
water, disarmed selection, hybrid/3D building and four-season tint checks.
Fixture and global lighting were restored without Save. Focused EditMode tests
were updated but not run while Joel's Editor remains in Play mode. Physical UI
and disk save/reload remain open. See
`Documentation/TREE_PLACEMENT_SEASON_LIGHTING.md` and
`Documentation/Validation/tree-placement-season-light-v01/`.

### September 18 — hedge-bordered Natural Grass Garden pieces

Added five Garden variants alongside the original Natural Grass patches: short
and long rectangles, 4 m and 6 m squares, and a 4 m circle. Each keeps the
exact Natural Grass center and frames it with the existing clipped hedge leaf
texture on a 0.54 m tall 3D perimeter; the circular variant has a continuous
beveled ring. Existing Garden prop placement, rotation, selection, Undo and
manual-only Save paths carry the new IDs. Worker/labor code is untouched.
Unity compiled, and an isolated render verified all five presentations,
source textures and mesh dimensions without altering the active Lot session
or calling Save. The temporary review script was removed. Focused EditMode
tests were written but not run while the Editor remains in Play mode.
Physical pointer interaction, disk save/reload and dense profiling remain
open. See `Documentation/GARDEN_HEDGED_GRASS.md` and
`Documentation/Validation/hedged-grass-v01/`. Earlier forest/regional suites
do not cover this work.

### September 18 — darker hedge and gray wrought-iron fence base

Joel's Garden Test screenshot showed that the new clipped-hedge perimeters
looked light blue-green and that the wrought-iron fence had a bright white
bottom. Regraded the shared hedge leaf tints to deep green for all seasons;
this affects both hedge-bordered lawns and earlier Georgian clipped-hedge
plots, without changing global season lighting or the source texture. The
straight and corner wrought-iron fence runtime materials now use a neutral
dark-gray tint, making the pale base unobtrusive while also subduing their
stone/brick atlas. Source textures and saved IDs remain unchanged. An
isolated 45° render was reviewed, Unity compiled, temporary QA code was
removed, and the active Lot was not saved. See
`Documentation/GARDEN_HEDGE_FENCE_COLOR.md` and
`Documentation/Validation/garden-hedge-fence-color-v01/`. Physical UI and
disk save/reload checks remain open.

### September 18 — three-tier running water on the stone fountain

Joel requested a first pass of running water on the existing stone garden
fountain. Added three inset water surfaces, eight streams from top to middle,
ten from middle to basin, subtle time-driven ripples and flow highlights, and
two small splash systems. All are children of the existing saved prop and
use shared geometry/materials with per-instance preview opacity. The live
fountain reattaches water after a script reload without a full Lot rebuild.
No source model, texture, saved ID, manual Save, or worker/labor logic changed.
The active Lot Camera and isolated fountain captures were reviewed; the live
fountain had one water assembly. The Editor was paused and left paused, so
motion was checked through two forced shader phases rather than by resuming
the Game. Unity compiled; temporary QA code was removed; no Save occurred.
Focused EditMode assertions are written but unrun in Play mode. See
`Documentation/STONE_FOUNTAIN_WATER.md` and
`Documentation/Validation/stone-fountain-water-v01/`.

### September 18 — fountain flow obeys gravity

Joel caught that the first fountain-water shader scrolled stream highlights
upward. The stream mesh UV runs from upper lip (0) to lower bowl (1); changed
the sine phase from `+ time` to `- time`, so visual flow advances toward the
lower bowl. Basin ripples already moved outward and were not changed. Unity
refreshed with zero assembly errors, and a source/UV direction check passed.
The Editor was left paused; no Save or Lot edit occurred. The earlier still
phase captures predate this direction correction.

### September 18 — low-poly Blender boxwood hedge visual study

Joel proposed a textured 1 × 3 m hedge with a subtly uneven mesh rather than
modeled leaf clusters. Built a separate 3 × 1 × 1 m Blender/FBX prototype and
dark neutral-green foliage image, leaving all existing Garden hedges and saved
IDs untouched. It has 616 triangles and one foliage material. An isolated
Unity comparison exposed bevel UV streaks, which were corrected in the source
generator; the final import is 422 vertices and 616 triangles with the intended
dimensions. Unity compiled and rendered the final study beside a current-
foliage control; the QA hook was removed. The active Lot stayed paused and was
not saved. The prototype is not yet a Garden Library item. See
`Documentation/LOW_POLY_BOXWOOD_HEDGE.md` and
`Documentation/Validation/low-poly-boxwood-hedge-v01/`.

### September 18 — approved Blender boxwood is placeable in Garden

Joel approved the softer Blender hedge study and asked to see it in-game.
Added **Boxwood Hedge** to the Garden Library with new saved ID
`low-poly-boxwood-hedge-3x1-v01`, preserving all older hedge IDs and pieces.
The 3 × 1 × 1 m FBX uses one dark neutral-green foliage material and 616
triangles. Its runtime presentation is grounded, supports translucent placement
preview, and uses the existing prop rotation, selection, Undo, and manual Save
routes. An isolated Garden scene rendered three pieces, including one quarter
turn, on Natural Grass beside Joel's cobblestone. ID recognition, 0°/90°
footprints, ground anchors, mesh budget, preview opacity, shared opaque
instancing material, and an isolated session JSON round trip passed. Unity
compiled; the QA hooks were removed, and
no active Lot data or disk save was touched. Physical pointer placement,
disk save/reload and dense profiling remain open. See
`Documentation/LOW_POLY_BOXWOOD_HEDGE.md` and
`Documentation/Validation/low-poly-boxwood-hedge-v01/garden-placement.png`.

### September 18 — refreshed parks branch from latest main

Fetched `origin/main` at `120b263` (automata PR #24) and fast-forwarded
`feature/parks-and-gardens` from `ceccaf6`. Reapplied the existing uncommitted
Garden work; Git auto-merged the two overlapping files without conflicts.
The branch is ahead of its remote by the two main commits; nothing was pushed.
The four Automata Library entries now include 18th Century Ladies Chatting,
Victorian Gentlemen Chatting, and Gentleman and Lady Strolling. All eight
facings and thumbnails for each catalog entry are present, including stroll
recolor masks. Unity refreshed with zero assembly errors, and `git diff
--check` passed. The active Lot was not saved. A recovery stash named
`parks-and-gardens-before-main-120b263` remains until the branch work is
committed or otherwise secured.

### September 18 — boxwood hedge forest-green correction

Joel's in-Lot screenshot showed the new 3 × 1 m boxwood hedge reading bright
lime beside darker Natural Grass pieces. Lowered its shared opaque and preview
material grade from RGB `(0.34, 0.42, 0.31)` to `(0.18, 0.25, 0.13)` without
changing the Blender mesh, foliage image, saved ID or older Garden hedges.
OnEnable now rebinds already-placed hedges after a script reload so the new
grade appears without a Lot rebuild or Save. An isolated Unity capture on
grass/cobblestone was reviewed, and Unity compiled with zero assembly errors.
The active Lot camera was unavailable, so the exact screenshot scene still
needs Joel's in-game check. Temporary QA code was removed; no Lot was saved.
See `Documentation/LOW_POLY_BOXWOOD_HEDGE.md` and
`Documentation/Validation/low-poly-boxwood-hedge-v01/forest-green-isolated.png`.

### September 18 — hosted Lot lighting and district tree orbit

Joel's district screenshots showed boxwood reading too dark during gameplay
and district tree billboards stretching after a hosted Lot camera rotation
until the next zoom. Hosted Lot editing now uses the district's ambient fill
and daylight intensity settings; the boxwood runtime tint is a brighter
forest green, RGB `(0.245, 0.345, 0.18)`. District flora batches now keep each
tree's ground anchor and camera-relative quad offset, and the existing sprite
shader faces the batched quad toward the current shared camera. This avoids
rebaking district forest cells on orbit. Both cached mesh identity and a 90°
render test passed; the focused forest EditMode suite passed 41/41, including
the two new orbit tests. Unity refreshed with zero assembly errors. The
screenshots supplied by Joel are the before-state; the final hedge appearance
still needs an in-game visual check in his district. No active Lot or save was
modified. See `Documentation/LOW_POLY_BOXWOOD_HEDGE.md`.

### September 18 — Civics / Parks lots and placed-Lot tree facing

Joel reported that trees still slanted when rotating a placed district Lot.
The prior district-flora batch fix addressed a different camera-orbit path:
these trees are children of the hosted Lot, so rotating that parent turns the
sprite planes. `DistrictWorldController` now asks only the affected hosted Lot
to realign its tree and building billboards immediately after a quarter-turn,
and after initial placement. Simple translation does not trigger this refresh;
zoom still uses its existing path. An isolated hosted Lot with a tree reproduced
the 90° misalignment, then passed the immediate-facing check without zoom.

Added saved Lot type value `7` for **Civics / Parks**, leaving all earlier type
values and existing Civics saves unchanged. It is offered in New Lot and Lot
Settings, appears in saved-Lot and district placement labels, and uses a civic
access contract. Garden Test can be reclassified through Lot Settings and an
explicit Save; no existing save was edited automatically. The visible Lot
Editor category and library now read **Garden/Parks**; internal Garden IDs and
placement behavior are unchanged. Two focused EditMode tests passed, Unity
compiled with zero assembly errors, and temporary QA wiring was removed. A
third isolated integration test now covers the actual district placement and
quarter-turn path, but it was not run because the Editor entered Play mode;
the live session was left untouched.

### September 18 — Lot construction thresholds and stockpiles

The Lot Stats draft now saves minimum population and minimum education score
(0–100), alongside the existing minimum era and construction materials. The
construction section exposes all ten district stockpiles in tonnes. District
placement checks these thresholds against current population and education,
plus existing era, treasury, material and access rules. Rejection charges
nothing; accepted placement deducts quoted materials once. Lumber keeps its
existing labor wood balance. Older Lot saves default the new thresholds to
zero, and existing placed Lots are not charged on load. The focused Lot Stats
and district simulation EditMode suite passed 10/10. No player save was
modified; Lot and district persistence still require explicit Save.

### September 18 — saved Lots move into Build categories

Removed the all-Lots district Build browser. Saved Lots now appear in their
authored categories: Residential, Commercial, Industrial and Mixed under Zoning;
Agricultural under a new Farms menu; Transportation under Transit; Civics under
Civic; and Civics / Parks under Parks. The Lot Editor now offers **Farm** as the
visible Agricultural type without changing its saved enum value `5`. Browsers
filter cached summaries only when opened and continue through the same placement,
requirement, Undo and manual Save path. No existing Lot was changed or saved.
The focused EditMode run passed 10/10. See `Documentation/LOT_BUILD_CATEGORIES.md`.

### September 18 — hierarchical Lot category and General requirements

Joel found the flat Civics / Parks type awkward and could not see the new build
requirements in General because they were placed in Stats. New Lot and Lot
Settings now choose a parent category; Civics reveals General or Park as its
subcategory, while Farms is its own parent. The saved `CivicsParks = 7` and
`Agricultural = 5` values remain unchanged. Lot Settings now has a General
popup beside Lot Behaviors. General contains identity, type, era, size, traffic,
base cost, minimum build era, population, education, access and all ten material
requirements. Stats retains people, seasonal finance, benefits and services.
General Apply changes only the in-memory Lot; explicit Save still persists it.
The focused category and requirement EditMode suite passed 30/30, and Unity
compiled without assembly errors. No player Lot or district save was modified.

### September 18 — refreshed parks branch with angled brick roads

Fetched `origin/main` at `1eaf191` and fast-forwarded
`feature/parks-and-gardens` from `120b263`. This includes the diagonal Antique
Brick district roads and stair-step smoothing. Reapplied the full uncommitted
Garden/Lot worktree from recovery stash `parks-and-gardens-before-main-1eaf191`;
the three overlapping source files auto-merged without conflicts. The stash
remains as a backup until the uncommitted work is secured. Unity's regional and
road EditMode suite passed 94/94; focused Lot and Garden checks passed 35/35;
forest checks passed 41/41. Unity compiled with zero assembly errors, temporary
QA wiring was removed, and no player save was written. Nothing was pushed.

### September 18 — numeric entry hotkeys and hedge catalog correction

General requirements use UI Toolkit `IntegerField`, which the old input-focus
guard did not recognize. Text and numeric fields now reserve their key events
before Lot shortcuts run, and physical key polling also respects their focus.
The screenshot's pale, protruding-leaf bush matched the older Props → 3D Hedge
asset, not the approved dark low-poly Boxwood Hedge already along the Lot edge.
The Props catalog now places the approved Boxwood Hedge with its matching preview;
the old saved ID remains loadable. Garden Test's saved Lot contains no old 3D
Hedge props. Two focused EditMode checks passed for text/numeric focus and the
approved boxwood's material/preview alpha. No player save was edited.

### September 18 — shallow house-front Garden beds

Added two 2 × 1 m rotatable Garden/Parks pieces: **Foundation Hedge & Flowers**
with a rear clipped hedge and front flowers, and **Framed Foundation Flowers**
with two short hedge ends, low shrubs and front flowers. They reuse the approved
Georgian plant cards, cropped canopy image and dark clipped hedge material. Each
is one new saved prop ID on the existing selection, rotation, Undo and manual
Save paths. An isolated Unity render and 2/2 focused EditMode tests checked the
presentation, dimensions, winter state and in-memory JSON round trip. The
temporary render script was removed; no player save or worker/labor code changed.
See `Documentation/FOUNDATION_GARDEN_BEDS.md` and
`Documentation/Validation/foundation-garden-v01/isolated-preview.png`.

### September 18 — five cottage foundation plantings

Added five more 2 × 1 m Garden/Parks foundation beds: unfenced Rose Bushes;
Picket Rose & Shrubs; Picket Cottage Flowers; Picket Rose Pair; and Picket
Rounded Shrubs. Four use a low, muted, weathered picket enclosure, while the
rounded shrubs are low-poly textured 3D meshes. New transparent rose and purple
phlox plant cards extend the earlier Georgian flowers; the original two beds
retain their saved IDs and appearance. The same `PlacedProp` rotation, selection,
Undo and manual Save routes apply. An isolated five-piece render was inspected,
and focused EditMode tests passed 3/3 for all seven foundation IDs, materials,
dimensions, winter state and in-memory JSON round trip. No active lot JSON or
worker/labor code changed. Physical Lot Editor interaction and disk reload
remain open. See `Documentation/FOUNDATION_GARDEN_BEDS.md` and
`Documentation/Validation/foundation-garden-v02/`.

### September 18 — straight white-picket flower and grass strips

Added three Garden/Parks props with an exactly 2 m authored white picket fence,
Natural Grass and planting both in front and behind: White Picket Roses, White
Picket Cottage, and White Picket Mixed. Each is one 2.2 × 1.8 m rotatable prop;
the existing independent fence and earlier garden IDs remain unchanged. The
source fence FBX and material maps are reused at 2 m, with rose, purple and low
Georgian flower cards on both sides. An isolated Unity render was inspected;
focused EditMode tests passed 3/3 for fence geometry, grass, front/rear plant
anchors, season/preview behavior, quarter-turn dimensions and in-memory JSON
round trip. No active save or worker/labor code changed. Physical Lot Editor
placement, selection/Undo and disk reload remain open. See
`Documentation/WHITE_PICKET_GARDEN_STRIPS.md` and
`Documentation/Validation/white-picket-garden-v01/`.

### September 18 — aged organic white-picket revision

Joel clarified that the three new strips should not reuse the pristine,
blindingly white standalone picket fence. Replaced their fence presentation
with a new cached 2 m mesh: eleven subtly uneven pickets, smaller end posts and
a matte warm-ivory aged-wood texture with cracked paint, gray grain and restrained
organic staining. Dense cottage planting now overlaps both sides and the rails,
following Joel's clematis, coneflower, daisy, black-eyed Susan, delphinium,
sedum, hosta, fern and groundcover reference. The existing standalone fence is
unchanged, and the three Garden saved IDs remain stable. Revised isolated render
inspected; focused tests passed 3/3. See
`Documentation/Validation/white-picket-garden-v02/`.

### September 18 — expanded aged-picket family and fixed Garden footer

Kept Joel's approved White Picket Roses and added six more stable Garden IDs:
Coneflowers, Daisies, Black-Eyed Susans, Hosta & Fern, Clematis, and Full
Cottage. All reuse the aged 2 m mesh, Natural Grass and front/rear planting
contract, with cropped regions of the dense cottage source providing distinct
plant emphasis without new draw-time image work. Garden/Parks now has nine aged
picket arrangements total. Its catalog grid is inside a vertical `ScrollView`;
the modal title/copy and Done actions are fixed outside the scroll region. An
isolated nine-piece render was inspected and focused EditMode checks passed 4/4
for all IDs, fence/grass/anchor contracts, season/preview, rotation, in-memory
round trip and fixed-footer source/style structure. No active save or worker/labor
code changed. See `Documentation/Validation/white-picket-garden-v03/`.

### September 19 — district heading and top-bar region navigation

Moved the existing region-return button before the gold/resource counters in
the top-left bar. The top-right district heading now uses a 21 px bold name,
followed by Year and season. The existing cached season label and metric refresh
keep the date current without adding scans or changing worker/labor behavior.
Unfounded districts retain the existing Not founded date state. Region-map
heading styles remain scoped separately. Source review and `git diff --check`
passed; live layout and fresh Unity compilation remain unverified. No player
Save, commit, push, or review-editor sync was performed: Joel explicitly asked
to preserve the uncommitted work and not commit it.

### September 19 — Not started dialog and hidden unstarted names

Header status now says Not started and opens Start District / Start Town, with
hover/focus captions beneath both choices. Start District initializes the
existing Founded/calendar fields without a building, updates local HUD widgets,
and records only in-memory Undo. The header remains clickable for starting a
town later. Town placement preserves an existing calendar and content, sets
Town designation after placing the founder, and adds only that Lot presentation.
Fort uses fortress-lot; City Center retains city-charter-house but is visibly
disabled because it has no assigned Lot (Joe was asked about this). Missing Fort
content is disabled too. Region labels/markers, tooltips and inspector names
hide unstarted placeholder names without changing stored names.

Six focused tests passed in an isolated scratch Unity project, and current
runtime code compiled. See Validation/district-start-v01 for results and limits.
Live visuals, Fort placement, disk reload and dense performance remain unchecked.
Existing composition-key/Undo whole-district work still runs once on explicit
town placement; flagged to Joe, unprofiled. No worker/labor optimization changes,
player saves, commits, pushes, or review sync. Existing uncommitted work retained.

### September 19 — explicit naming confirmation

Start District and Start Town now open a naming dialog prefilled with the
current name. OK applies the trimmed, nonblank name and continues startup;
Cancel leaves state unchanged. The District Info panel stages name/designation
edits and uses OK instead of X. Confirmation updates the existing upper-right
heading immediately without a district rebuild. Eight isolated Unity EditMode
checks passed, including naming dialog callbacks, Cancel, blank-name validation,
and heading identity/update; results in Validation/district-start-v01/naming-test-results.xml.
Physical-pointer/layout review remains open. No player saves, commits, pushes,
review sync, or worker/labor changes.

### September 19 — separate development-only Lots testing action

Added Lots beside Build/Terrain as an Editor/development-only dock action,
showing all saved Lot categories. Its transient placement mode bypasses all
construction requirements, zeroes cost/material charges, and bypasses authored
boat/shoreline placement checks. Bounds/overlap still apply. Normal Build
category placement retains all requirements. Cancellation, normal arming,
placement completion and navigation clear the testing mode; saves gain no flag.
Release player scripts omit the button and disable both UI/world bypasses.

24 focused EditMode tests passed in isolated scratch Unity. Non-development
macOS player scripts compiled, and compiled-assembly inspection confirmed the
button is absent and the bypass returns false. Evidence and remaining live/UI/
dense validation limits: Validation/testing-lots-v01. No player saves, commits,
pushes, review sync, or worker/labor optimization changes.

### September 19 — Tree Test click consumed by Select mode fixed

The separate Lots action left Select active, whose pointer branch intercepted
world clicks before pending-Lot placement. Armed Lots now take priority after
UI exclusions and before object inspection, Select/Move, and palette closing.
They also consume invalid ground clicks instead of starting selection. Existing
Build requirements remain, and the redundant footprint check on click is gone.
25 isolated tests passed; an unchanged copy of Joe's Tree Test placed at three
positions through the in-memory model with no charges. No world rendering or
physical mouse QA claimed. Evidence: Validation/testing-lots-v01/pointer-test-results.xml
and tree-test-placement-check.txt. Tree Test save hash stayed identical. No
player save, commit, push, review sync or worker/labor changes.

### September 19 — meadow zoom detail shift and smooth distance filtering

Interpreted zoom numbering as closest=1 (asked Joe asynchronously; no reply at
implementation). Zoom 1 uses old zoom 2 grass world scale (13.333m); zoom 2 uses
old zoom 3 scale (40m). Zoom 3+ (LOD2+) enables coarse isotropic mip filtering
for base meadow, hill meadow and the formerly unfiltered flat straw-patch sample.
Broad color fields remain; camera stops, mountain contract and source PNGs are
unchanged. Existing ground material settings apply on the next zoom change.

Isolated scratch Unity passed all six scale/filter assertions and shader checks;
24 flat/hill before/after renders generated, zoom-3 flat pair and hilly after
visually inspected. No added texture samples, mesh, render pass or per-frame scan.
No live dense-district performance claim or player editor control. Evidence and
limits: Validation/meadow-zoom-smoothing-v01. No player saves, commits, pushes,
review sync or worker/labor changes.

### September 19 — quarter-screen district edge panning and modal lock

District hover panning uses outer 25% strips, excluding their four 25% x 25%
corner intersections. Existing screen pointer events compute direction, with
menu/button/text/inspector exclusions; removed the old edge buttons so placement
clicks remain unobstructed. Existing speed/projection/clamping retained. Leaving
the screen or losing focus clears direction. Document/choice modals stop pan
polling and clear cached direction, including when text fields have focus.
Region arrow panning also blocks while a modal is open. Fresh pointer movement
after closing the dialog rearms hover panning.

23 isolated EditMode checks passed for zones, corners, boundaries, modal lock,
existing pan contracts and armed-Lot click ownership. Runtime compilation and
diff whitespace checks passed. Live physical hovering remains for Joe's review.
Evidence: Validation/quarter-edge-pan-v01. No player saves, commits, pushes,
review sync, worker/labor edits, added district scans, or presentation rebuilds.

### September 19 — direct district entry and Save diagnosis

Region tile clicks now call SelectRegionTile directly; hover help remains, with
its instruction updated to click-to-enter. No extra inspector click is needed.

Investigated reported missing saved town names. Read-only inspection of the
saved Test Region II found custom names (including Riverdale) persisted, but
those tiles have Founded=false. Current map intentionally hides all unstarted
names; renaming alone does not start a district, and Start Town naming precedes
founder placement. Did not reinterpret that state, migrate saves, or reveal
placeholder names. Asked Joe which name/location was missing; no reply yet.

Five isolated Unity EditMode checks passed: actual Save button first-save and
rename overwrite for both start states, both naming paths, and founding-state
round trip. Fixture verifies no write before explicit Save. Player files were
read only. Direct-click wiring reviewed in source; live interaction remains for
review. No player saves, commits, pushes, review sync, or worker/labor changes.

### September 19 — Fort click ownership and placement outline

Fort placement now receives world clicks before Select/inspection, sharing the
armed-Lot input gate. Pointer movement draws its footprint; final placement uses
the same coordinates. Cache the founder Lot when armed to keep preview free of
file reads and district scans. Overlap remains checked on click with feedback.
City Center explicitly says its Lot has not been created yet and stays disabled.

10 isolated tests passed, covering pointer ownership, preview/placement and town
start state, naming and region labels. See Validation/fort-placement-v01 for
results and limits. No live player saves/editor control, commits, pushes, review
sync or worker/labor changes.

### September 19 — slower close-view pan and clearer town labels

Zoom levels 1–3 (LOD0–LOD2) now use 35% of the previous panning speed for both
edge hover and arrow-key camera motion. Other zoom levels retain their speeds.
Town dots move 6px closer to names (top 4 to -2), and town labels get a 1.5px
black text outline. District label styling remains unchanged.

Isolated Unity runtime compilation, stylesheet import and existing focused
pan/modal/name-visibility checks passed; git diff --check passed. Perceived
speed and label appearance remain for live review. No player saves, commits,
pushes, review sync, or worker/labor changes.

### September 19 — Fort next-step slideout

Successful Fort placement now adds a right-side non-modal “Start with lumber”
information panel suggesting lumberjacks near trees. A 300ms ease-out entrance
slides it onscreen; X removes it immediately, with no exit animation. Hovering
it clears edge-pan motion. No persistent onboarding flags, worker changes,
automatic placement or saving. City Center and the future Lumberjack Camp Lot
remain unimplemented. The transient panel is not recreated on load/navigation.

Extended the isolated Fort placement test to verify the panel appears after
founding and closes synchronously without undoing founding. Passed; runtime
compilation, stylesheet import and diff whitespace checks passed. Entrance
appearance/timing still need live visual review. No player saves, commits,
pushes or review sync.

### September 19 — lumberjack and forestry cart Lot authoring entries

Added Lumberjack to Characters and Forestry Cart to horse-drawn vehicles.
Reuse existing axeman FBX/body+axe maps, embedded clips and 1.85 district scale;
reuse the existing forestry wagon ID and controller. No labor/worker edits.
These are authoring assets, not automatic camp production/delivery wiring.
See Migration/LUMBERJACK_LOT_LIBRARY.md for IDs, lineage and scope.

Isolated scratch fixture successfully instantiated the real lumberjack and
forestry cart, verified animation component/Chop clip, both original textures,
cart controller and JSON round trip of both prop IDs. Runtime compilation and
diff whitespace checks passed. No full Lot UI/district-render visual validation
or harvesting/delivery test claimed. Evidence: Validation/lumberjack-lot-library-v01.
No player saves, commits, pushes or review sync.

### September 19 — Work Tent building intake

Imported Joe's work-tent.zip into Buildings3D/WorkTentV01 with byte-identical
source files and a separate thumbnail. Available through the direct building
catalog under Industrial → Camps as work-tent-v01. Initial3.2m height including
pole tips;2,218 triangles. Shared matte material preparation, native model path.
Isolated Unity model/material/scale checks and visual render review passed;
see Migration/WORK_TENT_V01.md and Validation/work-tent-v01 for evidence/limits.
No player saves, commits, pushes, review sync or labor/worker changes.

### September 19 — first-class Lot Connectors and Dirt Entry

Added a Connector category to the Lot Editor. `dirt-entry-v01` is a 5 × 5 m
dirt driveway centered on a cardinal Lot edge, extending 2.5 m inside and 2.5 m
outside. It saves independently with stable ID, edge position, and explicit
pedestrian/vehicle permissions. Edge clicks select rather than duplicate an
existing piece; Delete removes it. Runtime and district-hosted Lot rebuilds
render connectors without changing the Lot footprint. Future Lumberjack Camp
logic can query inside/outside access points; no camp automation or labor
changes are included.

Isolated tests cover four-edge transforms, dimensions, permissions, click
ownership, edge limits, JSON/object registry, and cross-boundary presentation.
An isolated 40 × 30 m fixture render was inspected. See LOT_CONNECTORS.md and
Validation/lot-connectors-v01. No player saves, commits, pushes, review sync,
worker/labor changes, per-frame scans, or full district rebuilds.
# September 19 — 4 × 4 camp overlay

The Lot Editor Overlays modal now separates **1 × 1** and **4 × 4** surface
categories. Added the user-supplied Camp Ground as one 40 × 40 meter overlay.
Large overlays fit inside the lot, select from any covered cell, do not repeat
while dragging, survive the existing save format, and are removed if a resized
lot cuts through their footprint. Existing 1 × 1 overlays keep their one-tile
outside-lot painting behavior. Source lineage and the footprint contract are in
`Documentation/LOT_OVERLAYS.md`; focused validation is in
`Documentation/Validation/camp-overlay-4x4-v01/`.

Eight focused isolated EditMode tests passed, covering the new large-overlay
contract and the existing overlay placement, boundary, rotation and pedestrian
routes. Unity imported the new texture and compiled without errors. No player
Lot/district save, worker/labor change, commit, push or review sync was made.

### September 19 — Lot library scroll, stable resize view, and 2 × 2 camp ground

The Buildings modal now pins its heading, category controls and Close button,
with only its card grid inside a bounded vertical ScrollView. Lot dimension
changes preserve the current orthographic camera framing and rebuild overlays
and connectors against the resized base. Overlay anchors remap to keep their
world center when the base expands or shrinks.

The Overlays modal now supports 1 × 1, 2 × 2 and 4 × 4 categories, hiding sizes
that cannot fit the current Lot. Camp Ground retains its existing ID for save
compatibility but now occupies 2 × 2 cells (20 × 20 m). See LOT_OVERLAYS.md and
Validation/lot-editor-layout-resize-v01. No automatic or player save behavior
was added.

Twelve focused isolated EditMode tests passed, including existing top-down,
resize deletion, 1 × 1 boundary, rotation, sidewalk, and stair regressions.
Unity compiled the runtime and imported the stylesheet without errors. No player
save, commit, push, review sync, or worker/labor change was made.

### September 19 — Lot-owned Lumberjack Camp script

Added the built-in `cityforge-timber-camp-script-v1` behavior. Lot Behaviors →
Add Behavior → Lumberjack Camp now discovers the Lot's authored Lumberjacks,
Forestry Cart and vehicle Connector, creates an editable script with their
stable IDs, and requires no pasted JSON. Each placed Lot binds idempotently to
one saved district timber crew; authored actor prototypes are hidden in the
district while existing moving worker/wagon presentations run. Removing the
source Lot removes its crew and workers. The script owns actor bindings and the
forestry recipe while delegating bounded tree queries, pedestrian/road routing,
animations and resource transfer to existing shared services. Current route and
search budgets remain unchanged; persistence remains manual-only.

Isolated copy-on-write Unity validation passed 22/22 Lot script, 14/14 timber,
and 9/9 labor tests. No player Lot/district was opened or saved by QA. The user's
active Lumberjack Camp was manually saved from the live editor during the work;
this change did not modify that save. A physical UI and live district visual
cycle remain. See `Documentation/LOT_TIMBER_CAMP_BEHAVIOR.md` and
`Documentation/Validation/timber-camp-lot-script-v01/`.

### September 19 — district pan balance, Riverdale water, and town labels

Zoom 3 again uses its original district pan rate; only Zoom 1 and Zoom 2 retain
the requested close-view slowdown. Vertical pan distance compensates for the
district camera's 20-degree elevation so up/down and left/right travel at the
same screen-space speed. Region town labels explicitly retain the existing warm
ivory fill while using the black outline.

River water now has a stronger cool material grade, independent of the global
terrain/building lighting. An isolated copy of the real Riverdale tile built at
its saved Noon setting with one river, the expected river shader, and the new
blue tint. Focused pan and water tests passed 3/3; the label style has a focused
regression check. The original Riverdale save hash remained unchanged. No
player save, commit, push, review sync, or worker/labor change was made.

### September 19 — Lot exits, incremental tree groups, and road deletion

Lumberjack Camp crews now enter district navigation through the Lot Connector's
outside pedestrian access point. New crews start there; binding an existing crew
repairs its camp and wagon-home and moves only workers still trapped inside that
Lot. Workers already out in the district keep their position and route. This
fixes the mismatch between authored actors inside the Lot and district navigation,
which treats Lot interiors as blocked.

Painted family groups now mix in a harvestable Cilician fir at a one-in-five rate
in climates that allow it. Group painting inserts only the new presentations and
rebuilds affected flora batch cells. The previous path called `RefreshFlora` on
every pointer update, recreating every flora object and shadow in the district.
A Fir and Mountain group now guarantees its first successfully placed member is
the harvestable Cilician fir, while its remaining members retain the varied
fir/spruce selection and one-in-five harvestable chance.
A missing shadow-mesh color fallback was also added for mixed incremental batches.
An isolated 1,600-tree fixture measured 77.554ms for adding a 12-tree group versus
1,555.714ms for the former whole-district refresh (20.06x faster). This headless
EditMode CPU measurement does not claim live frame time, draw calls, allocations,
or long-duration stability; explicit coverage generation/load/Undo remain bulk
refresh boundaries.

Roads now includes **Delete Road**. Selecting it changes the world cursor to a
red X and supports click-drag deletion through the existing spatial road edit
session, refreshing only each removed cell and its neighbors. Escape returns to
the normal selector and restores the standard cursor.

Isolated focused validation passed 80/80 across Lot scripts, connectors, timber,
Lot simulation, forest coverage, flora batching and road placement. The separate
incremental/full-refresh profile passed. `git diff --check` passed. No player Lot
or district was saved, and no commit, push, merge, or review sync was performed.

### September 19 — Riverdale camp migration and town-name fill

Read-only inspection found Riverdale's placed Lumberjack Camp had
`BehaviorsInitialized=true` but an empty behavior list, so no crew or worker
simulation existed. Its authored cart also uses the earlier
`horse-lumber-wagon-v01` ID, while initial script discovery accepted only the
newer Forestry Cart ID. Older placed `lumberjack-camp` records now receive a
one-time built-in behavior migration, and both lumber-capable wagon IDs are
valid. A persisted check flag prevents an intentionally removed behavior from
being restored on later rebuilds.

An isolated test loaded the actual Riverdale region and current player Lot
read-only. It created one crew and two workers, verified the Connector outside
point was walkable and harvestable firs were in range, then confirmed a worker
left camp during simulation. The player region remained byte-identical. The
focused Lot-script, timber and Lot-simulation suite passed 45/45.

Region town labels retain their black outline and now set the warm ivory RGBA
fill directly on each town label, above the stylesheet cascade. The focused
runtime label check passed. No player save, commit, push, or review sync.

The live region-map review then showed Unity's 1.5 px TextCore outline visually
consuming the face of the 12 px town glyphs despite their opaque color value.
Town names now use two aligned layers: a black outlined backing label and a
separate solid warm-ivory foreground label. The face is therefore independent
of outline shader coverage while preserving the approved stroke.

### September 19 — Zoom 3 pan and district building-card scrolling

Player-facing Zoom 3 (`LOD2`) pans 30% faster for both arrow keys and edge
hovering. Zooms 1 and 2 retain their fine-control multiplier, and Zoom 4 onward
retain their previous rate.

Industry and saved-Lot cards no longer shrink to fit a modal viewport. Each card
keeps its own content height and selectable controls while the bounded content
area scrolls vertically; the modal Close bar remains outside the scroll area.
The shared saved-Lot rule covers Residential, Commercial, Industrial, Mixed,
Farm, Transit, Civic and Park Lot browsers.

### September 19 — Saved Industrial Lots in the Industry list

The saved `lumberjack-camp` Lot was already authored as `LotType.Industrial`;
it does not need to be deleted or recreated. It was available through **Build →
Zoning → Browse Industrial**, but the separate **Industry** window only listed
mines and Brickworks. That window now lists every saved Industrial Lot first,
with its preview, dimensions, cost, and a Place action. Opening either saved-Lot
view refreshes the catalog once so a newly manually saved Lot is visible.
Normal district placement requirements remain enforced, and the change does
not write either the Lot or district save.

### September 19 — blocked lumber carts and Riverdale mill placement

A loaded Lumberjack Camp cart now exposes a blocked-destination state when its
Lot-owned script cannot find a reachable Lumber Mill. The timber navigation
cache contains a receiver matrix built once from the district's direct Lot
collection and reuses the cached road graph. Lot placement and transform edits
invalidate it alongside road-topology changes; no building or Lot scan was
added to the per-frame simulation path.

The first blocked event opens a right-side warning reading **Lumber cart has no
mill to drive to** with a **Place Lumber Mill** button. The button arms the saved
mill through normal district placement. Dismissal is immediate and suppresses
the warning until the condition clears, preventing a repeated flyout every tick.

Lumber Mill placement now resolves the barge against the nearest local river
tangent after confirming the mill building is on dry land. The dock pose is
stored on the district placement and applied only to the hosted runtime copy of
the Lot. The canonical player Lot is not rewritten. Full barge depth/footprint
and downstream-route validation remain enforced. An isolated read-only fixture
using the actual Riverdale region accepted 20/20 sampled riverbank sides. No
player Lot or district was saved, and no commit, push, or review sync was made.

### September 19 — Zoom 2, mill moorings, and cart turnarounds

Player-facing Zoom 2 (`LOD1`) now pans 50% faster than its previous close-view
rate. Zoom 1 and the separately tuned Zoom 3 rate are unchanged.

Lumber wagons now travel at twice the baseline horse-cart pace. The shared cart
controller also detects when a new road route begins behind the team and plans a
complete forward turning arc before joining it. This applies to timber and the
other district road-delivery carts using the shared route setter, keeping the
horse ahead of the articulated forecarriage and rear axle during return trips.

The Lumber Mill barge no longer snaps to a wide river's centerline. Its district
dock override now places the full boat just inside the procedural river's 0.3 m
navigable-depth boundary and stores a separate dry worker endpoint immediately
beyond the wet shoreline. Existing centerline overrides upgrade in memory via a
versioned dock contract; the canonical Lot remains unchanged. Isolated Riverdale
coverage accepted 20/20 sampled bank sides. The 16/16 timber suite, focused pan,
runtime-copy, and turnaround checks passed. No player save, commit, push, or
review sync was performed.

### September 19 — Road delete tool and local surface commit

The Roads menu's existing **Delete Road** action remains a click-drag tool with
a red X cursor. Escape cancels it, restores the normal cursor, and returns to the
quiet district selector. Removing a cell continues to repair only that cell and
its eight immediate neighbors, so adjacent road textures update immediately.

Road deletion now has a specialized surface commit. It restores the affected
hill samples and terrain collider but skips unchanged ground decals and the
district-wide fine-grid presentation; the derived grid regenerates at its
existing district or terrain rebuild boundaries. In a read-only Riverdale
fixture, road model deletion plus local artwork repair measured about 5 ms. The
post-delete surface work fell from about 795 ms to about 281 ms, including 4,225
terrain samples and Unity's roughly 136 ms collider recook. No player district
or Lot was saved, and no commit, push, or review sync was performed.

### September 19 — district rebuild audit and local invalidation

An audit of normal-play district rebuild entry points found additional local
operations that were invalidating broad presentation layers. Flora selection
dragging now moves only selected renderers and their spatial render batches;
group rerolls replace only that group's members. Coal mines, quarries, and
Brickworks now refresh their own industry presentations instead of rebuilding
the district. Lot placement/movement, ordinary road placement, bridge
approaches, and mixed-selection deletion now use local surface commits that do
not regenerate the district-wide fine grid.

Complete district builds remain limited to initial load or district switching,
full undo snapshot restoration, and district-wide terrain replacement. River
geometry operations continue to rebuild the water layer because junction and
edge-clipping output depends on connected paths. Explicit forest-coverage
generation remains a bulk flora boundary. The complete inventory and remaining
linear scans are recorded in `Documentation/DISTRICT_REBUILD_AUDIT.md`.

Dense isolated profiling measured 5,265.29 ms for a complete flora refresh
with 5,000 trees versus 14.74 ms to move ten trees through the new local
path. Riverdale road deletion retained its bounded behavior at about 266 ms,
including terrain restoration and collider cooking. Focused validation passed
2/2 architectural policy tests, 60/60 combined flora batching,
road/quarry/Brickworks tests, and 2/2 isolated performance profiles. A second
dense fixture measured 127.36 ms to insert 12 trees among 1,600 existing trees
versus 2,112.70 ms for the explicit bulk flora path. No player district or Lot
was saved, and no commit, push, or review sync was performed.

The performance rule is also enforced in the API. Ambiguous complete-paint
methods were removed. Full district, flora-layer, and river-layer rebuild methods
are named `RebuildEntireDistrict`, `RebuildAllFloraPresentations`, and
`RebuildAllRiverPresentations`, and every call must supply a non-`None`
`DistrictBulkRebuildReason`. Local edit code has only local surface and
cell/ID-based presentation methods. Architectural tests reject restoration of
the former unqualified methods.

### September 19 — fixed Road actions and authored barge composition

The Road Family modal now keeps **Bridges**, **Delete Road**, and **Cancel** in a
fixed action row below the scrolling road-family cards. Delete Road no longer
falls into the card scroll content or overlaps the Bridges label; it still arms
the red-X click-drag tool and Escape restores the normal selector.

District Lumber Mill placement no longer replaces the barge's Lot-local X/Z
position with a separate computed mooring. The shoreline solver translates the
complete Lot, retains the authored mill/barge relationship, and may rotate only
the shallow-draft hull to follow the river. Script offsets attached to a prop now
rotate with that prop, so the dockworker destination remains on the actual barge.
Legacy placement position fields remain readable for save compatibility but are
not applied. Nearby rivers and segments are found through bounded spatial-index
queries with reused result sets. Focused validation passed 55/55 cargo-loading,
Lot-script, Road-menu, spatial-index, and read-only Riverdale bank tests,
including all 20 sampled sides. A separate read-only check of the already placed
Riverdale mill found the runtime barge at its authored `(7.34, -4.16)` Lot-local
position, in water 0.115 m deep, with the script endpoint 3.306 m from its center.
No player Lot or district was saved.

### September 19 — persistent placed-Lot Details panel

The placed-Lot inspector now remembers whether **Details & actions** is open
when either rotation action refreshes the panel. This keeps both rotation
buttons available for repeated turns and updates the displayed facing after
each turn without forcing the player through the Details control again.

The inspector's former small × is now an explicit **CLOSE** button. Closing the
panel leaves the Lot selected; clicking the Lot again reopens its inspector,
while selecting a different Lot starts with Details collapsed. Focused isolated
EditMode validation passed 6/6. No player Lot or district was saved, and no
commit, push, or review sync was performed.

### September 19 — saved Lot view becomes initial district orientation

New district Lot placements now use the Lot Editor view saved with the Lot as
their initial orientation. The diagonal views map exactly through the hosted
district camera contract: NE remains unturned, while SE, SW, and NW become the
corresponding grid-safe quarter turns. Waterfront Lots try the authored turn
first and retain their existing fallback rotations when another bank alignment
is required. Legacy Lots without a saved view and top-down saves retain the
zero-turn default. Existing placed Lots and their saved rotations are unchanged.

Focused isolated EditMode validation passed 10/10 across view mapping, legacy
fallbacks, and district Lot-site behavior. No player Lot or district was saved,
and no commit, push, or review sync was performed.

### September 19 — automatic district day cycle with staged lighting work

Founded districts now advance through the existing lighting presets while the
simulation is running: Morning lasts 60 seconds, Noon 300 seconds, Afternoon
60 seconds, Early Evening 10 seconds, and Night 30 seconds. Pause stops the
clock. Manual choices in the Sun tools reset the selected period, and the
current phase appears beside the district year and season. The elapsed period
is part of the existing manual district serialization; the clock never invokes
Save or otherwise writes progress automatically.

Time counting is constant-time and uses the active district reference rather
than searching region tiles per frame. At a phase boundary, terrain, sky,
buildings, and Lots change immediately. Projected flora shadows update in
bounded groups of eight and their existing spatial render batches are replaced
one at a time, retaining selectable tree presentations. This replaces the old
single-frame full flora shadow/batch refresh.

In an isolated 1,000-tree district, the original boundary path measured about
434–449 ms for daylight phases and 155 ms for Night. The staged path reduced
the boundary itself to 0.27–1.27 ms; its largest measured shadow/batch slice was
7.82 ms, and it completed across 305 editor update slices. Focused isolated
EditMode validation passed 12/12 clock-duration, persistence/manual-save,
lighting, and staging tests, plus the 1/1 dense performance fixture. No player
Lot or district was saved, and no commit, push, or review sync was performed.

### September 19 — universal Lot population contribution

Lot Editor → Stats now exposes **Population added** for every Lot category,
including Industrial, Commercial, Civic, Park, Farm, and Transportation Lots.
The existing serialized `Residents` field remains unchanged, so saved Lots stay
compatible. Placing a Lot adds its authored population to the district's cached
demographic state exactly once; deleting it removes the same amount. Reload,
undo/bulk restoration, and manual Lot definition updates continue through the
existing incremental simulation boundaries without routine district scans.

Focused isolated EditMode validation passed 19/19 across all eight Lot types,
placement/removal/reload behavior, demographics, requirements, dense-cache
behavior, Stats UI exposure, and JSON compatibility. No player Lot or district
was saved, and no commit, push, or review sync was performed.

### September 19 — founder food reserve with no founder population

Founder Lots carry an explicit zero-population override: neither the Fort nor
the future Town Center adds residents, even if its reusable Lot definition later
receives an authored population. Placing a Fort establishes a minimum district
food reserve of 250, while the Town Center establishes 500. Founding preserves a
larger existing food stockpile and applies the reserve only in the successful
placement path. Older founder placements adopt the zero-population override at
their next district-simulation rebuild without receiving a retroactive food grant.

Ordinary Lot population still updates the cached district total on add/remove,
and those residents consume food only at season boundaries. Routine seasonal
settlement does not rescan placed Lots. Focused isolated EditMode validation
passed 22/22 across district simulation, founder placement, zero-population
persistence through reload and definition edits, and Fort/Town Center food
reserves. No player Lot or district was saved, and no commit, push, or review
sync was performed.

### September 19 — Town Center building, repaired rear and interior activity

Imported Joe's hollow-window Town Center as Buildings → Civics → Town Center.
The original six source files remain unchanged. A reproducible Blender script
rebuilds the missing rear elevation, adds interior floors and lining, window
sashes/glass and five lanterns, and exports full and reduced visual meshes.
The default facing presents the entrance in the Lot Editor. Day, evening and
night use the existing lighting presets and per-instance emission controls.

An existing strolling-couple Automata clip supplies decorative upper-floor
activity behind the windows. It retains the room anchor, uses depth and room
clipping, and stops advancing when distant/offscreen. Nearby night lights are
also culled locally; distant prefabs and shadow copies have no active people.
This does not change population, labor, worker optimization or navigation.
No district scan/rebuild or automatic persistence was added. No player Lot,
district or region was saved; creating/assigning a founder Lot remains manual.

Focused isolated EditMode validation passed 11/11 (Town Center, existing door
controls and outdoor couple animation). Graphics-enabled isolated Unity checks
covered day/night, rear closure, moving occupants and actual Lot placement.
A 100-building asset-density fixture measured 27,618 versus 9,419 triangles per
full/distant building and 11 distant draw calls. Close views with five active
couples and 25 lamps measured 224 draws; this is not a full-city or long-duration
benchmark. Details, timings, captures and reproducible helpers are recorded in
`Documentation/Validation/town-center-v01/`; source lineage is recorded in
`Documentation/Migration/TOWN_CENTER_V01.md`. This change uses the automatic
committed Regions Review handoff, without pushing or merging.

### September 19 — brighter Town Center windows

Raised Town Center interior emission from 0.62 to 2.0 and attic emission from
1.3 to 2.4 in the builder and all near/distant/catalog prefab representations.
Day remains unlit; lanterns, people, light counts and culling are unchanged.
Updated the focused lighting assertions for both interior and attic emission.
The original geometry repair used Blender's background Python interface; its
editable master remains under `Authoring/Buildings/TownCenterV01/`.

### September 19 — bundled Town Center Civics Lot

The Town Center is now available during normal play under **Build → Civic →
Browse Civic Lots** as the read-only bundled Lot `town-center-civic-v01`.
It occupies 2 × 2 district cells, costs $2,500, requires road access, is
available from the Founders Era, and contributes zero population. The catalog
uses the existing Town Center thumbnail and loads this resource once into the
cached Lot summaries; it adds no routine district scan or rebuild.

The previously disabled City Center founder card is now the enabled **Town
Center** choice backed by this same Lot. Founder placement retains its existing
zero-population override and 500-food reserve. The bundled definition is not a
player Lot save, and no player Lot, district, or region was written during QA.

### September 19 — authored District Town Center founder Lots

Added the persisted Civics subcategory **District Town Center** as `LotType` 8,
preserving every earlier numeric value. New Lot and Lot General expose it under
the Civics parent. District Town Centers remain visible in **Build → Civic →
Browse Civic Lots**, and every saved, bundled, or mod Lot in the new category
appears as an independent choice in the Start Town founder browser.

The selected founder Lot retains its authored population, jobs, wages, seasonal
revenue/cost, services, and resource benefits. The Town Center-specific
zero-population override and 500-food grant were removed; zero population on the
bundled example is now simply its own authored setting. The Fort alone retains
its legacy zero-population override and 250-food reserve. Older placements using
the legacy `city-charter-house` identity keep their load compatibility.

Founder discovery refreshes the existing cached Lot catalog only when the Start
Town browser opens. Placement updates one simulation profile and performs no
routine district scan or presentation rebuild. Existing player Lots are not
recategorized or saved automatically; assign Civics → District Town Center and
use the ordinary Save action when an authored Lot should become eligible.
