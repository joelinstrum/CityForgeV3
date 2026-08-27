# Character Behavior Scripts

Character behavior is selected per placed character in the Lot Editor and saved
as a stable string identifier on `PlacedProp.BehaviorScript`. Each archetype also
has a lot-level default applied to newly placed characters.

## Native scripts

- `business-as-usual`: archetype-weighted ambient behavior.
- `harass-pedestrian`: a hooligan follows the nearest Victorian gentleman and
  stops at interaction distance.
- `evade-police`: a hooligan runs away while a historic policeman is within the
  ten-metre awareness radius.
- `fight-hooligan`: reserved, but unavailable until the hooligan asset has an
  authored combat/reaction animation set.

Only one script owns a character at a time. Manual arrow-key movement temporarily
overrides autonomous motion without changing the saved script selection.

## Future Python bridge

Lot saves must never contain executable Python. A future sandboxed adapter may
load reviewed Python from an authoring package and emit the same language-neutral
commands used by native scripts: acquire target, move toward or away, play state,
wait, and resume. The stable script identifiers and character archetype IDs are
the boundary between authored code and simulation state.
