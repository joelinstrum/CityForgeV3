# Time and light adjustments handoff

Continue City Forge V3 in `/Users/joelinstrum/dev/CityForge - V3` on
`feature/time-and-light-adjustments`.

The branch was created from `origin/main` at
`9d439cfe3cea0a1c8e89b1776076399862eb9186` on September 22, 2026. Start by
verifying the branch, HEAD, worktree, remotes, and stashes. Then read:

- `AGENTS.md`
- `Documentation/RESTART_HANDOFF.md` — later entries take precedence
- `Documentation/WORLD_LIGHTING_CONTRACT.md`
- `Documentation/LIGHTING_REFINEMENT_HANDOFF.md`

At branch creation, the live Unity editor had made whitespace-only serializer
changes to eight river `.png.meta` files under `Water/River/BanksV4`,
`BanksV5Wide`, and `RiverBlueV01`. They are not part of the time-and-light work.
Preserve and inspect them rather than discarding, staging, or committing them
implicitly.

## Goal

Continue global time-of-day and lighting refinement from the current shared
world-lighting implementation. Hybrid directional-render buildings, native 3D
buildings and props, terrain, water, roads, flora, gardens, and other dynamically
lit artwork should look as though they occupy the same world across Morning,
Noon, Afternoon, Evening, and Night.

Make corrections at a shared environment or representation-family boundary.
Do not tune individual Lots, buildings, gardens, trees, or textures. Whites
should read as white without bleaching the landscape, authored colors should
remain lively, and each time preset should retain a clear identity. Ordinary
surfaces must remain non-emissive. Preserve genuine nighttime window, lamp,
torch, fire, and headlight lighting.

## Current contract to preserve

- The district is the sole owner of its sun, ambient settings, and shared shader
  lighting state. A hosted Lot cannot rewrite them.
- Shared uniforms are published at environment initialization or an explicit
  time change, not through per-object material updates.
- Custom-lit artwork uses the shared hue-preserving `0.98` display-white
  shoulder.
- Hybrid directional building art uses its shared daylight exposure and
  registered directional shade; dusk, full-night art, and real emitters remain
  separate.
- Native buildings and native garden meshes use shared daylight indirect
  calibration. Garden meshes use their family-wide exposure rather than asset
  color overrides.
- The saved time preset is published before terrain, flora, or Lots are first
  composed. Later flora-shadow transitions remain bounded and incremental.
- Forest-cluster edge coverage and root-anchored canopy shadows are intentional;
  do not restore old per-trunk shadow cards or generic per-tree workarounds.

## Performance and persistence constraints

Follow the district performance rules in `AGENTS.md`. Use shared uniforms,
cached state, maintained aggregates, bounded work, and local invalidation. Do
not add per-frame district scans, renderer/material walks, redraws, presentation
rebuilds, or all-object comparisons. Do not trade time-of-day quality for a
district-wide update path.

Lot, district, and region persistence remains manual-only. Never save player
content during testing. Do not add autosaves or alter worker/labor behavior.

## Validation and handoff discipline

Use an isolated fixture for destructive, persistence, or graphics QA. Do not
drive or restart the open City Forge V3 editor unless Joe explicitly asks, and
do not sync, restart, or use `CityForge-Regions-Review`.

Validate representative pale and colorful hybrid art, the native Town Center,
garden props, trees and forest clusters, grass, roads, and water across all five
time presets. Pair graphics captures with focused numeric contract/regression
tests. Confirm that time changes do not introduce district scans, material
walks, full redraws, or save writes. Record evidence under `Documentation/Validation/`.

Commit validated work locally on `feature/time-and-light-adjustments`. Do not
push, merge, or create a PR unless explicitly requested.

## September 22 implementation

The requested adjustment pass is implemented on this branch. It includes
camera-relative edge-pan speed, narrower horizontal edge bands, clickable
year/season/time controls, Winter-to-Spring year rollover, explicit save
confirmation, expiring hover help, shared lighting refinements, and V04
depth-staggered autumn/winter forest art.

Implementation and validation details are recorded in
`Documentation/Validation/time-light-adjustments-v01/README.md`; seasonal art
lineage is recorded in `Documentation/Migration/FOREST_FAMILY_MIX_V04.md`.
