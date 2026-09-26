# Seed 123 rolling meadow comparison

QA screenshots for this study were removed from the repository at Joe’s request; the measurements and findings remain.

Captured September 25, 2026 from commit `bf89d32` in an isolated Unity 6000.1.12f1 project fixture. The open City Forge editor and saved district were not used. This is a controlled terrain/material render, not a full game screenshot. It contains the generated mesh, approved grass texture, current hill shader, district camera pose and shared time-of-day lighting. Clouds, trees, buildings and UI are absent.

## Fixed setup

- District: 2 by 2 region units (1,280 by 1,280 m); Landscape Seed 123.
- Relief Height: 35 m; Coverage: 40%; Vertical Relief Scale: 1.0.
- Camera: normal district LOD4 20° pose, 1,800 m radius, production orthographic-size calculation, 2048 by 1100 render.
- Material: LOD4 distant meadow filtering 0.2, far grass brightness 0.91, grass detail mip scale 1.0, far grass noise 0 for hills; existing slope/deep-shade lifts 0.5/0.225.
- For each time preset, only `_MeadowDetailEnabled` changes between 0 and 1. Geometry, camera, texture, seed, coverage and lighting are identical within each pair.

The isolated comparison covered detail on and off at both Afternoon and Noon.

## Measurements

- Generated mesh: 66,049 vertices, minimum Y **0.000 m**, maximum Y **45.500 m**, vertical span **45.500 m**. The generator's existing 1.3× rolling-hill calibration accounts for the difference from the 35 m UI value.
- 26.0% of mesh samples are below 0.1 m; 44.9% are above 5 m. Coverage creates both calm and raised ground.
- On terrain pixels sampled every eighth pixel, enabling meadow detail changes RGB channels by **1.12% mean absolute** in afternoon and **1.11%** at noon. The 95th percentiles are **3.03%** and **2.78%**, respectively. This uses displayed 8-bit RGB values, so it describes the render rather than linear shader values.
- Afternoon versus noon with detail enabled changes those channels by **16.2% mean absolute**. Time-of-day presentation is much larger than the meadow-detail contribution in this controlled view.

## Texture filtering probe

Three afternoon captures use the same on state, mesh, grass texture, camera and light:

- Mip scale 0.75, filtering 0.2: a barely visible increase in existing grass detail.
- Mip scale 0.5, filtering 0.2: more of the existing grass detail survives at district zoom.
- Mip scale 0.5, filtering 0: stronger fine grain begins to read as speckle. This was a fixture-only probe, not a production change.

The bare terrain still reads smoother than the reference, especially at noon. The mesh contains the requested height, so increasing macro color strength is unlikely to recover the missing form. A conservative grass-detail filtering adjustment is a candidate for a separate in-game visual pass; the strongest tested filtering combination should be avoided. No production shader, grass, geometry or lighting settings were changed for this comparison.

The September 25 live screenshot does not provide its seed, height, coverage, zoom or time-of-day settings. This control uses explicit settings and should not be treated as a pixel match to that screenshot.
