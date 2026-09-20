# Hill forest grounding and distant pan calibration

Validated September 19, 2026 in an isolated Unity 6000.1.12f1 clone.

Tree Coverage samples the candidate center and four points 12m away. Large
nine-tree art requires at most 0.4m height spread, compact five-tree art at most
0.9m, and steeper candidates use one family-matched tree rooted at its own
terrain point. This preserves one placement/renderer per candidate and avoids
the unavoidable shared-baseline distortion of a multi-tree bitmap on steep
relief. Existing forests require explicit regeneration; persistence stays
manual-only.

Each district zoom now has an explicit pan multiplier. Player-facing Zooms 4,
5, and 6 use 0.18, 0.07, and 0.035 respectively, versus 0.35, 0.525, and 1.3
for Zooms 1–3. Arrow and edge-hover panning share this calibration.

The focused isolated suite passed 16/16: deterministic forest generation,
family weights, flat/compact/steep selection, slope-tree resource resolution,
coverage persistence, pan multipliers, and isometric pan direction. The live
CityForge V3 editor compiled without new errors. `git diff --check` passed. No
player Lot, district, or region was saved; worker/labor behavior was unchanged.

Evidence: `hill-pan-results.xml`, `hill-pan.log`.
