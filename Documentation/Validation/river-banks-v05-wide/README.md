# River banks V05 Wide validation

Date: September 21, 2026

## Asset and selection contract

- Both shoreline images are 2172×724 opaque 8-bit RGB.
- Submerged gravel is 1536×1024 opaque 8-bit RGB.
- Unity imports all three with mipmaps, trilinear filtering, 8× anisotropic
  filtering, horizontal repeat and vertical clamp.
- Widths below 100 metres retain V04; widths of 100 metres and above use V05
  Wide. Generated mediums stop at 76 metres and majors start at 144 metres.
- Major banks carry an eight-metre presentation-only outer shoulder. The
  shader fade ends at the shoulder edge, while physical channel width remains
  unchanged.
- V1–V04 resources remain present and unchanged.

## Color validation

Mean HSL saturation is `0.2024` for the wide natural shoreline, `0.1974` for
wide open gravel and `0.1139` for wide submerged gravel. Their V04 sources are
`0.2594`, `0.2939` and `0.2526`, respectively. The new major-river art is thus
substantially less saturated rather than merely changing blue hue. Whole-image
mean grayscale luminance is `0.4542`, `0.4379` and `0.4463`; the pale exposed
gravel remains, with no brown or black wet outline.

## Repeat validation

The retained 2×2 sheets show a coherent material progression without cyan or
brown bands. Generated horizontal edges are visually compatible but not
pixel-identical: 16-pixel raw horizontal edge RMSE is `0.1559` for the natural
shoreline, `0.1893` for open gravel and `0.0927` for submerged gravel.
Production `RiverBankSurface.Strip` crossfades an offset sample at every
horizontal wrap boundary, so runtime does not expose those raw joins. Vertical
wrapping is not used for shoreline compositions.

## Automated validation

The focused `RiverBankAppearanceTests` and `DistrictRiverTests` suites passed
25/25. The retained report is `editmode-results.xml`. Coverage includes V04
preservation at medium width, V05 selection at major width, imports, shader
compilation and the actual `_MainTex`, `_EarthTex` and `_GravelTex` bindings.

Interactive artistic acceptance remains a Regions Review decision; automated
tests do not establish final in-game appearance.
