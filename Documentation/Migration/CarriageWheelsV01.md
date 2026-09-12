# Antique carriage v01

Source: `/Users/joelinstrum/Downloads/animals/antique+carriage+3d+model.zip`.

The original single-mesh asset is preserved under `source/`. The derivative retains all 4,461 source faces, original UVs and base-color texture. The driver, passenger body, undercarriage and shafts are static. Four rigid wheel meshes have independent axle pivots. Small disconnected inner rear-rim strips are assigned to the corresponding rotating wheel, not left on the body.

Authoring: `prepare.py` partitions the source; `export.py` scales to meters and exports. `Carriage_Wheel_Master_v01.blend` retains source-scale authoring; `Carriage_Game_Master_v01.blend` and `Carriage_Rolling_Wheels_v01.fbx` are the metric game derivatives. Metric scale is 5.5; total height ~2.645 m, width ~1.793 m and length including shafts ~5.401 m. The complete geometry/pivot contract is in `carriage-contract.json`.

`CarriageWheelController` derives signed wheel rotation from each axle pivot's actual traveled distance and its wheel radius. Wheels turn independently, reverse with backward travel, and stop with no motion. Placement teleports reset the movement baseline. There is no always-running animation clip and no driver/body animation. The route or future horse-team controller remains responsible for moving the carriage root.

Installed under Unity Resources `CityForgeV3/Vehicles/CarriageV01`. `ANTIQUE CARRIAGE` is available next to the horse in the 3D Characters placement library. This first version is a placeable carriage with rolling wheels; it does not yet attach horses or join district traffic. The supplied narrow outer shafts appear to fit a single horse. A two-horse derivative needs a wider harness arrangement or center pole; the original shafts were retained for this wheel-animation pass.

Unity preview: while playing, `City Forge > QA > Carriage > Open Rolling Wheels` opens a disposable lot and moves the carriage forward/backward with pauses using the normal lot camera. `Stop Preview` stops the demonstration. It does not save over a user lot. This preview uses editor-only movement; wheel animation itself is the installed runtime controller.

Validation: source and 45/90-degree wheel renders inspected in Blender; all source faces retained; Unity runtime/editor compilation passed. Game View findings are recorded separately in `game-validation.md` after inspection.
