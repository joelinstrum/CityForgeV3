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
