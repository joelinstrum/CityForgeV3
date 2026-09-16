# Dense district flora performance — 2026-09-16

Follow-up: automatic saves were subsequently removed at the user’s request. See [manual save validation](../district-manual-save-v01/README.md). The timings below document the earlier checkpoint implementation.

Worktree: `quarry-and-performance-updates`, branch `lot-updates`, base `6b1e833`.
Validated in the isolated `CityForge-Regions-Review` project, Unity 6000.1.12f1. The main `CityForge - V3` project was not modified or controlled.

## Findings and changes

- Tree sprites, textures, and the tree/shadow materials were already shared. However, each tree and its two-pass shadow still issued separate rendering work. District 9's 9,953 trees produced about 28,300 draw calls at LOD4.
- `DistrictFloraBatches` caches static geometry by 256 m spatial cell and sprite, preserving per-species artwork repair properties, tint, terrain-projected shadows, and per-cell culling. Individual renderers remain enabled selection handles with rendering suppressed. Moving/falling trees leave their batch; completed stumps rejoin static batches. A local change rebuilds only its cell/species group. There is no batch update loop during normal frames, panning, or zooming.
- Harvest presentation previously rebuilt every tree shadow. It now looks up changed IDs through the shared index and refreshes only those shadows. Tree-fall lookup likewise no longer scans the entire forest.
- Harvest state is no longer part of the spatial composition key. Incremental harvest presentation does not need to reconstruct the full district key or invalidate walking navigation. Spatial edits remain in the key; undo/reload explicitly invalidate the world.
- The isolated review had retained an older `LaborNavigation` implementation that called `DistrictCompositionKey(d)` repeatedly each frame. Its forest-sized string construction was responsible for substantial additional CPU cost. The worktree already used `_districtWorldCompositionKey`; the review was incrementally corrected to match that implementation. Other review-only differences were retained.
- Simulation events previously called `SaveDistrictEdit` independently, potentially saving the region multiple times per frame and filling editor undo history. Simulation systems now share the existing five-second checkpoint. Explicit edits still save immediately; leaving the district and quitting flush state. Successful saves reset the shared checkpoint. Save JSON is compact and retains the existing atomic replacement and data format. Empty editor-only save overrides are normalized to the default root after domain reload.
- Wildlife sighting density previously compared each mountain tree with every other mountain tree. It now uses the shared spatial buckets and stops after three nearby standing mountain trees. The shared index includes non-harvestable flora, while lumberjack queries retain their harvestability filter.

## Measurements

Testy / District 9; 9,953 trees, 32,650 trees across the saved region; Game view 2070 × 1008, LOD4. The probe warmed each mode and used rendered frame counts. It restored the camera pan, simulation pause state, and renderer flags after testing.

| Mode | Rendered frames | Median ms | 95th percentile ms | Maximum ms | Average draw calls |
|---|---:|---:|---:|---:|---:|
| Batched, simulation running | 4,200 | 14.77 | 15.73 | 110.03 | 2,551 |
| Batched, simulation running and panning | 1,200 | 14.55 | 15.76 | 107.74 | 2,553 |
| Batched, paused | 120 | 14.05 | 14.80 | 89.98 | 2,557 |
| Individual rendering, paused | 120 | 82.36 | 86.80 | 196.00 | 28,309 |
| Flora hidden, paused | 120 | 11.99 | 15.35 | 17.78 | 294 |

The paired rendering comparison uses the same trees, geometry, textures, camera, and paused simulation. Batching reduces draw calls by approximately 91% and the paused median frame time by approximately 83%. Early measurements with the stale review navigation code still present had a 149 ms running median, even with batching; correcting that path removed the per-frame CPU bottleneck.

The running and panning sample covers roughly 80 seconds, including periodic autosaves. This is an Editor measurement, not a standalone player benchmark or a long-duration soak test.

A direct integration check verifies that refreshing one tree leaves a sentinel on an unrelated shadow unchanged and reuses the cached sprite. Full-region compact saves remain about 90–105 ms for this 10.97 MB / 32,650-tree region. See the raw integration results for the latest exact timings.

## Validation

- 45 batch, spatial wildlife, district/regional flora, terrain, river, and region deletion cases passed, including selection of batched trees, local remove/re-add, lighting refresh, stable composition keys on harvest, spatial invalidation on movement, and save rollback/reload behavior.
- 62 existing economy, Brickworks, quarry, crane, labor, timber, and wood-resource cases passed; the actual Brickworks inspector check also passed.
- Live integration checks cover selective shadow refresh, sprite reuse, full-region save/reload tree counts, and checkpoint coalescing/reset.
- The paired screenshots were inspected for missing flora, textures, and shadows.
- Unity compilation and `git diff --check` passed.
- Review runtime synchronization used checked incremental patches and backups under `RecoveryBackups/flora-performance-v01`; files with unrelated review differences were not replaced wholesale.

## Remaining limits

Full-region autosave serialization and disk replacement are still synchronous. The measured ~90–105 ms save cost explains a remaining occasional hitch; the regular-frame improvements do not eliminate it. Bulk forest generation, lighting changes, and terrain reconstruction deliberately rebuild cached presentation geometry. This test does not prove the older unexplained game lockup is resolved.

`frame-profile.txt`, `integration-checks.txt`, test logs, screenshots, and the review-only helper sources accompany this note. No changes were committed or pushed as part of this task.
