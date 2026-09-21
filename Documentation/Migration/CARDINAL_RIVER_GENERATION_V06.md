# Cardinal river generation V06

Date: September 21, 2026

## Goal

Give the cardinal regional rivers a more natural cadence without weakening the
accepted direction, spacing, width, or district-center rules.

## District meanders

Generated paths now inspect every district they traverse before corner
rounding. A district that already contains at least two orthogonal turns keeps
its existing route. For a straight crossing, the generator selects the longest
forward run and inserts one compact seeded dogleg entirely inside that district.
The dogleg returns to the original centerline before crossing the district
border, adding four rounded corners and therefore exceeding the requested
minimum of two turns.

Dogleg amplitude is capped at 0.55 region units and further limited by the
available room inside both the district boundary and the river's assigned
corridor. This preserves the disjoint parallel-river spacing, west-to-east and
north-to-south flow directions, restrained lateral movement, and exact shared
geometry at district borders. Existing generated or hand-authored saved paths
are not rewritten until the user explicitly generates a new layout.

This is a procedural-code revision only. It creates no art assets or texture
derivatives, so no image prompt or processing lineage applies.

Validation is recorded in
`Documentation/Validation/cardinal-river-generation-v06/README.md`.
