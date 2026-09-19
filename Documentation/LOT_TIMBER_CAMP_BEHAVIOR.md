# Lot-owned Lumberjack Camp behavior

The Lumberjack Camp is now a built-in Lot behavior. In the Lot Editor, open
**Lot Behaviors → Add Behavior → Lumberjack Camp**. No JSON needs to be copied
or imported. The command discovers the Lumberjack characters, Forestry Cart,
and first vehicle-capable Connector already authored in the Lot, assigns their
stable instance IDs, and creates an editable
`cityforge-timber-camp-script-v1` document.

The portable script owns the actor bindings and forestry recipe: trees per
load, bundles per tree, harvest radius, unloading and retry timing, repeat, and
wagon speed. Each placed district Lot stores its own behavior instance and a
stable district crew binding. Rebuilding the district presentation reuses that
crew rather than creating another one. Removing the source Lot removes its crew
and workers. The authored Lumberjack and cart objects remain visible in the Lot
Editor as placement anchors; district presentation hides those prototypes and
uses the moving worker and wagon presentations driven by the crew state.

The script delegates capabilities to shared services. `DistrictHarvestIndex`
performs bounded nearby-tree queries; labor navigation supplies walking routes;
`DistrictTimberNavigation` supplies road and mill routes; existing character
and horse-carriage controllers present animation and movement. The script does
not scan all district flora or execute arbitrary user code. Current search and
route budgets remain unchanged.

A placed camp starts its script automatically. Its Lumberjacks claim reachable
trees within the configured radius, walk and chop, return raw trees to their
camp, and increment the crew's pending load. At the configured threshold the
Forestry Cart displays cargo, seeks a road-connected Lot containing the Lumber
Mill building, delivers timber bundles, and returns through the district road
network. The district's existing wage rules still apply. A camp without enough
treasury to staff its authored Lumberjacks reports that the crew could not
start. No progress is written until the player uses the normal district Save.

The live crew's camp and wagon-home point is the pedestrian Connector's outside
access point. Lot interiors are blocked in district navigation, so using the
authored character positions inside the Lot would strand the workers before
their first route. Binding also repairs older crew records whose camp or workers
are still inside this Lot. Workers already outside the Lot keep their current
positions and routes.

Validation used a copy-on-write temporary Unity project and did not open or
save a player Lot or district. Results are in
`Validation/timber-camp-lot-script-v01/`: 22/22 Lot script tests, 14/14 timber
routing/delivery/lifecycle tests, and 9/9 district labor tests passed. Coverage
includes automatic object binding, rejected missing actors/connectors, editable
script serialization, duplicate-free district binding, source-Lot crew cleanup,
tree delivery, reload, wagon routing, and payroll. A physical UI click-through
and live district visual cycle remain for hands-on review after the camp has the
new behavior attached and is manually saved.

September 19 follow-up validation used the same isolated project. The focused
road, connector, Lot-script, timber, Lot-simulation, forest-coverage and flora
batch suite passed 80/80, including repair of a previously stranded crew.

Older placed `lumberjack-camp` records can have `BehaviorsInitialized=true` and
an empty behavior list because they predate the built-in timber script. A
one-time, persisted compatibility check now discovers the Lot's authored
Lumberjacks, Connector, and wagon and installs the built-in behavior in memory.
The check accepts both the Forestry Cart and the earlier Horse & Lumber Wagon.
After it runs, explicitly removing the behavior remains respected. Disk state
still changes only through the normal district Save command.

Read-only QA loaded the actual Riverdale region and current Lumberjack Camp Lot
into the isolated editor. The migration created one crew and two workers, the
Connector outside point was walkable, harvestable firs were within the script's
search radius, and a worker left camp during ten simulated seconds. The original
Riverdale region file remained byte-identical.

When a camp wagon reaches dispatch capacity, its Lot-owned script now reports
whether the district routing service found a reachable Lumber Mill. The routing
service builds its receiver list once from the district's direct placed-Lot
collection and reuses the cached road graph; it does not scan every building on
each simulation tick. Adding, moving, removing, or reconnecting a Lot invalidates
that cache at the existing district-edit boundaries.

If no reachable mill exists, the cart keeps its load and the right-side warning
opens once for that blocked episode with **Lumber cart has no mill to drive to**.
**Place Lumber Mill** arms the saved Lumber Mill Lot through normal Build
placement, including its cost and requirements. Closing the warning suppresses
repeat interruptions until the blockage clears and occurs again.

The Lumber Mill now keeps the barge at its authored Lot-local position. River
placement translates the complete Lot toward the selected bank and may rotate
the shallow-draft hull to follow the channel; it no longer detaches the boat and
stores an unrelated water position. The loading script's object-relative dock
point rotates with the barge, so dockworkers finish at the actual hull. Legacy
position overrides remain readable but are ignored by the runtime presentation.
The mill must remain dry, the full barge footprint must remain in water, and a
downstream route must exist. The placement lookup uses bounded river and segment
spatial queries. Read-only Riverdale coverage accepted all 20 sampled riverbank
sides. Persistence remains manual-only.

The Lumberjack Camp wagon moves at twice the shared baseline cart pace. Shared
horse-cart routing detects a destination behind the team and builds a complete
forward turning arc before joining the return road route. The horse remains the
lead point throughout the maneuver; the forecarriage and rear axle follow the
same articulated kinematic contract.
