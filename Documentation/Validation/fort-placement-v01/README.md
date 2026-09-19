# Fort placement input repair

Fort was armed separately from regular Lots, so Select mode consumed its world
click and the pointer-move handler never drew its placement outline. Founder
placement now owns world clicks before selection/inspection and previews its
cached Lot footprint. The same footprint calculation drives final placement.
The outline shows bounds; overlap is checked on click and reports an open-area
hint. Preview introduces no district scan or disk reads. The existing placement
collision check and undo/composition work remain at the placement boundary.

City Center remains disabled with explicit copy explaining its Lot is coming
later. No placeholder building or save migration was introduced.

Ten isolated EditMode tests passed: regular/test/founder input ownership, founder
preview-to-placement coordinates and town start state, naming paths, and region
name visibility. Runtime compilation and git diff --check passed. QA used a
synthetic Fort Lot and an in-memory district in a separate Unity scratch project;
live Fort artwork/rendering and physical mouse interaction were not exercised.
No player saves, commits, pushes, review sync, or worker/labor changes.
