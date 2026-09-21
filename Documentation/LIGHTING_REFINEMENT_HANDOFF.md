# Lighting refinement handoff

Continue City Forge V3 in `/Users/joelinstrum/dev/CityForge - V3` on
`feature/lighting-refinement`.

Start by verifying the branch, HEAD, worktree, stashes, and current Unity compile
state. Read `AGENTS.md`, later-first `Documentation/RESTART_HANDOFF.md`, and
`Documentation/WORLD_LIGHTING_CONTRACT.md` before changing code. The branch
starts from the parks/gardens checkpoint plus the latest `origin/main`, including
the larger world-space default grass texture.

## Problem to solve

Refine the global district lighting and color response. Do not tune individual
Lots.

The September 20 screenshot at
`/Users/joelinstrum/Desktop/Screenshot 2026-09-20 at 1.35.36 PM.png`
shows two presentation families that do not yet look as though they inhabit the
same world:

- Older hybrid buildings are camera-facing directional renders. Their albedo,
  exposure, ambient occlusion, and much of their shade are baked into the PNG;
  `AlwaysVisibleBuildingSprite` intentionally uses `Lighting Off`. Pale siding
  is therefore already gray in the source pixels, and the registered daytime
  shade overlay further darkens it (`DirectionalShadeOpacityFor(Noon)` is 0.42).
- Native 3D buildings such as the Town Center use Unity's Standard lighting and
  receive the actual district environment.
- Converted artwork shaders use `CityForgeWorldLighting.cginc`. At noon the
  artwork path currently adds ambient RGB around 0.32–0.37 to sun intensity
  1.05 without an exposure-normalization step. Bright terrain and foliage can
  consequently reach or exceed display white, exaggerating saturation and the
  contrast with baked hybrid buildings.

## Desired outcome

Create one coherent, measurable exposure/color contract across native 3D
geometry, custom-lit terrain/flora/roads/rivers/props, and hybrid directional
building artwork. Whites should read as white in daylight without flattening
shadows or bleaching the landscape, and authored colors should remain lively.
Morning, noon, afternoon, evening, and night must remain distinct.

Use a global calibration for each representation family, not per-building or
per-Lot exceptions. Prefer bounded or exposure-aware shared lighting over raw
ambient-plus-sun multiplication. Decide deliberately how the hybrid sprite base
and its registered directional shade pass map into the same target exposure as
native geometry. Preserve full-night artwork and genuine emitters such as
windows and lamps; ordinary surfaces must not become emissive.

## Constraints

- Follow the district performance rules in `AGENTS.md`: shared uniforms,
  cached values, and local updates only. Do not introduce per-frame district
  scans, material walks, redraws, or presentation rebuilds.
- District and region persistence remains manual-only. Never save a player Lot,
  district, or region during validation.
- Preserve hybrid proxy registration, directional facing, selection, shadows,
  rain/wet reflection behavior, seasonal artwork, workers, and labor behavior.
- Validate representative pale and colorful hybrid buildings beside the native
  Town Center, trees, grass, roads, and water at all five time presets.
- Use an isolated fixture for destructive or graphics-capture QA. Work only in
  CityForge V3; do not sync or restart CityForge-Regions-Review.
- Record before/after screenshots and numeric contract tests. Commit validated
  work on `feature/lighting-refinement`; do not push or merge unless explicitly
  requested.

Likely starting points are
`CityForgeWorldLighting.cginc`,
`DistrictWorldController.RegionEnvironment.cs`,
`HybridBuildingPresentation.cs`,
`AlwaysVisibleBuildingSprite.shader`, and the existing
`WorldLightingContractTests.cs`.

## Completed refinement — September 20, 2026

The global calibration is implemented without per-Lot exceptions. Custom-lit
artwork now uses a hue-preserving 0.98 display-white bound, and the physical sun
budget was reduced so native Standard-lit geometry targets the same range.
Hybrid directional bases receive one shared 1.5 daylight exposure with a soft
highlight shoulder; their noon registered shade opacity is 0.24. Dusk/night
base treatment, full-night images, windows, lamps, and other real emitters are
unchanged.

The contract is published through shared uniforms at the existing environment
transition boundary. No per-frame scan, material walk, draw call, rebuild, or
persistence path was added. Isolated Unity validation passed 7/7 contract tests,
8/8 focused regressions, and 10/10 river/environment regressions. Before/after
and five-preset captures plus numeric measurements are in
`Documentation/Validation/lighting-refinement-v01/`.

## Hosted native-building follow-up — September 20, 2026

District-hosted native buildings now receive one shared 2.5 indirect-diffuse
scale in Morning, Noon, and Afternoon. Evening and Night remain at 1.0. The
scale affects only the native building shader's indirect diffuse contribution;
source albedo, direct sunlight, specular highlights, terrain and artwork remain
unchanged, and ordinary surfaces remain non-emissive. It is published once at
the existing environment transition boundary, with no Lot iteration or material
walk.

The obsolete bundled Town Center remains readable only as a hidden compatibility
source for already-placed `town-center-civic-v01` instances. It is no longer
published to the founder or Civic creation catalogs; the authored player Lot is
the visible Town Center. No player file was loaded into the test fixture,
rewritten, or saved. Evidence is in
`Documentation/Validation/hosted-lot-lighting-v01/`.

## Garden-prop follow-up — September 20, 2026

Garden billboard art already consumed the shared world-lighting uniforms, but
native meshes within the same compositions still used Unity's uncalibrated
Standard path. The aged picket fence therefore became gray when it stood in a
building's directional shadow. Native garden meshes now use the shared
`GardenPropPBR` shader and the same daylight indirect-diffuse uniform as native
buildings. Direct light, shadows, albedo, billboard art, grass, and night
lighting remain unchanged; ordinary garden surfaces remain non-emissive.

Shader assignment is a bounded local walk when one garden presentation is
created or loaded. Materials stay shared, and time changes remain one global
uniform write with no district scan or material walk. An isolated fixture passed
20/20 focused lighting and garden checks. A graphics-enabled neutral-versus-
district comparison verified that the fence returns to aged ivory while flowers
and grass remain stable. Evidence:
`Documentation/Validation/garden-prop-lighting-v01/`.
