# Quiet city UI V02 validation

Implemented in the `lot-updates` worktree and selectively merged into the isolated `CityForge-Regions-Review` project. The primary `CityForge - V3` project was not modified or controlled. Review-only helpers and save routing were retained.

## Behavior

- District idle view: six icon-and-number counters, location, small Save/Menu controls, and two Build/Terrain intent buttons.
- Population uses a family portrait; resources use cached artwork. Hover or keyboard focus exposes full resource labels and units. Clicking population opens Statistics; resource counters open Resources.
- Build/Terrain starts in Select mode, opens a horizontal category tray, and opens tool options only after choosing a category. B/T shortcuts toggle the trays. Escape clears selection and armed tools; empty land closes an unarmed tray. Active placement remains armed until explicitly canceled.
- Selected lots show a compact summary/preview. Details, rotation, deletion, and instructions remain in an expandable foldout. Operational warnings remain in the same inspector.
- Industry is available through Build and Menu. Labor, Resources, Statistics, district information, pause/resume, and navigation remain available through Menu. Save is still explicit.
- Region tools use a compact Terrain tray and selected-district card; map layers remain available through the small top-left icon. Escape or the card close control clears district selection.
- Lot editor styling and behavior were not changed by this pass.

## Checks

35 focused tests passed, including quiet startup, local palette toggles, clearing armed placement, preserving viewport/header identity, live resource values/tooltips, and the existing stats, business, lot-site, and timber suites. See `tests.txt`. Unity compilation and `git diff --check` passed.

Live checks used connected UI Toolkit button events in the isolated review, plus screenshots. Verified idle, Build, Terrain, selection, menu, regional tray, and tooltip layouts. The region screenshot includes Unity Editor's preexisting “No cameras rendering” overlay; the regional map is UI Toolkit content.

## Performance

Testy District 9: 9,951 flora. Twenty Build open/close cycles took 56.25 ms total (~2.81 ms/cycle), retaining screen, header, and camera. The separate local-palette check retained the world object and Transform count. No district data scans or world rebuilds were added to palette changes or HUD refreshes.

After caching widget references, 1,000 fixed HUD refreshes took 10.34 ms total (~0.010 ms/refresh), down from 388.88 ms for the first V02 implementation that repeatedly queried the UI tree.

HUD widget references are bound when a district HUD is composed, retained through local UI changes, and cleared/rebound on region/district screen composition. Resource artwork is cached. Counters use the existing simulation aggregates and direct stock fields.

Short 180-frame Editor samples: previous UI median 8.07 ms, p95 24.63 ms, max 34.38 ms, mean 538.81 draw calls; quiet idle median 6.16 ms, p95 6.86 ms, max 15.66 ms, mean 533.86 draw calls. These include simulation/world/Editor work and differing selected-versus-idle panel state. They are not a controlled renderer benchmark or proof that the earlier lockup is resolved. Managed allocation measurements previously returned unreliable zeroes in this Editor setup; no zero-allocation claim is made.

Player saves were not written. Testy save modification time remained 2026-09-16 15:45:20. No commits or pushes were made.
