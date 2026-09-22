# Cardinal river generation V07

Date: September 21, 2026

## Goal

Replace V06's visually repetitive out-and-back notches with continuous,
natural-looking regional drift while retaining the accepted cardinal angles,
direction balance, spacing, and per-district turn requirement.

## Stateful route walker

V07 builds each procedural route district by district. Every district crossing
receives one two-corner lateral shift, and the resulting cross-axis position is
carried through the border into the next district. Drift direction persists for
a seeded run before reversing: three to five districts for Major, three to four
for Medium, two to four for Small, and one to three for Stream. This creates
longer staircase-shaped S meanders rather than isolated bumps around a fixed
baseline.

Shift placement and amplitude vary per district. Maximum shift is scaled by
river class, from 0.42 region units for Stream to 0.9 for Major, and is further
bounded by the current district and the river's disjoint corridor. Approaching
a corridor edge reverses the drift. Unoccupied district centers exert a soft
18-percent attraction instead of acting as mandatory exact waypoints; occupied
centers are not targeted.

The logical route remains entirely north/south and east/west. Existing tangent
rounding softens only the transitions. Every complete district crossing retains
at least two measurable turns, parallel routes remain separated, and shared
district-border geometry remains exact. Existing saved geometry changes only
after explicit regeneration.

This revision changes procedural code only. It creates no art assets or texture
derivatives, so no image prompt or processing lineage applies.

Validation is recorded in
`Documentation/Validation/cardinal-river-generation-v07/README.md`.
