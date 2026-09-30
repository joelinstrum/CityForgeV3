# Close zoom hill soil experiment

September 27, 2026. The SimCity 4 hill reference shows small patches of warm
earth through grass on sloped faces. CityForge's district color maps begin at
player Zoom 3, while Zoom 1–2 use the world anchored MacroGrassV05 source.
This experiment changes only the rolling hill surface at Zoom 1–2 (LOD0–LOD1).
It does not add a terrain mesh, material, texture asset, renderer, or draw call.

The meadow shader checks physical surface grade. Soil begins to appear around a
0.16 rise/run grade and reaches full slope eligibility at 0.38. Within that
region, warmer openings in the existing grass art receive a muted earth tint.
The grass itself supplies the fine breakup, so the result follows the source
texture instead of forming broad procedural patches. The soil reveal strength
is 0.65 at Zoom 1–2 and zero at all farther zooms and on flat districts.

An isolated Unity 6000.1.12f1 render used a 640 m square district with seed
123 rolling hills, 35 m relief height, and 40% coverage. The mesh had 16,641
vertices; 6,959 had grade at least 0.16 and 2,452 had grade at least 0.38.
Paired Zoom 2 captures at the same camera position show fine earth flecks on
the steeper faces while flatter grass remains green. The first broad soil
patch prototype and a weak fine-noise version were discarded after visual
review. The final method uses only color variation already in MacroGrassV05.

The focused shader, close zoom, and world lighting tests passed in an isolated
project. An existing far grass test still expects 0.25 procedural noise at
LOD2; the current district color map path sets that value to zero. That failure
is unrelated to this shader change.

A separate 1280 m square district with 2,031 generated flora placements was
rendered at 1200 × 800, player Zoom 2. Across eight alternating timed captures
per state after warmup, the median Camera.Render plus GPU readback was 6 ms
with soil off and 6 ms with soil on. This short batch measurement does not
isolate shader GPU time, measure allocations or frame spikes, or establish
sustained dense district performance. Interactive visual and profiler review
in the open gameplay editor remains the next acceptance check; that editor was
not driven during this experiment.
