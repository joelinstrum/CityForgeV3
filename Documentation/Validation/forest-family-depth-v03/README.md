# Forest family depth V03 summer preview validation

Date: September 20, 2026

## Scope

- Added six versioned summer V03 group billboards: deciduous, mountain/fir and
  tropical, each in compact and large compositions.
- Routed summer/spring groups to V03; tropical remains season-neutral.
- Preserved V01 autumn/winter art and all V01/V02 source assets.
- Kept saved IDs, placement, scale, renderer counts, materials, shaders,
  batching and projected ground-shadow behavior unchanged.

V03 strengthens baked inter-tree depth with three overlapping tiers, rear-tree
darkening, feathered crown-to-crown shade and deeper canopy/trunk occlusion. It
does not bake shadows onto the ground.

## Asset validation

All six runtime PNGs are 1254×1254 8-bit RGBA. The compact mountain image was
regenerated with a framing-only edit after the initial crown reached the top
edge; its selected output has zero alpha on all four outer edges. Other assets
carry at most 6/255 antialias fringe at an outer edge, effectively at the 0.02
runtime/mipmap alpha cutoff, with transparent canvas backgrounds.

These are cutout sprites, not seamless/repeating textures; transparent boundary
padding is the relevant edge validation.

## Automated validation

- Unity 6000.1.12f1 import and script compilation: passed without C# compiler
  errors.
- `DistrictFloraBatchesTests`: 24/24 passed, including V03 resource/alpha,
  summer/spring and tropical routing, batching, camera-facing behavior, and
  compact/large shadow-proxy coverage.
- `RegionFloraGeneratorTests`: 14/14 passed, including generated family-group
  resource resolution.
- `git diff --check`: passed before commit.

Raw NUnit reports are stored beside this file. Final artistic and shadow-contact
acceptance remains pending in the isolated `CityForge-Regions-Review` editor.
