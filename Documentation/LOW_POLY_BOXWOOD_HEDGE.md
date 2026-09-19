# Low-poly boxwood hedge study V01

Joel proposed a 1 × 3 m clipped hedge formed by a slightly uneven Blender mesh
and a leafy texture, without separate leaf geometry. After approving the visual
study, he requested it in-game. The **Boxwood Hedge** is now a Garden Library
item with new saved ID `low-poly-boxwood-hedge-3x1-v01`. It does not replace the
existing Georgian hedge mesh, texture, saved IDs, or seasonal tint.

The Blender source generator is
`ArtStudies/LowPolyBoxwoodHedgeV01/build_lowpoly_boxwood_v01.py`. It produces a
3 m long × 1 m deep × 1 m tall ground-anchored mesh with a shallow single-segment
bevel, small irregularities on the sides and top, and a level bottom. The mesh
has 310 Blender vertices, 312 polygons, and 616 triangles. Unity imports it as
422 vertices after UV splits, still 616 triangles. One opaque foliage material
and one image replace thousands of modeled leaves. The final UV projection
keeps the bevel from stretching the foliage into visible stripes.

`boxwood-foliage-v01.png` is a separate darker, neutral-green image edit of the
existing `GeorgianClippedHedgesV01/clipped-leaves.png`; the old source is
unchanged. The ArtStudies and Unity Resources images are byte-identical, and
the Unity import preserves 1254 × 1254 pixels. The Unity comparison uses a
Standard material with original study tint RGB `(0.34, 0.42, 0.31)`, low smoothness, and the
new image. The FBX and texture are at
`Assets/CityForgeV3/Resources/CityForgeV3/Garden/LowPolyBoxwoodHedgeV01/`.
The Garden card uses a copy of the approved Blender study render as its preview.
The local `.blend` scene sits beside the generator but is ignored by the game
repository's Blender-source policy; the generator and image recreate it.

`Validation/low-poly-boxwood-hedge-v01/blender-study.png` shows the Blender
render. `unity-comparison.png` shows the original isolated study: current
foliage/tint on a simple control block at left and the Blender study at right.
The actual existing hedge may have different geometry than the control.
`garden-placement.png` shows three final Garden presentations on temporary
Natural Grass with the supplied cobblestone, including a 90° turn. The Garden
ID, 3 × 1 m and 1 × 3 m footprints, 616-triangle mesh, ground anchors,
half-opacity placement preview and isolated `LotEditorSession` JSON round trip
passed. Unity compiled with zero assembly errors. The active Lot was not
changed or saved; temporary QA code was removed.

The new piece uses the ordinary `PlacedProp` placement, rotation, selection,
Undo, and manual Save routes. Opaque pieces share one instancing-enabled
material; the translucent placement preview uses a separate shared material
and per-renderer alpha. An isolated material check verified sharing, preview
opacity, and return to the opaque material. Physical pointer interaction in
the Lot Editor, disk save/reload, and dense-placement profiling remain to
check during play.
The earlier forest/regional test suites do not validate this Garden work.

## Forest-green correction

Joel's in-Lot screenshot showed the placed hedge much brighter and more lime
than the Natural Grass pieces. The runtime opaque and preview materials now
use RGB `(0.18, 0.25, 0.13)`, a darker forest-green grade. Mesh, UV, foliage
image, saved ID, placement and shared-material behavior are unchanged. A live
hedge rebinds its material on script reload, so the color change does not
require a Lot rebuild or Save. The Garden card retains the approved dark
Blender render.

`Validation/low-poly-boxwood-hedge-v01/forest-green-isolated.png` is an
isolated Unity render beside the Natural Grass source and supplied cobblestone.
The active Lot camera and placed hedge were unavailable during this check, so
the exact screenshot scene remains for Joel to inspect in-game. Unity compiled
with zero assembly errors; the temporary QA hook was removed, and no Lot was
saved.

## District lighting and final color adjustment

The district screenshot exposed that a hosted Lot used flat noon ambient light
while the district map used sky/equator/ground fill and a stronger sun. Hosted
Lot editing now uses the district ambient and daylight intensity settings.
The shared sun's Lot-specific orientation remains in place for projected
shadows. The boxwood tint is now RGB `(0.245, 0.345, 0.18)`: brighter than the
previous near-charcoal grade, with green still dominant and blue suppressed.
This changes only the shared runtime hedge material, not its saved ID, mesh,
foliage texture or old hedge pieces. The previous isolated captures predate
this adjustment; judge the final grade in a district-hosted Lot. No active Lot
was saved.

## Props catalog correction

The bright, protruding-leaf hedge in Joel's September 18 Lot screenshot is the
older `hedge-3d-v01` **Props → 3D Hedge** asset. It has a different model,
texture and saved ID from the approved dark Boxwood Hedge lining the Lot edge.
The Props catalog now offers the approved Boxwood Hedge and its matching preview
instead. The old ID remains loadable so existing saves are not rewritten; the
saved Garden Test Lot contains Boxwood Hedge pieces but no old 3D Hedge pieces.
An isolated EditMode check passed for the approved opaque material and preview
alpha. No player Lot save was changed.
