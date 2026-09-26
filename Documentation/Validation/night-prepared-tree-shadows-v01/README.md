# Night preparation and longer district tree shadows

September 26, 2026. Individual tree shadow meshes now project toward screen
right and slightly up at Noon, and almost straight screen right at Afternoon.
Their horizontal travel is 2× and 1.05× the former travel, respectively. The
existing tree artwork, shadow opacity, terrain receiver, and direct scene
lighting remain in place. Morning's direction and length are unchanged.

The Night preset prepares the next Morning's projected tree shadows while
their batches stay hidden. Its source update budget is 32 trees per frame;
spatial shadow batches still rebuild one cell per frame. Morning reuses the
finished meshes and only changes batch visibility. If a low frame rate leaves
preparation incomplete at sunrise, shadows remain hidden until the last batch
is ready, avoiding a partially changed shadow field. Loading a saved district
at Night builds the Morning shadows as part of its normal bulk load.

## Headless dense fixture

An isolated Unity 6000.1.12f1 EditMode run used 5,368 individual trees from
five species across a 4×4 district, yielding 500 spatial shadow batches. This
is a synthetic fixture, not a direct timing comparison with the separately
measured saved district in `forest-clump-foreground-v01`.

| Measure | Result |
| --- | ---: |
| District rebuild | 2,387 ms |
| Enter Night | 0.73 ms |
| Night preparation | 732 ms across 668 bounded calls |
| Largest bounded call | 11.2 ms |
| Night → Morning | 0.23 ms |

At 30 fps, 668 calls span about 22 seconds of Night's 30 second phase. The
managed allocation counter returned zero for this headless run; it does not
measure native allocations. Shadow-batch count is a draw-count proxy; GPU frame
time, actual draw calls, and appearance require review in the open editor.

Validation: 39/39 `DistrictFloraBatchesTests` passed, including interrupted
night preparation and both requested travel multipliers. `FarForestCanopyTests`
passed 26/27; the existing `FirIndividualTrunksAlignWithTheirAtlasFootMargins`
test still expects an obsolete fir atlas slot.

Follow-up: Afternoon was reduced by 30% from the initial 1.5× experiment,
making it 1.05× the original afternoon travel. The direction is unchanged.
