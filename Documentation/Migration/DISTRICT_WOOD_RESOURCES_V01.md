# District wood resource tally — 2026-09-13

Joe accepted the Labor prototype and requested a wood icon/tonnage total along the top, increasing by 300 when each tree falls. The provisional balance is 100 wood per primarily wooden building (three buildings per tree). Building construction deductions/classification are not implemented by this change.

## Behavior

A compact stack-of-logs icon and `WOOD 300 tons` display appear along the top of every district, including unfounded districts. The icon uses native UI shapes. Labels refresh immediately on felling and after district load/rebuild; pointer interactions over the bar do not paint/select terrain behind it.

Every new successful felling credits 300 wood in the same saved transaction as the tree's Fallen state. Both automatic axemen and the manual felling hook use `DistrictTreeHarvest.FellAndCredit`. A second attempt cannot grant more wood. Return trips continue, but do not add the same yield twice. The shared yield is now 300; `ProvisionalWoodBuildingCost` records the 100-wood balance target.

## Save compatibility

- Existing district Wood totals are preserved, not rescaled.
- New tree `WoodCredited` and worker `CargoAlreadyCredited` flags persist with normal district snapshots.
- New felling sets the tree flag. Pickup copies that flag into cargo, so unloading already-counted wood adds nothing.
- Old saves omit both flags; old cargo and old fallen trees retain their remaining amounts and complete their original delivery-time credit exactly once. There is no retroactive 300 reward for old stumps or previously felled trees.
- Undo restores the full district's resource total, tree state and worker state together. Routine presentation never grants resources.

## Verification status

Runtime and EditMode tests compile using Unity's installed compiler into temporary outputs, without refreshing or restarting the active editor. Four new regressions cover immediate credit before pickup, no duplicate after reload/delivery, old uncredited cargo, old fallen trees and district undo. Existing labor/harvest yield expectations were updated. The combined Labor test menu selects 18 tests.

The fresh tests have NOT yet run in Unity, and the new HUD has NOT yet received normal docked Game view inspection. Unity is occupied by an unsaved Dry Goods Import QA lot. That live work is preserved pending permission to use the editor. `City Forge > QA > Labor > Check Wood HUD and Reload` checks a copied saved tree, its HUD tally, and a real RegionSaveStore reload/world rebuild. QA entry still requires the fresh splash and a temporary save root.

The accepted original flora artwork/materials are unchanged, with no brightness override. This change is kept separate from concurrent Dry Goods source and asset edits.

## Resources menu revision — September 13
Joe replaced the large Wood HUD with a Resources menu beside Labor. It opens a three-column modal with Wood, Coal, Stone, Iron Ore, Gold, Oil, Food, Jewels and Cloth in that order. Only the icons and quantities are visible on the cards; hover or keyboard focus shows the resource name in an explicit runtime caption. Counts use tons (`t`). The icon crops preserve Joe's supplied `/Users/joelinstrum/Downloads/ui/resources.png` artwork and baked backgrounds. The bars represent Iron Ore provisionally. Source remains untouched; crop coordinates/script are in CityForgeMCP `artifacts/ui/resources-v01`.

Wood keeps the existing `Labor.Wood` save field and +300-at-felling accounting. Eight future resources use additive `RegionCityTile.ResourceInventory` fields, defaulting to zero on older districts; gathering jobs for those resources are not implemented. Open modal totals refresh through the existing simulation UI tick. The saved-district QA command now opens this modal before checking felling and again after reloading the saved district.

Runtime and EditMode assemblies compile with installed Unity Roslyn. The combined suite now contains 19 tests, including inventory serialization and old-save defaults, but has not executed for this revision. Unity remains running the concurrent Dry Goods district review; its preview was preserved. Actual Resources modal rendering, hover/mouse interaction and saved-district runtime checks remain pending an available Unity session.
