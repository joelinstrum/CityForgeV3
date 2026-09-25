# Meadow light-detail experiment

Captured September 25, 2026 in an isolated Unity 6000.1.12f1 project. The open editor was not driven. The test uses the approved MacroGrassV05 texture and the existing terrain mesh with Seed 123, Relief Height 35 m, Coverage 40%, Vertical Relief Scale 1.0, and the production district camera pose. Afternoon and noon use the shared world-lighting presets. There are no clouds, trees, UI, or buildings in these controlled renders.

The **district** view is the normal LOD4 frame. The **crop** view holds the same camera angle and terrain center while reducing orthographic size to 52%, so each area of grass occupies nearly twice as many pixels. Each row varies only the indicated shader controls.

| View and light | Existing detail | Normal response only | Crest lift only | Combined |
| --- | --- | --- | --- | --- |
| District, afternoon | [PNG](district-afternoon-color.png) | [PNG](district-afternoon-normal.png) | [PNG](district-afternoon-crest.png) | [PNG](district-afternoon-combined.png) |
| Crop, afternoon | [PNG](crop-afternoon-color.png) | [PNG](crop-afternoon-normal.png) | [PNG](crop-afternoon-crest.png) | [PNG](crop-afternoon-combined.png) |
| District, noon | [PNG](district-noon-color.png) | — | — | [PNG](district-noon-combined.png) |
| Crop, noon | [PNG](crop-noon-color.png) | — | — | [PNG](crop-noon-combined.png) |

[The full meadow off switch](district-afternoon-off.png) preserves the earlier hill material to within one displayed 8-bit RGB level in a matched render. The shader uses `_MeadowDetailEnabled = 0` for that state.

## Implementation and controls

- The directional light detail comes from screen gradients of the existing world-anchored grass sample. Their world-space cross-strand component gently perturbs its lighting normal. No mesh normal, vertex height, grass artwork, or global sunlight changes.
- A separate raised, sun-facing slope mask adds a modest highlight after the existing darkest-slope lifts. Flat crests and away-facing slopes do not receive that lift.
- `_MeadowNormalStrength` defaults to **8** (range 0–20); 0 removes the new grass normal response.
- `_MeadowDetailMipScale` defaults to **0.75** (range 0.5–1); 1 restores the prior grass sampling sharpness when the meadow switch is on.
- `_MeadowCrestHighlightStrength` defaults to **0.18** (range 0–0.3); 0 removes the new highlight.
- `_MeadowDetailEnabled = 0` bypasses all meadow detail, including these additions, and restores the original texture sampling.

## Findings and limits

The first procedural normal-field prototype produced contour-like bands, especially at noon; it was discarded. The artwork-derived version shows irregular fine grass response in the closer view without those bands. Against the prior detail-only image, the combined shader changes displayed RGB by about **0.79% mean absolute** across the district in afternoon (**3.57% at the 95th percentile**) and **1.14%** in the close crop (**5.32% at the 95th percentile**). The crest effect is localized; image-wide averages understate its peak.

The terrain remains smoother than the target image. This pass tests grass lighting detail and slope highlights; it does not alter landform shape or create a new grass texture. The direct gameplay editor view remains the visual acceptance check.

The shader adds no textures, materials, renderers, or draw calls. In two eight-capture runs at 2048×1100, the bare-terrain full-off state took **8.08–8.87 ms** per capture and the combined state **9.62–9.66 ms**. Those timings include the previously existing meadow-detail cost, GPU readback, and image transfer; they do not isolate the incremental GPU cost of this change or establish dense-district frame performance. The populated-district limit remains to be checked in the Unity Profiler.

Unity's edit-mode `WorldLightingContractTests.OrdinaryWorldShadersHaveNoPrivateTimeOfDayLightingControls` passed after shader import and compilation.
