# Horse and wagon template

Established 2026-09-09 for Founders and Industrial-era horse-drawn transport.
Joel accepted the horse, turning and carriage: “VERY Impressive, i love the
horse's turning and the carriage. This is fantastic.” He designated this setup
as the horse-and-wagon template going forward and requested Slow/Fast speeds.
Preserve the accepted appearance and articulated motion when deriving wagons.
The Slow/Fast addition is an implementation of that request, not a separate
record of visual approval from Joel.

## Player behavior

- Place **HORSE & CARRIAGE** from the Characters library. It is one selectable
  prop with one horse, a driver, carriage, shafts, reins and harness details.
- Click the horse or carriage to open the existing inspector at the upper
  right, regardless of the current tool category. It exposes Drive, Stop,
  Slow, Fast and Delete. Re-selecting reopens a closed panel.
- Drive joins a reachable closed road cycle and continues around it. Stop
  parks without snapping the horse or either axle into a new pose.
- Slow is the accepted 1.06 m/s speed. Fast is exactly twice that: 2.12 m/s.
  Both buttons work while stopped or driving; changing speed does not replace
  the route or restart the horse. Speed transitions now ease toward the target;
  Slow is the default for existing saves.
- Clicking ground replaces the road-loop order with a destination order and
  uses the existing temporary direction indicator. Unreachable destinations
  and missing/unreachable road loops show a status instead of teleporting.
- Speed and parked pose are saved with the lot. Motion orders are transient;
  a fresh load restores the pose and chosen speed, not an active drive order.

## Canonical assets and lineage

Paths below are relative to `/Users/joelinstrum/dev/CityForgeMCP` unless noted.
Never overwrite these accepted source assets for a different wagon. Create a
versioned derivative, give it a new resource path/ID, and record its parent.

| Asset | Canonical source / export |
| --- | --- |
| Rigged horse | `artifacts/animals/horse/v01/Horse_Animation_Master_v01.blend` |
| Horse FBX | `artifacts/animals/horse/v01/Horse_Walk_Trot_Idle_v01.fbx` |
| Horse rig authoring | `artifacts/animals/horse/v01/build-rig.py` |
| Articulated carriage | `artifacts/animals/carriage/v02-horse-team/Carriage_Articulated_Master_v02.blend` |
| Carriage FBX | `artifacts/animals/carriage/v02-horse-team/Carriage_Articulated_v02.fbx` |
| Carriage authoring | `artifacts/animals/carriage/v02-horse-team/articulate.py` and `save-master.py` |
| Accepted articulation evidence | `artifacts/animals/carriage/v02-horse-team/game-validation.md` |
| Road Drive/Stop addition | `artifacts/animals/carriage/v03-drive/README.md` |
| Slow/Fast addition and evidence | `artifacts/animals/carriage/v04-speeds/` |
| Acceleration/braking and road shadows | `artifacts/animals/carriage/v05-easing-shadows/` |

Original inputs: `/Users/joelinstrum/Downloads/animals/horse+3d+model.zip`
and `antique+carriage+3d+model.zip` in that same directory. Original unhitched
carriage remains `antique-carriage-v01`; the assembled team is
`horse-carriage-v02`; horse ID is `horse-animated-v01`. Runtime changes v03/v04
keep the accepted team ID so existing lots work without replacing the prop.

Unity project: `/Users/joelinstrum/dev/CityForge - V3`.
Resources under `Assets/CityForgeV3/Resources/CityForgeV3`:
- Horse: `Props/Animals/HorseV01/Horse_Walk_Trot_Idle_v01.fbx`; albedo in that same directory.
- Carriage: `Vehicles/CarriageV02/Carriage_Articulated_v02.fbx`.
- Carriage albedo: `Vehicles/CarriageV01/base-color`.

Retain original UVs and coat/albedo textures. The carriage runtime material is
Standard, color multiplier (0.7, 0.7, 0.7), metallic 0 and glossiness 0.18.
The derivative retains all 4,461 original carriage faces, including driver.
Do not replace the accepted coat or carriage colors with a generic material.

## Rig, articulation and wheel contract

The horse uses the custom quadruped rig with hoof IK and walk/trot/idle clips.
There is no requirement to redo the rig for each vehicle. Instance the horse
presentation under the team root and let its gait follow actual movement.
The team root represents the horse; it leads a front-axle follower, which leads
an independent rear-axle follower. The carriage must not simply inherit the
horse's heading: the delay and independent axle headings are the accepted turn.

