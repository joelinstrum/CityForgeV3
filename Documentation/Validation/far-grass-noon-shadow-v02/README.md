# Far grass and noon shadow visual review

**Zoom label correction (September 24, 2026):** The captures and prose below
used enum labels (`LOD3`–`LOD5`) as if they were player-facing zoom numbers.
Player-facing Zoom 3 is `LOD2`, so the earlier `grass-zoom-3.png` actually
shows Zoom 4. The earlier “Zoom 3 follow-up” also changed Zoom 4. See
[`../player-zoom3-grass-v01/README.md`](../player-zoom3-grass-v01/README.md)
for the correction and a capture at the actual Zoom 3 camera distance.

September 24, 2026. Reviewed branch `feature/time-and-light-adjustments` at
`48ad7f9`, then tuned the far meadow against Joe's SimCity 4 screenshots.
The screenshots are visual references only. No pixels or assets were copied from
SimCity 4, and the canonical City Forge grass artwork was not changed.

## Visual decision

The supplied reference has fine, relatively even stipple over muted dark olive
variation. The prior zoom 4–5 capture showed larger soft mottling. The far
meadow's existing two world-anchored noise frequencies are now 2 m and 13 m
(previously 8 m and 33 m), with the broader noise contribution halved. Zooms
3, 4, and 5 darken the grass brightness by 11%, 9%, and 11%, respectively.
Zoom 3 also uses 1.82 times the grain frequency so the closer camera shows
a similar apparent stipple size.
The hue, 75 m artwork registration, and mountain material are unchanged.

The noon shadow's 0.8 projection scale was retained. In isolated zoom 1, 2,
and 3 captures, the two deciduous clump shadows meet their trunks and stay
behind the crowns; they remain legible at the wider capture. This is a visual
judgment on flat ground, not a claim about every tree species or slope.

Captured with the isolated Unity fixture
`/private/tmp/cityforge-shadow-pass-MBltYx`, at 1280 × 720. The working editor
on the V3 project was not driven or restarted. Grass captures suppress flora
batches to show terrain; shadow captures include the two-tree fixture.

- `grass-zoom-3.png`, `grass-zoom-4.png`, `grass-zoom-5.png`
- `grass-zoom-3-before-followup.png` preserves the earlier, weaker zoom 3 view
- `shadow-zoom-1.png`, `shadow-zoom-2.png`, `shadow-zoom-3.png`

## Validation and limits

- Focused grass/canopy/visual-fixture run: 19/19 passed, including the shader
  property and zoom contract check. The visual-fixture test lives only in the
  temporary Unity project, not the production test suite.
- Separate flora batch run: 33/34 passed. The failed
  `CachedTreeStillRendersAfterNinetyDegreeCameraOrbit` camera-render test saw
  zero control-view tree pixels instead of >2,000. This run is not green.
- The prior PR pass reported 52/52 focused tests, while its broader EditMode
  run had 1,138 passed and 92 failed. Neither result was rerun as a full suite
  for this review.
- A short 400-tree, 1280 × 720 synchronous `Camera.Render` comparison used
  30 samples per pass. Prior shader submission medians were 0.400 and 0.367 ms;
  tuned shader medians were 0.395 and 0.401 ms. Per-pass p95 ranged 0.448–
  0.656 ms and no per-render managed allocations were measured. The UnityStats
  draw-call counter returned zero in this offscreen batch run, so draw calls
  could not be compared. The shader uses the same number of noise evaluations
  and adds no renderers, materials, meshes, or district scans. This short
  submission check does not establish GPU cost or long-duration frame stability.

Interactive review in Joe's open Unity editor remains for Joe to perform.

## Zoom 3 follow-up

Joe liked the zoom 4–5 improvement but found zoom 3 nearly unchanged. Zoom 3
now uses the same 0.15 filtering, full grain strength, and 0.89 brightness as
zoom 5. Its grain frequency is 1.82 times higher in world space because the
closer camera otherwise makes the noise look coarser. The changed view was
re-rendered in the isolated fixture and compared with the retained before
capture; zoom 4 and 5 settings were not changed. The zoom contract, visual
fixture, and short dense render profile passed 3/3.

The zoom 3 profile used 400 trees and 30 synchronous 1280 × 720 renders per
pass. Before medians were 0.313 and 0.307 ms; after medians were 0.314 and
0.296 ms. No per-render managed allocations were measured. UnityStats again
reported zero draw calls in batch mode, so GPU cost, draw calls, frame-time
spikes, and long-duration stability remain unmeasured.
