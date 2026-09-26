# Atomic Shadow District time-of-day update

Temporary Unity test-result XML was removed from the repository; the reported test counts remain.

Validated September 25, 2026 in an isolated Unity 6000.1.12f1 project. The
open editor and original saved `Shadow DIstrict` were untouched.

The first soft-shadow prototype updated eight source trees per frame, then
recreated one complete flora cell batch per frame. With 20 saved trees this
needed eight visible steps despite only a few milliseconds of total CPU work.
For the opt-in prototype, a time-of-day change now updates all source shadow
meshes and copies their vertices and material properties into the existing
shadow batches before `SetTimeOfDay` returns. Tree billboard batches, material
instances, draw-batch counts, and batch GameObjects are retained. This path
runs only on explicit time-of-day changes and only while the district has at
most 64 flora handles. Larger prototype districts keep the bounded staged path.

Isolated EditMode measurements, including `SetTimeOfDay` and a no-op follow-up
sync, were:

| Fixture | Shadow batches | Morning | Noon | Afternoon | Visible steps |
| --- | ---: | ---: | ---: | ---: | ---: |
| Original saved 20 trees | 7 | 2.80 ms | 1.08 ms | 1.11 ms | 1 |
| Saved hill with four temporary grouped trees | 9 | 3.23 ms | 1.11 ms | 1.07 ms | 1 |
| Synthetic 64 grouped trees on the saved hill | 12 | 5.12 ms | 2.81 ms | 6.38 ms | 1 |

The focused canopy suite passed 26/26 and checks that the same batch meshes
remain in place, their vertices move, and night/day visibility changes in one
call. The broader flora batching suite passed 34/35. Its camera-orbit image
assertion failed with an invisible control tree, and the identical test also
failed against the previous committed code in this headless fixture. The
earlier full EditMode run had 92 failures and was not rerun.

These are EditMode CPU measurements. They do not establish rendered Game view
frame-time spikes, GPU cost, or long-duration performance. The one-frame path
is intentionally bounded to this prototype; dense district rollout needs
profiling before changing that limit.
