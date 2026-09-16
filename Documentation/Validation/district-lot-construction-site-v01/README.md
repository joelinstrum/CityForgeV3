# District lot clearing and construction ground

Placement uses the saved lot dimensions, quarter-turn rotation, shoreline offset,
and nudge to clear district flora inside its ground footprint. Failed validation
returns before clearing. Saved flora is removed; it does not return at completion.
Lot-authored landscaping remains part of the saved lot design.

The existing 32m flora index now maintains list slots for swap-removal. New
footprint clearing queries nearby buckets, filters by the exact rectangle, and
removes by ID without whole-forest scans or list shifting. District loading warms
the index; bulk replacement/undo/reload still rebuilds it. Legacy external
list-removal notifications invalidate slot positions; ordinary UI deletion now
uses the indexed removal path. Harvest workers retain stable tree IDs.

Removed flora is also removed from render batches. Changed batch cells are
rebuilt once per edit. Local elevation edits query only flora inside changed
areas; broad terrain changes retain the explicit full-update path.

A temporary lot-sized dirt surface uses the existing construction callbacks.
It follows the lot transform and waits for all started building sequences.
Completion removes only that surface, revealing the authored lot ground.
No per-frame polling, new autosaves, construction timers, or inventory changes.
Construction remains the existing presentation sequence; existing reload behavior
shows completed lots rather than saving animation progress.

Validation uses a separate temporary Unity project so the active review session
is not interrupted. Performance measurements below isolate the flora-clearing
operation; they do not establish full district frame time or long-run stability.
The inherited terrain-pad/collider/grid refresh and the existing lot-creation
work still occur when placing lots; this feature does not replace those systems.

## Results

25 checks passed in the temporary Unity project: 4 new lot-site checks, 13 timber,
2 wildlife/index, and 6 selection checks. Rotated-lot placement cleared only the
intended tree and dirt disappeared after construction; before/after renders are
archived. A warmed 40,000-tree index cleared 8 trees from a 40 × 20m rectangle in
2.354ms (first measured invocation, includes managed method warmup). This is
flora data removal time, not end-to-end placement/render batching time.

Compilation and git diff checks passed. Installed into the isolated Regions-Review
project; all four lot-site checks also passed in its compiled assembly. Captured
and restored the live in-memory Testy region, retaining the same farm instance
and treasury without writing a progress save. Replaying that farm placement in
District 9 cleared four district flora instances and displayed the construction
dirt surface with one pending building. After construction, the dirt surface
was absent with zero pending buildings. Lot-authored trees remain intentional.
Dirt uses a shared material and Unity's shared primitive mesh;
completion and lot deletion release the temporary surface/material references.
