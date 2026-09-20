# Town Center placement orientation and exterior lighting validation

Validation used a disposable copy of City Forge V3 with Unity 6000.1.12f1. The
live editor, player Lots, districts, and regions were not opened or saved.

`tests.xml` records 12/12 passing focused EditMode tests. Coverage includes the
exact camera quaternion from the player-authored Town Center whose saved octant
label is stale, all four earlier diagonal orientation mappings and legacy/top-
down fallbacks, plus the complete Town Center behavior suite. The material test
requires all five opaque exterior renderers to use the shared directional
building shader and verifies that interior, glass, and lantern materials remain
outside that change.

`noon-lighting.png` is a graphics-enabled capture made through the real
`LotWorldController` at Noon. It uses an in-memory 4 × 4 fixture and the shipped
`town-center-v01` package. The illuminated front and right elevations agree
with the building's southeast cast shadow; the former whole-building dark cast
from imported tangent normals is absent. Windows and lantern housings retain
their independent dark daytime appearance. This is a focused asset render, not
a representative dense-district performance benchmark.

The implementation adds no runtime enumeration, allocation loop, district
rebuild, or save path. Orientation is calculated once when a Lot placement is
prepared. Exterior light direction continues through the existing cached
per-building material-property update.
