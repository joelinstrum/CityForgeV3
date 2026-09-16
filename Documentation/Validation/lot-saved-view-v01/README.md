# Lot save/load camera continuity

Lot saves previously omitted editor camera state. LoadLot recalculated the view
using ApplyCameraFacing, whose native-building default differs from the empty-lot
hybrid-package camera. Placement correctly preserved the original view, so saving
then reopening could visibly turn the lot without changing any building rotation.

Explicit Save now records the actual camera pose, orthographic size, pan, facing,
orbit, zoom mode, top-down orientation, and inspection mode in optional EditorView
metadata. Load restores that pose after rebuilding presentation. Metadata copies
with LotSaveData.Copy; no per-frame serialization or autosave was added.
Older saves remain supported and use a deterministic default, independent of the
previously opened lot's pan/orbit/top-down state. Their original unsaved camera
cannot be inferred; saving again records the desired view. Object rotations and
user save files are not migrated or rewritten automatically.

Validated in Unity 6000.1.12f1 in CityForge-Regions-Review:
- 2 actual temporary-file save/load round-trips into freshly built worlds, with
  farmhouse, oblique/top-down views, matching camera pose/zoom/pan, unchanged
  building rotation, and a subsequent pan without rotation.
- 1 old-save fallback check after changing the previous world's view.
- 4 existing native/hybrid placement and hand/keyboard pan continuity cases.

All seven passed; Unity compilation and git diff checks passed. Test fixtures
write only unique temporary directories, removed at completion. Existing user
saves and the separate main project were untouched. Targeted three-way merges
preserved review-only runtime differences. Changes remain uncommitted.
