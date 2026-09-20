# Forest family mix V03 prompts and lineage

Date: September 20, 2026

Tool: OpenAI built-in image generation.

The two V02 deciduous candidates and four V01 mountain/tropical summer assets
were supplied only as visual-quality and species-mix references. Each output is
a new composition. Selected tool outputs were copied without pixel processing.

| Runtime/art-study file | Reference | Tool output |
| --- | --- | --- |
| `forest-deciduous-compact-summer.png` | V02 deciduous compact summer | `exec-08cc3097-4a1f-4f7d-b4cb-0e8fffef9947.png` |
| `forest-deciduous-large-summer.png` | V02 deciduous large summer | `exec-68d10d21-b295-4450-bf16-784544d59e24.png` |
| `forest-mountain-compact-summer.png` | V01 mountain compact summer, followed by a framing-only edit | `exec-6dfd55da-56e2-4d17-9ea4-973c1294699c.png` |
| `forest-mountain-large-summer.png` | V01 mountain large summer | `exec-71a91ce0-8c21-416e-9fc8-7dd1f075dc6a.png` |
| `forest-tropical-compact-summer.png` | V01 tropical compact summer | `exec-0ac2d5cc-d508-4232-826c-91f6468fcd9a.png` |
| `forest-tropical-large-summer.png` | V01 tropical large summer | `exec-bae1bc0e-7706-4bda-bdc6-fa6812869ad3.png` |

## Exact prompts

### Deciduous compact

```text
Use case: stylized-concept
Asset type: City Forge V3 runtime forest-group billboard texture, compact deciduous summer V03
Input image: existing V02 compact deciduous summer candidate as the style/species reference; create a new composition and stronger depth treatment
Primary request: one cohesive compact grove containing exactly five trees: four temperate deciduous trees and one dark evergreen fir. The grove must read as a deep three-dimensional stand at small in-game scale.
Style/medium: high-detail realistic photographic game sprite matching the reference's natural foliage and bark
Composition/framing: square canvas, three unmistakable overlapping depth tiers, staggered root positions, varied heights and irregular spacing. Rear crowns must be partially occluded by middle crowns; a foreground deciduous crown and trunk clearly overlap the middle tier. Integrate the fir within the grove. Keep all five trees identifiable and preserve transparent padding.
Lighting/mood: neutral summer daylight with deliberately strong baked inter-tree shade. Rear trees should be approximately 25–30% darker, cooler and lower-contrast than foreground trees. Add broad feathered cast shade where crowns overlap, deep ambient occlusion in canopy interiors and around covered trunks, and clear light-versus-shade modeling on foreground crowns. Make the depth difference obvious at thumbnail scale, without harsh black patches or a fixed directional ground shadow.
Color palette: restrained natural summer greens; no fluorescent yellow-green
Constraints: genuinely transparent RGBA background; clean fine leaf edges; exactly four deciduous plus one fir; no ground plane, grass, soil, rocks, text, watermark, frame, fog, haze, glow, halo, outline, or any ground shadow. Do not align trunk bases on one straight horizontal line.
```

### Deciduous large

```text
Use case: stylized-concept
Asset type: City Forge V3 runtime forest-group billboard texture, large deciduous summer V03
Input image: existing V02 large deciduous summer candidate as the style/species reference; create a new composition and stronger depth treatment
Primary request: one cohesive broad grove containing exactly nine trees: eight temperate deciduous trees and one dark evergreen fir. The grove must read as a deep natural stand at small in-game scale.
Style/medium: high-detail realistic photographic game sprite matching the reference's natural foliage and bark
Composition/framing: square canvas, three unmistakable overlapping depth tiers, staggered root positions, varied heights and irregular spacing. Rear crowns must be partially occluded by middle crowns; multiple foreground deciduous crowns and trunks clearly overlap the middle tier. Integrate the fir within the stand. Keep all nine trees identifiable and preserve transparent padding.
Lighting/mood: neutral summer daylight with deliberately strong baked inter-tree shade. Rear trees should be approximately 25–30% darker, cooler and lower-contrast than foreground trees. Add broad feathered cast shade where crowns overlap, deep ambient occlusion in canopy interiors and around covered trunks, and clear light-versus-shade modeling on foreground crowns. Make the depth difference obvious at thumbnail scale, without harsh black patches or a fixed directional ground shadow.
Color palette: restrained natural summer greens; no fluorescent yellow-green
Constraints: genuinely transparent RGBA background; clean fine leaf edges; exactly eight deciduous plus one fir; no ground plane, grass, soil, rocks, text, watermark, frame, fog, haze, glow, halo, outline, or any ground shadow. Do not align trunk bases on one straight horizontal line.
```

