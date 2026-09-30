# Summit soil on The Hills

September 27, 2026. The SimCity 4 reference has light earth mixed with grass
near the hill crown, with less earth farther down the face. CityForge's first
soil mask was based on surface grade. A read-only snapshot of the saved
"The Hills" district (city-033, 2 × 2 region units, seed 1209, 45 m relief,
60% coverage) showed the opposite pattern: brown slopes and green crests.

The meadow shader now uses world elevation relative to the rolling hill's
requested peak height after its 1.3× vertical calibration. Soil fades in from
38% to 82% of that height. The existing grass texture still breaks the edge
into fine openings, and the same world-anchored earth sample remains visible
across player zoom levels. The saved district's mesh peak was 58.5 m, matching
45 m × 1.3. No district save data or art texture was edited.

Three isolated render passes of that exact saved district were compared with
the SimCity screenshot. The first elevation pass placed earth correctly but
made the crown too orange. The next pass reduced coverage and used a lighter
tan. The final pass widened the grass openings slightly to retain readable
earth at medium zoom. At close and medium zoom the crown now has a grass and
earth mix, while lower slopes return to green; the farthest overview keeps a
quieter hint of the same placement. Color, camera angle, lighting, and terrain
shape still differ from the reference, so this is an art direction comparison,
not a claim of numerical image similarity. The open gameplay editor was not
driven.

The focused flat-to-hills and all-zoom material test passed 1/1 in an isolated
Unity 6000.1.12f1 project. A separate 1280 m district with 1,830 generated
flora placements showed a 10 ms median Camera.Render plus GPU readback for
both soil off and on at player Zoom 5 (eight timed samples per state after
warmup). This short run does not isolate GPU cost, allocations, draw calls, or
sustained frame spikes.
