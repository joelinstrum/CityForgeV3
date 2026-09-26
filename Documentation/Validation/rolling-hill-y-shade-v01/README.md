# Rolling hill Y and shading tune

Temporary QA screenshots and Unity test-result XML were removed from the repository; the measurements and findings remain.

## Scope

The existing seeded hill field, shape positions, radii, coverage, edge fades,
road/river constraints, grass texture, camera, and world light presets are
unchanged. `DistrictElevation.RollingHillVerticalScale` multiplies the sampled
hill field only after its prior height calibration. Its value is `1.30`; `1.0`
restores the prior Y relief. Existing UI `VerticalReliefScale` still multiplies
independently. Thus an unobstructed 80 m / 40% district now measures 104 m of
mesh relief. Mountains do not use this new factor.

`DistrictWorldController.RollingHillDarkSlopeLift` is `0.50` and sets the
meadow shader property of the same name. The shader compares each hill slope's
world-light response with level grass under the same shadow attenuation. It
lifts slopes at least 12% darker by half their *post-exaggeration* lighting
deficit; slopes within 3% of level, brighter slopes, and highlights receive no
lift. This offsets the darker normals caused by the 1.3x Y displacement.

## Checks

- Isolated Unity 6000.1.12f1 fixture; EditMode terrain elevation and surface
  cache tests: 14 passed, 0 failed (`editmode-tests.xml`). Tests cover mesh
  bounds, unchanged X/Z positions under relief scaling, local road and river
  updates, and flat breathing room.
- The fixture compiled the modified shader and rendered the same seeded 80 m,
  40% terrain at the same camera and afternoon/noon light presets before and
  after (`before-*.png`, `after-*.png`). No shader errors were logged.
- In the afternoon preview, the lower 10% of terrain luminance moved from
  60.7 to 65.4 (base median approximately 84), while median luminance moved
  from 83.8 to 83.1 and the 99th percentile stayed 100.7. The increased
  height also shifts projected pixels, so these are image distribution checks,
  not paired pixel measurements.

The preview is a synthetic district at the normal oblique terrain viewing
angle. Joe's open Unity editor was not driven. These renders do not establish
GPU frame cost or the appearance in every populated district.
