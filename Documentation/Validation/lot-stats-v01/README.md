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

New data deep-copies with the lot and round-trips through JSON. Old lots gain no
costs or requirements. Two Unity tests passed covering persistence, copy isolation,
construction aggregation and legacy defaults. Live UI validation is performed in
the isolated Regions-Review project, including section navigation for the long
form, preserving the in-memory Testy region. No
per-frame district scans, presentation rebuilds or new simulation loops are added.
