# Grouped tree shadow footprint review

QA screenshots for this study were removed from the repository at Joe’s request; the measurements and findings remain.

September 25, 2026. The soft hillside shadow prototype now sizes a true-angle
cluster's single shadow from its full compact or large envelope rather than
the root sprite's single-tree bounds. Composed forest sprites likewise retain
their full width. Individual trees keep the previous footprint. Each group
still has one terrain-following mesh and one flora batch member; there is no
renderer or shadow caster per piece.

Validation used an isolated Unity 6000.1.12f1 project and a temporary copy of
the saved `Shadow DIstrict` tile. Four of its 20 trees were temporarily replaced
in that copy with deciduous compact/large and mountain compact/large groups.
The player's save and open editor were untouched. The focused canopy EditMode
suite passed 26/26. The saved hillside fixture passed, produced nine spatial
shadow batches, and contained a shadow mesh spanning 16.508 m of elevation.
Morning, noon, and afternoon refreshes completed over ten budgeted calls each;
their total EditMode CPU times were 8.47, 5.34, and 5.04 ms in the final run.
These figures do not establish dense-forest frame-time or GPU cost.

Morning and noon captures at zooms 1 and 2 are included for visual review.
The grouped shadows remain limited to the named prototype district. A wider
rollout needs visual review and dense-district profiling.