### Mountain compact

```text
Use case: stylized-concept
Asset type: City Forge V3 runtime forest-group billboard texture, compact fir-and-mountain summer V03
Input image: existing V01 compact mountain summer billboard as the style/species reference; create a new composition with stronger depth
Primary request: one cohesive compact mountain grove containing exactly five trees: four varied fir/spruce conifers and one temperate deciduous tree. It must read as a deep three-dimensional stand at small in-game scale.
Style/medium: high-detail realistic photographic game sprite matching the reference's natural needles, foliage and bark
Composition/framing: square canvas, three unmistakable overlapping depth tiers, staggered root positions, varied heights and irregular spacing. Rear conifers are partially hidden by middle crowns; foreground conifers clearly overlap middle and rear tiers. Integrate the single deciduous tree naturally. Keep all five trees identifiable and preserve transparent padding.
Lighting/mood: neutral summer daylight with deliberately strong baked inter-tree shade. Rear trees should be approximately 25–30% darker, cooler and lower-contrast than foreground trees. Add broad feathered cast shade where crowns overlap, deep ambient occlusion inside conifer branches and around covered trunks, and clearly modeled light and shade on foreground crowns. Make depth obvious at thumbnail scale without harsh black patches or fixed directional ground shadow.
Color palette: restrained natural forest greens with believable conifer variation; no blue neon or fluorescent yellow-green
Constraints: genuinely transparent RGBA background; clean needle and leaf edges; exactly four conifers plus one deciduous tree; no ground plane, grass, soil, rocks, text, watermark, frame, fog, haze, glow, halo, outline, snow, or any ground shadow. Do not align trunk bases on one straight horizontal line.
```

### Mountain large

```text
Use case: stylized-concept
Asset type: City Forge V3 runtime forest-group billboard texture, large fir-and-mountain summer V03
Input image: existing V01 large mountain summer billboard as the style/species reference; create a new composition with stronger depth
Primary request: one cohesive broad mountain grove containing exactly nine trees: eight varied fir/spruce conifers and one temperate deciduous tree. It must read as a deep natural stand at small in-game scale.
Style/medium: high-detail realistic photographic game sprite matching the reference's natural needles, foliage and bark
Composition/framing: square canvas, three unmistakable overlapping depth tiers, staggered root positions, varied heights and irregular spacing. Rear conifers are partially hidden by middle crowns; multiple foreground conifers clearly overlap middle and rear tiers. Integrate the single deciduous tree naturally within the stand. Keep all nine trees identifiable and preserve transparent padding.
Lighting/mood: neutral summer daylight with deliberately strong baked inter-tree shade. Rear trees should be approximately 25–30% darker, cooler and lower-contrast than foreground trees. Add broad feathered cast shade where crowns overlap, deep ambient occlusion inside conifer branches and around covered trunks, and clearly modeled light and shade on foreground crowns. Make depth obvious at thumbnail scale without harsh black patches or fixed directional ground shadow.
Color palette: restrained natural forest greens with believable conifer variation; no blue neon or fluorescent yellow-green
Constraints: genuinely transparent RGBA background; clean needle and leaf edges; exactly eight conifers plus one deciduous tree; no ground plane, grass, soil, rocks, text, watermark, frame, fog, haze, glow, halo, outline, snow, or any ground shadow. Do not align trunk bases on one straight horizontal line.
```

### Mountain compact framing correction

The first mountain compact generation (`exec-45dec177-e32a-4710-8038-82df58bfc409.png`)
reached the top canvas edge with visible alpha. The selected output applies this
additional exact edit prompt:

