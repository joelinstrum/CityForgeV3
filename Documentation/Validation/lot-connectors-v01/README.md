# Lot Connector V01 validation

The Lot Editor has a first-class **Connector** category and a Dirt Entry card.
The saved piece is 5 × 5 metres, centered on a cardinal Lot edge, supports
pedestrians and wagons, and exposes access points 2.5 metres on either side of
the boundary.

Focused isolated Unity tests cover all four cardinal access transforms,
quarter-tile dimensions, worker/wagon permissions, edge placement limits,
stable JSON round trip, object-registry lookup, tool-click ownership, and a
runtime presentation that crosses the boundary without enlarging the Lot.
Related navigation, circulation, and older outside-road-connector tests were
included in the final focused run.

An isolated Unity render of a south-edge connector on a 40 × 30 metre fixture
was visually inspected. It uses the existing dirt-road texture and crosses the
green Lot boundary cleanly. The fixture did not load or save a player Lot.

The future Lumberjack Camp can query these access points, but worker and wagon
routing to a completed camp is not implemented or claimed here. No dense
district performance claim is needed: presentation rebuild iterates only the
connectors in one Lot at normal Lot load/edit boundaries; there is no per-frame
district scan.
