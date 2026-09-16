# New England barn validation

Validated in the isolated CityForge-Regions-Review Unity 6000.1.12f1 project.
Two focused EditMode tests cover original texture bindings, grounding, 7m height,
footprint, thumbnail, matching auxiliary-pass transforms, placement, rotation,
and session serialization. Live Unity screenshot records the barn over grass.
Original archive files verified byte-for-byte; SHA-256 manifest included.

The broader existing iron-fence test fails after nudging because it reuses the
renderer destroyed by the existing prop rebuild (UiFoundationTests.cs:4442).
The failure is archived; that unrelated test is unchanged. Focused checks instead
compare the existing iron and picket fence models with all three render passes.
No district performance or long-duration stress test was performed for this intake.
No user progress saves were written. Main shared project was not modified.

## Scale revision

Barn dimensions increased uniformly by 1.4: height 7 → 9.8m; footprint
8.11 × 10.06 → 11.354 × 14.084m. Existing test expectations updated.
The archived Unity screenshot/results above describe the original 7m intake.

The 9.8m revision was synced to the isolated review project with targeted
constant edits. Unity compilation and both barn tests passed, including
updated dimensions, grounding, matching render passes, and save round-trip.
Existing iron/picket fence render-pass alignment also passed. Results are in
`scale-revision-results.txt`.
