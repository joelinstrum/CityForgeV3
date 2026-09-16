# Main merge validation — 2026-09-16

Merged origin/main at 380c54c into lot-updates after 4ce1894.

- Resolved Terrain modal conflict by retaining Flora/Climate alongside main's Major/Large/Small river drawing, river removal, and national-pike controls. River generation uses Varied flow and copies all climate/flora settings.
- Removed incoming automatic persistence from road creation, river drawing/removal, and district sculpting. These edits remain in memory until the shared Save button is pressed. Updated action/error text accordingly. This supersedes automatic-save descriptions in the incoming RESTART_HANDOFF and road/river documentation.
- River regeneration now copies terrain settings rather than resetting climate and flora metadata.
- Preserved main's surface caching, road updates, river geometry, map layers, and undo gesture guard alongside this branch's flora batching and economy changes.

Validation: merged runtime and complete EditMode test assembly compile using Unity 6000.1.12f1 Roslyn and the isolated review project's existing Unity references, with outputs under /tmp/cityforge-merge-validation. Runtime has one existing unused-field warning for _roadTestVehiclesExpanded; no compile errors. Source audit confirms runtime region persistence is called only by the explicit Save callback. Merge-resolution whitespace/conflict checks pass. The full staged diff also reports pre-existing trailing whitespace in incoming Unity metadata and an extra EOF blank line in NATIONAL_PIKE_DIRT_ART.md; these upstream files are preserved unchanged.

This is compilation and source validation, not a new execution of Unity tests or a live UI/performance check. Neither running Unity project nor user saves were changed. Incoming editor QA helpers that assume automatic persistence require explicit Save actions when reused.
