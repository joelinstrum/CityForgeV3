# Mounted cavalry v01 — 2026-09-09

Joel requested the supplied horse and rider, with normal horse animation and restrained rider bobbing.

Source: `/Users/joelinstrum/Downloads/historical+cavalry+3d+model.zip`.
Original FBX/textures remain under `source/`; imported source scene is `Cavalry_Source_v01.blend`. The derivative is `Cavalry_Animation_Master_v01.blend`, exported as `Cavalry_Walk_Trot_Idle_v01.fbx`. Original white horse, tack, uniform, hat and rider are retained. The separate chestnut horse and wagon assets are unchanged.

## Rig and motion contract

`prepare-rig.py` adapts the established horse authoring script to the cavalry source; generated `build-rig.py` reproduces the complete rig, clips, export, measurements and previews. Source forward -X/up +Z becomes Blender forward -Y using `(-y,x,z)`; Unity forward is +Z. Authoring uses original source dimensions; Unity normalizes full mounted height to 3.0 m, footprint width 1.35 m/depth 3.1 m.

The custom 25-bone rig has ROOT, BODY, NECK, HEAD, two tail bones, front/hind limb chains with grounded hoof IK controls, and a RIDER bone parented to BODY. Bone landmarks follow this particular horse's posed anatomy. Automatic heat weighting is followed by a spatial rider override: 1,013 rider-region vertices are rigidly weighted to RIDER to keep the uniform, limbs and seated silhouette together. The source's 2,343 vertices become 2,316 after welding 27 coincident vertices. No vertex is left unweighted. Source surface defects remain.

Four-beat walk: 48 frames/1.6 s, 0.18 source-unit stride, 66% stance; diagonal trot: 30 frames/1 s, 0.255 stride, 48% stance; idle: 120 frames/4 s, planted hoof controls with gentle neck/head/tail motion. Rider inherits body movement, adds 0.002 source-unit bob in walk or 0.004 in trot, and a small pitch/roll. Rider/horse stay in one skinned rig and one synchronized animation clip. This is restrained seated motion, not a separately walking humanoid animation. Clips are baked, looping, and have no root travel.

Authoring previews: `Cavalry_Walk.png`, `Cavalry_Trot.png`, `Cavalry_Idle.png`. Authoring hoof loop errors are zero; idle hoof motion is zero. Walk rider vertical range 0.0118756 source units; trot 0.0316102. The live walk test including idle-to-walk transition measured 0.06278026 m rider excursion. The same generic motion-driven horse gait player chooses idle/walk/trot from actual travel and steps during turns.

## Unity integration

Stable ID `mounted-cavalry-v01`; **MOUNTED CAVALRY** in the 3D Characters library beside the horse. Resources under `CityForgeV3/Props/Animals/CavalryV01`, model `Cavalry_Walk_Trot_Idle_v01`, original atlas `base-color`. Existing horse material handling is reused (Standard, original atlas, neutral coat tint); the new source's white horse and blue/gold uniform are preserved.

`IsCavalry` identifies the variant; `IsHorse` includes it for existing presentation, gait, click-to-move, obstacle and ground handling. `HorseModelResource` selects the appropriate source. Cavalry gets its own height, texture and footprint, while the existing horse keeps its prior settings. Selection status reads “Horse and rider.” Select the mounted unit then click clear ground to ride there; the temporary destination arrow and existing animal pathfinding apply. Default travel remains 1.06 m/s. The rider's motion is embedded in the same clips, with no separate runtime bob script that could drift out of phase.

`CavalryImportPostprocessor` imports Generic animation without compression and assigns named looping clips. Adding the importer triggered Unity's broader FBX reimport pass; unrelated normal-map suggestions were ignored. New mounted variants should prefer established importer settings/metadata where possible to avoid another global import pass.

## Validation and reproduction

Runtime/editor code compiled; Unity imported all three clips. `Check Imported Gaits` sampled every imported clip at 61 points: four hoof bones, motion in walk/trot, zero hoof movement in idle, zero hoof and rider loop-end errors. `Preview Click To Ride` used the production command path to move 4 m: destination accepted, walk active, four hooves animated (0.2703352 m maximum sampled change), rider moved with the horse, endpoint error zero, final idle and travel speed zero. This is a command-path test, not physical mouse-coordinate validation. Normal docked Game view was inspected and saved as `game-view.jpg`.

QA menu: City Forge → QA → Cavalry → Check Imported Gaits / Open Mounted Rider / Preview Click To Ride. Reports are in this directory and Unity `QA/Cavalry`. The preview uses an in-memory Track variant and never saves. Current saved Track hash before/after: `ac47f66c53a80ede601f0d954bc2070d557cf7ca9998ce4456961b7c53a6cf53` (Joel had updated Track since the wagon work).

Future rider assets can reuse the gait generation and RIDER control, but must fit the new source's limb landmarks and rider weighting boundary, then repeat deformation/loop/Game-view checks. This pass provides mounted presentation and locomotion; no combat behaviors or dismount system are introduced.
