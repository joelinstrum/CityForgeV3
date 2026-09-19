# Town Center V01 validation

Follow-up window brightness: interior emission is now 2.0 (previously 0.62),
attic emission 2.4 (previously 1.3). `lot-night-brighter.png` and
`lot-night-brighter-moving.png` are the updated graphics-enabled actual Lot
camera captures. Earlier captures below retain the original comparison baseline.
`tests-brighter.xml` records the repeated focused suite with assertions for both
interior and attic brightness at night/evening/day. No lights, meshes or runtime
work were added by this adjustment; the original performance run was not repeated.

All Unity work used the disposable project
`/tmp/cityforge-town-center-fGIMMc/UnityQa` with Unity 6000.1.12f1.
Blender 5.1.2 ran in separate background processes. No player Lot, district or
region was opened or saved; the V3 editor was not controlled.

## Visual checks

- `unity-day.png`, `unity-night.png`: same-camera isolated package renders.
- `unity-rear-day.png`: rebuilt rear siding, stone base and wall closure.
- `unity-interior-0.png`: later animation phase; the walkers move behind the sash.
- `lot-day.png`, `lot-night.png`, `lot-night-moving.png`: the actual enabled Lot
  camera after `LotWorldController.Build`, `NewEmptyLot`, building placement and
  Noon/Night changes. Native camera angle is retained; framing is centred on the
  building. These are direct camera captures, not OS-mouse/Game-view UI tests.

Reviewed the front, side and repaired rear, night emission, five lanterns,
and multiple walking phases. The source Tripo roof/porch/lettering irregularities
remain; the new rear is an authored interpretation because no rear reference was
provided. The room animation uses the existing 2 fps, eight-facing couple atlas.
Walls, floors, window bars and explicit room clipping obscure the pair naturally;
both people are not continuously visible at every angle. This is decorative
activity on the upper floor, not a full indoor character simulation.

## Functional checks

The focused suite includes four Town Center checks, four existing Dry Goods door
checks, and three existing outdoor couple Automata checks. Tests cover night/
evening/day transitions, shared material isolation, local light culling,
clip advancement, retained room anchor through building rotation, shadow-copy
suppression, a collision ray against the repaired rear, a lower-poly distant
representation, and placement through the real Lot Editor code. Final results
are recorded in `tests.xml`. No persistence test writes a player save.

## Performance

Full representation: 27,618 triangles. Distant representation: 9,419 triangles,
65.9% fewer. Source meshes/materials, the walking atlas and sprite slices are
shared. One visible interior adds one actor renderer; five short-range lamps
are enabled only for a nearby, onscreen building at night. The distant prefab
contains no actor renderer or realtime lights.

`frame-profile.txt` measures 100 buildings in an isolated scene at 1400 × 1200,
with 30 warm-up frames and 120 recorded frames per configuration, in Editor
Play mode. The scene has a simple night directional light without shadows and
no district terrain, trees, workers or UI. This is a repeatable asset-density
fixture, not a complete city benchmark or a long-duration stability result.

| Measurement | Full mesh at distance | Distant mesh | Natural LOD, close camera |
| --- | ---: | ---: | ---: |
| Median frame time | 1.110 ms | 1.054 ms | 1.659 ms |
| p95 frame time | 1.411 ms | 1.302 ms | 1.946 ms |
| Maximum sampled frame | 1.602 ms | 1.410 ms | 2.006 ms |
| Median main-thread time | 1.112 ms | 1.034 ms | 1.632 ms |
| Median draw calls | 11 | 11 | 224 |
| Median submitted triangles | 2,763,482 | 943,582 | 907,386 |
| Median GC bytes/frame (whole fixture) | 4,154 | 4,154 | 4,154 |

The near view had five active couple renderers and 25 active lamp lights among
the 100 buildings. Extra per-pixel light passes account for much of the near
draw-call cost; it is not appropriate to claim 11 draws at every zoom. Profiling
lists/coroutines and the Editor contribute to the whole-fixture allocation
counter; it does not establish per-component allocation cost. Maximum reported
GC allocation was 9,434 bytes/frame in each configuration. GPU time was not
measured independently.

`performance.txt` additionally records synchronous update costs. Its direct
`Camera.Render` draw counters were zero and its allocation positive control
also returned zero; those two measurements are invalid, not evidence of free
rendering or zero allocation. Prefer the frame-based measurements above.

`TownCenterReview.cs.txt` and `TownCenterProfileDriver.cs.txt` preserve the
isolated QA helpers. They are not compiled into the game. Change their temporary
output path when reproducing elsewhere. Shader checks and actual renders were
run with graphics enabled; non-rendering NUnit checks used `-nographics`.

The source archive's six extracted files were byte-compared with `Source` and
remain unchanged. `git diff --check` passed. Regions Review is updated through
the workspace's committed handoff script; no push or merge is part of this work.
