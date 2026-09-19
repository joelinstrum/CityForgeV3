# Lot Stats authoring

Adds a bar-chart Stats button beside Main in the lot editor. The modal edits a
separate draft, applies changes in memory, and requires the ordinary Save action
to persist. Cancel leaves the original lot untouched. No autosave is introduced.

Reuses BasePlopCost (additive to authored building costs) and BusinessRates for
jobs, seasonal revenue and operating costs. Existing district seasonal settlement
consumes configured rates across all lot types. Net income is calculated with
64-bit arithmetic and displayed with a sign and green/red/neutral color.

Optional LotStats stores minimum era separately from architectural era, road and
waterfront requirements, residential/mixed-use residents, resource benefits and
per-season/per-delivery timing, service type and service capacity. Lot-authored
construction resources augment the existing shared construction requirement
calculator. District gameplay integration is now documented in the sibling
`district-statistics-v01` validation folder; the initial descriptive-only boundary
has been replaced by placement checks, resource consumption, demographics,
seasonal production and service effects. Existing lumber delivery yield remains
owned by its production behavior and is not credited twice.

Lot Stats now also authors minimum population and minimum education score (0–100).
The construction section exposes all ten district stockpiles in tonnes, including
lumber and bricks. These fields remain part of the draft until Apply and the
ordinary manual Lot Save. Older lots deserialize with both new thresholds at zero.
The focused Lot Stats and district simulation EditMode run passed 10/10 on
September 18, 2026; no player save was written by that run.

The later General-modal revision moves minimum era, population, education,
road/waterfront access, base cost and all construction resources from Stats to
Lot Settings → General. Stats continues to edit residents, jobs, wages, finances,
benefits and services. Both dialogs apply in memory; an explicit Lot Save is
still required. The focused category and requirement run passed 30/30.

New data deep-copies with the lot and round-trips through JSON. Old lots gain no
costs or requirements. Two Unity tests passed covering persistence, copy isolation,
construction aggregation and legacy defaults. Live UI validation is performed in
the isolated Regions-Review project, including section navigation for the long
form, preserving the in-memory Testy region. No
per-frame district scans, presentation rebuilds or new simulation loops are added.

September 19, 2026: **Population added** is now available in Stats for every
Lot category. The serialized `Residents` field remains unchanged for existing
Lot compatibility. Placing any Lot adds that value to the district population;
deleting it removes the same capacity and residents through the incremental Lot
simulation. Eight category cases and the existing placement/removal/reload,
demographic, requirement and dense-cache checks passed 16/16. Stats UI and JSON
compatibility passed 3/3. No player Lot or district was saved.
