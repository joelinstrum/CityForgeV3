# Cardinal river generation V09

Date: September 21, 2026

## Goal

Use the supplied hand-drawn river studies as a shape reference: preserve the
region grid's west-to-east and north-to-south organization while removing the
long, engineered straight reaches left by the former stair-step generator.

## Natural grid-oriented routes

Generated routes still have a single cardinal flow direction and remain inside
their non-overlapping placement corridors. Their centerlines now pass through
shorter, irregularly spaced macro anchors. Smooth interpolation and a small
seeded undulation are applied between every pair of anchors, so the river keeps
moving even where its broad direction follows a grid axis. The motion is
bounded by the same corridor used for spacing, preventing parallel rivers from
crossing.

Major rivers retain the broadest, slowest bends. Medium rivers, small rivers,
and streams use progressively shorter anchor spacing and finer sampling. Bend
direction persists for a random one to three reaches before reversing, which
allows broad sweeps and S-curves without a repeating zigzag cadence.

The generator remains deterministic for a saved seed. Existing saved river
paths are unchanged; the new geometry appears only after explicit generation
or regeneration. River counts, widths, flow directions, size-specific bank
styles, tributaries, confluences, exact district clipping, and hand-drawn river
data are unchanged.

This revision changes procedural geometry and tests only. It creates no texture
or image derivative, so no image prompt or processing lineage applies.

Validation is recorded in
`Documentation/Validation/cardinal-river-generation-v09/README.md`.