Current carriage dimensions/constants in meters:

| Measurement | Value |
| --- | ---: |
| Horse ahead of carriage model origin | 2.05 |
| Front axle ahead of carriage origin | 0.176 |
| Rear axle behind carriage origin | 2.0295 |
| Horse-to-front axle shaft length | 1.874 |
| Front-to-rear axle wheelbase | 2.2055 |
| Destination planner turning radius | 3.25 |
| Carriage normalized height, including driver | 2.644676 |

`Carriage_Forecarriage` contains front frame and shafts. At runtime it and
`Carriage_Front_*` wheels are parented, preserving their transforms, beneath
`Forecarriage Steering`. Body, driver and rear wheels follow the rear axle.
The wheel controller measures each wheel's actual signed travel and radius;
front wheels use the steering frame's direction and rear wheels the body's.
It must roll from distance, not a fixed animation timer. Four wheels are
expected. The driver and body are rigid; this is a kinematic follower, not a
suspension or wheel-contact physics simulation.

Five runtime leather LineRenderers add two shaft links, two reins and a girth.
Their endpoints follow the horse and carriage each frame. Preserve the source
modeled reins and original shafts. Keep serialized harness references and
reload-safe wheel discovery; earlier play-mode reloads lost nonserialized
collections and those were fixed.

## Runtime ownership

All runtime files below are under `Assets/CityForgeV3/Runtime` in Unity:

- `World/HorseCarriageController.cs`: forward-only tangent/arc entry and
  destination planning; sampled routes; fixed shaft and wheelbase constraints;
  loop cursor; speed; harness. No turn-in-place or reverse maneuvering.
- `World/LotWorldController.HorseCarriage.cs`: assembly, selection commands,
  road Drive/Stop, speed changes, obstacle checks, per-frame position/pose
  updates and presentation-rebuild restoration.
- `World/LotWorldController.Carriage.cs`: imports the model, material and
  articulated front-axle hierarchy; adds the wheel controller.
- `World/CarriageWheelController.cs`: distance-driven rolling and steering.
- `World/HorseGaitController.cs`: walk above 0.025 m/s, trot above 1.35 m/s,
  idle when stopped, turning steps. Walk reference stride speed is 0.53 m/s;
  trot reference 1.65 m/s; playback is capped at 2.5. Slow walks, Fast trots.
- `World/LotWorldController.Props.cs`: single-prop presentation, hit-test,
  placement/selection and rebuild integration. Existing teams select without
  teleport-dragging; new placement remains draggable.
- `World/LotWorldController.AnimalCommands.cs`: shared ground destinations
  and arrows. Other clicked objects take selection priority.
- `World/LotEditorState.cs`: `PlacedProp.CarriageFast`, `HasCarriagePose`,
  `HorseHeadingDegrees`, `CarriageHeadingDegrees`,
  `ForecarriageHeadingDegrees`, and ordinary position fields.
- `UI/CityForgeApp.cs`: carriage inspector overrides the tool-category panel;
  speed buttons highlight the selected setting. Deferred `RefreshLotEditor`
  avoids duplicate screens and tearing down elements during pointer dispatch.

Drive derives both directions from `VehicleRoute.FromNetwork` using the lot's
vehicle graph, a 1.05 m right-lane offset and four smoothing passes. It finds
an entry near the horse and tries forward joins at 6, 10 and 16 m look-ahead,
with a constrained arrival heading. It validates the approach plus two laps
against static obstacles before repeating a single lap. The destination
planner's default API only plans to the final waypoint; do not pass a road
polyline to it expecting intermediate points to be followed.

Obstacle tests cover horse, front axle, rear axle and axle midpoint using the
existing animal clearance and terrain rules. Validation rejects excessive
horse/front and front/body angles (45 and 55 degrees respectively). Moving
teams are currently excluded from static obstacle caching: this is not yet a
multi-vehicle traffic/yielding system. Open-path automatic traffic, reversing,
traffic law, deliveries, loads and multiple-horse teams are future work.

## Deriving the next wagon

1. Reuse the accepted horse rig/materials and the team control architecture.
2. Import the vehicle into a new versioned Blender source. Retain UV/materials;
   separate wheels at their true hub centers. Separate the front steering
   frame and shafts if its design has a steerable front axle.
3. Measure the new model's origin, shaft attachment, axle positions, radii,
   footprint and height. The constants above fit this carriage only; make
   per-vehicle configuration before hosting differently sized wagons together.
   Do not globally change these constants to fit a new wagon.
