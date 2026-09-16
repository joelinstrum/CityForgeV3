# District surface cache

District saves remain authoritative. Derived surface data lives in memory and is reused between edits; no external database is required.

## Shared components

- `DistrictSurfaceCache` snapshots river segments, road and lot footprints, and terrain settings by value. One update returns affected rectangles and advances the revision only when surface data changes. Unrelated simulation changes do not invalidate the surface.
- `DistrictDirtyGrid` maps affected rectangles to a matrix of chunks. Overlapping changes coalesce and each dirty chunk is consumed once.
- `DistrictSpatialIndex<T>` buckets spatial constraints for nearby queries. River surface sampling and elevation generation use this index before their exact geometry checks.
- `DistrictElevation` retains its height matrix. Local changes recompute affected samples; terrain settings changes rebuild the matrix.
- Ground grass and hill overlays retain unchanged chunk meshes and shared materials. Road refreshes retain road objects whose placement data is unchanged.

## Edit lifecycle

1. Update the edit preview during dragging. River dragging does not change saved data or textured geometry.
2. Apply the final model changes on release.
3. Call the relevant world refresh once. Road/lot drag previews use `deferSurfaceRefresh: true`; completion calls `CommitSurfaceChanges()` or a non-deferred refresh.
4. Surface refresh compares against the previous snapshot, updates local heights and decoration, and retains unaffected objects.
5. Save through the existing operation completion path.

Rivers, roads, and lot placement/movement/deletion share this invalidation path. Workers, their simulation, and their optimization are deliberately outside this change. New surface consumers should use the shared affected rectangles rather than rebuilding the district unconditionally.

## Remaining costs

River meshes and junctions still rebuild once per completed river operation. When height values change, terrain vertex upload, normal calculation, collider cooking, and grid presentation refresh still run at commit. Terrain setting or district-size changes intentionally invalidate the full surface. This is incremental surface caching, not a promise of constant-time edits for every district.

## Validation — September 15, 2026

- 50 targeted EditMode tests passed, including incremental height equality against full reconstruction after river/road edits and removals, dirty-cell coalescing, and snapshot invalidation.
- Live flat fixture redraw updated 107 of 400 decoration chunks with exactly one cache revision; river refresh took 85–89 ms versus the preceding 234 ms implementation.
- Live hilly erase updated 131 of 400 chunks; spatially indexed terrain constraints reduced the measured refresh from 3,099 ms to 1,408 ms.
- Flat and hilly incremental decoration geometry matched full deterministic rebuilds. No-op refreshes retained mesh instances and cache revision.
- Live road checks verified deferred refresh, one commit, and unchanged road instance retention.
- QA used isolated fixture state and restored the original screen afterward. Timings are Unity Editor observations, not guarantees for every map.
