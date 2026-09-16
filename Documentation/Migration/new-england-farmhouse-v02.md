# New England Farmhouse V02

Source supplied by Joe: `/Users/joelinstrum/Downloads/buildings/New England Farm 2/new-england-farmhouse.zip`.

Registered as **New England Farmhouse** in **3D Buildings → Residential**, stable ID `new-england-farmhouse-v02`. Available for `founders` and `industrial` lot eras. The older hybrid NewEnglandFarmhouse1780V01 and Founders Farmhouse & Cabin remain unchanged.

## Source and presentation

- All six archive files (FBX plus five JPEGs) copied byte-for-byte to `Assets/CityForgeV3/Resources/CityForgeV3/Buildings3D/NewEnglandFarmhouseV02/Source/`. Original geometry, UVs, textures and material connections preserved. Hashes and source measurements are in the validation folder.
- Source: one mesh, 13,167 vertices and 26,380 triangles, one material. Material links identify `_0.jpg` as albedo and `_2.jpg` as the normal map; roughness/metallic source files and the unused `_1.jpg` are retained.
- Uses the existing native-3D resource provider, shared matte material preparation, normalization, selection and shadow behavior. This is a native mesh entry, not a new hybrid billboard package. No automatic mesh reduction or fabricated LOD levels.
- Runtime contract: uniform height 9m including chimney (approximately 11.2 × 12.2m footprint), pitch −90°, base yaw 270°, ground-aligned pivot through shared normalization. Initial 90° yaw showed the opposite side; 270° exposes the covered entry in the default native-3D view.
- Scale checked in Unity beside the existing 9.5m Founders farmhouse/cabin: two storeys, door and roof proportions are consistent. This is visual calibration, not measured architectural survey data.
- Thumbnail rendered from the source in a disposable Blender scene. No source artwork edited; no extra lights or special material tuning added to runtime.

## Era filtering

Added optional `eraIds` to the shared building content contract and optional era arguments to both catalog views. The Lot Editor passes its current lot era. Existing entries with omitted/empty eras remain unrestricted, and direct ID lookup still resolves placed buildings regardless of the lot's current era. No existing save schema migration or building removal.

## Validation

Seven Unity checks passed: four era-filter cases, legacy unrestricted behavior, imported model/thumbnail/material references, and runtime normalization/ground contact plus camera preservation. Repeated after correcting default yaw. Actual windowed Game view inspected with both buildings, roof/porch/chimney visible and source textures intact; the new farmhouse is on the right of the comparison screenshot.

Isolated Regions Review contains the matching import and is left in the unsaved comparison lot for review. No user progress saves were written and the main Unity project was not modified. No dense-instance performance or authored-LOD claim is made. Changes are uncommitted.
