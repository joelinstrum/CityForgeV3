# Lumberjack and forestry cart in the Lot library

Reuses existing V3 artwork; no legacy port or canonical asset edits.

- `axeman-labor-v01`: Lot character entry using
  `Assets/Resources/Characters/AxemanLaborV01/Axeman.fbx`, original body and axe
  textures, embedded animation clips, and district presentation scale 1.85.
  Existing axeman thumbnail is reused. Generic Lot character placement,
  selection, movement and serialization apply.
- `horse-forestry-wagon-v02`: exposes the existing forestry wagon definition
  and horse/carriage presentation in the character library. The existing loaded
  lumber wagon remains a separate entry. No new forestry thumbnail was made.

These are Lot authoring/presentation assets. Placing them inside a Lot does not
register a district timber crew or connect a camp to production/delivery.
Existing district labor, routing and optimizations are untouched. Manual-only
persistence remains in effect. The lumberjack model contains Chop as well as
Idle/Walk; this change does not add a harvesting command to the Lot inspector.
