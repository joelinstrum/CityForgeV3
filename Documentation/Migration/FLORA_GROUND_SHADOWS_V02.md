# District flora ground shadows v02 — 2026-09-12

Supersedes prior visual acceptance of the upright sprite projection. Joe still saw
no useful shadows on default grass at close zoom. Checked his actual live district
(2597 flora shadows, Noon), without replacing the region or changing saved placements.

District shadows now use MeshRenderer silhouettes, explicitly projected to a horizontal
receiver plane using the shared sun ray and original sprite alpha/UVs. The dedicated
DistrictFloraGroundShadow shader draws that geometry without further vertex projection.
Depth testing remains LEqual; the existing road-stencil pass remains. Original tree
textures, brightness, and the Lot Editor projection path are unchanged. Generated
meshes have an owner component for cleanup on flora refresh. The cached live material
is explicitly switched to the new shader.

Visual acceptance: same camera, close view of Joe's actual default grass, shadows ON
and OFF. ON has clear cast silhouettes beside and behind the trunks; OFF restores plain
grass there. Screenshots are CityForgeMCP/artifacts/flora/shadows-ground-v02/
ground-mesh-final-on.jpg and ground-mesh-final-off.jpg. No framebuffer comparison was
substituted for visual inspection. Earlier test-grove results were insufficient.

Editor-only City Forge / Flora / Shadow Comparison On and Off toggle visibility without
changing saved district data or the camera. The standard flora paint QA remains available.

## Saved district orientation fix
Joe reported continuing missing shadows and diagonal slicing after reload. Found that
BuildCamera created an identity-rotation camera, then RefreshFlora copied that rotation
to saved trees; ApplyCameraPose was called only afterward. This differed from manually
refreshing/planting trees in an already posed district, invalidating prior acceptance.
Build now calls ApplyCameraPose immediately after BuildCamera, before creating flora.
Regression menu Refresh District Flora Pose rebuilds the current district from the same
in-memory data, reapplies zoom/pan, and asserts camera alignment for every tree.
Verified test district creation + populated district rebuild: alignment PASS, edges intact,
shadows visible. Screenshot district-reload-verified.jpg. This turn used the test district
because Unity was outside Play when verification began; no user save was replaced.
