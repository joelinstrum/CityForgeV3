# Continuous field profile tune

## Parameters

The existing two-field rolling generator remains. Only primary field sampling
coordinates changed. `DistrictElevation` exposes these code constants:

| Parameter | Value | Effect |
| --- | ---: | --- |
| `RollingPrimaryWavelength` | `0.805` | Geometric-mean wavelength as a fraction of the district's smaller span. |
| `RollingPrimaryAnisotropy` | `1.55` | Long wavelength = span × wavelength × anisotropy; short wavelength = span × wavelength ÷ anisotropy. The long:short ratio is now 2.40:1, down from 2.81:1. |
| `RollingDomainWarpFraction` | `0.18` | Shifts the primary field laterally by `(secondary field − 0.5) × span × 0.18`, bending crests using the existing broad secondary field. Zero removes this bend. |
| `RollingHillVerticalScale` | `1.30` | Existing post-field Y multiplier; unchanged. |

`HeightMeters` and UI `VerticalReliefScale` remain independent amplitude
controls. The primary field still contributes 84% and the secondary field 16%.
No new octave, higher-frequency sample, hill influence, coverage mapping,
shader, material, lighting, or camera change was made. The seed still selects
the same deterministic field orientation and offsets.

## Review and validation

- Matched before/after afternoon and noon captures are saved for seeds 123,
  42, and default 1209 at 80 m height and 40% coverage. Seed 123 retains its
  broad connected character; the change is intentionally subtle. Seed 42's
  long ridge becomes somewhat wider and less straight. Noon still reveals
  relief under the same flatter light.
- Unity 6000.1.12f1 isolated fixture: 20 targeted EditMode terrain, surface
  cache, and region-layer tests passed, 0 failed (`editmode-tests.xml`).
  An unobstructed 80 m request still reaches 104 m of mesh relief.
- At 35 m / 70% coverage, gentle interior samples (neighbor height difference
  below 0.25 m over five metres) for seeds 123, 1209, and 42 changed from
  18,665, 15,206, 20,347 to 18,090, 16,072, 22,569 of 46,656. Maximum
  five-metre steps changed from 2.017, 1.927, 1.912 m to 1.965, 1.938,
  1.903 m respectively.
- The dense copied district (5,353 flora, one river, 263,169 height samples,
  eight builds) measured 179.11 ms before and 168.17 ms after per generation
  in matched isolated runs. Earlier runs varied, so this is a regression check
  rather than evidence of a speed improvement. GPU frame cost was not profiled.

Joe's open Unity editor was not driven; no push or merge was performed.
