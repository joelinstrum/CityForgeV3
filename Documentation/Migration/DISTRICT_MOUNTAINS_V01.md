# Steep mountains and coal sites V01 — 2026-09-13

Joe found the mine's gentle hill too shallow and authorized remaking Little River Bend with much steeper relief and coal only against steep mountains.

Terrain → Hills now opens RELIEF, with a Steep mountains toggle. Gentle hills keep their original recipe and 60m maximum. Mountain mode uses a separate narrow, steep peak profile, heights up to240m, existing seed/coverage controls, and45m infrastructure clearance blending. Mountain visuals are a first geometry pass using existing grass materials; rocky textures, irregular ridges and tunnel excavation remain future work.

Coal generation v2 requires mountain mode and shared DistrictCoalMine.SuitableMountainSite geometry: low apron (0.1–12m altitude), no more than3m rise over the downhill5m access, at least8m rise behind the entrance at8m uphill and28m at30m uphill. Sampling every5m locates narrow foothill pockets. Both generation and build use the same eligibility; gentle hills yield no new coal. Existing developed deposits elsewhere retain their persistence protection.

Little River Bend was regenerated explicitly: Mountains=true, seed1209,height180m,coverage45%, actual peak178.66m. Removed former coal/mine sites, generated two qualifying sites and built first mine facing uphill. Inventory, existing rivers/roads/lots/flora and accepted artwork were retained. A before-region snapshot is in CityForgeMCP/artifacts/terrain/mountains-v01/before-region.json.

Validation: normal docked Unity Game-view inspection close to mine and at district scale; actual disk reload preserves elevation, two qualifying deposits and one rendered mine. HILLS QA:770flora,1river,1lot, collision heights and flora grounding pass. MINE QA: uphill facing, duplicate/flat rejection and developed-site retention pass. MOUNTAIN QA: all deposits satisfy strict face criteria and gentle terrain produces none. QA forces world composition before inspecting it, supporting the live district preloader. Evidence and ledger run: CityForgeMCP/artifacts/terrain/mountains-v01. Artistic approval pending.
