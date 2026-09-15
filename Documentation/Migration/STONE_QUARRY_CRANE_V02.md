# Stone quarry crane v02

## Source and derivative

- Canonical quarry: `CityForgeMCP/artifacts/buildings/stone-quarry/v01/v02_metric/StoneQuarry_metric_v02.blend`. Original Blender/FBX, UVs, textures, and v01 prefab are preserved.
- `Tools/Quarry/derive_crane_base.py` exports `Assets/CityForgeV3/Resources/CityForgeV3/Industry/StoneQuarryCraneV02/QuarryBase.fbx`, removing the fused upper crane while retaining the lower trestle and stone platform. The derivative reuses the original quarry material and atlas. `lineage.json` records source and face counts.
- `QuarryCranePresentation` adds a separate timber swivel jib, iron stays, moving rope trolley, pulley, hook and lifting slings. The existing forestry wagon is reused with its logs hidden and parked on the quarry apron beside the jib. No legacy project assets were copied.

## Behavior

- Each load lifts vertically, swings around the mast, lowers into its wagon slot, then settles. The wagon accumulates visible blocks, up to the existing eight-slot script limit. The empty hook returns to the pickup position after unloading.
- Crane pose is evaluated from the saved quarry phase and elapsed time. It does not run an independent timer or credit inventory. Pause, lack of wages, save/reload, and quarry rotation retain the correct suspended load.
- Default loading duration is 16 seconds. Existing sites with the former 4-second default receive a one-time upgrade, preserving the fraction of an in-progress load. Other customized durations are retained; later script edits are unchanged.
- Stone is still credited exactly once when loading completes. The existing full-wagon stockpile transfer remains in place; wagon travel is outside this change.

## Validation

- 52 EditMode tests passed, including slow loading, credit timing, migration, pause/reload and crane path checks.
- Isolated Play Mode review verified all eight landing slots, first-layer stone resting on the actual wagon deck, boom rotation, unchanged suspended load after reload/pause, and correct landing after a 90-degree quarry rotation.
- Full-size visual review confirmed visible stone accumulation on the wagon. Artifacts: `CityForgeMCP/artifacts/buildings/stone-quarry/v01/crane-live.txt`, `crane-loaded-wagon.jpg`, and `tests.xml`.
- Live simulation completed a crane loading cycle and activated the first wagon cargo block (`crane-running.txt`). The temporary review district was restored afterward.
