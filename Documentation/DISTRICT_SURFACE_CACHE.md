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
3. Call the relevant world refresh once. Road/lot drag previews use `deferSurfaceRefresh: true`. Local placement and movement complete through `CommitLocalSurfaceChanges()`, which updates affected terrain, collider, and decals without rebuilding the district-wide grid. Road deletion uses `CommitRoadSurfaceChanges()`, which also leaves unchanged decals alone. No public general surface-commit API exists; bulk presentation methods require a `DistrictBulkRebuildReason`.
4. Surface refresh compares against the previous snapshot, updates local heights and decoration, and retains unaffected objects.
5. Save through the existing operation completion path.

Rivers, roads, and lot placement/movement/deletion share this invalidation path. Workers, their simulation, and their optimization are deliberately outside this change. New surface consumers should use the shared affected rectangles rather than rebuilding the district unconditionally.

## Remaining costs

River meshes and junctions still rebuild once per completed river operation. When height values change, terrain vertex upload, normal calculation, and collider cooking still run at commit. General surface changes also refresh the grid presentation; road deletion defers that derived grid work until an existing bulk district or terrain rebuild. Terrain setting or district-size changes intentionally invalidate the full surface. This is incremental surface caching, not a promise of constant-time edits for every district.

## Validation — September 15, 2026

- 50 targeted EditMode tests passed, including incremental height equality against full reconstruction after river/road edits and removals, dirty-cell coalescing, and snapshot invalidation.
- Live flat fixture redraw updated 107 of 400 decoration chunks with exactly one cache revision; river refresh took 85–89 ms versus the preceding 234 ms implementation.
- Live hilly erase updated 131 of 400 chunks; spatially indexed terrain constraints reduced the measured refresh from 3,099 ms to 1,408 ms.
- Flat and hilly incremental decoration geometry matched full deterministic rebuilds. No-op refreshes retained mesh instances and cache revision.
- Live road checks verified deferred refresh, one commit, and unchanged road instance retention.
- QA used isolated fixture state and restored the original screen afterward. Timings are Unity Editor observations, not guarantees for every map.

## Validation — September 19, 2026 road deletion

- The Roads menu exposes **Delete Road**. Its active cursor is a red X, click-drag removes road cells, and Escape restores the normal selector and cursor.
- Riverdale read-only profiling measured the road edit session, model deletion, and nine-cell artwork repair at about 5 ms total.
- The former general surface commit took about 795 ms because it rebuilt the district-wide fine grid. The road-specific commit measured about 281 ms in the isolated Unity Editor fixture while still restoring 4,225 affected terrain samples and recooking the terrain collider. Unity's collider recook accounted for about 136 ms of that work.
- The player district was read only; validation did not save it.
