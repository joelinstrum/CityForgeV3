# Automatic direct-import building materials

As of 2026-09-10, catalog entries with `materialMode: embedded` (the default)
receive automatic matte preparation through `ApplyBuildingContentContract` when
the Lot Editor creates/reloads their visual instances. No per-building name,
texture filename, importer patch or manual material edit is needed.

`ImportedBuildingMaterials.Prepare` clones Unity Standard material slots,
sharing a clone between slots that originally shared a material. The world owns
and disposes these copies with its existing material cleanup. Original assets
are never modified. Original color, albedo texture, UV scale/offset, normal and
emission are copied intact. Metallic=0, metallic/gloss packed-map keyword off,
smoothness0.1, specular highlights off, glossy environment reflections off.
Disabling those white reflection contributions is the approved weathered
wood/stone default; adding saturation or painting a tint is not the fix.

`ImportedBuildingTexturePreparation` responds to editor project changes and
initialization. It reads actual Standard material connections on eligible
catalog models: albedo/emission=sRGB, normal=NormalMap/linear, metallic-gloss and
occlusion=linear data. Numeric Tripo texture names are never guessed. Conflicting
roles for the same texture are left for author review. Missing/unbound maps
cannot be reconstructed by this step. Texture files themselves are unchanged.

Authored `Building3DPackageInstance` packages, custom shaders, explicit `pbr`
material contracts and opaque-prop profiles retain their existing behavior.
Use `runtimeProfile: authored-materials` for a deliberately authored reflective
material contract. This is an explicit opt-out, not a requirement for ordinary
wood/stone imports. The runtime default is intentionally matte, so true metal
architecture needs an authored contract.

Validation menu: **City Forge → QA → Check Automatic Building Materials**.
Checks cottage source assets remain unchanged; cloned slots retain color,
texture/UV/normal/emission; matte settings/keywords are present. Actual noon and
afternoon Game-view evidence: `artifacts/buildings/wooden-cottage/v02-shared-materials`.
The matched noon roof sample changed from RGB160/149/133 to127/115/99 with source
color/tint unchanged. This prevents the repeated default-material washout;
it does not promise identical appearance between different viewer cameras,
exposures or light rigs. Visual QA still applies.
