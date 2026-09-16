# Riverbank V1 validation — September 16, 2026

- `editmode-results.xml`: 66/66 passed, completed 16:30:45 UTC.
- `live-checks.txt`: dated material/clipping/cache checks and two production Shape → Repair cycles for each saved-layout copy, including save/reload and undo. Earlier entries with fewer bank bands belong to intermediate candidates; final implementation uses all ten bands per river.
- Additional live checks passed: synthetic Shape and Erase UI Toolkit pointer events, deferred mesh/data/cache mutation, one cache commit on release, unaffected decoration retention, saved undo, full-vs-incremental decoration equivalence, deferred road commit and retained road objects.
- Normal windowed Game-view inspection used `ScreenCapture` from the running Game view, guarded against maximization. Final previews are in local Git-ignored `QA/RiverBanks/final-bend.png` and `final-closeup.png`. Intermediate captures are retained locally as iteration evidence.
- Three original V3 save JSON SHA-256 hashes matched before/after. Fixtures restored. Separate Review editor and worker/labor code untouched.
- No new physical OS-mouse input validation; scripted pointer checks are distinguished from mouse input. Initial synthetic QA had duplicate empty river IDs, corrected before valid visual checks. A later UI gesture sequence ran before district loading completed; rerunning on a loaded isolated fixture passed. Neither issue altered user saves.

The implementation is ready for Joel's visual review. Reference material and exact generation prompts are in `../../RIVER_BANKS.md` and `../../RIVER_BANK_MATERIAL_STUDY.md`.
