# Forestry and timber deliveries

Labor → Axemen opens a portrait card, then information, crew count and Place.
Place closes the modal and arms an axeman model at the pointer. Click dry, open
district ground to create the crew at that exact point; Escape cancels without
charging wages. Each drop creates its own stable crew/worker/wagon identities.
The preview draws above foliage; actual workers retain ordinary occlusion.

The existing $250/axeman seasonal wages remain. The current harvestable species
is Cilician fir. New crews work within their script's harvest radius.

A wagon parks on the nearest reachable dry road center within 12m of the drop.
If there is no such road, it waits at the crew's point until roads reach it.
Workers return to their own crew location; collected tree loads appear aboard
the wagon once the configured threshold is reached.

## Script

The Script button edits/imports a declarative JSON recipe, not arbitrary Python.
A recipe is copied into each crew's saved state. Editing a recipe preserves
cargo and progress. Run/Pause controls are available for each crew in Labor.

```json
{
  "schema": "cityforge-timber-script-v1",
  "treesPerLoad": 3,
  "bundlesPerTree": 4,
  "unloadSeconds": 3,
  "retrySeconds": 5,
  "harvestRadiusMeters": 100,
  "repeat": true,
  "fastWagon": false
}
```

DistrictTimber separates phase transitions from host routing/motion callbacks.
The phase sequence is gather → dispatch → outbound → unload → return-route →
returning. Enabled, timers, inventory, destinations, current pose and route
waypoints serialize with the district. Host navigation reconstructs motion on
reload; it does not teleport the wagon to camp.

## Roads and mill handoff

Navigation searches connected dry district road cells and sorts reachable mill
road endpoints by route distance. Rotated lot dimensions and shore offsets are
included. Mills are lots containing the lumber-mill-v01 native asset.

The shared articulated horse/wagon controller rounds the route and validates
the swept horse/front-axle/rear-axle positions against roads and water. A wagon
waits if roads are disconnected, removed, flooded, or lack room for a forward
turn. Provide turning space at both ends; a narrow dead end may not permit a
return turn. No off-road or across-water shortcut is generated.

Unloading credits the placed mill's TimberBundles once and clears wagon cargo.
Spendable Wood is now credited only when dock workers place bundles aboard the
barge: 75 wood per bundle (900 per default 12-bundle shipment). Felling, tree
pickup and wagon unload add no spendable wood. Existing balances are preserved.
Only each simulation step's newly loaded bundles count, so partial-load reloads,
sailing and repeat resets cannot re-credit cargo. Lot-editor previews award no
resources. Raw tree fields and legacy credit flags remain readable in old saves.

Cargo-loading definitions now expose requireTimberDelivery (default true).
In district hosts, a fresh barge shipment reserves its capacity from delivered
timber before workers start. Repeat resets the reservation and waits for the next
batch. Existing in-flight cargo is preserved on migration. Lot editor previews
can still demonstrate loading without incoming wagons; scripts can explicitly
set requireTimberDelivery=false to use independent district loading.

Saved source lots and supplied model assets are unchanged. The axeman portrait
is generated from the existing runtime model using CharacterThumbnailBuilder.
A dormant presentation factory allows wagons to work even if the player has
never opened the lot editor.

## Verification

61 focused tests passed (labor, timber, cargo loading and lot scripts). Live
windowed Game-view review used a transient district and temporary save folder:
two workers harvested, three trees filled a wagon, the wagon drove to the mill,
unloaded, returned, and began another shipment. Dock workers reserved 12 bundles,
loaded the barge, and it sailed. Mid-trip saved-state reload preserved progress.
Also checked the real pointer-move handler and visible ghost above foliage,
and wagon creation without a lot-editor factory. No QA crews were placed in
the user's actual District 9. Physical mouse clicking/dragging remains a
hands-on acceptance item because desktop input automation was unreliable.

Evidence: CityForgeMCP/artifacts/behaviors/forestry-delivery-v01/.

## Barge-only resource credit — 2026-09-14

76 focused tests passed, including harvesting/manual felling without credit,
legacy cargo, partial-load reload, two loading/repeat cycles and saturation.
Live copied District9: one additional tree collected with no resource credit;
3/6/12 loaded bundles yielded225/450/900 wood. Reload at6 preserved the balance
exactly. The source district balance was not changed by QA. Evidence:
CityForgeMCP/artifacts/behaviors/barge-wood-credit-v01/.
