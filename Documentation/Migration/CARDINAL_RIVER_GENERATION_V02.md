# Cardinal river generation V02

Date: September 20, 2026

## Goal

Add controlled variety to the accepted cardinal-axis baseline without
introducing diagonals or curves. Automatically generated trunks now resemble
irregular stair steps: a river runs in its overall flow direction, turns
sharply across the grid, then resumes its original direction.

## Runtime behavior

- Every generated segment remains exactly north/south or east/west.
- Forward run lengths are seeded and deliberately varied so consecutive spans
  do not repeat mechanically when more than one length is available.
- Cross-axis steps choose seeded positions around a channel baseline. The
  channel returns to that baseline before leaving the map, preserving an
  unambiguous overall flow direction.
- District paths use integer 10-meter grid spans. Regional trunks use integer
  region-grid spans and retain exact clipping at district boundaries.
- Tributaries remain straight perpendicular connections to a forward trunk
  segment. Their junction point is inserted into the trunk so both paths share
  the exact coordinate.
- Width, depth, amount, direction, lot scoring, persistence, and manual/local
  editing behavior remain unchanged.

## Source lineage

V02 is a procedural-code revision of Cardinal River Generation V01. It adds no
new artwork, texture prompt, or derived asset. RiverBlueV01 and the accepted
terrain and flora assets remain unchanged.

Validation is recorded in
`Documentation/Validation/cardinal-river-generation-v02/README.md`.
