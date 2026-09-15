# District labor v01 — 2026-09-13

Joe requested a Labor menu/modal to assign any affordable number of axemen, at a provisional $250 per axeman per season, dispatched from a future lumber mill. The current dispatch point is a small temporary timber stack on dry, open district land. This avoids requiring an unbuilt mill asset. Only Cilician Firs currently support harvesting.

## User flow

LABOR is at the upper left of the district editor, under REGION. Enter a nonnegative worker count; the modal shows seasonal cost, amount due now, treasury and stored wood. ASSIGN AXEMEN pays and applies the assignment. CANCEL makes no assignment or payment. The count field receives keyboard focus: enter a number, Tab, then Return to assign. FIND CAMP frames the dispatch point. Existing Pause/Go controls stop/run labor; opening a modal or editing with an active pointer gesture temporarily suspends work.

Workers reserve distinct trees, route to them, play the supplied Chop clip, fell a tree on the third strike, wait for its fall, collect eight provisional wood units, and return them to camp. Inventory credit occurs only on delivery. They repeat without individual commands. Reducing assignments sends excess workers home and deposits their cargo before removing them. No mill, farmer, stone/iron worker, stockpile capacity design, carrying animation or offline/district-background simulation is included yet.

## Provisional economics

A season is 600 seconds of active simulation, initially Summer. This new labor/economy clock does not change accepted flora seasonal artwork. Wages are charged up front for newly funded slots and again at each season boundary. Reducing staff gives no refund; reusing already-paid slots in the same season does not charge again. Insufficient funds reject new assignments or suspend work at the next season until an affordable assignment is applied. Counts have no artificial design cap; current treasury bounds hiring. Large crowds are not benchmarked.

## Persistence and undo

`RegionCityTile.Labor` stores assigned count, funded slots, season/elapsed time, dispatch point, wood inventory and individual worker IDs/slots, location, heading, target ID, phase/progress, route and cargo. Missing labor in older saves defaults to empty. Region saves include resource transactions immediately, ordinary progress every five active seconds, district exit and application exit. There is no offline time catch-up.

Existing five-step district undo snapshots include labor, tree state and treasury together. Wage/resource transactions participate in undo; routine movement/time autosaves do not consume history. Undo pauses active workers so they do not immediately repeat the reverted action.

## Code and asset contracts

- `DistrictLabor`: saved simulation and economy; no Unity scene objects or UI dependencies.
- `DistrictLaborNavigation`: four-metre routing grid, one-metre segment sampling, avoids water and placed/legacy founder lot footprints. No bridge routing, crowd collision or general navmesh yet.
- `DistrictWorldController.Labor`: supplied 3D axeman/axe, existing playable animation component, temporary dispatch marker.
- `CityForgeApp.DistrictLabor`: modal, simulation host, persistence and status updates.
- `Assets/Resources/Characters/AxemanLaborV01`: v02 supplied character/axe FBX plus unchanged original base-color maps. Model imported as Generic; original Idle/Walk plus Chop; 1.85 presentation scale gives approximately 1.8 m height. Source lineage is CityForgeMCP `artifacts/characters/axeman/chop-v02-axe/`.
- Original accepted flora PNGs, materials, camera registration and brightness contracts are unchanged. No per-tree brightness overrides.

## Verification

Focused EditMode tests cover wages/overflow rejection, season renewal and insolvency, reservations and exact-once wood delivery, mid-cargo JSON reload, staff reduction with cargo, unreachable/deleted trees, older saves, moved targets and routing around water. Results: `QA/DistrictLaborV01/labor.xml`.

Normal docked Unity Game view: loaded an actual saved district into a temporary save root; entered 2 using the focused modal field and submitted via Tab/Return; treasury changed 277025 → 276525. Distinct tree targets were reserved. First tree became a stump and 8 wood reached camp. A full RegionSaveStore reload and DistrictWorldController rebuild preserved 2 workers, 8 wood and 276525 treasury exactly. Close-up inspection showed the textured original worker at approximately 1.8 m height. Evidence and real-save hashes: CityForgeMCP `artifacts/characters/axeman/labor-v01/`.

Keyboard submission is verified. Physical mouse automation produced inconsistent pointer destinations in Unity, so a reliable mouse-only Labor workflow remains for Joe to confirm. Do not misreport it as tested or approved. No user save is intentionally modified by QA; QA entry requires a fresh splash and copies a saved district to a temporary root. The existing checkpoint PR #9 was already merged; this feature is a separate local checkpoint.
