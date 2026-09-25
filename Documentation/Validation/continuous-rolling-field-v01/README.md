# Continuous rolling field

## Change

The previous rolling generator was discrete: two to five bounded elliptical
hill influences, with a second broad shoulder term only where those objects
overlapped. The new rolling source samples two continuous, rotated Perlin
fields. The primary field has a long axis of 1.35 times the district's smaller
dimension and a short axis of 0.48 times that dimension. It contributes 84% of
the field. A second broad field at 1.1 and 0.75 times the dimension contributes
16%, bending the long rises into shoulders and saddles. There are no smaller
octaves, separate hill centers, or radial falloffs.

Coverage shifts the low end of the continuous height response, increasing the
prevalence of broad relief as it rises. It does not choose a hill count or add
small features. The existing `HeightMeters`, UI `VerticalReliefScale`, and
`DistrictElevation.RollingHillVerticalScale` still control the calibrated
maximum Y independently from the field wavelength. An unobstructed 80 m
request with the existing 1.30 vertical scale still produces 104 m of relief.

Mountain generation, river and road clearances, district edge levelling, mesh
sampling, grass, and the approved hill shader and deep-shade setting were not
changed.

## Validation

- Unity 6000.1.12f1 isolated fixture: 20 EditMode tests passed, 0 failed
  (`editmode-tests.xml`), covering seeded regeneration, gentle and connected
  rolling areas, coverage prevalence, 80 m mesh bounds, mountain region layers,
  and local road/river surface updates.
- At 35 m / 70% coverage, seeds 123, 1209, and 42 retained 18,665, 15,206,
  and 20,347 gentle interior samples of 46,656 each (five-metre neighbor
  difference below 0.25 m). Their largest neighbor steps were 2.017, 1.927,
  and 1.912 m. The field stayed more than 1 m above baseline at 46,572,
  46,656, and 33,664 of those samples, respectively, so the shapes do not
  repeatedly drop to zero between rises.
- `before-seed123-*.png` and `after-seed123-*.png` use the same seeded 80 m /
  40% district, oblique camera, grass, and light presets. Additional after
  views show the default seed 1209 and seed 42.
- A copied dense district with 5,353 flora, one river, and 263,169 height
  samples measured 192.45 ms per generation before and 200.32 ms after over
  eight runs; retained memory measured 1,273,856 versus 1,246,720 bytes.
  This is a generation-time check, not a GPU frame-time profile.

Joe's open Unity editor was not driven. No unrelated assets were edited, and
no push or merge was performed.
