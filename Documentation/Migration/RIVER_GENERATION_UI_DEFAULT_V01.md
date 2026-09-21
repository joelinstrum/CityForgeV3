# River generation UI default V01

Date: September 21, 2026

## Problem

Regions saved before automatic river generation can have both river amount
settings set to `None`. The map-level Regenerate Rivers action passed those
values directly to the generator. It validly returned an empty list, leaving
the user with no visible change or explanation.

## Resolution

- Opening the Rivers panel with no saved selection now defaults the draft to
  one major river plus two small rivers, producing the intended three-river
  V04 layout.
- Map-level Regenerate Rivers opens that panel when the saved selection is
  empty instead of silently applying an empty layout.
- The generation callback rejects an empty selection with an explicit prompt.
- Remove Rivers remains the deliberate way to clear existing river geometry.

No river geometry, texture, lighting, terrain, flora, or saved region is
modified merely by opening the panel.

Validation is recorded in
`Documentation/Validation/river-generation-ui-default-v01/README.md`.