4. Create a distinct presentation/resource and prop ID; connect harness points
   to the actual geometry. Reuse movement and distance-based animation.
5. Verify forward start, corners, U-turn, Stop/resume, Slow/Fast, blocked
   destination, save/load and rebuild continuity. Inspect driver, coat, shafts,
   reins, wheel hubs and ground clearance in the normal docked Game view.
6. Record the derivative's source paths, measurements, parent assets,
   screenshots, checks and Joel's acceptance. Preserve this baseline.

## Acceptance fixture and checks

Joel's saved `Track` is the real acceptance fixture, a 60 x 60 m lot with a
20-piece closed road loop and one team. Load it read-only for testing; do not
silently overwrite Joel's save. Editor QA lives in `Assets/Editor`:
`HorseCarriageQa.cs`, `CarriageDriveQa.cs`, `CarriageSpeedQa.cs`.
Menu: **City Forge > QA > Carriage**.

The v03 controller check covered two laps (406.06 m), no blockage, maximum
hitch error 0.00000358 m, shaft angle 26.15 degrees, axle angle 24.47 degrees,
and stable Stop. UI callback checks started walking, stopped and resumed with
one editor screen. Shared screen hit-test selected the carriage from Main.
Joel subsequently accepted the live turning and carriage as quoted above.
Remote physical mouse automation had a coordinate mismatch; do not confuse
callback/hit-test checks with a physical mouse test, and do not add compensating
input-coordinate changes to the game. Use the normal Game view for visual QA.

Speed-specific results are retained in `artifacts/animals/carriage/v04-speeds`.

Final speed validation (2026-09-09): one simulated second produced 1.060229 m
at Slow and 2.120098 m at Fast (ratio 1.999661). Fast completed two laps without
blocking, with maximum hitch error 0.00000322 m. Saved speed and moving state
survived a presentation rebuild. Both button callbacks correctly highlighted
the selected mode; live Fast selected trot at 2.119944 m/s, Slow selected walk
while retaining the route. Runtime/editor compilation passed. See `checks.txt`,
`live.txt`, `integration.diff` and `game-fast.jpg` in the v04 artifact directory.

## Eased motion and road shadows — 2026-09-09

Joel accepted Slow/Fast (“fantastic, so good!”) and requested acceleration,
deceleration and shadows under both horse and carriage on dirt roads.
`HorseCarriageController.CurrentSpeed` now approaches the selected cruise speed
at 0.55 m/s² acceleration and 0.85 m/s² deceleration. Slow takes about 1.93 s
to reach cruising speed and Fast about 3.85 s. Stop retains the route while
braking, then parks; Fast takes about 2.5 s and 2.64 m to stop. Speed changes
also ramp. The inspector reports STOPPING, disables repeat Stop, and allows
Drive to resume during braking. Destination paths reduce their speed near the
end using remaining distance and braking rate. An obstacle can still stop the
team immediately to prevent driving through it.

Current speed, braking state and remaining route distance are included in the
transient MotionState preserved through presentation rebuilds. They are not
persisted as active movement in lot saves. The saved Slow/Fast choice remains.
Older QA checks expecting immediate starts/stops must use the new motion
contract; the v03/v04 measured distances are historical pre-easing results.

`World/HorseCarriageGroundShadow.cs` adds two independent soft contact
footprints beneath the horse and carriage, using the existing
`CityForgeV3/VehicleContactShadow` shader at render queue 3150, with depth test
LEqual. Each footprint follows its own heading; a small sun-direction offset
adds directional shading. A 4-by-8-cell mesh samples terrain at every vertex
and sits at terrain + 0.145 m, following the established street-vehicle road
shadow height. Day opacity is 0.38, reduced to 0.18 in rain/night. These are
soft ground/contact shadows, not detailed projected animal silhouettes.
Ordinary mesh shadow casting remains enabled. The shadow group is named
`Projected Prop Silhouette`, so existing hit-test/front-recovery exclusions
apply. Preview ghosts do not create contact shadows. Generated materials and
meshes are released with the component. Preserve these ordering and depth rules;
an Always depth test would paint shadows over vehicles/buildings.

The new editor checks are in `CarriageEaseQa.cs`, under the same Carriage QA
menu: Check Easing and Shadows, Open Road Shadow Closeup, and Preview Gentle
Drive Stop. Close-ups pan/zoom the ordinary docked Game camera. Source diffs,
measurements and screenshots are recorded in the v05 artifact directory.

