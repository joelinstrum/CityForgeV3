# Natural Grass Garden patches

The Lot Editor base option formerly labeled **Default Grass** is now **Natural Grass**. Its saved ID remains `default-grass`, and it still loads `CityForgeV3/Art/Regions/default-grass-texture`; existing lots do not need a migration. The source PNG was not changed or copied.

The Garden library offers five independent, quarter-turn rotatable `PlacedProp` pieces:

| Piece | Saved ID | Footprint |
| --- | --- | --- |
| Short rectangle | `natural-grass-short-rectangle-v01` | 4 × 2 m |
| Long rectangle | `natural-grass-long-rectangle-v01` | 6 × 3 m |
| Square | `natural-grass-square-v01` | 4 × 4 m |
| Large square | `natural-grass-large-square-v01` | 6 × 6 m |
| Circle | `natural-grass-circle-v01` | 4 m diameter |

Each piece has one horizontal quad with the same grass texture and five-metre texture density as the base lot ground. The grass stays fully opaque beneath its flat 0.12 m charcoal-brown border, so the Lot base does not show through as a pale gap. The circle is radially clipped at its outer edge, with only a tiny antialias transition hidden beneath the border; it is not a round image stretched across the rectangle. The border uses one shared material and a cached combined mesh per footprint. This follows the mixed beds' muted edge color without a raised frame. Materials and border meshes are shared per footprint; selection, movement, Undo and manual Save use the existing prop route. The presentation reads the Lot season and time of day for ground tint. No worker or labor code changed. Place the patches beside brick overlays to form planted edges to walkways; overlap ordering needs a separate in-Lot review.

The pieces can be arranged beside the existing brick sidewalk overlays, flower beds and clipped hedge plots. They do not create pedestrian network segments; the brick overlays retain their current path behavior. Overlay/prop overlap order has a designed render queue but has not been validated through physical in-Lot placement.

Validation: `Validation/natural-grass-patches-v03/` shows the current full-to-border appearance in the active Lot Camera; the visible pink gap from V02 is gone. The active Lot session JSON was unchanged by capture, and no Save occurred. The V02 isolated runtime check covered all five IDs, border meshes, circular mask and session JSON round trip. Existing in-memory presentations made before the border change can retain the old borderless look until rebuilt; saved IDs need no migration. A physical mouse pass for catalog placement, selection and Undo, a disk save/reload check, and a direct brick-overlay overlap capture remain outstanding. The earlier forest 39/39 and regional 83/83 results do not cover these pieces.
