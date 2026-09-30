# Softer hill lighting and tan summit soil

September 27, 2026. Compared the user's SimCity hill screenshot from 8:37 AM
with the City Forge "The Hills" screenshot from 1:50 PM. A sample of bright
earth-colored pixels in the two screenshots was roughly RGB (120, 105, 76)
versus (166, 103, 74): the latter was conspicuously redder. These samples
include grass, lighting, and screenshot processing, so they are a visual target,
not a texture calibration standard.

The district hill shader had multiplied the horizontal terrain normal by 3
before directional lighting. That made a smooth height-field shoulder read as
a narrow dark ridge. The multiplier is now 1.6. The height field, its 58.5 m
peak in the saved district, and the corner rounding from the previous pass
remain intact. The existing mountain soil texture is tinted toward neutral tan
only in the meadow's summit reveal; its source file and other uses are intact.

Rendered the exact saved "The Hills" district in an isolated Unity 6000.1.12f1
project at zoom levels 1, 2, 3, and 5, with the cloud layer hidden for a clear
comparison. At zoom 3 the long dark shoulder becomes wider and lighter while
the rise remains visible. In a representative slope crop, the tenth-percentile
luminance rose from 69 to 73 and the ninetieth percentile fell from 96 to 92
on an 8-bit scale. The soil openings read less orange in the render. Camera and
lighting differences prevent a direct pixel match to the SimCity screenshot;
the user's open editor was not driven.

Validation: `DistrictElevationTests` passed 15/15, and
`HillMeadowAssetsSupportContinuousTerrainBlending` passed 1/1, including its
shader error check. This change adds no textures, draw calls, terrain samples,
or per-frame CPU work.
