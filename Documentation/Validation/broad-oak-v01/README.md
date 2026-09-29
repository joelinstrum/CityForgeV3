# Broad Oak V01

September 29, 2026. Added a new deciduous tree with the persistent flora ID
`broad-oak`. It appears as **Broad Oak** in the district and lot tree libraries
and is eligible in generated temperate deciduous woodland. Existing `oak` and
`mature-oak` placements keep their IDs and artwork.

The three seasonal source images came from Joe's
`/Users/joelinstrum/Downloads/oak/` folder. Their PNG contents were copied
unchanged to `Assets/CityForgeV3/Resources/CityForgeV3/Flora/BroadOakV01/`
under ID-specific filenames; SHA-256 hashes match the supplied files. Spring
uses the summer image because no separate spring image was supplied. The
1312 × 1199 cutouts render at 80 pixels per metre, with seasonal trunk-foot
pivots measured from the visible artwork. Unity imports them as readable,
mipmapped, trilinear, alpha-aware, clamp-wrapped textures.

District oak sprites join the existing budgeted seasonal flora update path.
The new tree uses the ordinary tree selection, batch, and shadow systems;
it adds no per-frame district scan.

Validated in an isolated Unity 6000.1.12f1 project without driving the open
editor. The new seasonal art and district calendar transition test passed
1/1, `DistrictFloraBatchesTests` passed 46/46, and the three seeded climate
generation cases passed 3/3. The region generator suite passed 13/14: its
existing `RetiredTreePlacementsKeepTheirIdsAndLoadCurrentArtwork`
assertion expects `angel-oak-spanish-moss` to remap to another ID, while the
current tree mapping retains that ID. This failure is unrelated to Broad Oak.
