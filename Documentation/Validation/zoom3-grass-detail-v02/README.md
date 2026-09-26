# Zoom 3 grass detail from the source texture

QA screenshots for this study were removed from the repository at Joe’s request; the measurements and findings remain.

Joe's populated-district screenshots show fine, even grass at player-facing
Zoom 2 and larger diagonal streaks at Zoom 3. Both zooms already use the same
75 m grass image. Zoom 3 was sampling a softer mip level at its farther camera
and adding full-strength procedural noise. Matching brightness and distant
filtering in the previous pass did not remove that difference.

This pass keeps Zoom 3's shared artwork, world registration, brightness, and
zero distant filtering. It requests one mip level more detail from the grass
image (gradient scale 0.5) and reduces procedural grain strength from 1.0 to
0.25. Zoom 2 and the other zoom settings retain their previous sampling. The
shader's base grass texture sample count is unchanged; it uses a different mip
level. Mountain ground remains on its separate shader.

In an isolated 4 × 4 flat-district render, the revised Zoom 3 capture
has finer, more even detail than the previous Zoom 3 capture
and is closer to the Zoom 2 reference.
These captures contain no trees or buildings; the populated Unity view still
needs Joe's visual assessment, especially while panning for shimmer. His open
editor was not driven or restarted.

The grass material and shader checks passed 2/2 in the isolated Unity fixture.
A 1,777-tree, 4 × 4 district render profile passed 1/1. Thirty synchronous
2048 × 1096 renders per pass had 0.318–0.320 ms median CPU submission before
and 0.310–0.318 ms after, with zero managed allocations in both. The offscreen
fixture reported zero draw calls and batches, so GPU cost and frame-time spikes
remain unmeasured. The run is too short to establish long-duration stability.
The broader EditMode run's previous 92 failures were not rerun.
