# Ford Model T v01 source lineage

- Source: `/Users/joelinstrum/Downloads/3d models/Meshy_AI_Vintage_Ford_Model_T_0802170137_texture_fbx`
- Source type: Meshy-generated textured FBX with external PBR maps.
- Source status: read-only; no downloaded file was modified.
- Derivative: `Authoring/Vehicles/FordModelT/v01_optimized/CF_Vehicle_FordModelT_v01.blend`
- Builder: `Authoring/Vehicles/FordModelT/v01_optimized/build_model_t.py`
- Review image: `Authoring/Vehicles/FordModelT/v01_optimized/model-t-v01-preview.png`
- Geometry report: `Authoring/Vehicles/FordModelT/v01_optimized/model-t-v01-report.json`

The source contained approximately 1.26 million triangles. The derivative
provides 30.6K, 11.1K, and 2.9K triangle levels of detail, animation-ready
wheel pivots, steering parents, corrected metric scale, and an explicit PBR
material. Version 01 is black only and deliberately retains its visible driver.
Unity pilot publication is now complete at
`Assets/Resources/CityForgeV3/Vehicles/FordModelT/CF_Vehicle_FordModelT_LOD0_v01.fbx`.
The runtime presentation replaces the former green cube traveler without
changing circulation simulation. Unity preserves the FBX import correction,
adds route heading, grounds the visual, and spins all four wheels from distance
traveled. QA evidence is stored in `QA/LotEditorV46/`; the complete EditMode
suite passes 115/115.

V47 promotes the pilot from a first-segment ping-pong demonstration to a
continuous graph-derived loop. `VehicleRoute` extracts the closed cycle,
offsets it into a right-hand lane, rounds corners, and samples position,
heading, and steering by real distance. Boundary branches remain excluded until
the traffic manager owns spawning and despawning. The V47 EditMode suite passes
116/116, with four route-position renders in `QA/LotEditorV47/`.
