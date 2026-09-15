# District stone deposit artwork and quarry selection

Date: 2026-09-14

## Artwork lineage

- User-supplied canonical source: `/Users/joelinstrum/Downloads/buildings/Stone Mine/stones-for-quarry.png`. Original is unchanged.
- SHA-256: `0f7287ae4d1324d589d472deed08bd7be6ad874e91b47832c52a3095b5183989`.
- Byte-identical project copy: `Assets/CityForgeV3/Resources/CityForgeV3/NaturalResources/StoneV01/stones-for-quarry.png`.
- The source already contains alpha transparency; no image modification is used in the game.
- Runtime: one camera-facing sprite per unbuilt deposit, 9 meters wide, ground-anchored pivot (0.5, 0.07), existing neutral time-of-day tint. Replaces the procedural sphere outcrop.
- Quarry menu thumbnail: `Assets/CityForgeV3/Resources/CityForgeV3/Industry/StoneQuarryV01/MenuThumbnailV01.png`, rendered from the existing StoneQuarry prefab using `StoneQuarryReview.ImportArtwork`. Source model and textures are unchanged.

## Interaction

Industry contains one entry each for stone and coal, with available-deposit and built-industry counts. Add Stone Quarry / Add Coal Mine enters deposit selection and frames the district. Bouncing green arrows mark eligible deposits. Clicking one revalidates the site and builds only that industry, using existing save/undo behavior. Escape, Cancel, opening a modal, or leaving/recomposing the district clears the arrows. Manage retains existing per-site controls outside the main resource list.

Bear sightings use the bottom notification area for six real-time seconds. An unchanged sighting does not restart the timer; pausing simulation does not prevent expiration.

No legacy project files or layouts were copied.

## Verification

- Unity compilation succeeded; 42 EditMode tests passed, including six-second real-time notice expiry and resource persistence.
- Isolated runtime fixture: two stone deposits produce one menu entry with a loaded thumbnail; Add Stone Quarry exposes two arrows.
- UI navigation-submit on the right-hand arrow builds only that quarry, saves it, removes selection arrows, and produces one ton of stone after a completed mining/loading cycle.
- Bear notice expires while the district is paused and does not reappear when the same sighting is refreshed.

## Blocked-deposit visibility

All unbuilt stone deposits remain visible during placement: green arrows indicate buildable sites and amber arrows explain their blocking reason when clicked. The single stone menu entry counts total sites, ready sites, blocked sites, and existing quarries. Add Stone Quarry remains available when all remaining deposits are blocked so their locations and requirements can be inspected. Eligibility is checked again on selection. Trees do not block construction: a successful quarry build removes vegetation within the existing 20-meter clearance radius, including fallen trees and stumps. Vegetation outside that radius remains. Validation runs before clearing; rejected placements leave trees untouched. Construction and clearing share one save/undo operation and do not credit harvested wood.
