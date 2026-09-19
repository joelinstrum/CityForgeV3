# Meadow zoom detail — September 19

Zoom numbering here counts the closest district stop as 1 (internal LOD0).
Zoom 1 now uses the previous zoom 2 world texture size (40 * 44 / 132 =
13.3333 metres); zoom 2 uses the previous zoom 3 setting (40 metres). All
farther stops retain the 40m source repeat and enable smooth mip filtering.
Camera distances, framing, number of stops and building LODs are unchanged.

From zoom 3 (LOD2), MeadowGradients applies an isotropic minimum 1/8-source
sampling footprint. Both base/hill offset compositions and the previously
unfiltered flat-ground straw patch sample use it. This removes fine grain in
both directions at the shallow camera angle while retaining broad color fields.
The first two stops retain ordinary texture derivatives. Default smoothing is
zero for other users of the shared shader. Mountain terrain retains its separate
material contract; no source image or import setting was modified.

Validation used a separate Unity project and temporary terrain/camera fixtures:
24 renders (before/after, six stops, flat and hills), 16,641 vertices / 32,768
triangles per ground, one ground mesh and material per capture, 1280x720 output.
The six scale/filter settings passed assertions; the shader compiled and rendered
without errors. Zoom-3 flat before/after and hilly after images were inspected.
Adjacent-pixel luminance difference in the central image region fell from
0.013261 to 0.000421 for flat ground and 0.012645 to 0.000475 for hills. This is
an image-detail measurement, not a GPU/frame-time benchmark. It does not establish
dense-district performance, allocations, or long-duration stability.

Texture sample counts are unchanged (four base, five flat with patches, eight
hilly), with no new geometry, draw pass, per-frame scan, or rebuild. Render fixture
uses the district camera angle and zoom sizes, but is not a live district capture.
Changes apply to an existing ground material on the next zoom change. No player
editor was controlled, no player progress saved, and no commit/push/review sync.

MeadowBefore.shader is a scratch validation derivative of the pre-change shader,
renamed for side-by-side rendering, not a runtime or replacement source asset.
MeadowZoomCheck.cs is the isolated validation script; it is not runtime code.
