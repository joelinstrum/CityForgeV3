# River blue V01 validation

Date: September 20, 2026

## Asset checks

- Base water: 1254×1254, 8-bit RGB, full coverage.
- Crest atlas: 1254×1254, 8-bit RGBA, genuine transparency, approximately
  0.041 mean alpha coverage, with generated alpha preserved during RGB cleanup.
- A 2×2 repeat inspection of the base texture showed no visible boundary seam.
- Existing unversioned river textures remain unchanged.

## Runtime scope

The runtime changes only the two resource paths. The existing
`RiverWaterSurface` shader and all motion, depth, lighting and performance
parameters remain unchanged.

## Automated validation

- Unity 6000.1.12f1 import and script compilation completed without C#
  compiler errors.
- `DistrictRiverTests`: 10/10 passed, including both V01 runtime resource
  lookups and existing generation, editing, surface-sampling and flow behavior.
- Runtime assets imported with mipmaps, trilinear filtering, 8× anisotropic
  filtering and uncompressed Default-platform texture data.

The raw NUnit report is stored beside this file. Final color, crest density and
motion acceptance remains pending in the isolated `CityForge-Regions-Review`
editor.

## Controlled render comparison

An additional isolated-review test rendered the same generated river twice
through the production `RiverWaterSurface` material: first with the active V01
base/crest resources, then with the previous unversioned resources. The test
asserted that the live material held the exact textures returned by both V01
resource paths before capturing. It passed 1/1.

`active-left-previous-right.png` places the active V01 render on the left and
the previous render on the right. Their whole-frame pixel RMSE is only
0.0110705, confirming the visual change is real but too subtle. The active
render is slightly more teal, while deep-water darkening, bed transmission and
the restrained whitecap contribution suppress most of the intended blue and
crest change. `active-river-render.png` is the standalone active capture.

This comparison does not constitute artistic acceptance. The next revision
should calibrate the existing material's color/depth and crest controls while
preserving its current flow animation.
