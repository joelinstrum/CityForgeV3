# Cilician harvest district prototype — 2026-09-13

Select one or more existing Cilician Firs with the district rectangle. Choose Fall direction, then Fell firs. After the 1.5-second animation, Clear wood leaves stumps. Both completed actions support district Ctrl/Command+Z. Other tree species retain their existing behavior.

Standing uses the unchanged approved artwork/material path. Four directional sheets and a separate stump use the authored shared pivot and PPU. Harvested sprite hit bounds exclude transparent animation-canvas padding. Projection shadows retain the shared flora shader and update atlas UVs during playback; these remain billboard projections, not a full 3D fallen-tree collision/shadow model. No per-tree brightness override or original asset changes.

## Saved state and worker interface

PlacedDistrictFlora gains HarvestState (Standing=0, Fallen=1, Stump=2), HarvestDirection (0..3), and RemainingWood. Old saves default Standing. Fell stores the durable Fallen state immediately, while the transient player shows the animation. Loading during a fall therefore shows the settled tree and never replays/duplicates yield. Undoing felling restores standing and zero wood; undoing clearing restores fallen wood. Editing one tree refreshes only that tree, so concurrent fall animations are not interrupted.

CityForgeApp.BeginDistrictTreeFall(instanceId, direction) is the future chopping-complete hook. It rejects non-Cilician, already harvested, or active user gestures. TakeDistrictTreeWood(instanceId, amount) returns only the amount successfully removed, supports partial pickup, rejects pickup during the active fall, and leaves a stump at zero. Stable instance ID/position/scale persist. Calls must target the currently open district on Unity's main thread. Workers must reserve trees/pickups and credit stockpiles on delivery; those systems are not included.

Eight units per tree is an explicit prototype yield, not balancing. Clear wood discards the remaining prototype wood without crediting resources. No worker animations, physical log piles, hauling paths, growth or stump-removal tool are included. Direction labels follow the source-space compass; worker-relative mapping and clearance checks must be verified during worker integration.

## Verification and limits

- Five EditMode tests passed: old-save compatibility, species/yield guards, partial pickup serialization and depletion, all 96 imported frames plus stump, and undo restoration.
- Runtime QA loaded existing saved Cilician ID 974d16fb7fc24da5918bd9bb9684a254, edited only a deep copy in a temporary save folder, blocked pickup during fall, rebuilt the world from a saved stump, then undid clearing/felling and compared the entire district JSON exactly. Passed.
- Normal docked Game view inspected standing/fallen/stump. A later isolated view hides neighboring flora only in the temporary copy for clarity. QA menu invoked the fall/clear actions; physical toolbar clicks were inconclusive, so toolbar input is not claimed verified.
- New harvest controls were enlarged after inspection for readable labels/click targets. Detailed directional collisions, animated occlusion at every time of day, and end-to-end worker inventory are not verified.
- All real region save hashes unchanged. Existing animator-controller files were left alone.

Evidence: CityForgeMCP/artifacts/flora/cilician-harvest-runtime-v01. Art source, full animation preview and material contract: CityForgeMCP/artifacts/flora/cilician-harvest-v01. Joe runtime appearance acceptance pending.
