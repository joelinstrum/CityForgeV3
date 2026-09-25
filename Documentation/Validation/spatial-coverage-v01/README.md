# Spatial coverage for rolling terrain

## Coverage before and after

Before this change, every mesh sample used the same district-wide continuous
field. Coverage only changed the low value in its height transfer:

`low = lerp(0.31, 0.18, coverage)`;
`form = smoothstep((field - low) / (0.78 - low))`.

It did not restrict the field spatially. For the requested Seed 150 / 35 m /
Vertical Relief Scale 1.0 case, changing Coverage from 10% to 40% moved the
share of mesh samples above 5 m only from 67.4% to 70.6%.

Now Coverage activates a deterministic sequence of nine broad, soft-edged
regions: `active = min(9, 12 × coverage)`. Fractional activation partially
weights the next region. At 10%, one region is fully active and a second has
20% weight; at 40%, four are full and a fifth has 80% weight. The centers are
seeded and spread across the district. Each region uses an elongated,
low-frequency warped envelope with a smooth full-width fade. The envelopes
combine as `local = 1 - product(1 - weightedRegion)`, allowing neighboring
areas to overlap without a seam.

From 40% to 100%, a broad background envelope gradually fills the remaining
district: `background = smoothstep((coverage - 0.4) / 0.6)`. The final rolling
height source is `form × (background + (1 - background) × local)`. This makes
Coverage control where the continuous field participates. The calibrated peak
still uses the existing `HeightMeters × VerticalReliefScale ×
RollingHillVerticalScale` contract. A requested 35 m with 1.0 UI scale
therefore still measures 45.5 m with the unchanged 1.30 code multiplier.

The primary geometric-mean wavelength was reduced from 0.805 to 0.42 of the
district's smaller span. With anisotropy 1.35, its long and short wavelengths
are 0.567 and 0.311 of that span. The broad secondary field and slope shading
are unchanged. Rolling terrain also uses a longer district-edge fade (22% of
the smaller span) to avoid a steep rise at the border. Road and river clearance
widths are unchanged; mountain generation is unchanged.

## Seed 150 measurements

All rows use Height 35 m and UI Vertical Relief Scale 1.0. Fractions cover all
66,049 height samples, including level district borders.

| Coverage | Before: above 5 m | After: above 5 m | After: below 0.1 m | Active regions |
| --- | ---: | ---: | ---: | ---: |
| 10% | 67.4% | 21.6% | 63.7% | 1.2 |
| 25% | 69.1% | 34.5% | 44.5% | 3.0 |
| 40% | 70.6% | 45.7% | 21.9% | 4.8 |
| 70% | 73.5% | 49.9% | 18.0% | 9.0 plus background |
| 100% | 76.2% | 51.2% | 17.6% | 9.0 plus full background |

Lowlands below 5 m remain part of the rolling landscape at high Coverage; the
10% and 40% cases show the intended large change in meaningful relief area.
`before-*` and `after-*` images show both requested Coverage settings under the
same afternoon and noon light. Seed 150, height, UI scale, camera, grass, and
shader settings are identical within each comparison.

## Checks and cost

- Unity 6000.1.12f1 isolated fixture: 21 targeted EditMode tests passed,
  including the exact Seed 150 / 10% and 40% cases, deterministic regeneration,
  local road/river updates, mesh relief, and mountain region layers.
- In three 35 m / 70% districts, maximum five-metre neighbor height steps were
  1.46 m (seed 123), 1.54 m (1209), and 1.97 m (42).
- Copied dense district: 5,353 flora, one river, 263,169 height samples. Eight
  full generation runs averaged 203 ms before and 225 ms after in matched
  isolated runs. Full terrain creation or replacement triggers this work;
  local road/river edits continue to update only affected samples. This is a
  generation-time CPU measurement, not a GPU frame-time profile. Additional
  region-envelope evaluations account for the added work. The primary field
  is skipped where the envelope is exactly zero.

Joe's open Unity editor was not driven. No push or merge was performed.
