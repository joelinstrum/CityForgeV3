# Georgian flower and thicket border

Garden's first piece is `georgian-flower-thicket-border-v01`, a 4 × 1.5 m rectangular flower row with thicket behind it. It remains loadable for existing placements but has been retired from the Garden catalog in favor of the complete mixed beds documented in `GARDEN_BEDS.md`. The long edge carries a low, restrained stone rim around a dark earth bed. Individual flower and shrub cutouts supply height, while one plan-view photographic layer keeps both rows legible at Lot Editor scale. Sources and prompts are under `ArtStudies/GeorgianGardenBorderV01/`; unchanged runtime copies are under `Assets/CityForgeV3/Resources/CityForgeV3/Garden/GeorgianBorderV01/`.

The Garden menu owns the catalog entry. Placement uses the existing `PlacedProp` ID, position, quarter-turn rotation, selection, drag, and manual Save path. The footprint swaps from 4 × 1.5 m to 1.5 × 4 m on odd turns. The runtime assembly shares three cached Sprites and two simple ground materials. No flora IDs or prior prop IDs are repurposed.

Spring shows reduced pale flowers and fresh thicket; summer is the full photographic baseline; autumn mutes and thins the flowers and warms the thicket; winter removes flowers and the plan-view bloom layer while showing a separate woody thicket cutout. The season is read from the current Lot preset and is never written into the prop record. This is a visual treatment, not plant growth simulation.

The assembly uses 24 upright sprite renderers, one overhead sprite, and five small ground meshes per section. A large repeated garden may need a later batching pass. It has no baked ground shadow; the crossed cards and thin edging are an approximation at oblique views. This work does not change worker/labor code or saving behavior.

Validation: `Validation/georgian-garden-border-v01/README.md`.
