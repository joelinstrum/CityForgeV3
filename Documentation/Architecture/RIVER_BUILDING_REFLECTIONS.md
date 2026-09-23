# Opt-in river building reflections

The first pilot reflects the native 3D lumber mill (`lumber-mill-v01`) in the
district river. It is deliberately not a district-wide planar reflection.

- Enabled at district LOD0–LOD2 only; LOD3–LOD5 perform no reflection capture.
- Candidate roots are registered when a hosted lot is added, indexed in 64 m
  spatial buckets, updated when that lot moves, and removed when it is deleted.
- The pre-render callback queries only the close camera area. It chooses at
  most one visible mill whose footprint reaches nearby river water. An
  unfinished/invisible building is skipped.
- Lot General exposes a River Reflections switch on every lot. Existing saved
  lots default to on; turning it off is stored only by an explicit lot Save.
  District candidate registration respects the lot setting on load/add.
- One isolated 256 px-high capture (width 256–512 px) renders the selected
  mill at most once every 0.2 seconds. The river shader projects and lightly
  distorts it only on actual water within 45 m of that mill. Its projector is
  anchored to the mill and water level, so camera panning cannot slide the
  image. The larger reflection extends toward the river center/viewer, not
  sideways along the bank. Its sides and far end fade into the water, while
  the water's existing opacity/depth/bank gradient remains authoritative.
- Direction uses the nearby river tangent and two local cross-channel samples
  when a capture is selected, never a district-wide lookup. The capture
  precomputes a four-value world-space UV basis instead of projecting three
  matrices per water pixel. Resolution and five-per-second limit are unchanged;
  the wider local shader footprint still shades more water pixels.
- No second district scene, per-frame lot enumeration,
  autosave, or all-lot presentation rebuild is involved. The capture is
  released on district teardown and disabled immediately at a wider zoom.

The extra pass is a deliberate cost: one additional low-resolution camera
draws the selected mill's two active mesh submeshes on each capture. In an
isolated graphics-enabled Unity 6000.1.12f1 batch fixture with 1,600 trees,
one deep river, one mill, and a 960×600 main render, eight warmed camera
renders measured 0.42 ms median / 0.46 ms max with the reflection disabled,
versus 0.60 ms median / 0.99 ms max when forcing a capture every render.
Managed allocations were 0 B in both paths. These are synchronous CPU call
times, **not** GPU timings or long-duration frame-time guarantees. The batch
editor reported zero draw calls even while rendering, so draw-call counts
could not be measured there; the additional two submeshes are the bounded
geometry estimate. Joe's live dense-district GPU/Frame Debugger check remains
the acceptance boundary before enabling this for more building types.

After the longer/deeper reflection mapping and UV-basis optimization, a
separate 1,600-tree, one-river, one-mill, 960×600 isolated batch check measured
0.398 ms median / 0.897 ms max with reflections off and 0.576 ms median /
0.658 ms max with a forced capture each render (eight warmed samples per path).
Managed allocations during `Camera.Render` were 0 B in both paths. These are
CPU call times, not GPU timings or long-duration stability evidence. The larger
shader footprint needs a live-editor GPU/Frame Debugger check before wider rollout.
