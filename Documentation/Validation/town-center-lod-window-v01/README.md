# Town Center distant LOD and window validation v01

Date: September 21, 2026

## Cause and repair

- The reduced `TownCenterFarShell.fbx` keeps its metre conversion and upright
  rotation on its root MeshFilter. The builder multiplied that transform by
  the same root's inverse, cancelling both. The saved shell was consequently
  100 times too small and flat, so only the unreduced interior/window pieces
  remained recognizable at district zooms. The builder now bakes the complete
  imported transform. The regenerated far shell is 14.20 m wide and 10.53 m
  high, matching the full shell within the intended simplification tolerance.
- Day glass now has a restrained cool, reflective tint instead of behaving
  like a nearly invisible opening into an unlit room. It remains transparent,
  non-emissive, and compatible with the existing real night emitters.
- The interior strolling-couple card now stays parallel to its authored facade
  instead of copying the pitched world camera rotation. Its existing
  eight-direction artwork selection still follows the view. Moving the card
  near the glazing makes the full walking artwork visible through successive
  windows while the room clip prevents it from drawing over the exterior.

## Evidence

- `close-front.png`: daylight glass is legible without hiding the interior.
- `distant-lod.png`: the reduced 3D LOD is upright, correctly scaled, and keeps
  the complete Town Center silhouette. This is a reduced mesh, not a billboard.
- `automata-sequence.png`: six successive frames show the couple traversing the
  upper windows without the former camera-induced thin slice.

## Automated checks

- `TownCenterTests`: 7/7 passed in an isolated Unity project.
- `WorldLightingContractTests`: 9/9 passed.
- `Building3DPackageTests`: 12/13 passed. The single failure is the unrelated,
  pre-existing `NYBrownstoneLight` expected ground offset (`1.72`) versus its
  current resource value (`1.34`); no Town Center assertion failed.

The graphics-enabled isolated capture completed without shader errors. No
player content was loaded or saved. The open V3 editor and
CityForge-Regions-Review were not driven or restarted.
