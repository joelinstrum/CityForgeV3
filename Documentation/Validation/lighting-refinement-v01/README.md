# Global lighting refinement validation

Validation ran in a copy-on-write isolated copy of City Forge V3 with Unity
6000.1.12f1. The fixture constructed an in-memory scene from the real meadow,
road, river-water, flora, hybrid-building, and native Town Center assets and
shaders. It did not load or save a player Lot, district, or region.

## Contract

The world-lighting budget is now bounded at a brightest channel of 0.98. The
bound scales all channels together, preserving hue and sub-white contrast.
The five maximum camera-facing artwork illumination values are:

| Preset | Sun intensity | Artwork peak | Hybrid base exposure |
| --- | ---: | ---: | ---: |
| Morning | 0.56 | 0.968 | 1.50 |
| Noon | 0.68 | 0.980 | 1.50 |
| Afternoon | 0.60 | 0.950 | 1.50 |
| Evening | 0.14 | 0.205 | 1.00 |
| Night | 0.035 | 0.153 | 1.00 |

Hybrid daylight exposure uses a hue-preserving shoulder above 0.82. Noon's
registered directional shade opacity is 0.24 instead of 0.42. Evening/night
base tints, full-night artwork, window overlays, lanterns, and other genuine
emitters retain their existing paths.

## Results

- `world-lighting-tests.xml`: 7/7 focused contract and active-shader tests
  passed.
- `focused-lighting.xml`: 8/8 hybrid exposure, preset distinction, flora/road,
  sprite depth, and native Town Center exterior tests passed.
- `river-lighting.xml`: 10/10 river-network and district-environment regressions
  passed with the calibrated noon sun value.
- All active shaders named by `WorldLightingContractTests` loaded and reported
  supported. The isolated compile and graphics capture logs contained no C# or
  shader errors.
- `noon-before-after.png` places the previous contract on the left and the new
  contract on the right. Fixed image crops measured mean noon brightness moving
  from 0.363 to 0.271 for grass, 0.266 to 0.213 for autumn foliage, 0.283 to
  0.335 for the pale hybrid, and 0.356 to 0.398 for the colorful hybrid.
  Bright-channel saturation fell from 0.49% to 0% in the colorful hybrid crop
  and from 1.24% to 0.004% in the native Town Center crop.
- `after-all-presets.png` is ordered Morning, Noon, Afternoon, Evening, Night.
  It confirms visibly separate presets, readable pale siding, retained colorful
  facades/foliage, and dark non-emissive nighttime surfaces.

The runtime change adds two global uniform writes only at an existing world
environment transition. It adds no per-frame district scan, material walk,
draw call, mesh update, redraw, or presentation rebuild. The shader bound is a
few scalar operations in existing fragment passes; no texture sample or pass
was added. This fixture is a visual/contract check, not a long-duration dense-
district GPU benchmark.
