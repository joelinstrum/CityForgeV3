# Automatic direct-import building materials

As of 2026-09-10, catalog entries with `materialMode: embedded` (the default)
receive automatic matte preparation through `ApplyBuildingContentContract` when
the Lot Editor creates/reloads their visual instances. No per-building name,
texture filename, importer patch or manual material edit is needed.

`ImportedBuildingMaterials.Prepare` clones Unity Standard material slots onto
the shared `CityForgeV3/Experimental3DBuildingPBR` shader. A Lot reuses one
prepared copy for every occurrence of the same source material and disposes the
cache through its existing material cleanup. Original assets are never modified.
Original color, albedo texture, UV scale/offset and normal are copied intact.
Metallic is zero, smoothness is 0.1, color controls remain neutral, and ordinary
surfaces remain non-emissive. The shared shader receives the district's global
daylight indirect scale without any per-building update or realtime light.
Transparent and explicitly emissive Standard materials remain on their authored
shader so the opaque-building conversion cannot turn glass opaque or absorb a
deliberate night emitter.
Prepared materials enable GPU instancing, allowing repeated compatible meshes
such as camp tents to share a draw when Unity's renderer constraints permit it.

`ImportedBuildingTexturePreparation` responds to editor project changes and
initialization. It reads actual Standard material connections on eligible
catalog models: albedo/emission=sRGB, normal=NormalMap/linear, metallic-gloss and
occlusion=linear data. Numeric Tripo texture names are never guessed. Conflicting
roles for the same texture are left for author review. Missing/unbound maps
cannot be reconstructed by this step. Texture files themselves are unchanged.

Ordinary `Building3DPackageInstance` packages now use the same path; package
membership alone is not a lighting exception. Custom shaders, explicit `pbr`
material contracts and opaque-prop profiles retain their existing behavior.
Use `runtimeProfile: authored-materials` for deliberately authored materials or
night emission, as the Town Center, Dry Goods Store and segmented Saltbox do.
This is an explicit opt-out, not a requirement for ordinary wood/stone imports.
The runtime default is intentionally matte, so true metal architecture needs an
authored contract.

Validation menu: **City Forge → QA → Check Automatic Building Materials**.
Checks cottage source assets remain unchanged; cloned slots retain color,
texture, UVs and normals; shared neutral settings are present. Actual noon and
afternoon Game-view evidence: `artifacts/buildings/wooden-cottage/v02-shared-materials`.
The matched noon roof sample changed from RGB160/149/133 to127/115/99 with source
color/tint unchanged. This prevents the repeated default-material washout;
it does not promise identical appearance between different viewer cameras,
exposures or light rigs. Visual QA still applies.
