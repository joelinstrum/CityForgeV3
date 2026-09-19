# Supplied cobblestone and stone garden fountain

Joel's `/Users/joelinstrum/Downloads/textures/brick/cobblestone-texture.png`
replaces the rejected generated dark cobblestone in Lot Base and Overlays. The
source PNG is copied byte-for-byte to
`Assets/CityForgeV3/Resources/CityForgeV3/LotTextures/CobblestoneSuppliedV01/cobblestone-texture.png`
(SHA-256 `bcd68c2bc5bd79aea48dc259097ec98da9ba4fe66fa6e47c620efdaabedf72af`).
Unity preserves its full 1250 × 1250 resolution. Both choices display
**Cobblestone** and share the same resource. The saved ID `dark-cobblestone-v01`
is retained so earlier placements update without migration. A 10 m base repeat
matches a 10 × 10 m overlay; existing base boundary and overlay extension rules
apply. The rejected generated source remains in ArtStudies for lineage but is no
longer a runtime asset.

Joel's `stone+fountain+3d+model.zip` (SHA-256
`4ccfb5879c0cbe09b83d7a5d075b98b75c09a3a9f232d7a598d979930521b617`)
contains an FBX and five texture maps. They are copied unchanged into the
versioned `Resources/CityForgeV3/Garden/StoneFountainV01/Source/` directory.
The new Garden library **Stone Fountain** option uses saved prop ID
`stone-garden-fountain-v01`. Its 3D presentation binds the supplied albedo,
scales the circular basin to 3 m diameter, centers it and sets its foot on the
lot ground. The original model has 2,420 vertices and 4,836 faces; its Blender
source bounds are about 0.877 × 0.878 × 0.981 units. It is a static decorative
fountain; no water simulation or pedestrian route was added. The ordinary prop
placement, quarter-turn, selection, Undo and manual-only Save routes apply.
Worker/labor code is untouched.

Validation: `Validation/stone-fountain-and-cobblestone-v01/`. Unity imported and
compiled both assets. An isolated renderer check confirmed the full 1250-pixel
cobblestone resource in both catalogs, the fountain mesh/albedo, a grounded 3 m
footprint, and an in-memory lot session round trip with prop rotation. The
preview uses a temporary 3D scene layer and the same Lot surface shader for the
cobblestone. Temporary objects were destroyed and no Save was called. The two
focused EditMode tests are written but not run because the Editor is in Play
mode; stopping it could disturb Joel's current test. Physical pointer placement,
selection/Undo and disk save/reload remain to verify in the normal UI. Earlier
forest 39/39 and regional 83/83 suites do not cover this work.

The fountain later gained presentation-only running water; see
`STONE_FOUNTAIN_WATER.md`. The original static-import notes above describe
the initial fountain version.
