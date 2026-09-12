# Food and vegetable wagon v01 — 2026-09-09

Requested source: `/Users/joelinstrum/Downloads/food-wagon.zip`, single horse.
Original FBX/textures are retained under `source/`; source scene is `Food_Wagon_Source_v01.blend`. `build.py` creates the separate metric derivative `Food_Wagon_Articulated_Master_v01.blend` and export `Food_Wagon_Articulated_v01.fbx`. Earlier wagon and horse assets remain unchanged.

All 4,638 polygons and original UVs are retained: body 3,498; front steering assembly 306; front wheels 183/174; rear wheels 251/226. Driver, produce, crates, barrel and sacks stay on the body. The higher front geometry including the driver's hands stays rigid; the lower shafts/axle steer with the front wheels. Four wheel meshes have independent measured hubs. Source surface defects and slight asymmetry remain. `wheel-separation-45.png` and `partition.json` record authoring inspection.

Raw forward -X/up +Z is converted with `(-y,x,z)*5.0` to Blender forward -Y, then Unity +Z. Height 2.301941 m. Front axle average Z +0.04 m, rear axle -1.69 m, wheelbase 1.73 m; horse offset 2.02 m, drawbar 1.98 m. Placement width 2.05 m, length 6.5 m. Wagon shadow footprint 2.15 by 3.2 m centered at Z -0.95 m. Horse reuses the accepted existing rig and footprint.

Resource directory `CityForgeV3/Vehicles/FoodWagonV01`, FBX `Food_Wagon_Articulated_v01`, texture `base-color`. Runtime Standard material uses original base color, tint 0.8, metallic 0, gloss 0.18. FBX importer preserves hierarchy and disables imported materials/animation/cameras/lights.

Stable ID `horse-food-wagon-v01`; **HORSE & FOOD WAGON** in the 3D Characters library beside the other wagons. Only per-model definition, membership, library entry and resources are added. Motion, horse gait, wheel controller, harness, selection/Drive/Stop, Slow/Fast, acceleration/braking, saved pose/speed and rebuild continuity reuse the shared implementation without changes.

Validation: runtime/editor code imported and compiled successfully. The food wagon completed two Track laps; four wheels moved; maximum hitch error 0.000003576 m, maximum front/body steering 20.08119 degrees. First 50 ms speed 0.0275 m/s; Fast cruise 2.12 m/s; braking after one second 1.27 m/s; speed/braking preserved through rebuild; parked stable at zero; one horse, five harness lines, two contact shadows. Live production hit-test and Drive/Stop callbacks verified horse walk, rolling wheels and a complete stop in the normal docked Game view. This is callback/hit-test verification, not physical mouse-coordinate validation. Reports and `game-view.jpg` are retained here.

QA: City Forge → QA → Food Wagon → Open Track Preview / Check Shared Driving / Preview Drive and Stop. Preview uses an in-memory Track variant, without saving. Saved Track SHA256 stayed `3ff1c169b7f5c37fd0f2c0983be2b5f67acce8fca23a6c243e076624a0f74c72`. Automatic district food delivery remains outside this visual/driving template.
