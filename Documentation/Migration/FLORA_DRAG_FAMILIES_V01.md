# District flora family painting — 2026-09-11

Choose Tropical, Deciduous, or Fir and Mountain, then PAINT FAMILY GROUPS.
Click places a group; holding the left button and moving paints more groups.
Release ends and saves the stroke. Tab rerolls the complete latest stroke within its family.
PAINT SINGLE TREES supports the same drag behavior, with one tree per stamp.
Individual species and stones retain single-click placement.

Arming placement now explicitly selects Terraform / Flora / Trees. Buttons use short,
family-independent labels so Fir and Mountain does not produce an oversized action label.
This hardens the reported fir placement failure; its original mouse/UI cause was not
conclusively reproduced. All fir resources are present and actual fir placement now passes.

Brush spacing: 24 metres for groups, 5 metres for singles; maximum 8 stamps per pointer
move. Holding still does not repeat stamps. Road/water exclusions remain enforced.
Pointer release, leaving the viewport, or moving onto a button ends the stroke.
Each stroke shares one GroupId; rendering refreshes after new stamps and saves on finish.

Validation: Unity compiled successfully. City Forge / Flora / Check Mountain Paint
runs only from a fresh Play-mode splash and uses the disposable district-scale fixture.
It verifies family arming, every mountain texture, click placement, stationary stability,
drag placement, one group, family purity, termination, and renderer counts after deferred
destruction completes. Result: click=12, stroke=35, rendered=35. Visually inspected in
normal docked Game view. This invokes brush methods; it is not an automated physical
mouse-drag test. Existing family picker/reroll checks are in FloraFamilyQa.

Code: CityForgeApp.RegionEditor.cs; editor-only CityForgeApp.FloraPaintQa.cs and
Assets/Editor/FloraPaintQa.cs. Evidence: CityForgeMCP/artifacts/flora/drag-families-v01/.

## Selection transition fix
The district family tabs previously filtered the catalog without updating the active
placement ID. Selecting Fir after a stone therefore retained the stone. District tree
category/family selection now arms the selected family immediately, clears the individual
ID, and defaults to group mode unless single-tree painting was active. Stones restores
the last chosen stone (Mostly Large initially). Lot catalog filtering is unchanged.

Extended Check Mountain Paint exercises stone restoration, switching Trees, and an actual
NavigationSubmitEvent targeted at the Fir family button after selecting a stone.
Passed in docked Play view: click=12, stroke=33, rendered=33, all Mountain, no stones.
The test checks brush methods after the button event, not a physical mouse drag.

## Seasonal Fraser and district repair parity
Snowy Fraser resolves to fraser-fir-small outside Winter in the shared presentation
and resource resolvers. Winter retains the repaired snowy asset. Saved IDs are unchanged.
Districts currently request Summer presentations; this change does not add district
season progression. Lot season switching already supplies the actual season.
District sprites now call FloraTreeRepairs.Apply, matching the existing Lot correction
(including Cilician exposure 2.2, previously omitted in districts). Original art untouched.
QA passed: seasonal resources for every SeasonPreset, winter-only snow identity,
Cilician renderer exposure 2.2, placement/drag and renderer counts (9 / 31 / 31).
Screenshot: CityForgeMCP/artifacts/flora/drag-families-v01/seasonal-fir-pass.jpg.

## 2026-09-12: remove per-tree brightness
At Joe's request, removed species-specific exposure values from FloraTreeRepairs
and removed the _FloraExposure property, uniform, and multiplication from the sprite
shader. Both lot and district sprites now use the artwork without this brightness
multiplier; old material property blocks cannot apply it. Removed obsolete QA assertion
requiring boosted Cilician exposure. PNG files unchanged. Saturation, opacity, geometry,
season selection, and shadow code unchanged. Shadow regression investigation deferred.

## 2026-09-12: district ground shadows
Reproduced near-invisible flora shadows on fresh default grass. Shadow renderers,
opacity, sun rays, and receiver height were valid. At the 20-degree camera pitch,
projecting the tilted billboard nearly canceled its ground-plane extent at noon.
Districts now request _UprightSource=1 in ProjectedFloraShadow: reconstruct a vertical
source from the original local vertices, root, scale, and horizontal right direction.
Lot projections default to 0, retaining their previous geometry. Local-space projection
requires DisableBatching=True. Original depth test, silhouette alpha, receiver height,
road pass and opacity retained; diagnostic shader changes removed. No tree brightness
settings or artwork changed.
Verified in Unity Play: default grass now shows tree silhouettes attached at their roots
at noon, LOD1. Flora QA also asserts enabled shadows with upright projection and opacity.
Visual evidence: artifacts/flora/tree-repairs-v02/shadow-upright.jpg in CityForgeMCP.
