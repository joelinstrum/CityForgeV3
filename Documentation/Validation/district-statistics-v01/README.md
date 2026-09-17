# District statistics and lot gameplay

A Statistics button beside Save opens People, Economy and Services tabs. The
former hard-coded population of 4,300 is removed. Empty/legacy unconfigured lots
contribute no invented residents. Minimum build era is independent of visual era.

Placement checks the region era, cash, construction inventory (lumber/wood,
stone, brick/bricks and food), road boundary and river frontage. Failed checks
change no treasury, inventory or vegetation. Accepted lots consume the quoted
materials once. Existing placed lots are grandfathered; loading does not charge
construction again. Road/waterfront conditions are placement requirements, not
ongoing operating gates. Frontier samples query the existing road presentation
index and river surface system only on placement, never during every hover.

Population is saved in 101 fixed age buckets with four people per modeled
household. New residents use a deterministic 20% age-10 / 65% age-35 / 15% age-70
arrival mix (integer remainder goes to working-age residents). Ages advance one
year every four seasons; the last bucket is 100+. No births, deaths, unemployment
migration or individual household records are simulated in this first model.
Removing housing removes its population proportionally from current age buckets.

School capacity covers children; healthcare, recreation and culture cover the
population. Education moves toward 20 + 80 * school coverage by at most 5 points
per season. Health moves toward 25 + 45 * food coverage + 30 * healthcare coverage
by at most 10 points. Happiness moves toward a weighted food/employment/health/
recreation target by at most 10 points. Scores are 0–100. One tonne of food feeds
100 residents per season (rounded up); output is credited before food consumption.
Initial scores are explicit game-balance defaults: education 20, health 70,
happiness 60. Education level labels partition the score into four bands.

Household income = filled lot jobs * average authored seasonal wage / households.
Wage per job is editable in Lot Stats, initially $150. This is the household-income
component of authored operating expense, not an additional treasury deduction.
The panel identifies lot jobs and distinguishes lot net finances from separate
industry/worker payroll. School/health/happiness scores are simulated indicators;
this version does not yet model educational wage premiums or mortality.

Seasonal lot benefits add real district inventory. Per-delivery benefits use the
shared completed-delivery API, currently called by cargo behavior completion.
They do not fire on load/selection, and the delivery's existing lumber credit is
excluded. A farm without a delivery behavior should use Per season. Existing
regional quarry/brickworks and forestry delivery behavior stays in charge of its
own inventory; no new wagon routing or synthetic deliveries are introduced.

A ConditionalWeakTable caches per-district contributions, with an instance index
and a definition-to-instance index. District load/undo/bulk restoration rebuilds
contributions once. Add/remove edits update a single profile and fixed-size age
and resource arrays. Saving an edited lot definition updates only its existing
instances in the active cached district. Other districts refresh on entry.
Seasonal finances now use cached aggregates rather than rereading every lot each
season. No new per-frame scans, terrain rebuilds, renderers or draw calls are
introduced. Existing world-build and placement presentation work is retained.

Validation: Unity compilation and 29 checks cover authored stats persistence,
copy isolation, legacy defaults, transactions, cached finance/production,
aging, food/services, existing-lot updates, delivery deduplication, removal,
reload, existing lot clearing and timber delivery behavior. A 10,000-lot fixture
measures incremental add/remove CPU time and steady-state allocations. See
results and performance files. These targeted checks do not establish long-run
stability or resolve the previously unexplained game lockup.

The isolated review uses the latest manually saved Testy region; tests do not
write player saves. Lot and region saves remain explicit. Changes are uncommitted.
