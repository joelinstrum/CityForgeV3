# Generic road-tile wagon delivery

DistrictRoadDelivery accepts any destination footprint (or point), searches the connected road graph, and chooses its nearest reachable road tile within 30 metres of the destination. Quarry/Brickworks and timber/Lumber Mill dispatch both use this service. No tree queries, water queries, per-axle collision searches, receiving bay alignment, or U-turn planning occurs in cargo delivery. Existing road tiles are authoritative. Initial off-road approach is limited to 30 metres; disconnected destinations retain cargo and retry. Road edits invalidate active routes. Return trips also stop at a nearby road tile, and saved mid-cell routes avoid unnecessary backtracking.

Movement uses the shared SetRoadDeliveryRoute helper and the existing wagon animation/acceleration controller. Cargo accounting, pause behavior, and reload persistence remain intact.

Validation: 7 quarry/delivery tests and 11 timber tests passed in the isolated Regions Review editor. Includes tree-independent routing, generic footprint destinations, disconnected roads, return trips, mid-cell reload, paused/deleted destinations, and exactly-once cargo accounting. Unity compilation and git diff --check passed. No claim of a completed long-duration performance soak test.
