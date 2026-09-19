# Complete fixed-size stone bridges

September 18, 2026. Supersedes new construction with the modular stone variants in BridgesV01–V03. The player chooses only complete supplied stone models that reach dry banks; no stone mesh is cut, repeated, flattened, or stretched to fit a river.

## Canonical sources

Both archives remain unchanged in `/Users/joelinstrum/Downloads/buildings/bridges/`:

- `stone+bridge+3d+model.zip`: SHA-256 `edc4d94123782898fd5cb39376e825482bea335759f803170a2ca37afe84e7b9`; 9,336 triangles.
- `stone+arch+bridge+3d+model-long.zip`: SHA-256 `6f55a3fe86c6b0d7d6df7a590bb898480ad4551c4925373d965ef87e0898fa5c`; 18,605 triangles.

`Tools/export_whole_bridges.py` imports the supplied FBXs, exports each complete mesh with its UVs/normals/topology, copies the source albedo byte-for-byte, and renders catalog previews. Axis conversion reverses handedness, so triangle winding reverses too. The only size calibration is one uniform scale per model: original 35 meters/source unit, long calibrated to the same 6.855 m overall width. Their fixed lengths are 34.330 m and 50.370 m. This preserves all proportions. A rigid origin translation places the first roadway endpoint at Y=0; source vertical irregularity and the different endpoint heights remain intact.

Runtime packages: `StoneOriginalV01` and `StoneLongOriginalV01` under `Assets/CityForgeV3/Resources/CityForgeV3/Bridges/`. Style IDs are `stone-original` and `stone-long`. The old `stone` ID resolves only for existing saves; it is absent from the new-build catalog. Its assets remain available to avoid losing saved crossings or travel links.

## Fixed placement

The bounded crossing check finds the wet interval, centers the complete bridge over it, and requires both model endpoints across the deck width to be dry. It expands road anchors outward as necessary for graded approaches, checks their water/occupancy footprint, and enforces the existing 20% road approach grade and 240 m total crossing limit. Each endpoint gets its own approach elevation and length; the bridge itself receives only placement translation and heading rotation. It is never refitted geometrically.

The crossing chooser omits unavailable fixed models. The catalog before a crossing is drawn lists all models with their fixed sizes. Covered wooden bridges keep their existing behavior. Separate grass approach geometry remains bridge-owned and does not mutate the heightfield or autosave. Fixed placement fields serialize with the bridge and reconstruct unchanged on reload.

See `Documentation/Validation/fixed-stone-bridges-v01/README.md` for bank-fit, topology, travel, undo/reload, and runtime checks.
