# Cardinal river generation V03

Date: September 20, 2026

## Goal

Soften the hard right-angle corners in the V02 stair-step river layout while
preserving its long cardinal runs and seeded variation.

## Runtime behavior

- Each generated 90-degree stair corner is replaced by a short quadratic
  tangent curve sampled with four centerline segments.
- The curve enters and exits parallel to its adjoining cardinal runs, so the
  river still reads as following the district grid rather than as a freeform
  diagonal channel.
- A corner consumes at most 42 percent of either neighboring segment and has a
  maximum radius of 1.8 grid cells. Curves on opposite ends of a short run
  therefore cannot overlap.
- Endpoints, seeded stair layout, long/short run variation, overall flow
  direction, district clipping, widths, depths, and tributary junctions remain
  unchanged.
- Hand-drawn regional rivers and district-local sculpting remain authored and
  do not receive automatic rounding.

## Source lineage

V03 is a procedural geometry revision of Cardinal River Generation V02. It
adds the shared `RiverPathGeometry.RoundOrthogonalCorners` helper and introduces
no artwork, texture prompt, or derived asset. RiverBlueV01 remains unchanged.

Validation is recorded in
`Documentation/Validation/cardinal-river-generation-v03/README.md`.
