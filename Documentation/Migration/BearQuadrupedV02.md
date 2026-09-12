# Bear replacement — quadruped V02

Replaces the broken Tripo bear presentation with the rebuilt 20-bone bear and a two-second, 24 fps walk in place. The existing `bear-animated-v01` identifier stays stable so saved bear placements resolve to the replacement automatically. No navigation or additional behavior is added.

Runtime asset: `Resources/CityForgeV3/Props/Animals/BearQuadrupedV02/Bear_Quadruped_Walk_Unity_v02.fbx`. Explicit `base-color.jpg` binding preserves the supplied texture. Exact-path import rules use Generic animation and name the loop `Bear_Walk` for the existing playable animation driver. Export includes the duplicate cycle endpoint.

Authoring source: `/Users/joelinstrum/dev/CityForgeMCP/artifacts/animals/bear/v01/Bear_Quadruped_Walk_v01.blend`. Original faulty runtime FBX and texture are archived under `Authoring/Animals/Bear/RetiredOriginalV01`, outside shipped Resources. Source surface defects were explicitly accepted by Joel.

Validation: passed in the normal, docked Unity Editor Game View (6000.1.12f1). The existing Animated Bear QA lot loads the replacement, the front and rear leg poses change during playback, and the bear stays in place. The Console showed zero errors. Import metadata confirms Generic rig, Bear_Walk, loopTime enabled, frames 1–49. Evidence: `/Users/joelinstrum/dev/CityForgeMCP/artifacts/animals/bear/unity-v02/game-validation.json` and adjacent Game View captures. The existing Lot Camera was temporarily zoomed to orthographic size 2.5 for inspection; no alternate camera was used. Standalone player was not rebuilt.
