# Covered wagon / two-horse team v01 — 2026-09-09

Joel requested the covered wagon with two horses, reusing the accepted horse/wagon behavior.

Source archive: `/Users/joelinstrum/Downloads/covered+wagon+3d+model.zip`. Original FBX and textures are preserved under `source/`; imported source scene is `Covered_Wagon_Source_v01.blend`. `build.py` creates the separate metric derivative `Covered_Wagon_Articulated_Master_v01.blend` and exports `Covered_Wagon_Articulated_v01.fbx`. Existing horse, carriage and lumber-wagon source assets remain unchanged.

All 4,771 source faces and original UVs are retained. Final partition: body 3,474; front steering assembly 436; front wheels 174/163; rear wheels 292/232. Canvas, cargo/body and seated driver remain rigid. Four wheels have independent measured hubs; front wheels, axle and paired hitch steer together. Source surface defects and slight wheel asymmetry remain. `body-only.png`, `wheel-separation-45.png` and `partition.json` record authoring inspection.

## Dimensions and resources

Source forward -X/up +Z becomes Blender forward -Y with `(-y,x,z)*6.1`, then Unity forward +Z. Overall height 2.7773728 m; average front axle Z -0.16775 m; rear axle Z -2.29665 m; wheelbase 2.1289 m. Horse-team midpoint is 2.45 m ahead of the body, drawbar length 2.61775 m. Two horse centers are 1.0 m apart. Placement width 2.15 m, length 7.3 m; planning turn radius 3.75 m. Wagon contact shadow is 2.25 by 3.8 m, centered at Z -1.4 m; each horse has its own terrain-following footprint.

Stable prop ID `horse-covered-wagon-v01`; library label **TWO HORSES & COVERED WAGON** in the 3D Characters library. Resources are `CityForgeV3/Vehicles/CoveredWagonV01/Covered_Wagon_Articulated_v01` and `base-color`. Runtime Standard material uses original base color, tint 0.8, metallic 0, gloss 0.18. FBX importer preserves hierarchy and disables imported animation/materials/cameras/lights.

## Reusable two-horse extension

`HorseWagonDefinition.HorseCount` supports 1 or 2; `HorseSpacing` controls paired centers. The covered variant adds its own model dimensions/anchors. `CreateHorseCarriagePresentation` instantiates two existing horse rigs at local X ±spacing/2. The controller root stays at the team midpoint; the wagon follows this midpoint through its front and rear axle constraints. Both horses share the leading heading and retain spacing while turning before the front axle and wagon body. Each horse's existing gait controller measures its own actual world movement, so inner and outer turn travel drives their respective animation speed.

`SecondHorse` is optional. Each horse has two traces, two reins, and a girth (10 harness lines total). Each receives an independent road-contact footprint, plus the wagon footprint (3 total). Midpoint-based hitch error remains correct for both team sizes. Route planning and actual movement check both horses' swept center paths as well as the existing midpoint/axle paths, preventing the added horse from being omitted from clearance checks. The existing single-horse behavior is retained.

Drive/Stop, Slow 1.06 m/s, Fast 2.12 m/s, acceleration 0.55 m/s², braking 0.85 m/s², click-to-destination, wheel roll, saved speed/heading and rebuild motion continuation use the same shared implementations.

## Validation

Runtime/editor/EditMode assemblies compiled successfully. `CoveredWagonQa` exercised the carriage, lumber wagon, and covered wagon through two Track laps each. All routes accepted, 4/4 wheels moved, starts/braking were smooth, and speed/braking survived a presentation rebuild. Covered wagon maximum hitch error 0.000003576 m; maximum steering angle 22.7849 degrees; 2 horses, 10 harness lines, 3 contact shadows. Both existing single-horse wagons retained 1 horse, 5 lines, 2 shadows and passed the same checks.

Live normal docked Game view: production hit test selected the covered wagon; Drive/Stop callbacks activated both horse rigs in walk at measured spacing 0.9999983 m, with 2.705138 m wheel travel; then parked at zero. This is a callback/hit-test check, not a physical mouse-coordinate test. `game-view.jpg` records final appearance. QA reports are copied beside this README. Menu: City Forge → QA → Covered Wagon → Open Track Preview / Check Shared Driving / Preview Drive and Stop.

The preview replaces the carriage only in an in-memory copy of Track and does not save. Saved Track SHA256 remains `3ff1c169b7f5c37fd0f2c0983be2b5f67acce8fca23a6c243e076624a0f74c72`. District freight behavior is outside this lot-editor wagon template.
