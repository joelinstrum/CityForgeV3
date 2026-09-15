# Wooden lumber barge v01

Imported the user-provided wooden+barge+3d+model.zip as a versioned native Unity prefab under Vehicles/WoodenBargeV01. Original FBX/UV/albedo/normal retained; original roughness and metallic packed for Standard shader. 1,779 triangles, normalized to 10m length, ~3.25m width, ~2.10m height, grounded and centered. Source ZIP unchanged; derivative inspection Blend in workspace artifacts/vehicles/wooden-barge/v01.

Transport → Boats → Place Wooden Lumber Barge arms placement. Existing persisted prop storage supports boat position and quarter-turn rotation. Boat-specific selection, rotation and delete controls added. After placement the tool disarms so dragging selects/moves rather than duplicates. Boats added to placement priority, preview, prop context and grid context.

Validation: Unity compilation; prefab/material/10m dimension validation; actual UI Toolkit Place button plus viewport down/up created one selected boat; 90-degree rotation survived Save As and reload. Separate Wooden Barge Review v01 lot saved; original saved Lumber Mill lot not changed. Main Unity left showing this barge review lot. Static import only: river-route movement and waterline/floating behavior are future work.

## Boat library update
Boats opens a searchable thumbnail library. Select Wooden Lumber Barge, then click the lot to place. Future boat assets are registered using individual BoatCatalog JSON entries under Resources/CityForgeV3/Boats/Catalog.
