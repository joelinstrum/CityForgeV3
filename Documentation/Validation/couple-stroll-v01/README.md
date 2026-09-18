# Gentleman and Lady Strolling V01 validation

The clip was reviewed in the isolated `CityForge-Regions-Review` Unity project
on 18 September 2026. The final focused EditMode result in
`focused-automata-editmode.xml` passed 6/6 Automata tests. It exercised the
brick-road clearance, resource loading, one-group placement, selection and
yellow outline, dragging, quarter-turn rotation, and visibility schedules.
The Automata menu card was present and enabled.

The live captures `couple-stroll-frame-00.png`, `-06.png`, `-19.png`, and
`-28.png` show the start, first stop, second stop, and return walk on an unsaved
empty lot at spring noon. These were recaptured after moving the figures to
about half a metre apart. They nearly touch, remain grounded, and stay inside
the selected 8 × 8 m footprint. `base-direction-loop.gif`, `route-sequence.png`,
and `directional-contact-sheet.png` show the complete authored loop and all
eight views. All 320 source frames were checked for nonempty alpha and at
least 18 px of clear margin around the frame. In the pure side view, the art
centroid moves about 128 px over the outbound 4 m, matching 32 px/m, rests at
both stops, then returns. The turn back and reset turn are visible in the
sequence sheet.

The supplied southward-road screenshot exposed a separate rendering issue.
The authored frame alpha reaches up to 1.14 m below the pivot, but the catalog
had declared zero. The brick-road artwork draws at queue 3002 and writes
depth, while ground decals draw at 3003; the default sprite drew at 3000.
`brick-road-before-frame-19.png` and `-35.png` show the pair hidden on a real
brick-road tile in an unsaved neighborhood test lot. Raising only the art or
changing only its queue was insufficient. The final catalog declares 1.35 m
of visible art below the pivot, and one shared Automata material draws at
queue 3004. `brick-road-fixed-frame-19.png` and `-35.png` show the full pair
on the road with the actual updated runtime code. The focused road test checks
both the depth clearance and draw order.

The review editor stalled during one 1400 × 900 explicit camera capture. It
was restarted, and the return frame was captured at 900 × 600. The stall did
not occur during the focused test or normal review setup. No disk save was
made; the review project was returned to the latest manually saved Testy,
District 9. Dense-district frame-time, allocation, draw-call, and long-run
stability measurements remain open. The clip is one shared renderer per
visible placement, but first use loads eight 2048 × 800 atlases and 320
sprite slices, with about 52.4 MB of uncompressed pixel data.

## Garment recolor follow-up

The same source rigs generated aligned red/green garment masks for all 320
frames. An authoring preview caught a first pass that colored cream lace and
skin; the final UV classifier keeps those areas original. The final atlases
were checked for the correct 2048 × 800 dimensions and nonempty color channels
in all eight facings. Mask pixels just beyond a silhouette are harmless:
the base sprite still supplies alpha and those pixels never draw.

`recolor-editmode.xml` records 3/3 focused EditMode tests passed in the isolated
Unity project. They cover resource loading, the south brick-road clearance,
independent dress and outfit colors on two placements, a shared material,
per-renderer mask assignment, and undo. The Unity capture
`two-recolored-couples-in-unity.png` shows blue/brown and sage/navy pairs on an
unsaved review lot; `unity-recolor-detail.png` enlarges those same rendered
pixels. The selected pair's inspector displayed its own palette.
`garment-authoring-preview.png` enlarges one frame to inspect dress, trim,
skin, and coat edges. The close Unity camera render stalled as an earlier high
zoom capture did, so the isolated review editor was restarted without saving.
This capture problem is a review limitation, not a pass on close-up runtime
rendering. The review project was returned to the latest manually saved Testy,
District 9, and the main Unity project was untouched.

Eight additional 2048 × 800 masks total about 39.3 MB expanded RGB24 or
roughly 6.6 MB as BC1/DXT1 without mipmaps, depending on platform import.
They are shared across placements. Shader cost is one additional texture
sample per visible sprite pixel; one property block changes when a facing or
color changes. Dense-district CPU, allocation, draw-call and frame-time
measurements remain open. No district scans, full redraws, or disk saves were
added.
