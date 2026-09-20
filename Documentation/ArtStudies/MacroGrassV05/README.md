# Macro Grass V05 source

V05 is a deterministic color refinement of the approved V04 texture. The V04
input is retained as `v04-input.png`; no new generative edit was used, so its
grain, registration, seamless edge treatment, and 75 m composition remain
unchanged.

The production transform uses ImageMagick `-modulate 102,125,108`: 2% more
brightness, 25% more saturation, and a restrained hue move away from yellow
olive toward meadow green. Mean color moves from V04 `#626939` to V05
`#5A7134`. This is the middle of three evaluated adjustments; the strongest
candidate was rejected as too vivid for the colonial countryside palette.
