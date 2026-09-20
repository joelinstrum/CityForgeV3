# Saved Lot build categories

The district Build rail routes saved Lots by their authored Lot type. The old
all-Lots browser is removed. Each browser reads the cached Lot content summaries
when the user opens it and displays only the matching type; no Lot save is
rewritten or recategorized.

| Lot type | Build menu |
| --- | --- |
| Residential | Zoning → Browse Residential |
| Commercial | Zoning → Browse Commercial |
| Industrial | Zoning → Browse Industrial |
| Mixed | Zoning → Browse Mixed Use |
| Agricultural | Farms → Browse Farms |
| Transportation | Transit → Browse Transit Lots |
| Civics | Civic → Browse Civic Lots |
| Civics / Parks | Parks → Browse Parks |
| District Town Center | Civic → Browse Civic Lots; Start Town founder chooser |

The Lot Editor displays the existing Agricultural save type as **Farm** and
offers **Farms** as its parent category in New Lot and General. Its persisted enum value remains `5`;
the Civics / Parks value remains `7`. Category selection changes discovery only.
The existing placement footprint, requirement checks, material deduction,
Undo and manual Save path remain shared by every browser.

Saved Industrial Lots also appear directly in the district **Industry** window
alongside resource industries. This gives production Lots such as the
Lumberjack Camp an obvious home while retaining **Zoning → Browse Industrial**
as the complete Industrial Lot browser. Opening either catalog refreshes saved
Lot metadata once; it does not save or modify a Lot or district.

September 18, 2026: 10 focused Unity EditMode cases passed, covering all eight
routes, the Farms menu, Farm choice and Agricultural save round trip, plus the
existing Civics / Parks category test. Unity compiled with zero assembly errors.

The subsequent Lot Editor revision makes category selection hierarchical. In
New Lot and General, choose parent **Civics**, then subcategory **General**,
**Park**, or **District Town Center**. Every manually saved District Town Center
becomes eligible in the Start Town founder chooser. **Farms** is a separate
parent category. Existing saved type values remain unchanged. Lot Settings now
opens a General modal containing name, type,
dimensions, traffic, build cost, minimum era, population, education, access and
material requirements. Stats retains people, seasonal finances and benefits.
The focused category and requirement EditMode suite passed 30/30, including
legacy resource-name aliases in General.

Bundled, read-only Lots use the same category browsers and placement contract
without being copied into the player's manual-save folder. The Town Center is
published this way as a 2 × 2 District Town Center Lot under **Civic → Browse Civic Lots**.
Its catalog entry is loaded once with the other cached Lot summaries; opening
the browser does not scan district objects or write a Lot, district, or region.

## Development-only Lots browser

The all-Lots testing browser is now a separate district dock action beside
Build and Terrain, available only in the Editor and development builds. It
shows all saved Lot types and places them without construction costs,
materials, era/population/education thresholds, access or shoreline/boat
requirements. Lot bounds and overlap checks still apply. The existing Build
category browsers always enforce the full construction requirements.
Testing is a transient placement mode; it does not alter saved definitions or
persist a bypass flag. Shipping builds omit the dock action and disable the
bypass. Validation: `Validation/testing-lots-v01/` (24 focused tests plus a
non-development player-script compilation and compiled-assembly gating check).
