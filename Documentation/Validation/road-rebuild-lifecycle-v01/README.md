# Roads disappearing after district rebuild — 2026-09-16

The incremental road refresh introduced on main retained `_roadArtworkRoot` across `ClearWorld`. In Play Mode, destruction is deferred: a subsequent `Build` saw the old inactive root as non-null and attached replacement roads to it. They were destroyed at frame end. The meadow update's presentation rebuild exposed the problem; the base meadow/road render queues were still ordered correctly.

ClearWorld now releases the road root reference and clears its visual-state cache, so RefreshRoads creates a root under the new district content. No shader priorities, artwork, district data, save behavior, or per-frame work changed.

Validation in the isolated Regions Review project, Testy / District 9:
- Unity runtime, test and editor compilation succeeded.
- Two Play Mode rebuilds retained all 52 road objects after a frame, with an active road root under the current district content.
- District JSON remained identical across the test; no progress-save calls were made.
- The new cleanup regression test passed. It checks actual null rather than Unity's destroyed-object fake-null and verifies that the visual cache is cleared.
- Inspected the actual Unity Game view: roads are visible between the quarry, Brickworks and riverfront with the new meadow and banks.

Applied the small runtime patch to the review using a three-way merge, retaining its helper changes. A recovery snapshot of current in-memory region data was used across script reload; existing progress-save files and the main Unity project were untouched. This is a focused lifecycle check, not a performance soak.
