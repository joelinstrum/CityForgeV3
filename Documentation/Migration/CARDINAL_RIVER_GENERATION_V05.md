# Cardinal river generation V05

Date: September 21, 2026

## Goal

Make procedural river quantities explicit and make Generate Rivers reliably
search for a layout that does not cross an existing building.

## River controls and persistence

- Major remains a zero-or-one checkbox.
- Medium rivers use a zero-through-three dropdown.
- Small rivers and streams each use a zero-through-five dropdown.
- The saved terrain settings carry a version marker and independent counts for
  medium rivers, small rivers, and streams. Older saves retain their former
  meaning: Few streams migrates to two small rivers and Many migrates to five.
- A region with no legacy selection still opens with the established default
  of one major plus two small rivers. An explicitly saved all-zero V05 choice
  remains empty and receives visible guidance if generation is requested.

## Layout behavior

Each size group alternates between west-to-east and north-to-south. An odd
remainder is assigned to whichever direction has fewer routes across the full
layout. Four small rivers therefore produce exactly two west-to-east and two
north-to-south routes.

Generated widths are 144–228 meters for Major, 48–76 for Medium, 24–36 for
Small, and 10–16 for Stream. The existing cardinal corridors, restrained bends,
rounded corners, clipping, manual-river preservation, and north-to-south flow
contract remain intact.

Districts containing buildings are no longer used as preferred center targets.
Generate Rivers evaluates as many as 24 fresh deterministic candidates before
reporting that it cannot find a building-safe layout. The prior one-candidate
behavior could look like an inert button when its first layout collided.

This revision changes procedural code and UI only. It creates no art assets or
texture derivatives, so no image prompt or processing lineage applies.

Validation is recorded in
`Documentation/Validation/cardinal-river-generation-v05/README.md`.
