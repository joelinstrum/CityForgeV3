# Lot overlays

Lot overlays are authored surface pieces placed on the 10-meter major grid. The
overlay library groups them into 1 × 1, 2 × 2, and 4 × 4 footprint categories.
The editor hides a category whenever that footprint cannot fit inside the
current lot.

## Footprint contract

- Existing overlay definitions default to `1 × 1` cells. No save migration is
  required because saved instances continue to store their texture ID, anchor
  cell, and quarter-turn rotation.
- Larger definitions declare `FootprintWidthCells` and
  `FootprintDepthCells`. The saved anchor is the lower-left occupied cell.
- A large overlay must fit entirely within the lot. Clicking anywhere on the
  intended area snaps its anchor to a valid position; dragging does not stamp
  copies.
- Selection checks the complete occupied footprint. The selection highlight and
  rendered quad use the same dimensions.
- Removing a major row or column that intersects any part of a large overlay
  removes that complete piece.
- The existing 1 × 1 behavior remains unchanged, including painting one tile
  beyond the lot edge.

## Camp Ground 2 × 2

`camp-overlay-4x4` is retained as the stable save-compatible ID, but the piece
now occupies a single 20 × 20 meter footprint in the **2 × 2** category. Its
canonical source is
`/Users/joelinstrum/Downloads/buildings/tents/4x4 camp overlay.png` and its
SHA-256 is
`b821c7752d99f1784796d964ef7a1eb3a9649347df6495c30d6e1632ad62d37e`.
The imported PNG is byte-identical to that source.

When lot dimensions change, overlay anchors are remapped to retain their world
center and their presentations are rebuilt against the new base immediately.
Large overlays that no longer fit after shrinking are removed from the in-memory
Lot edit; persistence remains manual through the Save action.