V05 verification: Fast ramps from 0.0275 m/s on the first 50 ms step to 0.55 m/s
after one second and 2.12 m/s at cruise. Braking leaves 1.27 m/s after one
second and parks after 2.5 seconds over 2.640465 m. A rebuild during braking
preserved speed/state; parked pose stayed stable. An 8 m destination finished
at zero measured position error with final pre-stop speed 0.0837 m/s. Emergency
obstacle stop passed. The live preview ended at speed zero, horse idle, with
four wheels; two contact shadows were inspected on dirt in the normal Game
view. Runtime/editor/EditMode assemblies compiled successfully. Track's saved
source remained unchanged. See v05 `README.md` and recorded checks/screenshots.

## Lumber work-wagon variant — 2026-09-09

Joel requested applying this complete template to
`/Users/joelinstrum/Downloads/wooden+wagon+3d+model.zip`, with additional work-wagon
variants expected. New ID `horse-lumber-wagon-v01`, library label **HORSE & LUMBER
WAGON**, resource `CityForgeV3/Vehicles/LumberWagonV01/Lumber_Wagon_Articulated_v01`.
It uses the existing chestnut horse. The original carriage remains separately
available and its measurements/behavior are unchanged.

`HorseWagonDefinition.cs` now holds per-vehicle resource/texture, height, horse
spacing, front/rear axle offsets, turning radius, placement bounds, tint, shaft
and driver hand anchors, and road-shadow footprint. The shared controller uses
`Definition.ShaftLength` and `Definition.Wheelbase` throughout planning,
clearance, restore and motion. Legacy static constants still describe the
original carriage only; new code must use instance `Definition` measurements.
`LotWorldController.IsHorseWagon(id)` includes both variants throughout selection,
movement, obstacles and pose/rebuild handling. Inspector titles follow the
vehicle definition; Drive/Stop and Slow/Fast remain shared.

Lumber metric contract: scale 5.4, height 2.4001557 m, horse offset 2.12 m, front
axle +0.0702 m, rear axle -1.8468 m, wheelbase 1.917 m; shadow size 2.1 by 3.3 m,
center Z -0.95 m. Preserve all 4,663 faces and original UVs. Its asymmetric source
wheels have individual pivots; front wheels and shafts belong to the steering
assembly while driver/load remain on the body. Asset lineage, authoring script,
Blender master, FBX, material contract, reports and Game view are in
`artifacts/animals/lumber-wagon/v01/`. Use that directory's README for reproduction.

Verification: both carriage and lumber wagon completed two Track laps with four
rolling wheels, smooth acceleration/braking, stable hitch geometry and motion
preserved on rebuild. Live wagon selection/Drive/Stop callbacks activated horse
walk and wheel movement, then stopped at zero. Two contact shadows were inspected
in normal Game view. Track preview is in memory; the saved user lot is unchanged.
Future work-wagon variants should supply a new asset path/ID and measured
`HorseWagonDefinition`, then join `IsHorseWagon` and the library. Reuse the motion,
gait, wheels, inspector and shadow implementations.

## Covered wagon and paired horses — 2026-09-09

New ID `horse-covered-wagon-v01`, library **TWO HORSES & COVERED WAGON**.
Source: `/Users/joelinstrum/Downloads/covered+wagon+3d+model.zip`.
Authoring/master/export/inspection and full dimensional contract:
`artifacts/animals/covered-wagon/v01/README.md`.

The shared definition now supports `HorseCount` of 1 or 2 and `HorseSpacing`.
Covered uses two existing horse rigs, centered ±0.5 m around the leading root.
That root remains the drawbar midpoint; do not use the first horse's displaced
position for hitch-length calculations. Each horse uses its own actual-motion
gait, five harness lines and terrain-following contact shadow. Front steering,
rear tracking, wheel roll, easing, speed controls and persistence remain shared.
Clearance checks cover both horses' turning paths as well as the midpoint/axles.

Covered geometry: all 4,771 source polygons/UVs retained; scale 6.1; height
2.7773728 m; front axle -0.16775 m, rear axle -2.29665 m, wheelbase 2.1289 m;
horse midpoint +2.45 m, drawbar 2.61775 m, turn radius 3.75 m. Four separate wheel
pivots, steerable front frame/hitch, rigid canvas/body/driver. Resources under
`CityForgeV3/Vehicles/CoveredWagonV01`. Existing carriage/lumber definitions default
to one horse and retain their approved measurements.

