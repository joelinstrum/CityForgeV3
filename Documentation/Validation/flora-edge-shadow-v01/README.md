# Flora edge and projected-shadow validation v01

> Superseded September 21, 2026 by
> `Documentation/Validation/flora-cluster-edge-shadow-v02/`. This fixture
> exercised individual tree sprites, while the reported defect was in the V03
> multi-tree cluster path. Its diagnosis and visual acceptance claim must not
> be used as evidence for forest clusters.

Date: September 21, 2026

## Cause

The shared flora billboard and ground-shadow materials both retained pixels at
only two-percent alpha. On the upright artwork this exposed the green RGB
padding around transparent tree edges. On the strongly foreshortened ground
projection, implicit texture sampling could select a coarse mip whose averaged
alpha filled much of the transparent source rectangle, making the shadow look
like a detached dark card rather than the tree silhouette.

## Shared repair

- District and standalone Lot flora use an 0.08 shared cutout threshold.
- Seasonal district clusters use the same summer/autumn threshold; winter
  remains at its established 0.12 threshold for open branches.
- District ground shadows use a shared 0.12 threshold and a -1.5 mip bias for
  the existing single silhouette sample. Mipmaps remain enabled.
- Every tree retains its authored texture and pivot. There are no per-species,
  per-tree, or saved-placement overrides.

`fixed-close-grove.png` is a graphics-enabled isolated render of six different
tree families. Fine leaves remain present, while the projected silhouettes are
organic and meet their trunk anchors. The batch property check confirmed that
each shadow samples its source tree texture rather than a white fallback.

## Validation and performance

An isolated Unity project passed 40/40 focused flora-batch, afternoon-lighting,
world-lighting, and native-flora shader checks. The render completed without
shader errors.

The change modifies constants on the two already-shared cached materials and
the LOD selected by the shadow shader's existing texture sample. It adds no
texture samples, materials, meshes, draw calls, allocations, district scans,
per-frame updates, redraws, or rebuilds. Time-of-day changes retain their
bounded eight-tree shadow slices and one-cell batch rebuild schedule.

No player content was loaded or saved. The open V3 editor and
CityForge-Regions-Review were not driven or restarted.
