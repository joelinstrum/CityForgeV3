# Testing Lots browser — September 19

Lots is an independent district dock action beside Build and Terrain, compiled
only for UNITY_EDITOR or DEVELOPMENT_BUILD. It opens the cached catalog across
all Lot types. Every Build category continues to filter its own Lot type and
uses the ordinary construction quote.

Only placements armed by the testing browser bypass cash cost, minimum era,
population, education, materials, road/waterfront access and boat/shoreline
validation. They consume zero cash/materials. Bounds and Lot collision checks
remain. The bypass is transient UI state, cleared by normal arming, cancellation,
placement completion and navigation; it is never saved in Lot/district data.
Release code both hides the button and disables the bypass, including the
world-level boat-validation override. Ordinary simulation after placement is
unchanged; this is a placement tool, not a perpetual free-operation mode.

24 EditMode tests passed in the isolated scratch project: seven requirement
bypass/isolation cases, eight Build category routes, Farms compatibility, and
eight district Lot simulation/construction checks. An initial run lacked the
Government House package manifest used by the Farms round-trip test; copying
that fixture dependency fixed the scratch setup and all tests passed.

Separately compiled non-development macOS player scripts, loaded the resulting
runtime assembly, confirmed TestLotToolsAvailable returns false, and verified
the testing dock-button identifier is absent from its metadata. Release-check
source and result are retained here. This checks script compilation/gating,
not a packaged release launch. `git diff --check` passed.

Physical placement, visual UI inspection and dense performance were not run.
Existing placement footprint scans, composition-key calculation and undo
serialization remain shared with regular placement; this change adds no
per-frame scan or district presentation rebuild. Source artwork, worker/labor
optimization and manual persistence are unchanged. No player saves, commits,
pushes or review-editor sync were performed.

## Tree Test placement click correction

The first-class Lots action resets the tool to Select. The old pointer handler
entered selection before reaching the pending-Lot branch, consuming the world
click even though the placement preview/requirements were valid. Armed Lot
placement now owns the click immediately after UI/button exclusions, before
inspection, selection, move, or palette-closing routes. Invalid ground clicks
remain owned by the placement tool rather than falling through to selection.
The redundant pre-placement footprint check on click was also removed; the
placement method still validates it once. Normal Build requirements remain.

25 isolated EditMode tests passed, including default-Select pointer ownership
for testing and ordinary Lots, cancellation restoring normal dispatch, the
seven requirement bypass cases, eight Build routes and existing simulation
checks. A read-only copy of Joe's Tree Test (4 x 4 cells, no construction gates)
placed through PlaceDistrictLot into three separate in-memory districts at
normalized positions .1, .5, .9 with zero treasury and no charge. This model
check did not render a world or synthesize a physical mouse click. Live visual
placement remains for Joe's check. Original Tree Test SHA-256 before and after:
`e48d5ab3f16c3aac1fa4179c3d9a606d6e97f24a202c7ed090a9fb119a11e60c`.
No player editor control, save, commit, push or review sync.
