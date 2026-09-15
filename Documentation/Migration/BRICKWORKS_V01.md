# Brickworks v01

## Artwork lineage

- User-supplied source: `/Users/joelinstrum/Downloads/buildings/bricksworks/brickworks+3d+model.zip`.
- Canonical archive, extracted FBX/textures, and SHA-256 provenance are preserved in `Authoring/Buildings/BrickworksV01/Source/`.
- `Tools/Brickworks/import_asset.py` creates a centered metric derivative (18.861 × 24 m footprint, 11.213 m high) without changing the supplied geometry, UVs or color atlas. The supplied design is the visual authority for this import; the existing approved masonry assets are unchanged.
- Runtime model/material/thumbnail: `Assets/CityForgeV3/Resources/CityForgeV3/Industry/BrickworksV01/`.
- Four material review angles and a top-down plan are in `Authoring/Buildings/BrickworksV01/Review/`. This asset uses its full 3D mesh in the district. No legacy project files were copied.

## Placement and production

- Industry → Add Brickworks is locked until a Stone Quarry is placed in that district. Simulation validates the same prerequisite before clearing trees or adding the building.
- Placement preview supports R to rotate and Escape to cancel. It rejects water, lots, steep terrain, overlapping industries and roads under the building footprint. Successful placement clears nearby trees and supports district undo/save.
- Initial tuning: 1 ton of delivered stone becomes 1 ton of bricks every 30 simulation seconds. No new building price or worker payroll has been introduced.
- Stone remains counted in the district resource inventory when quarry loading completes, as before. Delivery puts that same cargo into a Brickworks input queue; firing consumes the queued stone and subtracts it from district Stone while adding Bricks. Delivery itself never credits stone again.

## Quarry transport

- A full wagon now waits for a reachable, enabled Brickworks instead of deleting its cargo through the old stockpile-reset timer. The crane returns its hook before departure.
- Routes use connected dry road cells plus short, clear industrial access lanes. Full articulated horse/wagon clearance is checked, including turning room. A blocked road stops movement; a removed or disabled destination leaves cargo on the wagon and triggers another destination search.
- The wagon unloads for 8 seconds at the receiving stop, then returns empty to the quarry loading bay. Position, headings, phase, destination, cargo and progress persist through saves. Reopening a save replans from the saved wagon position.
- A bottom notice says “Bricksworks required” when no usable destination/route exists. It uses the existing six-second notice lifetime and does not continuously retrigger for the same unresolved condition. Quarry management retains the waiting explanation after the notice fades.

## Validation

- 57 EditMode tests pass, including prerequisites, rejected placement, connected/disconnected/flooded roads, retained full cargo, destination removal, pause, mid-unload reload and exact material conversion.
- Isolated live review verified the menu lock, missing-destination notice, road delivery, reload during travel, four tons unloaded, four tons converted, and empty wagon return to the crane bay.
- Both short and longer road routes completed the full round trip after reload. Consecutive duplicate junction points are removed before corner rounding.
- Live review uses a temporary save root, separate from the user's regions. Review evidence is preserved in `QA/BrickworksV01/`.
