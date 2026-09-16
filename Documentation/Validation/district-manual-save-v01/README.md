# Manual region/district saves — 2026-09-16

Supersedes the automatic checkpoints described in `../district-flora-performance-v01/`.

- Added SAVE beside INDUSTRY in the district's top menu row, using the existing button component. The region map uses the same explicit save action.
- The button saves the entire open region, including all districts, and briefly displays SAVED. A failure displays SAVE FAILED with the error in its tooltip and allows retry.
- Removed simulation timers, event-driven disk writes, and saves on district exit or application quit. Edits and undo remain in memory; flora, climate, and river generation no longer write automatically. Creating a new region also remains in memory until Save.
- Editor undo snapshots remain independent from persistence. Existing save data and atomic file replacement are unchanged.

Live validation uses a separate scratch save folder and restores the user's region afterward. It checks that edits, undo, active simulation beyond the former five-second interval, generation, and leaving the district create no save files; then submits the actual regional and district Save buttons and reloads the results. See `results.txt` and the review helper source for the checks.

Implementation was synchronized to the isolated review using checked incremental patches with backups in `RecoveryBackups/manual-save-v01`. The main project was not modified. No commit or push was made.

All live checks passed. Unity compilation and `git diff --check` passed. The Save button placement was visually checked in Testy / District 9.
