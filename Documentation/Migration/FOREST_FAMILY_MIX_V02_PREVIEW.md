# Forest family mix V02 deciduous preview

Date: September 20, 2026

This non-destructive preview tests whether baked inter-tree shade and layered
tree placement give grouped deciduous billboards more depth without adding
runtime lights, shaders, renderers, or geometry.

Two new 1254×1254 RGBA summer textures were generated with OpenAI's built-in
image generation tool using the corresponding V01 compact and large textures as
visual-quality and species-mix references:

- `forest-deciduous-compact-summer.png`: four deciduous trees and one fir.
- `forest-deciduous-large-summer.png`: eight deciduous trees and one fir.

The V02 textures bake only broad canopy-to-canopy shade, ambient occlusion, and
foreground/middle/rear tonal separation. They contain no ground plane or baked
directional ground shadow. The selected tool outputs were copied without pixel
processing. Exact prompts, reference paths, and output IDs are preserved in
`Documentation/ArtStudies/ForestClustersFamilyMixV02/prompts.json`.

Runtime routing uses V02 only for deciduous summer artwork. Spring intentionally
shares summer artwork. Autumn and winter deciduous textures, mountain/fir
groups, tropical groups, saved flora IDs, placement density, scale, batching,
and projected ground-shadow behavior remain unchanged on V01. This narrow route
is intended for isolated Regions Review evaluation before the V02 family is
accepted or expanded.
