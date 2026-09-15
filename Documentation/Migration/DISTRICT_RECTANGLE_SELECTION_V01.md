# District rectangle selection v01 — 2026-09-12

Select owns the entire rectangle gesture: pointer capture, deferred selection, suppressed competing selection/movement/deletion and camera keys, and a single commit on left-button release. Crossing UI or leaving the selection surface does not commit. Escape, capture loss, focus loss, or changing screens cancels and restores the previous set.

Selection uses the visible screen rectangle in any drag direction. All intersecting editable district flora (trees and stones), lot footprints, road cells, and river strips are collected together. Flora uses sprite-card bounds, including transparent padding; lots are selected as whole district lots. Generated terrain/leaf decoration is not an editable object. Clicking uses a small hit rectangle. Move is a separate Select-category tool so beginning on an existing tree or selection never accidentally moves it. Mixed river/object deletion refreshes all affected objects.

## Verification

- Unity EditMode: all five DistrictSelectionGeometryTests passed (reverse direction, isometric empty corner, crossing, containment, edge contact).
- Saved district city-033 in region 62cf8923205e4a2994561301f10c28d9 loaded from disk: 2,418 flora; decal/reload check passed with 21,768 patches and 2,082 aligned trees.
- Gesture contract on that saved district passed: 300 flora + 1 lot + 1 river; selection deferred until release, reverse drag identical, cancellation restored previous selection, Delete suppressed during drag, serialized data unchanged.
- Physical drag in normal docked Unity Game view: input trace confirmed tool=Select; after release active=False, move=False, 6 flora + 1 river selected. Automatic review initially rejected a drag as a potential Move operation; a fresh empty-selection preview and explicit input tracing resolved this safely.
- All six saved region SHA-256 hashes unchanged after testing. No flora artwork, exposure or brightness changes.
- Evidence: CityForgeMCP/artifacts/district-selection-v01 (geometry.xml, verification.log, physical-selection.jpg, save hashes).

Joe acceptance pending. Manual roads coverage and physical Escape/drag-outside-window coverage remain useful follow-up checks; current saved fixture has no district road cells. Selection geometry and method-level cancellation are tested.
