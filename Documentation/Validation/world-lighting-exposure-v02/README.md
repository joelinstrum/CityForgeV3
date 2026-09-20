# World lighting exposure V02 validation

Date: September 20, 2026

## Change

Reduced the shared noon sun intensity to 0.64 for district, ordinary standalone
Lot, and native-3D Lot environments. Ambient color, sun color and direction,
screen tint, shadow strength, and every non-noon preset remain unchanged.

District artwork previously received approximately `(1.37, 1.374, 1.368)` at
full noon illumination before texture multiplication. Values above display white
clipped bright foliage and compressed baked texture contrast. The calibrated
sum is approximately `(0.96, 0.970, 0.978)`, retaining a bright noon while
preserving highlight headroom. This changes shared uniforms only and adds no
per-object update, renderer, material, or shader pass.

## Automated validation

- Unity 6000.1.12f1 script compilation: passed without C# compiler errors.
- `WorldLightingContractTests`: 5/5 passed, including the new noon highlight-
  headroom guard.
- `UiFoundationTests.NoonUsesHighHardSunWithoutWashoutExposure`: 1/1 passed.
- `git diff --check`: passed before commit.

Raw NUnit reports are stored beside this file. Visual acceptance remains pending
in the isolated `CityForge-Regions-Review` editor across representative terrain,
buildings, individual trees, and grouped trees.
