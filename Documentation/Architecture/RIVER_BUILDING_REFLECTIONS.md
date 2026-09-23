# Opt-in river building reflections

The first pilot reflects the native 3D lumber mill (`lumber-mill-v01`) in the
district river. It is deliberately not a district-wide planar reflection.

- Enabled at district LOD0–LOD2 only; LOD3–LOD5 perform no reflection capture.
- Candidate roots are registered when a hosted lot is added, indexed in 64 m
  spatial buckets, updated when that lot moves, and removed when it is deleted.
- The pre-render callback queries only the close camera area. It chooses at
  most one visible mill whose footprint reaches nearby river water. An
  unfinished/invisible building is skipped.
- The Lot Editor exposes a per-mill River Reflection switch. Existing saved
  mills default to on; turning it off is stored only by an explicit lot Save.
  District candidate registration respects the switch on load/add.
- One isolated 256 px-high capture (width 256–512 px) renders the selected
  mill at most once every 0.2 seconds. The river shader projects and lightly
  distorts it only on actual water within 40 m of that mill. Its projector is
  anchored to the mill and water level, so camera panning cannot slide the
  image. The projected footprint is compressed toward the mill by 20%. The water's
  existing opacity/depth/bank gradient remains authoritative.
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
