# Chestnut horse animation foundation, v01

Source: Joel's `horse+3d+model.zip`. The original archive is unchanged; extracted FBX and texture maps are preserved in `source/`. The source includes a bridle but no pulling harness.

## Assets

- `Horse_Animation_Master_v01.blend`: editable custom 24-bone rig with four hoof IK controls, neck/head and two tail joints. Not Rigify-generated. All 1,142 welded mesh vertices receive deformation weights; original UVs and coat atlas preserved.
- `Horse_Walk_Trot_Idle_v01.fbx`: baked Generic rig, three loops, no humanoid retargeting. About 2,500 triangles.
- Walk: four beats, 48 frames at 30 fps, 1.6 seconds; nominal game speed 0.53 m/s.
- Trot: diagonal pairs, 30 frames at 30 fps, 1 second; nominal game speed 1.65 m/s.
- Idle: 120 frames at 30 fps, 4 seconds; gentle head/neck and tail movement, stationary hooves.
- Each take includes its closing endpoint. Unity imports 0–48, 0–30, and 0–120 respectively.

## Game integration

`horse-animated-v01` appears as HORSE in the character library. It stands idle when placed. `HorseGaitController` watches actual movement of its presentation root and selects idle, walk, or trot, matching playback speed to travel. Turning also plays the stepping cycle. Each instance starts with a different animation phase.

Horse roots are scaled to 2.2 m overall height (ears included), roughly 1.65 m at the withers. Unity local +Z is forward and +Y is up. Motion belongs to the future vehicle/team controller; the horse animation does not apply root motion or run bear threat/roaming behavior. No harness or vehicle attachment was added in this stage. Turning currently reuses the walk cycle rather than a dedicated planted-foot pivot clip.

Coat uses the unmodified source sRGB base-color JPG, Standard shader, metallic 0, smoothness 0.12, and neutral 0.7 brightness tint for the bright lot lighting. No fence maps or source metallic map are applied to fur. Model/texture live under `Resources/CityForgeV3/Props/Animals/HorseV01`.

## Verification and preview

The Blender hoof check confirms movement of all four legs in walk/trot, zero idle hoof displacement, and zero loop endpoint displacement. The FBX round-trip check compares 15 deformed mesh samples across all three takes, with maximum nearest-vertex error below 0.000001 source meters. `check-export-offset1.py` uses Blender's one-based frame origin; Unity uses zero-based imported clips.

Unity scripts compiled successfully. Use normal windowed Play mode, continue the splash, then **City Forge > QA > Horse > Open Animated Pair**. This creates a disposable two-horse QA lot and cycles idle, walk, trot, stepping turns, and idle. It is a motion showcase, not a finished harness/team system. After the first cycle it writes `QA/HorseV01/report.json` in the Unity project. `game-pair.png` is captured from the normal docked Game View. The existing inactive-world coroutine error occurs during QA lot creation; no standalone player was built.

`integration.patch` records changes to existing game files. The authoring scripts and new runtime/import/QA scripts are retained alongside the assets. Do not rerun `integrate.py` on already modified source: it records pre-change backups and applies the initial installation edits.

Final live result: PASS, two horses; idle/walk/trot/turn stepping observed; idle hoof displacement 0 m, walking hoof displacement 0.239 m, trotting hoof displacement 0.560 m. Full report: `game-validation.json`.
