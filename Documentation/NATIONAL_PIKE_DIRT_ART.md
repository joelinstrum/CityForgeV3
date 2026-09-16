# National pike dirt artwork

Generated with the built-in image tool on 2026-09-15; no legacy artwork port. Runtime PNGs are unchanged copies of the selected generated originals in `Assets/CityForgeV3/Resources/CityForgeV3/Roads/NationalPikeDirtV1/`. The prior DirtRoadV1 artwork is preserved.

## Rendering contract

10 × 10 metre tiles, 8 metre carriageway. Straight N/S, curved N/E, T E/S/W, cross N/E/S/W. An endpoint uses the straight image with a rounded end mask. `NationalPikeDirt.cginc` supplies common port geometry and blends the straight material at tile edges; source PNG silhouettes alone are not calibrated tiles. District and lot renderers both apply this contract. Texture alpha is retained in source files. The map uses a simplified tan line because individual 10m textures are about one pixel at region scale.

## Selected source lineage and prompts

### straight

Source: `exec-bbecc0ec-11ed-4af1-b640-3bf7c654f12b.png` under `/Users/joelinstrum/.codex/generated_images/01a0a283-bfd2-71b3-b184-e664ea54ee8f/`.

Use case: stylized-concept. Asset: production Unity top-down road tile texture, square 1024x1024. A historic national pike dirt road STRAIGHT piece, running vertically from exact top edge to exact bottom edge, road centered and 88% of canvas width. Orthographic directly overhead, flat surface, warm muted tan compacted earth, fine gravel, subtle wagon wheel wear, irregular softly feathered shoulders. Truly transparent background outside road, no grass, no border, no shadows, no perspective, no text or objects. Road must extend fully through top and bottom edge with matching width and color for seamless adjacent tiles. Nearly full tile wide. Restrained realistic painterly game texture.

### corner

Source: `exec-7eeea8f1-96e7-4975-b5af-a68a20a2f64d.png` under `/Users/joelinstrum/.codex/generated_images/01a0a283-bfd2-71b3-b184-e664ea54ee8f/`.

Use case: stylized-concept. Asset: production Unity top-down road tile texture, square 1024x1024. A historic national pike dirt road 90 degree CURVED CORNER connecting centered TOP and RIGHT edges only. Smooth rounded inner and outer bend, matching road width at both exits, each exit road 80% of edge width. Orthographic directly overhead, flat surface, warm muted tan compacted earth, fine gravel, subtle wagon wheel wear, irregular softly feathered shoulders. Truly transparent background outside road, no grass, no border, no shadows, no perspective, no text or objects. Road must extend fully through every specified connected edge with matching width and color for seamless adjacent tiles. Nearly full tile wide. Restrained realistic painterly game texture.

Reference: the selected straight PNG, used for matching material appearance.

### t-junction

Source: `exec-0c6b0ecf-d00d-498a-ad22-a760f8813f63.png` under `/Users/joelinstrum/.codex/generated_images/01a0a283-bfd2-71b3-b184-e664ea54ee8f/`.

Use case: stylized-concept. Asset: production Unity top-down road tile texture, square 1024x1024. A historic national pike dirt road T JUNCTION connecting centered LEFT, RIGHT, and BOTTOM edges only. Broad continuous compacted-earth intersection, each exit road 80% of edge width. Orthographic directly overhead, flat surface, warm muted tan compacted earth, fine gravel, subtle wagon wheel wear, irregular softly feathered shoulders. Truly transparent background outside road, no grass, no border, no shadows, no perspective, no text or objects. Road must extend fully through every specified connected edge with matching width and color for seamless adjacent tiles. Nearly full tile wide. Restrained realistic painterly game texture.

Reference: the selected straight PNG, used for matching material appearance.

### four-way

Source: `exec-e1030452-a495-4ae4-8b48-fe01090971f5.png` under `/Users/joelinstrum/.codex/generated_images/01a0a283-bfd2-71b3-b184-e664ea54ee8f/`.

Use case: stylized-concept. Asset: production Unity top-down road tile texture, square 1024x1024. A historic national pike dirt road CROSS INTERSECTION connecting centered TOP, RIGHT, BOTTOM, LEFT edges. Broad continuous compacted-earth intersection, each exit road 80% of edge width. Orthographic directly overhead, flat surface, warm muted tan compacted earth, fine gravel, subtle wagon wheel wear, irregular softly feathered shoulders. Truly transparent background outside road, no grass, no border, no shadows, no perspective, no text or objects. Road must extend fully through every specified connected edge with matching width and color for seamless adjacent tiles. Nearly full tile wide. Restrained realistic painterly game texture.

Reference: the selected straight PNG, used for matching material appearance.

