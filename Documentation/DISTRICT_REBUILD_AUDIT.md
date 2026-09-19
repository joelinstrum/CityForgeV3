# District rebuild audit

Local edits must update their model, spatial cache, affected presentation, and
bounded terrain area. They must not reconstruct the district world. Full work is
reserved for loading or switching districts, restoring a complete undo snapshot,
and operations whose input covers the whole district.

## Full rebuild boundaries

| Path | Trigger | Status |
| --- | --- | --- |
| `RebuildEntireDistrict` | Initial district load or switching districts | Required bulk boundary |
| `RebuildEntireDistrict` | Restoring a complete district undo snapshot | Required bulk boundary because object identity is replaced from JSON |
| `RebuildEntireDistrict` | District-wide hill or region terrain replacement | Required bulk boundary |
| `RebuildAllRiverPresentations` | River deletion, replacement, shaping, or movement | Bulk water boundary; all junctions and clipped endpoints are recomputed |
| `RebuildAllFloraPresentations` | Initial load or explicit district forest-coverage regeneration | Bulk flora boundary |

QA fixtures may call these APIs directly because they intentionally construct or
replace complete test state. Those calls are not part of normal play.

## Local rebuilds removed on September 19, 2026

- Dragging one or several flora placements used to destroy and recreate every
  flora presentation on every pointer movement. It now moves the affected
  renderers, repairs only their spatial render batches, and retains every other
  tree object and selection handle.
- Rerolling one planted flora group used to refresh the complete forest. It now
  replaces only the members of that group.
- Placing or removing a coal mine, quarry, or Brickworks used to invalidate the
  district composition and call the complete world builder. Each operation now
  refreshes only its industry presentation and invalidates navigation.
- Lot placement and movement, road placement, bridge approaches, road deletion,
  and mixed-selection deletion could rebuild the district-wide fine grid. They
  now commit affected terrain samples, collider data, decals when needed, and
  local presentations without recreating the grid.
- Completing a selection drag used to run road repair and a road-layer scan even
  when the selection contained only flora or Lots. Road work now runs only when
  a road actually moved.

## Remaining bounded or scaling work

- Moving selected roads still scans the road collection in `RefreshRoads`, but
  retains every unchanged road object. Replace that scan with explicit old/new
  cell deltas if dense-road profiling shows it is material.
- `DistrictCompositionKey` serializes or visits spatial collections after edit
  boundaries and when deciding whether a recomposed district screen needs a
  world build. It does not itself rebuild the district, but it is linear work and
  should be replaced by maintained spatial revision counters.
- Coal presentation refresh currently recreates the coal-resource layer rather
  than one deposit. Quarry and Brickworks refreshes already retain unaffected
  instances. Coal should gain the same ID-indexed presentation map.
- River presentation refresh remains district-wide because junction meshes and
  district-edge clipping depend on neighboring river paths. A future local river
  graph may bound this to the edited river and connected junctions.

## Review rule

The old ambiguous `Build`, `RefreshFlora`, `RefreshRivers`, and general surface
commit APIs no longer exist. `RebuildEntireDistrict`,
`RebuildAllFloraPresentations`, and `RebuildAllRiverPresentations` require an
explicit `DistrictBulkRebuildReason`; passing `None` throws. Architectural tests
also reject reintroduction of the old APIs. Any new bulk call requires a
documented reason and representative profiling. Otherwise the edit must use an
ID or cell lookup and update only affected spatial buckets and presentations.

## Validation

- A synthetic dense district with 5,000 trees measured 5,265.29 ms for a
  complete flora refresh and 14.74 ms for moving ten trees through the bounded
  presentation path. The other 4,990 tree presentation objects were retained.
- A second dense fixture with 1,600 existing trees and 12 additions measured
  127.36 ms for incremental insertion versus 2,112.70 ms for the explicit bulk
  flora path.
- The read-only Riverdale road fixture measured about 5 ms for model deletion
  plus nine-cell artwork repair and 266.39 ms for the bounded terrain/collider
  commit. It updated 4,225 terrain samples without rebuilding the fine grid.
- Focused validation passed 2/2 architectural policy tests, 60/60 flora
  batching/road/quarry/Brickworks tests, and 2/2 dense performance tests. No
  player district or Lot was saved.