```text
Use case: precise-object-edit
Asset type: City Forge V3 runtime compact mountain forest-group billboard texture
Input image: V03 compact mountain summer candidate; this is the edit target
Primary request: change only the framing so the complete five-tree grove fits comfortably inside the same square canvas. Scale the entire existing grove down slightly and/or move it downward as one intact unit to create a clean genuinely transparent margin above the tallest conifer and around every canvas edge.
Constraints: preserve exactly the same five trees, species count, tree shapes, relative positions, overlaps, colors, strong baked inter-tree shading, alpha-edge quality, and overall composition. Do not redesign, replace, add, remove, recolor, relight, crop, or rearrange any tree. Keep the background genuinely transparent RGBA. No ground plane, ground shadow, text, watermark, frame, halo, outline, fog, or new elements.
```

### Tropical compact

```text
Use case: stylized-concept
Asset type: City Forge V3 runtime forest-group billboard texture, compact tropical summer V03
Input image: existing V01 compact tropical summer billboard as the style/species reference; create a new composition with stronger depth
Primary request: one cohesive compact tropical grove containing exactly five trees: two fan palms, one date palm, and two tropical broadleaf trees. It must read as a deep three-dimensional stand at small in-game scale.
Style/medium: high-detail realistic photographic game sprite matching the reference's natural palm fronds, foliage, trunks and bark
Composition/framing: square canvas, three unmistakable overlapping depth tiers, staggered root positions, varied heights and irregular spacing. Rear palms/broadleaf crowns are partially hidden by middle crowns; foreground foliage clearly overlaps middle and rear tiers. Keep all five trees identifiable and preserve transparent padding.
Lighting/mood: neutral tropical daylight with deliberately strong baked inter-tree shade. Rear trees should be approximately 25–30% darker, cooler and lower-contrast than foreground trees. Add broad feathered cast shade where fronds and crowns overlap, deep ambient occlusion inside dense foliage and around covered trunks, and clearly modeled light and shade on foreground foliage. Make depth obvious at thumbnail scale without harsh black patches or fixed directional ground shadow.
Color palette: restrained natural tropical greens and bark browns; no fluorescent yellow-green
Constraints: genuinely transparent RGBA background; clean frond and leaf edges; exactly two fan palms, one date palm, and two tropical broadleaf trees; no ground plane, grass, soil, rocks, text, watermark, frame, fog, haze, glow, halo, outline, or any ground shadow. Do not align trunk bases on one straight horizontal line.
```

### Tropical large

```text
Use case: stylized-concept
Asset type: City Forge V3 runtime forest-group billboard texture, large tropical summer V03
Input image: existing V01 large tropical summer billboard as the style/species reference; create a new composition with stronger depth
Primary request: one cohesive broad tropical grove containing exactly nine trees: three fan palms, two date palms, and four tropical broadleaf trees. It must read as a deep natural stand at small in-game scale.
Style/medium: high-detail realistic photographic game sprite matching the reference's natural palm fronds, foliage, trunks and bark
Composition/framing: square canvas, three unmistakable overlapping depth tiers, staggered root positions, varied heights and irregular spacing. Rear palms/broadleaf crowns are partially hidden by middle crowns; several foreground trees clearly overlap middle and rear tiers. Keep all nine trees identifiable and preserve transparent padding.
Lighting/mood: neutral tropical daylight with deliberately strong baked inter-tree shade. Rear trees should be approximately 25–30% darker, cooler and lower-contrast than foreground trees. Add broad feathered cast shade where fronds and crowns overlap, deep ambient occlusion inside dense foliage and around covered trunks, and clearly modeled light and shade on foreground foliage. Make depth obvious at thumbnail scale without harsh black patches or fixed directional ground shadow.
Color palette: restrained natural tropical greens and bark browns; no fluorescent yellow-green
Constraints: genuinely transparent RGBA background; clean frond and leaf edges; exactly three fan palms, two date palms, and four tropical broadleaf trees; no ground plane, grass, soil, rocks, text, watermark, frame, fog, haze, glow, halo, outline, or any ground shadow. Do not align trunk bases on one straight horizontal line.
```
