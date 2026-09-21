# Forest cluster edge and shadow validation v02

Date: September 21, 2026

## Corrected diagnosis

The prior v01 check used six individual-tree sprites and did not reproduce the
reported V03 forest-cluster path. The V03 family composites contain a broad
chromatic antialias fringe, while their shadows bypass the texture projection
shader entirely. Cluster shadows instead used five or nine opaque procedural
trunk strips whose normalized positions described older V01 compositions.
Those stale strips are the detached dark rectangles in the close screenshot.

## Shared repair

- Every forest cluster now casts one feathered directional footprint derived
  from its shared root and sprite bounds. There are no per-tree coordinates,
  per-family coordinates, or opaque synthetic trunk quads.
- V03 depth-shaded summer/spring family composites use one shared 0.50 alpha
  coverage cutoff. Their interiors are nearly opaque; clipping the sub-half
  coverage fringe removes chromatic edge spill without recoloring foliage.
- Individual trees return to their established 0.02 cutoff and texture shadow
  path. Older winter cluster art retains its 0.12 open-branch cutoff.
- The rejected v01 mip bias and blanket 0.08/0.12 material changes were
  reverted.

## Validation and performance

A clean isolated Unity import passed all 26
`DistrictFloraBatchesTests`, including all five legacy cluster identities,
compact/large family art, all seasons, axis-aligned sun rays, cached camera
orbits, staged time changes, local cell rebuilds, and the V03 cutoff contract.

A graphics-enabled isolated render used the actual V03 large deciduous,
mountain, and tropical composites. At close range their outer contours no
longer showed the chromatic fringe, and the ground presentation contained
broad feathered canopy/contact shade without detached rectangular bars or
shader errors.

Cluster shadow geometry falls from approximately 270 vertices for a
five-tree summer composition and 486 for a nine-tree composition to 42
vertices for either size (84–91% fewer). Ground registration falls from five
or nine bounded raycasts to one per cluster update. Updates remain confined to
creation, movement, seasonal replacement, and the existing bounded
time-of-day slices; spatial batches still rebuild only affected cells. No
per-frame scan, material walk, redraw, or player save was added.

An unrelated existing
`FloraSpriteRendererUsesNativeLitCutoutShadowReceiver` test still throws a
null reference while trying to locate its standalone Lot test renderer before
reaching its material assertions. The district-cluster suite and render do not
depend on that fixture.
