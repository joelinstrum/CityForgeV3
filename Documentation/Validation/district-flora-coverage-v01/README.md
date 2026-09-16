# District flora coverage

District Terraform → Flora → Forest opens a district-only coverage modal. The Trees/Stone placement modal also has a Coverage tab; the coverage modal retains access to Trees and Stones. Sparse and Wooded choices share the same component as regional Terrain → Flora.

District coverage inherits regional climate. Desert disables Sparse, Wooded and Generate; tropical harvesting limitations are shown as in the regional menu. No district climate override is introduced.

The existing RegionFloraGeneration planner now accepts an optional district scope. Scoped generation never assigns regional settings or touches neighboring district trees/metadata. Coverage choice and seed persist on each district. Full-region generation sets those fields for every generated district so subsequent district menus show the correct selection. Both scopes use the same planting mask, palette, preservation and transactional save logic.

The district UI schedules generation and commit separately, allows cancellation before commit, and records the undo boundary only after persistence succeeds. Successful generation refreshes district flora and invalidates only that district's harvest index. It does not rebuild unrelated districts or reset buildings, deliveries or neighboring labor. Existing manually planted and harvested trees are retained. Save failure restores tree lists and coverage metadata without creating an undo entry.

## Validation

39 model/regression cases passed in the isolated Regions Review Unity editor. New cases verify target-only generation, untouched neighbor serialization and harvest index, regional-setting preservation, per-district coverage save/reload, Undo snapshots, failed-save rollback, foreign-target rejection, Desert restrictions, and metadata updates after regional generation. Existing climate/flora, rivers, regional controls and region-deletion cases still pass.

The editor harness directly invokes NUnit test methods and parameterized cases with setup/teardown. Live UI tests use a disposable two-district region and an isolated scratch save root. They exercise the Forest flyout button, Desert controls, Coverage/Trees/Stones navigation, cancellation, generation/persistence and the actual district Undo command. Results and a visually reviewed screenshot accompany these notes.

Source changes were applied to the isolated review through checked patches after backups, preserving review-only differences. The user's Testy region is not populated with test-generated trees. The main CityForge project is untouched. This is not a long-duration simulation stress test; earlier large-region persistence measurements and the unresolved lockup limitation remain applicable.

Final live checks passed: Forest opens the modal, Desert gates choices, all three tabs work, cancellation preserves the target, Wooded generation saves/reloads (530 trees in the fixture), and the actual Undo command restores the exact paused district snapshot. The test fixture pauses after district entry, which otherwise resets the pause flag. The review is restored to Testy / District 9 with the coverage modal open. Unity compilation and `git diff --check` passed. All changes remain uncommitted on `lot-updates`.
