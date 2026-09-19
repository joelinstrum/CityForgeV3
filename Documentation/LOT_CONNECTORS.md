# Lot Connectors

Connectors are first-class Lot pieces that cross a Lot boundary. They are
separate from surface overlays, props, and the older road-traffic port markers.
Each saved connector has a stable instance ID, asset ID, cardinal edge,
position along that edge, and explicit pedestrian and vehicle permissions.

The first asset is `dirt-entry-v01`. It is a 5 × 5 metre dirt entrance centered
on the selected Lot boundary, leaving 2.5 metres inside and 2.5 metres outside.
It reuses the canonical `RoadsDirtV01/dirt-road-square` texture. The visible
tracks are wide enough for the forestry wagon, while the access contract permits
both pedestrians and vehicles. Placement snaps to the closest cardinal edge and
is accepted only within five metres of the boundary or within the 2.5 metre
outside apron. A second click on the same edge position selects the existing
piece instead of duplicating it. Delete removes the selected connector.

`LotWorldController.ConnectorAccess` returns the saved connector's inside and
outside points. Future Lumberjack Camp behavior should use this contract for
worker and wagon routing. This intake does not create that camp behavior, change
district labor, or infer connector placement from artwork. Lot rotation works
through the existing district-hosted Lot transform, and the connector does not
expand the Lot footprint or collision bounds.

Connectors are additive optional save data. Older Lots load with an empty list;
existing Lot IDs, rotation, Undo, and manual Save behavior are unchanged.
