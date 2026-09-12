# Single-horse carriage v02

One chestnut horse is attached to the accepted carriage as a single selectable lot prop (`horse-carriage-v02`). The original unhitched v01 carriage and canonical horse assets remain available and unchanged. Choose **HORSE & CARRIAGE** in the 3D Characters library; select either visible part of the placed team and click ground to drive it.

The horse leads a forward-only path with a 3.25 m minimum turning radius. Circle arcs joined by tangents give continuous heading changes and allow U-turns; several arrival headings and turn directions are considered. The whole convoy's sampled sweep is checked against the existing static lot obstacles and terrain. If no valid route is found, the existing red destination indicator is shown. The path planner is designed for open lot-editor maneuvering, not road-network traffic or reversing into confined spaces.

The horse leads a 1.874 m shaft connection to the steerable front axle. The rear axle follows that front axle at a fixed 2.2055 m wheelbase. The front wheels and shafts steer together, while the body and rear wheels follow on their own heading. This is a lightweight kinematic follower, not a suspension/axle physics simulation. Body and driver remain rigid. The existing wheel controller measures traveled distance for each wheel; the horse's existing gait controller measures actual travel. Added leather girth, shaft straps and reins connect the team visually and follow the two headings.

The horse is positioned 2.05 m ahead of the carriage asset origin, between the original shafts. All three headings and the horse's position are saved in the lot. Active route progress and the independent carriage pose survive presentation rebuilds. Fresh loads restore the parked pose; they do not intentionally resume transient orders from another session.

Implementation: `HorseCarriageController.cs`, `LotWorldController.HorseCarriage.cs`, an updated reload-safe `CarriageWheelController.cs`, and the source changes in `integration.diff`. The v02 FBX/Blend derivative separates the front frame and shafts from the body while retaining all original faces and UVs. No canonical FBX or Blend file was overwritten. `*.before` files preserve pre-change sources.

QA: City Forge > QA > Carriage > Open One Horse Team runs a bounded turn/return demonstration in the normal docked Game View. It performs a presentation rebuild during travel and verifies pose/route continuity and serialized pose fields. This is an editor-only demonstration invoking production commands; it is not a physical mouse-input test. Final findings are in `game-validation.md`.

## Current template

The accepted team now also supports automatic road-loop Drive/Stop and saved
Slow/Fast speeds. See [Horse and wagon template](../Templates/HorseAndWagon.md)
for the complete current contract, asset lineage and instructions for future wagons.
