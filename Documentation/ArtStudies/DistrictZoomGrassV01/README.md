# District-wide grass color experiment V01

The three 2048 × 2048 textures in `Assets/CityForgeV3/Resources/CityForgeV3/Terrain/DistrictZoomGrassV01/` cover the district mesh once at player-facing Zoom 3, 4, and 5. Zoom 6 reuses the Zoom 5 image. Zoom 1 and 2 retain the existing 75 m tiled `MacroGrassV05` grass. This experiment applies only to the ordinary meadow ground material, not the separate mountain material.

Lineage: `MacroGrassV05/colonial-countryside-grass-v05.png` supplied the palette reference. `generated-source-zoom-5.png` was generated as broad, flat albedo for the entire district. Zoom 4 was generated from that map and the grass reference; Zoom 3 was generated from Zoom 4 and the grass reference. These 1254 px canonical generated files are preserved here. The runtime textures are resized 2048 px derivatives, adjusted to a common mean RGB color near (96, 120, 52), and imported with mipmaps and compression. The original grass asset is untouched.

The `MeadowGroundSurface` shader samples these maps with the terrain's 0–1 UVs. It bypasses the tiled grass's extra far-view grain, filtering, and brightness adjustment for these maps. Terrain mesh normals, world lighting, and shadows still determine hill lighting; the artwork contains no intentional hill shading. The controller's **Use district-wide grass at Zoom 3–5** field switches the experiment off, restoring the tiled grass at every zoom.

The first pass switches images at discrete zoom stops. It does not crossfade, so changes in map composition may be visible while zooming. Hosted lot ground receivers keep their original grass material and may show color boundaries at the farther zooms. These are visual review points before adopting this approach.
