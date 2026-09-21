# River banks V04 validation

Date: September 21, 2026

## Asset contract

- `shoreline-light.png`: 2172×724, opaque 8-bit RGB.
- `open-gravel-light.png`: 2172×724, opaque 8-bit RGB.
- `submerged-gravel-light.png`: 1536×1024, opaque 8-bit RGB.
- Unity imports all three with mipmaps, trilinear filtering, 8× anisotropic
  filtering, horizontal repeat and vertical clamp.
- V1–V3 resources remain present and unchanged.

## Value validation

Whole-image grayscale mean luminance increased from `0.3612` to `0.5335` for
the natural shoreline, from `0.3555` to `0.5219` for open gravel, and from
`0.3400` to `0.6021` for submerged gravel. This verifies that V04 is materially
lighter, not merely shifted from brown toward blue.

## Repeat validation

The retained 2×2 sheets show no continuous dark or brown boundary. Generated
source edges are visually compatible but not pixel-identical: 16-pixel raw
horizontal edge RMSE is `0.1644` for the natural shoreline, `0.1984` for open
gravel and `0.1036` for submerged gravel. Production `RiverBankSurface.Strip`
crossfades an offset sample at every horizontal wrap boundary, so the runtime
contract does not expose those raw joins. Vertical wrapping is not used for the
two shoreline compositions; the submerged sampler uses a narrow uniform band
and retains the same bounded sampling method as V1–V3.

## Automated validation

Focused EditMode suites passed 23/23, including resource availability,
importer settings, shader compilation, water calibration, junction blending,
bank mesh interpolation, and a material-binding test verifying `_MainTex`,
`_EarthTex` and `_GravelTex` resolve specifically to the three BanksV4
resources. `editmode-results.xml` is the retained final report.

Interactive artistic acceptance remains a Regions Review decision; automated
tests do not establish that the in-game bank matches the approved concept.
