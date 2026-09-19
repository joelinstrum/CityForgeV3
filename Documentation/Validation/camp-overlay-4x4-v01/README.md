# Camp overlay 4 × 4 V01 validation

Historical result: this 4 × 4 footprint was superseded by the 2 × 2 Camp Ground
contract documented in `../lot-editor-layout-resize-v01/README.md`. The stable
texture ID remains unchanged for save compatibility.

- Confirm the Overlays modal exposes separate **1 × 1** and **4 × 4** buttons.
- Confirm **Camp Ground** is shown only in **4 × 4**.
- On a 4 × 4 lot, place the camp surface and verify it fills the lot as one
  piece without drag-painted duplicates.
- Click each corner of the surface and confirm the same piece is selected.
- Rotate, delete, undo, and manually save/reload through the normal player flow.
- Confirm a lot smaller than 4 × 4 rejects the camp surface.

Automated coverage checks the catalog footprint, resource import, placement,
render size and center, covered-cell selection, drag suppression, JSON round
trip, undersized-lot rejection, and major-strip deletion.

On September 19, 2026, all 8 focused EditMode tests passed in the isolated
`DistrictStartScratch` project. That run included the existing 1 × 1 boundary,
drag rotation, connected sidewalk, and stair-overlay regressions. Unity imported
the 1254 × 1254 PNG without compile errors, and the source artwork was visually
inspected at original resolution. No player Lot or district was saved.
