# Main integration — 2026-09-15

Merged origin/main a63c834 (PR #10) into feature/dry-goods-native-v05.

- Adopted main's shared selectable-object contract, advisory rotation conflicts, industrial demolition, incremental deletion, larger Brickworks footprint, zoom improvements and river junction updates.
- Preserved cached labor navigation, idle navigation fast paths, R / Shift+R rotation, and cargo-safe wagon pose/destination updates. Connected these to shared inspector actions.
- Retained local migration notes and authoring tools; retired superseded selection/deletion implementations.
- Adjusted the road-routing test fixture to keep the road outside the doubled Brickworks footprint.
- Unity compile succeeded. 62 focused EditMode tests passed (2026-09-15 20:09:54–20:09:56 UTC).
- Live isolated fixture passed shared quarry/Brickworks selection, keyboard rotation, undo and incremental deletion. Original scene restored.

PASS: single-tree delete kept terrain meshes, remaining tree, and UI screen; no loader; undo restored tree. Delete including save: 4.91 ms. 30 navigation checks / 5,000 trees: former full-key work 571.06 ms, cached 0.004 ms. This is a CPU hot-path benchmark, not overall FPS.
