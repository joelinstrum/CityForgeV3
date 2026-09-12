# Lumber work wagon v01 — 2026-09-09

Requested by Joel as the first work-wagon variant of the accepted one-horse carriage template.

## Source and lineage

Source archive: `/Users/joelinstrum/Downloads/wooden+wagon+3d+model.zip`.
Extracted original FBX/textures are under `source/`. `Lumber_Wagon_Source_v01.blend` preserves the imported source. `build.py` produces the separate metric derivative `Lumber_Wagon_Articulated_Master_v01.blend` and `Lumber_Wagon_Articulated_v01.fbx`. The original carriage and horse sources are not changed.

All 4,663 source polygons and their UVs/material assignments are retained: body 3,317; forecarriage 393; front wheels 227/187; rear wheels 277/262. The source is slightly asymmetric, so each wheel uses its own measured hub. `partition.json` records the split. `wheel-separation-45.png` inspects the wheels rotated independently of the body. The driver, lumber, ropes and stakes remain rigid on the body. The front axle and shafts steer together.

Raw forward is -X, up +Z. Convert to Blender forward -Y using `(-y,x,z)*5.4`; exported Unity forward is +Z. Height including driver is 2.4001557 m. Average front axle Z is +0.0702 m, rear axle Z -1.8468 m, wheelbase 1.917 m. Horse center is +2.12 m ahead of body origin. Source wheel skew is retained; no source remodeling was requested.

## Runtime installation

Resources: `CityForgeV3/Vehicles/LumberWagonV01/` contains the new FBX and original base color. Model import preserves hierarchy, disables imported materials/animation/cameras/lights, and runtime applies the source color via Standard shader, tint 0.8, metallic 0, gloss 0.18.

New library item: **HORSE & LUMBER WAGON**, in 3D Characters beside **HORSE & CARRIAGE**. Stable prop ID: `horse-lumber-wagon-v01`. Reuses the accepted chestnut horse prefab and animation. Selecting either team opens the upper-right driving inspector. Drive follows a reachable road cycle; clicking ground commands a destination; Slow/Fast cruise at 1.06/2.12 m/s. Acceleration/braking remain 0.55/0.85 m/s². Front steering, rear tracking, actual-distance wheel roll, horse gait, five harness lines, saved headings/speed choice, rebuild motion continuity and dirt-road contact shadows are shared.

`HorseWagonDefinition` is the per-model contract for resources, height, horse and axle offsets, turning radius, placement dimensions, material tint, harness anchors and carriage shadow footprint. `IsHorseWagon` centralizes variant membership across selection, placement, motion, obstacles, pose restoration and inspector. Existing carriage constants remain as compatibility aliases; runtime calculations use the instance definition. Future variants should add measurements/resources and a stable ID/library item rather than fork the controller.

## Validation

Runtime, editor and EditMode assemblies compiled successfully. `LumberWagonQa.Check Shared Driving` exercised both original carriage and lumber wagon through two Track laps: routes accepted, all four wheels moved, two contact shadows, smooth starts/stops, and braking/speed preserved through a presentation rebuild. Lumber maximum hitch error was 0.000003815 m, maximum front/body steering angle 21.685 degrees. First 50 ms speed 0.0275 m/s, Fast cruise 2.12 m/s, braking after one second 1.27 m/s, parked stable at zero.

Live normal docked Game view: production selection hit test selected the wagon; Drive/Stop button callbacks ran, horse used walk, wheel travel reached 2.709846 m, then team stopped at zero. This is a callback/hit-test check, not a claim of physical mouse-coordinate verification. `game-view.jpg` shows the final parked team, upper-right controls, source texture and road shadows. QA preview uses an in-memory Track variant and never saves it. Saved Track SHA256 before/after: `3ff1c169b7f5c37fd0f2c0983be2b5f67acce8fca23a6c243e076624a0f74c72`.

QA commands: City Forge → QA → Lumber Wagon → Open Track Preview / Check Shared Driving / Preview Drive and Stop. Numeric reports copied here from Unity `QA/LumberWagon`. The source mesh's surface defects and asymmetric wheel geometry remain. This is the lot-editor horse/wagon template; automatic district freight behavior is not added.
