# District undo v01 — 2026-09-12

Joe accepted rectangle selection: “The selection rectangle is working very well.”

Ctrl+Z (also Command+Z on Mac) restores one completed district edit per key press. Keep five prior district snapshots in memory; clear them when leaving or switching districts. No redo or disk-persisted undo history. This does not add undo to the separate Lot Editor.

Existing district save boundaries now capture immutable JSON snapshots. This includes group deletion and movement, flora placement/strokes/rerolls, roads, lots, rivers, founding, district designation/name and lighting. No-op saves and selection/camera changes consume no entries. Name entry commits on completion rather than on every character. A snapshot restores IDs, transforms, object properties and treasury together, then saves and rebuilds through the normal district loading path. Selection outlines clear after restoration.

Physical GetKeyDown owns the shortcut to avoid double undo from UI events and auto-repeat. UI consumes the shortcut only outside text fields. Ctrl/Command+Shift+Z does not undo. Active pointer gestures block undo. Accepted flora artwork, materials, brightness and terrain decals are unchanged.

Validation: Unity compiled; four focused EditMode tests passed (five-entry bound, no-op/new branch after undo, district reset, mixed objects/treasury restoration). Runtime QA deep-copied the loaded saved district and used a temporary save folder; mixed deletion, exact restoration, saved-file reload, empty history and no-op checks passed. All user region hashes unchanged. Physical Ctrl/Command+Z and docked visual review were not separately verified in this pass; runtime restoration was exercised through the QA menu. Evidence: CityForgeMCP/artifacts/district-undo-v01.

Joe acceptance of undo pending.
