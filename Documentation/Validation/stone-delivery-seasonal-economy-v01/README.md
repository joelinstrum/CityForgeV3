# Stone delivery and seasonal business economy

Quarry loading now adds only wagon cargo. Completed unloading at an active Brickworks credits the delivered stone and immediately converts it 1:1 into bricks. Missing/paused destinations retain cargo; full inventories retain the entire load until capacity is available. Lumber and barge resource yields are unchanged.

Quarry delivery version 2 reclassifies previously credited in-transit cargo once, including version 0 saves whose cargo must be derived from loaded blocks. Previously delivered Brickworks stock converts on its next active tick. Existing production totals survive; delivery averages track new completed deliveries because old saves do not contain delivery counts. The inspector labels the average explicitly in tons per delivery.

Lumber mills and Brickworks default to $1,500 per season, 10 employees, and $0 cash revenue. This uses the user's seasonal preference to resolve the later reference to a monthly cost. Cash revenue is distinct from resource production. Other industrial, commercial, and mixed-use lots display "Not configured" until rates are specified. Coal mines also expose the shared unconfigured stats; quarries expose their existing two employees and $500 seasonal payroll.

Shared BusinessRates on lot content support configured seasonal cost, revenue and employees. Configured is explicit so older Unity JSON saves cannot accidentally turn missing settings into configured zero rates. Lot copies preserve the settings independently.

Business costs/revenue settle once when the existing 600-second season advances. Initialization does not retroactively charge the opening season. Paused buildings retain employees and costs; removed buildings cease billing. Negative treasury balances retain expenses when cash runs out. Quarry/axeman payroll rules remain unchanged. Lot definitions are read only at settlement, never for business accounting on each simulation tick. The season clock also advances in districts without forestry workers.

Validation is performed in the isolated Regions Review project. Changes are applied as checked diff hunks after backing up touched review sources; review-only helpers are preserved. No main-project Unity control or source edits. The test harness invokes NUnit test methods and parameterized cases inside the Unity editor and records each result; it does not modify user region data. Panel verification reads the actual Testy / District 9 Brickworks selection. Results accompany this document.

This work does not establish that the previously reported long-duration lockup is resolved.

Verified: 62 NUnit cases passed across business economy, Brickworks/delivery, quarry, crane, labor, timber, and wood inventory. Unity compilation succeeded after removing an unrelated rotation fixture missing its runtime dependency from the review project. Live Brickworks panel text passed assertions; the attached screenshot was visually inspected. `git diff --check` passed.
