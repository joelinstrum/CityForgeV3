# River bank color and confluence V01

Date: September 21, 2026

The brighter River Blue V02 calibration exposed two pre-existing presentation
issues: the nearly transparent broad water edge mixed blue water with brown
submerged gravel into a purple-brown band, and a tributary ended abruptly where
its water mesh was clipped against another river.

## Bank-water transition

No bank or water artwork was recolored or replaced. The water edge opacity is
raised from `0.08` to `0.28`, its geometric fade width is narrowed from `0.22`
to `0.14`, and depth blend softness is reduced from `0.56` to `0.34`. This
retains a narrow readable gravel shallows while preventing a broad substrate
color cast across major-river banks.

## Confluences

Junction ownership is now deterministic by channel width rather than river
serialization order. The widest river keeps its complete water footprint;
smaller rivers are clipped beneath it. The smaller surface receives a smooth
vertex-alpha fade over the final `6–18m`, scaled from its channel width, and
the existing water shader consumes that alpha after its normal depth and
submerged-surface calculations.

This adds no textures, materials, draw calls, runtime animation, or per-frame
work. The fade is baked into the existing river mesh during the explicit bulk
river rebuild. River routes, grid angles, flow vectors, V01 texture assets,
V02 color calibration, accepted grass, grid behavior, and ground-decal default
remain unchanged.
