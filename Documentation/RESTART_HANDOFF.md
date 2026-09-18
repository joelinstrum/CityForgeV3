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
