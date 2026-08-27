# Building Intake V2: source-derived shadows by default

All newly imported 3D buildings use schema
`cityforge-v3-hybrid-building-package-v2`.

V1 packages are legacy-compatible and remain loadable. Do not use V1 when
creating another building package.

## Required shadow workflow

1. Normalize and center the authored source mesh using the approved metric,
   door, foundation, and rotation contract.
2. Generate a simplified proxy from that normalized source using voxel remesh
   followed by restrained decimation. Preserve the complete silhouette,
   including towers, steeples, domes, chimneys, roof steps, and major porches.
3. Export the generated mesh as `CF_PROXY_BUILDING_GENERATED`. Required
   compatibility objects may share this source-derived mesh.
4. Set `shadow.projectionMode` to `projected-mesh`.
5. Inspect projected morning and afternoon shadows in-game from all four
   building rotations. A rectangular semantic-vertex shadow is not an
   acceptable substitute for an imported 3D building.

## Enforced package checks

`HybridBuildingPackage.Validate` rejects every V2 package that omits either:

- `shadow.projectionMode: projected-mesh`; or
- `CF_PROXY_BUILDING_GENERATED` in `primitive.requiredObjects`.

Asset-specific tests must additionally load the generated proxy and verify
that it retains meaningful source-derived geometry and the complete authored
height. Towered buildings should use the New England church and Norwalk
Courthouse tests as the minimum regression pattern.

## Orientation and scale gate

Before catalog publication:

- verify the formal entrance in the default in-game view;
- record the correct `frontFacingQuarterTurns` rather than expecting the user
  to rotate the building after placement;
- compare door, story, and total height against an approved scale reference;
- review the building beside at least one established game asset;
- rebuild the proxy after every scale correction.

The package is not ready for active-catalog registration until all of these
checks pass.
