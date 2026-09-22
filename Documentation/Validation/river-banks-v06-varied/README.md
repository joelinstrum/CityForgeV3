# River banks V06 varied validation

Date: September 21, 2026

## Asset contract

- Four bank images are 2172×724 opaque 8-bit RGB.
- Neutral submerged gravel is 1536×1024 opaque 8-bit RGB.
- Unity imports all five with mipmaps, trilinear filtering, 8× anisotropic
  filtering, horizontal repeat and vertical clamp.
- V06 applies only at the existing 100-metre wide-bank threshold. Medium and
  smaller rivers retain V04, and all V05 files remain unchanged.

## Color and repeat validation

Mean HSL saturation is `0.1885`, `0.2073`, `0.1591`, and `0.2005` for the four
bank compositions. Neutral submerged gravel is `0.0308`, down from `0.1139`
for V05 submerged gravel. Its color is carried by neutral stone values rather
than a blue-water wash.

The generated horizontal edges are compatible but not pixel-identical.
Sixteen-pixel raw left/right edge RMSE is `0.1821`, `0.1763`, `0.2066`, and
`0.1598` for the four banks, and `0.1322` for submerged gravel. Production
`RiverBankSurface.Strip` crossfades an offset copy at every horizontal wrap;
the new reach-level variant transition is also a smooth crossfade. Vertical
wrapping is not used.

## Automated validation

The focused `RiverBankAppearanceTests` and `DistrictRiverTests` suites passed
25/25. Coverage verifies all V06 imports, shader compilation, V04 preservation
below major width, deterministic V06 selection and all four bank plus neutral
submerged texture bindings.

The full river regression passed 82/82 EditMode tests:

- `DistrictRiverTests`: 13/13
- `CityForgeV3.Tests.MapChromeTests`: 8/8
- `RiverBankAppearanceTests`: 12/12
- `DistrictRiverSculptTests`: 17/17
- `RegionRiverDrawingTests`: 6/6
- `RegionRiverGeneratorTests`: 13/13
- `RegionRiverNetworkTests`: 10/10
- `RegionTerrainMenuTests`: 3/3

Interactive visual acceptance remains in the isolated
`CityForge-Regions-Review` workspace after repository sync.

## Submerged-edge follow-up

The major-river bank shader now lifts the neutral bed multiplier from `0.80`
to `0.94` and broadens its cross-bank blend from `0.015–0.13` to
`-0.15–0.38`. At the shader's 16-metre cross-bank scale, the transition grows
from `1.84 m` to `8.48 m`. Major-river water uses `0.42` edge opacity, `0.34`
deep-water start, `0.58` depth softness and `0.82` near-submerged opacity so
blue water progressively dominates that overlap. V04 values remain unchanged
for medium rivers and streams.

Fresh validation passed 25/25 focused bank and district-river tests and 82/82
full river-regression tests with graphics enabled. Unity compilation and the
`RiverBankSurface` shader completed without errors.
