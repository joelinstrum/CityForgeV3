# Connected rolling terrain tune

Temporary QA screenshots and Unity test-result XML were removed from the repository; the measurements and findings remain.

The existing hill count, seed sequence, center positions, primary radii,
crest profile, coverage mapping, grass, height scale (`1.30`), and light
direction are unchanged. Each existing hill now has a very broad, low shoulder
field. The shoulder contributes elevation only where two or more existing
fields overlap. This joins nearby rises through smooth saddles while retaining
flatter space away from overlaps. The peak is recalibrated to the same requested
height, so an unobstructed 80 m district still measures 104 m with the existing
vertical scale. Mountains are unchanged.

The previous hill slope lighting remains. `_RollingHillDeepShadeLift` adds a
`0.225` lift toward level-grass illumination only for the deepest slope shade;
the 3–12% midtone treatment and highlights are unchanged. No texture or
painted colour field was added.

## Validation

- Isolated Unity 6000.1.12f1 fixture: 14 targeted EditMode elevation and
  surface-cache tests passed, 0 failed (`editmode-tests.xml`). They include
  80 m mesh relief, deterministic regeneration, road/river clearances, local
  updates, and nearly flat area checks across three seeds.
- In 35 m / 70% test districts, samples below 1 m remain 16.6%, 23.4%, and
  11.3% of the interior for seeds 123, 1209, and 42, respectively. The
  district edges and river/road corridors also remain level.
- `before-*.png` and `after-*.png` render the same seeded 80 m / 40% terrain,
  oblique camera, and light presets. `deep-shade-off-afternoon.png` uses the
  final geometry with only the new deep-shade parameter set to zero.
- Comparing identical afternoon geometry with deep-shade off/on: pixels in the
  55–65 luminance band gained 4.0 on average; those in the 65–75 band gained
  2.7. The 75–85 band changed by 0.001 on average; brighter bands did not
  change. The shader compiled with no errors.
- Matched isolated dense-district generation (5,353 flora, one river, 263,169
  height samples, eight builds) measured 185.84 ms before and 192.45 ms after
  per build. Retained memory was 1,261,568 versus 1,273,856 bytes. This is
  a generation-time measurement, not a GPU frame-time profile.

Joe's open Unity editor was not driven; these are synthetic and copied-region
fixture checks. No push or merge was performed.
