# Close zoom hill soil visibility correction

September 27, 2026. A player generated hills in a new district and could not
see exposed dirt. The first experiment only multiplied the grass color on a
narrow warm-pixel mask. In a paired render, just 3.1% of image pixels changed
by more than 20 color levels, so the result was easy to miss. The Generate Hills
action already rebuilt the ground and set the shader parameter correctly.

The corrected meadow shader uses the existing brown earth texture beneath the
grass at player Zoom 1–2. Grass color supplies the small gaps, while surface
grade controls how much earth can show. Reveal begins at 0.10 rise/run and
reaches full slope eligibility at 0.32. Flat ground and player Zoom 3–6 keep
their previous appearance. This reuses the mountain brown resource; it adds no
renderer or draw call, but does add a texture sample to close zoom rolling hill
shading. Steep mountain terrain retains its separate shader.

An isolated Unity 6000.1.12f1 project recreated the default relief dialog
settings: seed 1209, 35 m height, 60% coverage, and 1× vertical scale. On a
640 m district, 11,567 of 16,641 mesh vertices had grade at least 0.10. Paired
1200 × 800 renders at player Zoom 2 showed clear brown earth on the sloped face
and green level ground. A 1280 m district also showed soil on its slopes; its
shallower geometry had only five vertices at or above grade 0.32. The isolated
test started with a flat district, applied those default hill settings, checked
the soil texture and shader keyword, then verified that only Zoom 1–2 set a
nonzero reveal. It passed 1/1. The open gameplay editor was not driven.

In an isolated 1280 m district with 1,830 generated flora placements, twenty
alternating 1200 × 800 captures gave eight timed samples per state after warmup.
Median Camera.Render plus GPU readback was 8 ms both with soil disabled and
enabled. This short batch does not isolate GPU shader cost, allocations, draw
calls, or sustained frame spikes; interactive profiler review remains useful.
