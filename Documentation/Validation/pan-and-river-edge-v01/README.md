# Faster panning and major-river edge feather

Date: September 22, 2026

## Change

- Camera-relative edge panning is three times the preceding calibration: 24%
  of the current orthographic half-height per second instead of 8%.
- Major-river water begins nearly clear at its outer mesh edge. Its opacity and
  blue color build across a wider, later depth ramp so neutral submerged gravel
  is visible before it gradually yields to deep water.
- Medium rivers and streams retain their existing mountain-water calibration.

This is a shared presentation calibration only. River geometry, collision,
editing, persistence, textures, bank materials, and surface-cache behavior are
unchanged. No per-frame scans, material walks, extra draw calls, or autosaves
were added.

## Validation

The supplied close-view screenshot was inspected as the visual reference. Its
hard blue boundary corresponds to the previous 0.42 opacity at the major-river
water mesh edge; the new value is 0.03, with deep-water start moved from 0.34
to 0.46, blend softness increased from 0.58 to 0.82, and shallow submerged
opacity reduced from 0.82 to 0.62.

Thirteen focused EditMode tests passed in the isolated fixture
`/tmp/cityforge-time-light-qa.CsqOcO`: the complete riverbank appearance suite
and the camera-relative pan-speed regression. The open City Forge V3 editor was
not driven or restarted, no player content was saved, and
`CityForge-Regions-Review` was not used.
