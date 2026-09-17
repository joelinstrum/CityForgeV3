# District rain

The Terraform Environment (cloud) menu offers Rain and Clear Skies. Rain starts
one real-time weather event: five seconds gathering cloud cover, ten seconds
of rainfall, then four seconds clearing. Rain emission is gated on complete
coverage. Repeating Rain restarts the sequence; Clear Skies cancels it. Leaving
the district cancels transient weather. It is not saved and does not modify
regional climate, terrain, roads, workers or water simulation.

The overcast canopy is visible at the two farthest zoom levels, matching the
existing clouds. Rain and overcast ground shading remain visible at all zooms.
The canopy extends beyond the district with a feathered irregular margin; its
continuous body closes all holes before rain. Cumulus highlights reuse the
approved CloudsV01 artwork. Fair-weather cloud bodies fade as overcast arrives.

DistrictRainStorm owns two fixed renderers using one shared four-vertex screen mesh, two materials and a real-time state clock. The shader projects the cloud
plane at terrain height +170m and clips rainfall to the district footprint.
It uses bounded procedural streaks resembling the lot's pale elongated rain,
not the lot's particle collision or wet-reflection scans. There are no per-frame
allocations, district scans, mesh uploads, or building reflection rebuilds in
this effect. This is an artistic weather overlay, not volumetric simulation;
rain does not collide with roofs or accumulate water.

Tests cover rain gating/duration, clearing, cancellation, repeat activation,
resource reuse and shader compilation. Live review runs a complete cycle with
the district simulation paused, checks unchanged district JSON, captures the
actual windowed Game view, and records editor frame statistics. The review
restores simulation state and does not save progress.

Validation on September 16: 80 tests passed at 22:45:09 UTC. A paused isolated
copy of Little River Bend (2,947 flora) completed a real-time cycle with rain
starting at 5.01s and ending at 15.01s, returning to clear without district data
changes. Short editor samples: clear median 13.40ms/p95 36.37ms; rain median
15.51ms/p95 36.17ms. Editor draw counts include other views/UI and varied across
reloads; the effect owns exactly two renderers (one at closer zooms). This is
not a standalone GPU or allocation benchmark. Final visual-only UV calibration
followed that profile; sample counts stayed unchanged. Far and close captures:
QA/RiverBanks/Pine-Ridge-224335825.png and Pine-Ridge-224408825.png.

## Rain mist

District mist matches the lot editor's grey-green fog colour (.72,.73,.72),
using 28% maximum opacity in the existing rain pass. Its independent envelope
builds over the first two seconds of rainfall, stays at full strength through
the last drops, then fades over the four-second clearing phase. It remains
visible at close zooms, where the overhead cloud canopy is hidden. Clear Skies
and leaving the district still cancel all weather immediately.

The mist is composited behind the streaks, clipped to the same district footprint,
and adds no renderer, texture fetch, particle system or terrain work.

## Temporary snow test

Snow in the Environment menu reuses the five-second cloud build-up, then runs
a 25-second snow sequence: ten seconds of falling flakes and accumulating
cover, ten seconds of settled snow, then a five-second melt. Mist and clouds
clear after falling stops; the ground snow remains independently. Rain, Clear
Skies, restarting Snow or leaving the district resets the temporary cover.
Nothing is saved. This is a temporary ground-cover test, not a seasonal or
roof/tree snow simulation.

One extra renderer references the existing terrain mesh, follows terrain mesh
replacement by reference and blends a slope-aware pale snow layer. It does not
write heights, clone/upload the terrain mesh or alter river water. Existing
river/bank overlays and structures keep their own materials. The snow material
and renderer are disabled when empty and destroyed with the district.

83 tests passed 2026-09-16 23:01:49 UTC. Live isolated saved Little River Bend
(2,947 flora) checked full cloud cover before snowfall, settled snow after the
flakes stopped, partial melting, complete cleanup and unchanged district JSON.
Captures QA/RiverBanks/Little-River-Bend-230310760.png (falling), 230320745.png
(settled), 230330752.png (melting) inspected. Short close-zoom editor frame
medians clear/snow 13.57/15.27ms; p95 28.27/28.85ms. These measure the complete
weather presentation, not just the snow-cover pass. QA uses bank-snow-review,
~34 seconds; wait for DONE in /tmp/cityforge-snow-check.txt before restoring.
