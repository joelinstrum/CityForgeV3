# Macro Grass V04 source

V04 retains the natural grain from the V03 built-in image-generation result
instead of isolating and flattening it into a near-solid field. The unmodified
1254 × 1254 generator output is retained as `generated-source.png`.

Production processing reduces saturation to 85%, resizes to 4096 × 4096 with
Lanczos filtering, applies only a 0.25-pixel softening pass, and feather-matches
a 320-pixel strip along each opposite edge. Its mean color is muted olive
`#626939`, darker than V03's `#6B7849`. V04 contains the close-range detail in
the albedo, with no procedural color-noise layer.

The exact built-in image-generation prompt is recorded in the V03 source
study; V04 is a deterministic production refinement of that same output.
