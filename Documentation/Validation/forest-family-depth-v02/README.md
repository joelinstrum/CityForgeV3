# Forest family depth V02 preview validation

Date: September 20, 2026

## Scope

- Added versioned compact and large deciduous summer V02 billboard candidates.
- Routed only deciduous summer/spring groups to V02 for isolated visual review.
- Left V01 artwork, autumn/winter routing, other families, placement, scale,
  batching, and projected ground-shadow behavior unchanged.

These are transparent cutout billboards, not seamless/repeating textures. The
relevant boundary check is transparent canvas padding. Both PNGs are 1254×1254
RGBA. Maximum edge alpha is 1/255 for compact and 3/255 for large, below the
runtime and mip-preservation alpha cutoff of 0.02 (approximately 5/255).

## Automated validation

- Unity 6000.1.12f1 source-worktree import and script compilation: passed with
  no C# compiler errors.
- `DistrictFloraBatchesTests`: 24/24 passed, including V02 route/resource,
  alpha, seasonal, batching, and cluster-shadow coverage.
- `RegionFloraGeneratorTests`: 14/14 passed, including generated group resource
  resolution.

Raw NUnit reports are stored beside this file. Final artistic acceptance depends
on visual inspection in the isolated `CityForge-Regions-Review` editor.
