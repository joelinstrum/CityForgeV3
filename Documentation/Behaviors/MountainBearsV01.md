# Mountain bears and frontier marksmen

Mountain-family tree clusters can attract an occasional bear. A cluster requires three standing fir/mountain trees within 25m on walkable ground. First sighting countdown is 180 simulation seconds; later intervals are 240–420 seconds, with at most one natural bear. A marksman prevents spawns within 55m.

Forestry workers stop harvesting when a bear comes within 30m, release their tree reservation, and retreat on walkable ground. They hold until the original work area clears by 45m, then resume work or delivery. Existing raw cargo is preserved. Workers do not frighten bears.

Labor → Frontier Marksman → Place arms a ground-placement preview. Find, Move and Remove are available in Labor. A guard within 55m fires a warning shot upward, sends the bear away, and suppresses natural sightings for five minutes. The bear is unharmed. Guard placement, bear state, timers and shot counts persist with the district. Simulation pause freezes behavior. This version protects district forestry workers; it does not alter separate lot-local bear behavior.

Implementation: DistrictWildlife.cs (simulation/save state), DistrictWorldController.Wildlife.cs (shared actor presentation), MarksmanWarningShot.cs (raised musket, flash and synthesized report), CityForgeApp.DistrictWildlife.cs (Labor controls). Uses existing bear and musketman assets. No new asset source files were overwritten. The wildlife rules currently live in C#; they are not an editable Python behavior script.

Validation: 80 focused EditMode tests passed (4 wildlife tests plus existing forestry, cargo, routing and behavior coverage). Isolated live district encounter verified retreat, warning shot, fleeing and worker resumption. UI controller QA exercised the real card/details/place callbacks and ground placement, persistence and clearing placement mode. Normal windowed Game screenshot verifies readable cards. Physical mouse placement was not separately validated. Shot pose review was staged while paused; actual shot/flee state was observed independently. Actual District 9 restored afterward with Labor open; no QA guards or bears copied into it.

Evidence: tests.xml, marksman-ui.txt, labor-library.png, shot.json/txt and resumed.json/txt.


## Seasonal marksman wages — 2026-09-14
Marksmen now cost $250 each per season, matching axemen. Hire charges on successful ground placement; insufficient funds reject placement. Moving is free, removal ends future charges without refund. DistrictLabor advances the shared season even when marksmen are the only labor; axemen renew first, then marksmen as a group. Unpaid guards remain placed but neither suppress sightings nor fire warning shots. Labor shows the rate, seasonal total, off-duty state and a Pay Marksmen Wages action. Payment is idempotent; paid state persists across reload. Existing guards retain their current season on migration and start paying at the next renewal.
83 focused tests pass, including new coverage for standalone guards, mixed payroll, insufficient funds, off-duty protection, repayment, pause and save/reload. Normal windowed Game Labor wage display checked. Actual District9 restored with Labor open; no QA guards added. Evidence artifacts/behaviors/marksman-wages-v01/tests.xml.
