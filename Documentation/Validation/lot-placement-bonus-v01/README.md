# One-time Lot placement Bonuses

The Lot Editor exposes **Bonus** as a separate settings category beside Stats.
It can author non-negative one-time additions for all ten existing district
stockpiles: lumber, coal, stone, iron ore, gold, oil, food, jewels, cloth, and
bricks. The values round-trip with `LotSaveData`, deep-copy independently, and
default to no grant for older Lots. Apply changes the in-memory Lot draft; only
the ordinary explicit Save action writes it.

`DistrictLotSimulation.Add` is the new-instance boundary. Once an instance ID
has been accepted, its Bonus is added through the shared bounded stockpile
accessors. A repeated callback for that ID is ignored. Load/rebuild and saved
definition updates reconstruct cached aggregates without applying Bonuses;
removal does not claw resources back. A genuinely new second instance receives
its own grant. Values saturate at the existing stockpile integer limit.

This is independent of recurring per-season and per-delivery Benefits. In
particular, the lumber mill's operational lumber remains conditional on dynamic
tree-trunk delivery. Gold targets the district Gold resource, not Treasury
cash. The placement path updates one simulation profile and ten fixed resource
slots at most; it introduces no district enumeration, presentation rebuild,
per-frame loop, or automatic persistence.

Focused isolated EditMode results are recorded in `tests.xml`. Coverage includes
all ten resource slots, duplicate placement callbacks, removal, district reload,
definition edits, a second placed instance, JSON/copy compatibility, legacy
defaults, the Bonus modal's ten fields, and District Town Center founder grants.
No player Lot, district, or region was saved.
