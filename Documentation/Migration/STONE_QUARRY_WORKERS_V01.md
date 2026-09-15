# Stone quarry workers v01

Date: 2026-09-14

## Source and derivatives

- Reuses `Assets/Resources/Characters/AxemanLaborV01/Axeman.fbx` and its existing body texture through `CreateAxemanVisual`.
- The canonical FBX, original axe, textures, and forestry animation remain unchanged.
- `QuarryWorkerPresentation` hides the axe only on quarry instances and creates a wood-handled, double-point iron pickaxe as a runtime mesh.
- The quarry-only animation starts from the source idle pose, adds a shoulder draw-back, overhead lift, accelerating strike and recovery, and positions both hands with two-bone arm IK. Two workers use staggered 2.4-second cycles.
- Two work stations are placed at the quarry apron, each with a stone face to strike. Worker rigs follow the quarry transform, including rotation.

## Simulation and wages

- Every built quarry has two workers at $250 each per season ($500 total).
- The first season is charged on construction, before vegetation is cleared. Insufficient treasury rejects construction without changing the site.
- Payroll is recorded per quarry and season, preventing duplicate charges on save/reload or repeat ticks. Existing saved quarries initialize payroll when simulated.
- Seasons advance in quarry-only districts; payroll renews alongside other labor wages. Removed quarries no longer incur wages. Pausing a quarry does not dismiss its workers.
- Insufficient funds stop mining and loading until the full crew can be paid. The quarry menu displays wages due and provides a payment action; normal simulation also pays outstanding quarry wages when funds become available.
- Presentation does not award resources or advance payroll. The saved quarry simulation remains responsible for mining, loading, and crediting stone.

## Validation

- Unity EditMode suite: 49 passed, 0 failed (2026-09-15 01:32 UTC), including initial/seasonal payroll, save/reload, insufficient funds, legacy sites, removal, and existing quarry/labor/resource checks.
- Isolated Play Mode fixture: two active miners, original axes hidden, different cycle phases and changing pick-head positions verified. Close-up visual review confirmed shoulder lift and downward strike at the two stone work stations.
- Review artifacts: `CityForgeMCP/artifacts/buildings/stone-quarry/v01/workers-preview.jpg`, `workers-motion.txt`, and `tests.xml`. The fixture uses a temporary save root and was restored after review.
