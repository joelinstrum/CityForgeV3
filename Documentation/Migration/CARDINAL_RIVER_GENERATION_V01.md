# Cardinal river generation V01

Date: September 20, 2026

## Goal

Establish a deliberately simple geometry baseline before introducing tuned
curves and variety. Automatically generated rivers now follow the two district
grid axes exactly: north/south or east/west.

## Runtime behavior

- District generation emits one straight, two-point channel spanning the
  district. Its cross-axis origin is snapped to the existing 10-meter district
  lattice.
- Regional generation emits straight full-span trunks. Additional streams are
  straight perpendicular tributaries running from a region boundary to a
  trunk, producing exact right-angle junctions.
- Flow direction metadata and point order are preserved for all four supported
  directions, so water animation can still run north-to-south, south-to-north,
  west-to-east, or east-to-west.
- Existing depth, width, candidate scoring, lot-intersection reporting,
  clipping, persistence, and regional amount settings remain in use.
- The generated curvature value is intentionally zero for this prototype.

## Compatibility boundary

Hand-drawn regional rivers and district-local Shape, Soften, Erase, and Redraw
geometry are unchanged. Existing saved geometry is not rewritten. This lets a
later revision add controlled bends to generated rivers without changing the
manual editing contract.

## Source lineage

This is a procedural-code revision of the existing
`DistrictRiverGenerator` and `RegionRiverGenerator`; it introduces no new art,
texture prompt, or derived texture asset. RiverBlueV01 and all accepted terrain
and flora artwork remain unchanged.

Validation is recorded in
`Documentation/Validation/cardinal-river-generation-v01/README.md`.
