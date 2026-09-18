# House repainting V01 — September 18, 2026

The isolated Regions Review project compiled the repaintable near and far
shaders. Focused EditMode tests passed 4/4 in `editmode-tests.xml`, including
two identical houses with independent paint, invalid hex rejection, and paint
surviving an in-memory lot reload. The paint edit did not request a full Lot
Editor refresh. This review did not write a lot, district,
or region save.

The live Lot Editor exposed paint presets and a custom hex field on the
selected colonial house. `near-blue-sage.png` shows the eave-front house in
blue and the gable-front house in sage. Roofs, windows, brick chimneys, and
stone bases remain recognizable. `far-original.png` shows the same two houses
using their original, untinted directional artwork at district zoom levels 4
and 5. The cyan marks in the captures are the existing selection outline.

Paint application is local to the selected building. The source atlases and
directional art remain untouched. The bright-neutral paint heuristic can
include some light trim and omit deeply shadowed siding. Dense-district CPU,
allocation, draw-call, and frame-time measurements remain outstanding; this
short visual review does not establish 100-house performance or long-duration
stability. Testy, District 9 was restored from its latest manual save after
the visual review.
