# Forest family mix V03 summer preview

Date: September 20, 2026

V03 expands the grouped-tree depth study to all six family/footprint summer
compositions and deliberately strengthens baked inter-tree shading. The new
1254×1254 RGBA assets live at
`Assets/CityForgeV3/Resources/CityForgeV3/Flora/ForestClustersFamilyMixV03/`.

Compact billboards retain five depicted trees and large billboards retain nine:

- Deciduous: four/eight deciduous trees plus one fir.
- Fir & Mountain: four/eight fir or spruce trees plus one deciduous tree.
- Tropical compact: two fan palms, one date palm, two broadleaf trees.
- Tropical large: three fan palms, two date palms, four broadleaf trees.

The compositions use three overlapping depth tiers, staggered roots, rear trees
approximately 25–30% darker/cooler than the foreground, feathered crown-to-crown
shade, and stronger canopy/trunk ambient occlusion. They contain no ground plane
or baked directional ground shadow. Runtime renderer counts, materials, shaders,
batching, placement, saved IDs, scale, and projected ground shadows are unchanged.

Summer and spring family groups route to V03. Tropical remains season-neutral,
so every tropical season resolves its V03 summer art. Deciduous and mountain
autumn/winter groups remain on V01 until matching derivatives are created from
accepted V03 silhouettes. V01 and V02 assets remain untouched for lineage and
rollback.

Exact prompts, reference paths and selected tool-output IDs are recorded in
`Documentation/ArtStudies/ForestClustersFamilyMixV03/prompts-and-lineage.md`.
