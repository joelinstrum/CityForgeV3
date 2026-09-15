# District rolling hills — September 13, 2026

Joe authorized Little River Bend as the shared, replaceable test landscape. It is tile city-033 in the Testy region, 62cf8923205e4a2994561301f10c28d9. This saved district now has seed 1209, hill height setting 45 m, coverage .75, peak 37.44 m, one west-to-east shallow river, one existing lot and road, and 770 flora items. The river cleared 215 submerged flora from the original 985. This is the actual saved district, not a temporary test copy. Workers remain paused during review. No other district was intentionally edited.

## Player controls
Terraform → Terrain → Hills opens height (0–60 m), coverage (10–100%), and seed inputs. Apply saves and rebuilds; Flat removes relief; Cancel leaves it alone. Ctrl+Z undoes an applied change. The full new-district wizard, climate, mountains, beaches, lakes and ponds remain future work.

## Runtime contract
RegionCityTile.Hills stores version/seed/height/coverage; old saves default flat. DistrictElevation generates deterministic broad caps without consuming Unity random state. Triangulated ground and MeshCollider share one mesh; all terrain sampling follows those exact triangles. Nominal ground spacing 5 m, capped at 512 segments per dimension. Existing river corridors and road/lot pads blend to zero elevation. This first milestone keeps infrastructure level instead of implementing graded roads, bridges or stepped foundations.

Flora roots, ground decals, workers/camp, placement guides and grid follow elevation; flora shadow vertices sample the receiving ground. Original flora images/material appearance are unchanged; no per-tree brightness overrides. Camera pan height follows the ground, retaining the existing rotation. Old district objects deactivate before deferred destruction so reload queries do not see stale renderers.

World refreshes elevation around changed river, road and lot placements. District composition includes hills settings, so loaded or undone relief rebuilds normally. Existing flora billboard depth and water presentation contracts remain in use; hill-specific terrain occlusion/shadowing and steep or extreme terrain are not expanded in this milestone.

## Validation
22 Unity EditMode tests passed (three elevation, nine labor, five wood resources and five tree harvest tests). The terrain checks cover deterministic disk-serializable settings, old flat defaults, level river/road/boundary corridors, mesh sampling and Unity random preservation. Runtime and test assemblies compile.

Actual saved Little River Bend was loaded, modified, saved, reloaded through RegionSaveStore, rebuilt and inspected in normal docked Game view. QA compares saved tile JSON and every generated height, verifies flora bases outside rivers, and tests downward collider hits against sampled ground. Log: HILLS QA PASS district=Little River Bend peak=37.44 flora=770 rivers=1 lots=1 savedReload=true collider=true.

Real keyboard input in the modal changed height 45→40 and saved it; Ctrl+Z restored 45 on disk. Modal and district views inspected. Physical mouse automation did not activate the Hills button consistently, matching the existing automation problem; mouse-only UI acceptance remains for Joe. No large-population performance benchmark or comprehensive lighting/terrain combination claim.

Evidence and crop-free Unity captures: CityForgeMCP/artifacts/terrain/hills-v01. The initial immediate reload probe encountered stale renderers, resolved by deactivating old content before destruction. A live scripting reload produced existing UI-root/QA-save-root errors; use a fresh Play session when refreshing scripts. Normal saved-district reload passed after the fix.

## Continuation
The live Unity checkout includes separate concurrent Dry Goods/lighting work. The isolated hills checkpoint is based on the Resources checkpoint b0aedc8; only the hills diff against pre-work snapshots belongs to this branch. Do not stage unrelated lighting/assets or switch the live branch. Review Little River Bend with Joe before expanding the wizard.
