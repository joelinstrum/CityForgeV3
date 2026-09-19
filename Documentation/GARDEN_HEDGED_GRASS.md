# Hedge-bordered Natural Grass

The Garden library now includes five new `PlacedProp` choices. The five original Natural Grass patches remain available and retain their saved IDs.

| Piece | Saved ID | Footprint |
| --- | --- | --- |
| Short rectangle | `natural-grass-hedged-short-rectangle-v01` | 4 × 2 m |
| Long rectangle | `natural-grass-hedged-long-rectangle-v01` | 6 × 3 m |
| Square | `natural-grass-hedged-square-v01` | 4 × 4 m |
| Large square | `natural-grass-hedged-large-square-v01` | 6 × 6 m |
| Circle | `natural-grass-hedged-circle-v01` | 4 m diameter |

Each uses the existing Natural Grass quad at its existing five-metre texture density. A 0.38–0.40 m wide, 0.54 m tall clipped hedge covers the grass patch's muted border and frames an open lawn. Rectangles use four contiguous mesh volumes; the circle uses a 64-segment closed beveled ring. Both reuse the Georgian clipped hedge leaf texture, shared material and seasonal coloring. The flat muted rim remains underneath the foliage, with a narrow 0.03 m outer line. No new raster asset or save schema was added.

The new IDs use the existing Garden prop placement, rotation, selection, Undo and manual Save paths. Season, time-of-day and preview opacity updates reach both the grass and the hedge. Worker/labor code is unchanged.

Validation: `Validation/hedged-grass-v01/all-five.png` is an isolated render at 45° azimuth, with the five presentations on a temporary comparison plane. The fixture verified that each Garden ID resolves, each grass center uses the exact base grass texture, each perimeter uses the existing clipped-leaf texture, and mesh bounds/height match the footprints. The active Lot session was not touched and no Save was called. Focused EditMode tests are written but were not run because the Editor remained in Play mode. A physical mouse placement, selection/Undo, disk save/reload and dense performance pass remain to be checked. The prior forest 39/39 and regional 83/83 runs do not test these Garden pieces.
