# Shared world-lighting contract validation

Validation ran in an isolated copy of CityForge V3 with Unity 6000.1.12f1.
No player Lot, district, or region was saved.

- 57/57 combined focused EditMode tests passed for the shared contract,
  hosted-Lot sun ownership, Town Center/native building emission, roads,
  rivers, terrain, garden artwork, grass patches, natural resources, quarry,
  quarry-crane, and existing garden geometry.
- After the final snow, automata, generic-color, and fountain shader conversions,
  the 4/4 world-lighting contract tests passed again. The broader 7-test command
  also exposed an unrelated stale snowfall-size source assertion; the current
  snowfall implementation already used different foreground/background ranges
  before this work and was not changed.
- Every active shader named by `WorldLightingContractTests` loaded and reported
  supported in Unity's built-in renderer.
- `git diff --check` passed.
- The open CityForge V3 editor recompiled without new C# or shader errors.

An earlier class-level run reached Unity's existing native `Camera.Render`
crash in
`UiFoundationTests.ExperimentalBrownstoneCastsMorningAndAfternoonShadowsOnAllReceivers`.
That run was discarded; the focused runs above excluded the preview-capture
test and completed normally.

The test fixture was disposable. CityForge-Regions-Review was not synced,
restarted, or modified.
