# Meadow patches V1

Ordinary district terrain retains the approved MeadowV01 base. An opt-in
MEADOW_PATCHES shader variant softly blends the existing HillsV01 crest meadow
into warmer dry patches and adds restrained richer-green variation between them.
Both canonical PNGs and their import settings are unchanged. Artwork lineage is
recorded in HILL_MEADOW.md; this change reuses that asset without a derivative.

Two deterministic fields at 110m and 28m, evaluated on existing terrain vertices,
control transitions across many 10m tiles. They use terrain-local metres, never
camera position, time, lot boundaries, or zoom-scaled texture UVs. Flat terrain's
alternate detail also uses a fixed 40m mapping. Existing hill-detail mapping is
retained. Dry blend tops out at 62%, using a cubic center boost over the original 52%
blend; patch coverage and faint edges remain effectively unchanged. Lush
modulation is deliberately restrained.
The hue study runs before the dry blend to preserve straw's warmth.

Only ConfigureMountainGroundMaterial opts ordinary district ground in; shared
shader consumers such as lot ground and painted overlays default off. Mountain
materials retain their separate contract. No road/bank ordering, geometry,
simulation, save data, or surface-cache invalidation changes. No runtime scans,
extra renderers, draw passes, textures, timers, or repaint requests are added.

Flat ground adds ONE mipmapped texture fetch per fragment (5 vs 4). Hills reuse
their existing four-sample crest composition (8 total remains 8), with additional
blend arithmetic. This avoids imposing the older hill blend's full sampling cost
on flat districts. Strength can be set to zero for a visual comparison; disable
MEADOW_PATCHES for a comparison that also removes its GPU work.

Validation: see Validation/meadow-patches-v01 and Validation/meadow-patches-v02. Short Editor timings are not
isolated GPU measurements or long-duration stability evidence.
