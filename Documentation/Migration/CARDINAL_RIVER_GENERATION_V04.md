# Cardinal river generation V04

Date: September 21, 2026

## Goal

Turn the rounded cardinal prototype into a spaced regional layout: fixed
downstream directions, no intersecting parallel routes, a single substantially
wider major river, stronger district-center alignment, and frequent restrained
bends instead of long straight runs or large lateral swings.

## Generation contract

- Procedural rivers flow only west-to-east or north-to-south. Legacy saved flow
  selections remain readable but no longer override this layout contract.
- The total is split evenly by direction, favoring west-to-east when the count
  is odd. Three rivers therefore produce two west-to-east routes and one
  north-to-south route.
- Rivers sharing a direction receive disjoint cross-axis corridor bands. Their
  centerlines and rounded bends remain inside those bands, preventing parallel
  routes from intersecting.
- Deep-river selection now means zero or one procedural major river. Both
  legacy Few and Many values generate exactly one major, so old saves cannot
  create multiple majors.
- The major width range is 144–228 meters, exactly three times the former
  48–76 meter procedural range. Small rivers remain 14–24 meters wide.
- Small-river amounts are now two for Few and five for Many. A major plus Few
  small rivers is the intended three-river layout.

## Route shape

Each corridor selects nearby district centers and preserves those centers as
exact path points when they can be reached without leaving the corridor or
exceeding the restrained center-seeking swing. Gaps of at least four region
units receive a compact seeded dogleg. Lateral movement is capped at 1.25
region units, substantially below V02's broad cross-region steps. V03 tangent
rounding remains in use at each generated bend.

## UI and compatibility

The region Rivers panel now presents one visible major-river toggle plus two or
five small rivers. Existing hand-drawn and district-sculpted rivers remain
unchanged, and existing saved geometry is never rewritten automatically.

This is a procedural-code and UI-copy revision of V03. It introduces no new
artwork, texture prompt, or derived texture asset. RiverBlueV01 remains
unchanged.

Validation is recorded in
`Documentation/Validation/cardinal-river-generation-v04/README.md`.
