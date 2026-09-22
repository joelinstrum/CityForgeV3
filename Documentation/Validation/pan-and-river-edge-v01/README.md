# Faster panning and major-river edge feather

Date: September 22, 2026

## Change

- Camera-relative edge panning is three times the preceding calibration: 24%
  of the current orthographic half-height per second instead of 8%.
- The rejected broad-transparency calibration was reverted completely. Existing
  major-river blue, shallow opacity, deep-water start, blend softness, and
  submerged opacity are restored.
- Visual water now reaches the calculated bank waterline and uses two additional
  mesh rows to feather only the final 1.2 metres. The established shallow water
  remains visible immediately inside that narrow coverage edge.

This is a shared presentation adjustment only. Navigation/collision width,
editing, persistence, textures, bank materials, and surface-cache behavior are
unchanged. The existing water mesh has two more cross-channel rows but remains
one material and one draw call. No per-frame scans, material walks, or autosaves
were added.

## Validation

The second supplied close-view screenshot was inspected as the visual reference.
It confirmed that broad low-opacity water mixed blue with the neutral dark bed,
recreating the rejected purple-brown shallow band. The 0.42 edge opacity, 0.34
deep-water start, 0.58 softness, and 0.82 near-submerged opacity are restored.
The new coverage feather is independent of those color/depth values.

Thirteen focused EditMode tests passed in the isolated fixture
`/tmp/cityforge-time-light-qa.CsqOcO`: the complete riverbank appearance suite
and the camera-relative pan-speed regression. The open City Forge V3 editor was
not driven or restarted, no player content was saved, and
`CityForge-Regions-Review` was not used.