Validation: all three wagons completed two Track laps, with all four wheels
moving, stable hitches, smooth braking, and state retained on rebuild. Covered
has 2 horses/10 lines/3 shadows. Both horse rigs walked in the live Game preview
and stopped at zero. Saved Track remained unchanged. Future single/two-horse
vehicles can reuse this definition/controller; no second horse rig is needed.

## Food and vegetable wagon — 2026-09-09

Source `/Users/joelinstrum/Downloads/food-wagon.zip`, one horse. New stable ID
`horse-food-wagon-v01`, library **HORSE & FOOD WAGON**, Resources
`CityForgeV3/Vehicles/FoodWagonV01/Food_Wagon_Articulated_v01`. The `Food` definition
sets height 2.301941 m, horse offset 2.02 m, front axle +0.04 m, rear axle -1.69 m,
wheelbase 1.73 m and drawbar 1.98 m. Four wheel pivots; front axle/shafts steer;
driver and produce stay rigid. Original 4,638 faces/UVs retained. Source/master,
export, measurements and validation: `artifacts/animals/food-wagon/v01/README.md`.

No shared controller changes were needed. Two Track laps passed with all four
wheels moving, stable hitch geometry, smooth braking and motion retained through
rebuild. Live Game-view Drive/Stop and horse walk passed; two road-contact shadows.
Track preview is unsaved. Reuse this same definition-and-assets workflow for
subsequent single-horse work wagons.

## Mounted horse and rider — 2026-09-09

The family now includes **MOUNTED CAVALRY**, ID `mounted-cavalry-v01`, from
`/Users/joelinstrum/Downloads/historical+cavalry+3d+model.zip`. This is a mounted
animal variant, not a wagon team. Source and full reproduction/rig contract:
`artifacts/animals/cavalry/v01/README.md`. Rigged master:
`Cavalry_Animation_Master_v01.blend`; generated build script `build-rig.py`.
Resources: `CityForgeV3/Props/Animals/CavalryV01/Cavalry_Walk_Trot_Idle_v01`.

Retains the supplied white horse and blue/gold rider. The established hoof-IK
walk/trot/idle authoring is fitted to this horse's anatomy; a new RIDER bone
inherits body movement and supplies subtle bob/sway. The rider is rigidly weighted
to that bone, keeping uniform and seated pose intact. Motion is baked into the
same clips, so rider and horse cannot drift out of phase. Total rig 25 bones;
Unity mounted height 3 m. Existing chestnut horse and wagon rigs are unchanged.

Runtime `IsHorse` includes `IsCavalry`; `HorseModelResource` chooses the source.
Height/texture/footprint are cavalry-specific. Existing HorseGaitController and
click-to-move animal commands handle movement; selection status says “Horse and
rider.” Default speed 1.06 m/s, idle after arriving, gait follows actual travel.
Do not route this asset through the wagon drawbar/axle controller.

Imported walk/trot/idle clips have zero sampled hoof/rider loop-end error.
Live 4 m command reached its destination exactly, animated all four hooves and
the seated rider, and returned to idle. Normal Game-view screenshot and QA reports
are saved with the asset. Track was previewed in memory without saving. New rider
variants still require landmark and rider-weight fitting; reuse the script/control
pattern rather than assuming a new source has identical geometry.

## Library thumbnail for every new variant

Render the completed runtime team and add its clickable library card using
[CHARACTER_THUMBNAILS.md](CHARACTER_THUMBNAILS.md). This captures the correct
horse count, harness, driver and cargo without a separate manual Blender scene.

## Mounted trapper — 2026-09-09

Source `/Users/joelinstrum/Downloads/trapper.zip`; new ID `mounted-trapper-v01`,
library **MOUNTED TRAPPER**, Resources `CityForgeV3/Props/Animals/TrapperV01`.
Reproduction: `artifacts/animals/trapper/v01/README.md`. The 25-bone cavalry
workflow is fitted to this diagonally oriented source horse; original albedo,
trapper outfit, rifle and pelts are retained. Rider is rigidly weighted to RIDER;
saddle cargo follows BODY. Idle/walk/trot are baked with small rider bob/sway.
Runtime height 3 m. `IsMountedRider` now groups cavalry and trapper, while each
keeps its own model and texture. Both use existing horse gait and ground commands.
The library thumbnail generator includes the trapper as its fourteenth entry.
