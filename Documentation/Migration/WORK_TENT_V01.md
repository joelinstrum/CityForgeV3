# Work Tent V01

Source: Joe-supplied `/Users/joelinstrum/Downloads/buildings/tents/work-tent.zip`.
All six extracted files are byte-identical to the archive; hashes recorded in
Validation/work-tent-v01/source-sha256.txt. No legacy project files were used.
Canonical files live in Buildings3D/WorkTentV01/Source; thumbnail is a separate
Blender render derivative. Blender inspected material links: image0 is albedo,
image2 is normal, plus explicitly named metallic/roughness maps. Normal imports
as linear NormalMap; metallic/roughness data are linear. Shared runtime matte
preparation preserves source color. No repaint or global lighting changes.

Catalog: Buildings → Industrial → Camps → Work Tent, ID `work-tent-v01`.
Uses the existing direct FBX BuildingContentCatalog path, like Wooden Cottage;
no hybrid building-package or new runtime renderer is introduced. Pitch -90,
yaw90, height3.2m including pole tips. Bounds approximately3.59×3.20×5.10m,
including guy ropes. Scale is an initial authoring choice, not a measured source
specification. 2,218 triangles; original UVs and mesh remain intact.

Isolated Unity check passed catalog lookup, source loading, albedo bindings and
metric normalization. Source and Unity renders inspected; entrance is visible
in the chosen three-quarter view. No live Lot placement, all-rotation shadow
review, dense-district performance or production LOD validation is claimed.
Existing generic building placement/rotation/serialization is used unchanged.
No player saves, commits, pushes, review sync or worker/labor edits.
