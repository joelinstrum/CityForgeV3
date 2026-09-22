# Time, light, camera, and seasonal flora validation

Date: September 22, 2026

## Scope

This pass covers district edge panning, the compact calendar/time controls,
manual-save feedback, expiring hover help, shared Morning/Afternoon/Evening
lighting, and the V04 autumn/winter forest-cluster derivatives.

## Automated evidence

Tests ran in the isolated copy-on-write fixture
`/tmp/cityforge-time-light-qa.CsqOcO`; the open City Forge V3 editor was not
driven or restarted.

- 77 distinct focused EditMode tests passed with no failures: 26 forest batch
  tests, 9 lighting contract tests, 30 focused UI/camera tests, and 12 map
  chrome/calendar/save tests.
- Unity imported all eight V04 seasonal PNGs and their new metadata in the
  isolated fixture.
- A larger `UiFoundationTests` headless attempt reached the existing native
  `Camera.Render` preview test and exited with signal 139. This is the known
  headless graphics limitation documented by the project and is not counted as
  a product failure; the non-rendering subset passed separately.

## Visual and numeric checks

- All eight V04 files are 1254 x 1254 RGBA PNGs with transparent backgrounds.
  Contact-sheet review confirmed that autumn and winter retain the V03
  staggered depth/diamond layouts rather than the V01 single-row layouts.
- Autumn art uses muted ochre, russet, dull gold, brick red, olive, and subdued
  evergreen color instead of the previous uniformly vivid palette.
- Morning sun elevation is 36 degrees, shortening cast shadows; its shared key
  is softened and ambient fill is 25% higher. Afternoon ambient fill is 25%
  higher. Evening retains a visible blue-green ambient/background floor.
- Edge panning now uses 12.5% horizontal activation bands (half the prior 25%)
  and derives motion from the actual camera orthographic size at 8% of visible
  half-height per second. It no longer depends on stale zoom-step state.
- Calendar tests verify Winter to Spring increments the displayed year, a year
  click performs four normal season boundaries, and the clock follows the
  five-preset sequence.
- Save tests verify that only the explicit district Save button writes and that
  a successful write presents `District Saved`.

## Performance and persistence audit

The camera change is constant-time arithmetic. Calendar and clock controls use
direct district references and existing bounded season/time transition paths.
Lighting remains shared state published only at environment initialization or
an explicit time change. Seasonal artwork continues through the existing
cached resource catalog. No per-frame district scans, all-object comparisons,
material walks, full presentation rebuilds, or autosave paths were added.

No player-content save was issued during QA. `CityForge-Regions-Review` was not
used. The eight pre-existing river `.png.meta` whitespace changes remain
outside this work.
