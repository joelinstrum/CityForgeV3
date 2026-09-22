# River banks V06 varied migration

Date: September 21, 2026

Major rivers at 100 metres and wider now select the versioned
`BanksV6Varied` family. V06 replaces the single dominant wide-bank composition
with four smoothly blended compositions and removes baked blue from the bank
and submerged-gravel artwork. Water color remains the responsibility of the
existing animated water surface.

The new resources are:

- `bank-01-neutral.png`
- `bank-02-bars.png`
- `bank-03-open.png`
- `bank-04-cobbles.png`
- `submerged-neutral.png`

The shader selects two seeded variants for each irregular physical reach and
crossfades between them. Each source uses a different repeat phase, direction
and slight longitudinal scale. Selection is deterministic and uses existing
physical-distance UV data, so district clipping and reloads do not move the
pattern. Medium and smaller rivers retain V04; V05 remains present and
unchanged.

Artwork lineage is in
`Documentation/ArtStudies/RiverBanksV06Varied/README.md`. Validation is in
`Documentation/Validation/river-banks-v06-varied/README.md`.
