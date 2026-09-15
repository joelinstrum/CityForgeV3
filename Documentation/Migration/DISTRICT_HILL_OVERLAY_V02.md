# Broad hill surface swaths

Joe requested fewer, larger areas after the first integration read as thousands of small stamps. This revision replaces 14 m placement spacing / 8–12 m footprints with 110 m spacing / 110–180 m footprints and deterministic 90 m jitter. Little River Bend now has 45 placements, previously 2686.

Coverage uses a soft irregular radial boundary in patch coordinates. Fine detail samples a mirrored interior of the unchanged accepted v02 source texture at independent world coordinates; enlarging coverage does not enlarge grass detail. Interior sampling avoids repeating the source's transparent perimeter. Height/slope masks and per-vertex river masking preserve level infrastructure and water. Mesh sampling remains at no more than 5 m spacing. Ground illumination and 20% maximum opacity remain unchanged. No flora or per-tree brightness changes.

Runtime compilation passed. Actual Little River Bend disk save/reload passed: 45 patches, signature -21.01495, grounded vertices, peak 37.44 m, 770 flora, 1 river, 1 lot, collider validation. District on/off captures use the normal docked Unity Game view. Artistic acceptance remains Joe's. No broad regression suite or performance benchmark rerun for this presentation change.

Original generated texture and previous integration checkpoint f847cbc remain preserved. This revision changes only DistrictHillGroundOverlay.cs and HillGroundOverlay.shader plus documentation.

Close on/off inspection caught hidden red RGB in transparent source pixels when the first shader draft ignored alpha. Final shader multiplies source alpha as well as broad mask; reloaded and recaptured. Final close and district captures show no red flecks. Source raster was not edited.
