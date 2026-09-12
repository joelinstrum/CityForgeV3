# Bear behaviors V03

- Calm near ordinary people; no aggression is added.
- Roams for 12–24 seconds, then looks around in place for 2–4 seconds.
- Retreats from musketmen within 10 m and the existing wooden fort watchtower within 18 m. During retreat, clear distances expand to 14 m and 23 m; a two-second cooldown prevents repeated switching at the boundary.
- Walks at 0.20 m/s and retreats at 0.36 m/s, with matching playback-rate changes for the accepted short-stride walk.
- Uses local steering, lot bounds, conservative building/prop footprints, and terrain slope checks. This is a basic local avoidance routine, not a full path planner. Ordinary people are not obstacles or threats; this pass adds no attacks or combat.
- Saves current position through the existing placed-prop data. Transient roam timers restart when the presentation is rebuilt or the lot is loaded.

`Bear_Walk_Idle_v03.blend` derives from the approved V01 bear rig. The new idle keeps all four paws planted and gently rotates HEAD and NECK. FBX exports both actions with closed cycle endpoints. The runtime uses the existing V02 base-color texture without modification.

Unity files are under `Assets/CityForgeV3/Runtime/World/`: `BearBehavior`, `BearRoamingAgent`, and `LotWorldController.Animals`. The shared animation driver gains playback-speed control. `BearQuadrupedImportPostprocessor` maps the two FBX takes to `Bear_Walk` and `Bear_Idle`, without humanoid retargeting.

Use **City Forge > QA > Bear Behaviors > Open Roaming Bear** while in Play Mode to see the default behavior. **Run Behavior Checks** exercises the actual lot with an ordinary person, a stationary musketman, and a fort watchtower, and records results in `QA/BearBehaviorV03/report.json`. It opens a new disposable lot and does not save over existing lots.

## Validation

Runtime and editor assemblies compiled successfully. Live Game View probe passed: roaming distance 4.88 m, ordinary person ignored, idle position fixed, idle head excursion 8.35 degrees, and musketman/tower retreat each increased separation by 4.05 m. The policy checks also verify cooldown, return to roaming, and exclusion of the clock tower. Import metadata contains looping Bear_Idle (4 seconds) and Bear_Walk (2 seconds). Blender idle samples show zero paw displacement.

Evidence: `/Users/joelinstrum/dev/CityForgeMCP/artifacts/animals/bear/v03/game-validation.json` and adjacent normal windowed Game View screenshots. The existing QA lot setup can log an inactive-world coroutine warning while opening a new fixture; the behavior probe completes successfully afterward. Standalone player has not been rebuilt.
