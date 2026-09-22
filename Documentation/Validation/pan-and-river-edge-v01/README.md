# Faster panning and major-river edge feather

Date: September 22, 2026

## Change

- Camera-relative edge panning is three times the preceding calibration: 24%
  of the current orthographic half-height per second instead of 8%.
- The rejected broad-transparency calibration was reverted completely. Existing
  major-river blue, shallow opacity, deep-water start, blend softness, and
  submerged opacity are restored.
- Visual water now reaches the calculated bank waterline and applies a smooth
  world-scaled opacity gradient across the final 6 metres of a major river
  (1.5 metres for smaller rivers). The established shallow water remains visible
  immediately inside that controlled edge gradient.

This is a shared presentation adjustment only. Navigation/collision width,
editing, persistence, textures, bank materials, and surface-cache behavior are
unchanged. The existing water mesh remains one material and one draw call. The
two shader uniforms are assigned once when the river material is built; no
per-frame scans, material walks, or autosaves were added.

## Validation

The second supplied close-view screenshot was inspected as the visual reference.
It confirmed that broad low-opacity water mixed blue with the neutral dark bed,
recreating the rejected purple-brown shallow band. The 0.42 edge opacity, 0.34
deep-water start, 0.58 softness, and 0.82 near-submerged opacity are restored.
The new coverage feather is independent of those color/depth values.
Close-view follow-up showed the initial 1.2-metre feather still read as a hard
line. The current shader uses a smoothstep over 6 physical metres rather than a
normalized fraction, keeping the gradient visible without scaling it into the
large discolored band produced by the rejected experiment.

Thirteen focused EditMode tests passed in the isolated fixture
`/tmp/cityforge-time-light-qa.CsqOcO`: the complete riverbank appearance suite
and the camera-relative pan-speed regression. The open City Forge V3 editor was
not driven or restarted, no player content was saved, and
`CityForge-Regions-Review` was not used.
