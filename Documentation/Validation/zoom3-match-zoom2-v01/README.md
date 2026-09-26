# Zoom 3 grass matched to Zoom 2 base

QA screenshots for this study were removed from the repository at Joe’s request; the measurements and findings remain.

Player-facing Zoom 2 (`LOD1`) and Zoom 3 (`LOD2`) already sampled the same
world-anchored 75 m grass texture. Zoom 3 had extra distant filtering (0.15)
and was darkened to 0.89 brightness. This pass sets both to Zoom 2's values:
zero distant filtering and 1.0 brightness. Zoom 3 retains its existing full
procedural grain and 1.82 grain frequency so detail survives the farther
camera. No grass asset or texture coordinates changed. All other zooms retain
their prior settings. Mountain ground uses its separate material path.

The isolated Unity fixture rendered an empty 4 × 4 district at the actual
camera stops: Zoom 2 and Zoom 3. The captures show
the base palette matching more closely, with visibly finer variation at Zoom
3. The fixture has no trees, roads, or buildings, so Joe's open editor remains
the visual check in a populated district. The editor was not driven or
restarted.

The grass material contract and offscreen capture tests passed 2/2 in the
isolated fixture. The broader EditMode run's previous 92 failures were not
rerun and should not be described as green.
