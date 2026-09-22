# Cardinal river generation V08

Date: September 21, 2026

## Goal

Retain grid-compatible river geometry while removing the remaining engineered
spacing, repeated bend cadence, mandatory district-center alignment, and
crossing full-region lines.

## Irregular route placement

Directions are now seeded rather than evenly split, while layouts containing
multiple rivers retain both west-to-east and north-to-south routes. Entry lanes
use minimum-clearance random sampling instead of equal partitions. Midpoints
between sampled entries define non-overlapping envelopes for parallel rivers,
so routes can cluster or leave broad open areas without crossing each other.
Entry and exit offsets are selected independently.

## Size-specific meanders

Turns are no longer tied to districts. Each route advances by randomized
forward reaches and makes an orthogonal lateral shift inside its envelope.
Maximum straight-run length, shift amplitude, and corner radius vary by class:

- Major rivers use 3.6–7.2-unit reaches and shifts up to 2.3 units.
- Medium rivers use 2.6–5.6-unit reaches and shifts up to 1.7 units.
- Small rivers use 1.7–4.2-unit reaches and shifts up to 1.2 units.
- Streams use 1.15–3.1-unit reaches and shifts up to 0.8 units.

This produces broad, slow major meanders and progressively more active smaller
watercourses. The former minimum-turns-per-district and district-center target
rules are removed. Long cardinal reaches remain available for bridges and
riverside construction, while existing tangent rounding softens transitions.

## Tributaries and confluences

Paths are generated largest-first. When a later watercourse meets an earlier
one, it is truncated at the first intersection and records the earlier path as
its parent. It therefore joins rather than crossing through. Small rivers and
streams have seeded chances to remove their edge-origin reach and begin inside
the region; if they join another river, the inland headwater flows to that
confluence. Generated flow orientation is now persisted separately from endpoint
geometry so a short tributary retains its intended grid direction.

The confluence check is bounded to the explicit generation action and at most
14 requested procedural paths; it does not run per frame or during ordinary
map refreshes. Building avoidance retains the existing bounded 24-layout search.

This revision changes procedural code, saved generated-path metadata, UI copy,
and tests only. It creates no art assets or texture derivatives, so no image
prompt or processing lineage applies. Existing saved paths remain readable and
are not rewritten until explicit regeneration.

Validation is recorded in
`Documentation/Validation/cardinal-river-generation-v08/README.md`.
