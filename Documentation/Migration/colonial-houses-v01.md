# Colonial Houses V01

The two supplied archives and preview images are preserved byte-for-byte in
`SourceArchives/ColonialHousesV01/`. The second archive retains its supplied
filename, `colonia-house-2.zip`. File sizes and SHA-256 hashes are in
`colonial-houses-v01-source-manifest.json`. The FBX meshes and their original
base-color and normal maps were copied unchanged to new paths under
`CityForgeV3/Buildings3D/ColonialHouseA/` and `ColonialHouseB/`. Metallic and
roughness source maps remain in the canonical archives; the game presentation
uses dielectric material settings appropriate to clapboard, stone, and brick.
No source UVs, openings, chimney geometry, or brick texture were changed.

**Colonial Eave-Front House** (`colonial-eave-front-house-v01`) has its door on
the long façade. **Colonial Gable-Front House**
(`colonial-gable-front-house-v01`) has its door on the gable end. Both are in
Residential → Colonial in the lot library and available to the district
builder. Each is a real-time FBX at nearby zoom levels, with a shared
source-derived eight-direction billboard at distant district zoom levels.
The shared Blender camera and foundation-center render contract are in
`Tools/build_colonial_houses_far_views.py`; the trimmed views retain 50 px/m.
The source dimensions were uncalibrated. Both are provisionally set to 10 m
high, giving approximate mesh footprints of 8.7 × 7.8 m and 8.3 × 8.3 m,
respectively. Unity grounds the imported mesh after scaling. The front doors
face source negative Y, matching the established schoolhouse yaw contract.

The existing distant-billboard component had been choosing an angle from the
camera's position relative to each building even for an orthographic camera.
That made two neighboring buildings show inconsistent facings. It now uses
the camera's parallel view direction for orthographic projection; perspective
cameras keep their position-based lookup. Each distant instance still does
only a constant-time direction check, with shared textures and material, and
switches only on a discrete zoom change. No district scan, repaint, or autosave
was added.

The existing PBR import path creates a material for each placed renderer when
a lot or district presentation is reconstructed. It runs on loading/rebuild,
not per frame; each source house has one roughly 16–17k-polygon mesh. The
far representation reduces each distant instance to one quad. This is an
existing cost retained by the port. Dense-district CPU, allocation, draw-call,
and long-duration measurements remain outstanding. Winter-specific far snow
art is also not authored in this initial port; the neutral view is reused.

## Lot Editor repainting

The two house catalog entries now opt into the reusable `repaintable` surface
contract. The Lot Editor offers original, five preset paint colors, and a
six-digit custom hex input for the selected instance. `PlacedBuilding3D.PaintHex`
stores that choice in the existing manually saved lot data; empty means the
source artwork. Other building entries receive no paint control unless they
opt in. There is no asset-ID condition in the editor or renderer.

The near PBR shader tints bright, low-chroma atlas pixels, preserving source
shading and leaving the dark roof, stone foundation, and brick chimney largely
unchanged. This is a procedural paint selection on the original art, not a
replacement of the canonical textures. Its color selection is intentionally
approximate on small white trim and light stone; a future asset can provide
an authored paint mask if it needs stricter material boundaries. District
zoom levels 4 and 5 use the original untinted directional billboards, so
paint is visible only on the near 3D representation. Repainting changes only
the selected instance's property blocks and save field. It does not reconstruct the lot or
district, recompose the Lot Editor, write a save, or duplicate meshes and source
textures. The inspector updates its paint controls and unsaved marker locally.
The existing
per-renderer material allocation on reconstruction remains; paint itself does
not claim a draw-call improvement. Dense-district measurements are still
needed before claiming improved 100-house frame performance.
