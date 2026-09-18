# District bridges V01 validation

Work branch: `feature/district-bridges`, based on `origin/main` at `1eaf191`.
Unity: 6000.1.12f1. Tests and renders use `/Users/joelinstrum/dev/CityForge-Regions-Review`. The separate editor in `/Users/joelinstrum/dev/CityForge - V3` was not controlled or modified. Replaced review-project files were backed up under its `RecoveryBackups/bridge-sync-*` directories.

## Player flow

1. Build → Roads → Bridges offers Covered Wooden Bridge and Stone Arch Bridge.
2. Choose a preferred bridge and draw a road toward a river. The first channel cell interrupts the road gesture. The bounded planner finds dry approaches and an opposite bank.
3. A side modal previews the chosen bridge in the world and displays span and cost. Switching style replaces only the preview. Cancel/Escape removes the preview without constructing or charging for a bridge. Dry road tiles drawn before the chooser remain as a separate undo step.
4. Build creates the bridge and bank connection road tiles, charges construction, and records an in-memory undo step. Only the explicit Save action persists progress.
5. The bridge catalog lists built crossings and provides a confirmed removal action. Approach roads remain; bridge cost is not refunded.

Bridges are separate saved spatial records. They do not paint ordinary road tiles under river water. Timber/quarry road delivery graphs contain an explicit bank-to-bank connection, including resuming a saved wagon partway across a bridge. Workers and wagons query a shared local deck/ramp height. Bridge add/remove advances the navigation revision. Ordinary road drags are now prevented from placing ground road tiles in river channels.

## Automated checks

Final result: **87/87 passed**, zero failures (1.82 seconds test execution). See [focused-tests.xml](focused-tests.xml). The final graphics batch exited successfully after all transaction and restoration checks.

Actual Unity views: [covered wooden bridge](covered-wood.png), [stone bridge](stone.png), [25-bridge stress fixture](dense-bridges.png).

Focused EditMode fixtures cover bridge planning, four diagonal orientations, opposite-bank search bounds, occupied corridor rejection, serialization, undo snapshots, road delivery connectivity/resume, navigation-cache invalidation, catalog composition/cancel, spatial-index removal, and both asset packages. Existing road placement, road lifecycle, labor, undo, and river-bank tests are included in the final regression result.

`Assets/Editor/DistrictBridgeQa.cs` renders both styles over an actual procedural river and checks the real UI construction transaction: cancellation, price, world presentation, in-memory undo, and reload. It also exercises a dense bridge-only scene and records the retained edit-boundary costs. Run with:

```
Unity -batchmode -projectPath /Users/joelinstrum/dev/CityForge-Regions-Review \
  -executeMethod DistrictBridgeQa.Run -quit -logFile /tmp/cityforge-bridge-render.log
```

Output defaults to `/tmp/cityforge-bridge-qa`; `CITYFORGE_BRIDGE_QA` can override it. This creates diagnostic artifacts only, not region saves.

## Performance boundaries

- Crossing search is capped at 240 meters/26 grid advances. Corridor validation samples at most 121 longitudinal stations × five lateral points. It queries indexed nearby river segments, nearby bridges, and the existing lot occupancy cache.
- River lookup now has a top-level segment-bounds bucket index in addition to each river's segment index. Bucket insertion deduplicates a river within each bucket. Travel uses indexed loops without enumerator boxing.
- Bridge add/remove changes one bridge mesh pair and its spatial buckets. No district presentation rebuild occurs on construction/removal. Undo/load intentionally rebuild presentation from restored data.
- Road topology stamps replace per-tick road hashing in timber and quarry navigation. Timber navigation and delivery graphs are cached by road-list identity/count, maintained topology revision, and bridge revision. Full restoration explicitly invalidates the network. A trip still searches the connected road graph, but finding nearby service roads is a bounded grid lookup rather than sorting every road.
- The pre-existing district composition key and terrain surface cache still enumerate district collections at edit completion. These existing paths were disclosed during implementation. They are retained to keep local road-pad updates and the current undo/UI contract coherent; they are not called by bridge-height queries or routine bridge rendering. Their measured cost is reported separately. A future general editor revision system should replace them.

Numbers in `report.txt` are short synthetic measurements, not a long-duration performance guarantee. Managed heap deltas are approximate and can reflect collection. `GC.GetAllocatedBytesForCurrentThread` reported zero even for allocating operations in this Unity runtime, so its zero result is not used to claim allocation-free mesh construction. Synchronous `Camera.Render` timing is render submission cost, not full game frame time; UnityStats availability in batch mode is recorded rather than assumed. Dense Automata profiling remains open.

## Recorded short-run measurements

See [report.txt](report.txt) for raw output. Warm construction: covered wood 5.54 ms, stone 3.33 ms; each assembled bridge has two renderers. Both construction operations grew the sampled managed heap by about 2.39 MB. These are one-off placement costs, not recurring frame allocations.

The 25-bridge fixture used 50 bridge renderers. Thirty synchronous render calls took 0.61 ms median / 0.91 ms maximum for CPU submission. **UnityStats returned zero in this batch path, so actual draw-call and GPU/full-frame timings remain unverified.** 100,000 travel-height queries took 45.46 ms with zero sampled heap growth. The 10,000-object index fixture returned at most two candidates for each of 100,000 local queries.

The retained composition-key boundary took 66.04 ms and about 6.44 MB sampled heap growth at 20,000 flora + 10,000 roads. This remains a material edit-completion spike and warrants a general revision-key refactor. Constructing the 10,000-road delivery graph took 3.09 ms / about 700 KB; 100,000 subsequent cached-network lookups took 35.80 ms with zero sampled heap growth. Network reuse replaces repeated construction and road hashing during steady-state delivery.

## Current limits

- Straight crossings only, 60–240 meters including ramps; cardinal roads and Antique Brick diagonal strokes. The planner rejects insufficient bank space, intersecting lots/bridges, and steep approaches.
- Partial bay length fitting is deliberate and documented with the asset lineage. Prices are provisional gameplay values.
- Bridge deck elevation is fixed when built. Changing river geometry, water level, or terrain beneath a finished bridge does not redesign its approaches; remove/rebuild the crossing after substantial terrain changes.
- Foundations reach the bed, but boats do not yet reserve a clear navigation span through piers. There is no structural simulation.
- Removing a crossing under active travelers can strand them; pause/move traffic before removal. Long-running delivery and dense mixed districts need further profiling beyond the focused transaction and synthetic scene checks.
- Catalog images are source/authoring views; runtime screenshots show the actual fitted geometry and approaches.
