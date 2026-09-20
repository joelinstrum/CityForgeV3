# Macro Grass V03 source

`generated-source.png` is the unmodified 1254 × 1254 output from the built-in
image-generation tool. V03 replaces the flat V02 color field and the rejected
shader-generated close detail with one authored albedo. It targets 75 m × 75 m
of terrain with quiet neutral micro-grain, no orange debris clusters, and no
meter-scale procedural brightness modulation.

Production processing isolates the generated high-frequency detail from a
35-pixel blurred copy, blends it 82% toward the V02 olive base color, resizes it
to 4096 × 4096, lightly softens the resized grain, and feather-matches a
320-pixel strip along each opposite edge. V01 and V02 remain recoverable.

Built-in edit prompt:

> Use case: precise-object-edit. Asset type: seamless Unity terrain base-color texture covering about 75 m x 75 m. Edit the supplied texture while preserving its muted colonial olive-green average color. Rebuild its surface detail as very fine, low-contrast, non-directional neutral grass and soil grain at roughly 0.1-0.5 m scale. Keep broad value drift extremely faint and amorphous. Perfectly top-down orthographic, flat albedo, uniform coverage, tile-compatible edges. Remove and avoid all orange, red, bright yellow, and rust marks; cloudy blobs; liquid mottling; diagonal waves; brush strokes; streaks; bands; swirls; cellular islands; repeated clusters; recognizable plants, blades, leaves, flowers, stones, paths, objects, text, watermark, lighting, shadows, highlights, vignette, mirrored symmetry, and obvious tiling. Restrained local contrast, evenly distributed micro-detail with no clusters.
