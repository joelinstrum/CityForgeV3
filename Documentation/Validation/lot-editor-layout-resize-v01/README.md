# Lot Editor library and resize validation V01

This pass covers three related authoring fixes:

- The Buildings library keeps its title, use categories, subcategories, and
  Close button fixed. Only the card grid scrolls within the modal.
- Changing Lot dimensions preserves the current camera position, orthographic
  rotation, and zoom. Size-dependent ground, grid, overlay, connector, and road
  presentations refresh against the new footprint.
- Overlay categories are 1 × 1, 2 × 2, and 4 × 4. Categories larger than the
  current Lot are hidden. Camp Ground uses the 2 × 2 footprint while retaining
  its existing stable ID.

Focused automated coverage checks the scroll-view structure and containment,
2 × 2 Camp Ground geometry and persistence, 4 × 4 category rejection on a
2 × 2 Lot, centered remapping from a 2 × 2 to 4 × 4 Lot, exact camera framing
preservation, strip deletion, and existing 1 × 1 overlay behavior.

On September 19, 2026, all 12 focused EditMode tests passed in the isolated
`DistrictStartScratch` project. Unity compiled the runtime and imported the
updated stylesheet without errors. The checks include existing top-down view,
row/column deletion, sidewalk routing, stairs, rotation, and outside-edge
overlay regressions. No player Lot or district was saved.
