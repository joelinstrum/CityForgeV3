# Warm hill soil across district zooms

September 27, 2026. The first visible soil pass used the mountain brown scree
resource without color correction. Against the meadow it read purple, and the
soil parameter was zero at player Zoom 3–6. The shader already blends soil
after the district grass color map, so the far grass textures did not need to
be regenerated.

This pass warms only the soil sample, leaving the shared source texture and
grass untouched. Rolling hill soil reveal remains 0.9 at Zoom 1–2, then uses
0.75 at Zoom 3–4 and 0.65 at Zoom 5–6. The slope and grass opening masks are
world anchored, so the same exposed areas remain in place as the camera zooms.
Flat districts and steep mountain terrain keep their separate appearances.

In isolated Unity 6000.1.12f1, paired 1200 × 800 renders of a default
640 m rolling hill district showed ochre earth on the sloped faces at all six
player zooms. The comparison used seed 1209, 35 m height, 60% coverage, and
1× vertical scale. The focused flat-to-hills and zoom transition test passed
1/1, including confirmation that the distant grass map keyword remained active.
The open gameplay editor was not driven.

For a 1280 m district with 1,830 generated flora placements, eight timed
captures per state after warmup at Zoom 5 gave an 11 ms median for both soil
disabled and enabled. This measures Camera.Render plus GPU readback in a short
isolated batch. It does not isolate GPU cost, allocations, draw calls, or
sustained frame spikes.
