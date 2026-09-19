# District bridge sources and derivatives — V01

The StoneV01 derivative described below is superseded by [BridgesV02](../BridgesV02/README.md). Its runtime package was removed; the source archive and this historical record remain unchanged.

September 18, 2026. Both models were supplied by Joe for City Forge. Original archives and extracted art remain unchanged outside the game repository, matching the project's existing external Blender-source policy.

| Source | Original location | SHA-256 |
| --- | --- | --- |
| Covered wooden bridge | `/Users/joelinstrum/Downloads/buildings/bridges/covered+wooden+bridge+3d+model.zip` | `c912b1f9778d4673a19bb2ab1bf779d2fe1b49485dfe6bc2a91b61376d3bf6b3` |
| Stone bridge | `/Users/joelinstrum/Downloads/buildings/bridges/stone+bridge+3d+model.zip` | `edc4d94123782898fd5cb39376e825482bea335759f803170a2ca37afe84e7b9` |

## Derived runtime packages

`Assets/CityForgeV3/Resources/CityForgeV3/Bridges/{CoveredWoodenV01,StoneV01}` contains exported mesh data, byte-preserved source albedo, and catalog images. No canonical roads, architecture, or source textures were repainted. Albedos import at 2048 pixels; catalog images at 512. Original texture files retain all their pixels.

Mesh JSON stores positions, split normals, UVs, and triangle indices. It is parsed once per style per district-world lifetime. Each placed bridge assembles one body mesh plus one approach mesh; style materials are shared. Repetition does not add one renderer per bay. Geometry and materials are explicitly released when a bridge is removed or the district is rebuilt.

### Covered timber bridge

Source FBX: `tripo_convert_439c97c5-c968-46a5-911b-ac010fb20b80.fbx`, 8,580 triangles. Original longitudinal axis is Blender world Y. Provisional scale: 30 meters/source unit. Source deck datum: Z=.1425.

- Entrance: original Y <= -.24, connector at -.24.
- Middle: Y[-.24,0] mirrored about Y=0, preserving UVs; welded center seam. Identical profiles at both ends permit repetition without openings.
- Far entrance: reflected entrance with corrected winding and positive scale.
- Separate source pier: Y[-.10,.24], Z<=.135; centered at each middle bay. Only the lower pier is fitted to the riverbed at runtime.
- Nominal middle bay: 14.4 meters. Cap profiles have 208 matching positions.
- Triangles: entrance 2,686 each; middle 4,132; pier 1,225.

External authoring/review folders: `covered-wooden-bridge-review-v01` and `covered-wooden-bridge-modular-v01`, beside the archive. The latter contains the packed Blender scene, GLB/FBX, seam report, and deck/clearance checks.

### Stone bridge

Source FBX: `tripo_convert_b7d9247b-9286-4d8b-9202-b2e7f737ceef.fbx`, 9,336 triangles. Original longitudinal axis is Blender world X. Provisional scale: 35 meters/source unit.

The original center deck has an irregular slope (about Z=.049 to .126). The derivative samples that deck, subtracts its longitudinal height profile, rotates the longitudinal axis to Y, cuts at -.30, and reflects the negative half. This produces a level, repeatable travel surface while retaining the masonry texture, arches, and parapet geometry. The far entrance is the reflected near entrance. Lowest foundation vertices extend down to the riverbed; the upper arches are not stretched vertically.

- Nominal middle bay: 21 meters.
- Triangles: entrance 2,812 each; middle 5,928.
- External authoring scene: `stone-bridge-review-v01/stone-modular.blend`.

### Span fitting and travel

Entrances retain their authored length. Runtime chooses a whole number of middle bays, then shares the residual longitudinal fit across those bays (roughly 0.8–1.3 times nominal at the supported sizes). Width and roof height are unchanged. This is not whole-model stretching. Two 10-meter road-surface ramps connect bank road centers to the bridge. Approaches use the same unlined road material and time tint as district roads.

Travel uses a smooth analytical deck/ramp surface, independent of incidental bumps in the source mesh. Pedestrian travel corridor is five meters wide; placement reserves a nine-meter-wide corridor. Bridges currently support straight cardinal and 45-degree crossings, 60–240 meters total including approaches, with at most 20% approach grade. Curved bridges and navigable-boat clearance are not implemented.

## Reproduction

`Tools/prepare_covered_bridge.py` reproduces the wooden modular authoring scene from its external review scene. `Tools/export_bridge_meshes.py` exports both runtime packages using background Blender (5.1.2 used here). Paths at the top point to the preserved local source/review directories. The exported Unity-axis conversion reverses triangle winding after swapping Y/Z. No third-party editor UI or layouts were copied.

The initial review scenes were made by importing the supplied FBXs with Blender's standard FBX importer and retaining their materials/textures. To reproduce on another machine, restore the two original archives and review scenes, then update the explicit roots in the scripts. See `Documentation/Validation/district-bridges-v01/` for runtime validation and screenshots.
