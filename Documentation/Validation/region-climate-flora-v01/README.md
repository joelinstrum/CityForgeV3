# Regional climate and flora generation

Regional map → Terrain now includes Flora and Climate alongside Rivers. Climate choices are Temperate, Desert, Tropical and Mediterranean. Defaults preserve old saves as Temperate with no generated coverage. Climate must be saved before generating coverage; generating rivers preserves saved climate/flora settings, and generating flora preserves river settings.

Flora offers Sparse (good for agriculture) and Wooded (good for lumber). Desert disables both options and the generation action, and the model rejects direct forest generation in Desert. Changing climate preserves existing trees and developed districts. District planting filters families/species by climate; Mediterranean allows the temperate and tropical palettes. Warm climates substitute non-snowy artwork for existing Snowy Fraser Firs without changing the saved tree identity. Climate rules expose snow eligibility; this change does not add a new seasonal weather/snowfall simulation.

Sparse uses a jittered 64 m grid with 35% occupancy. Wooded uses a jittered 24 m grid with 78% occupancy. Seeds and stable district IDs make fixtures repeatable; each UI generation picks a fresh seed. Temperate and Mediterranean coverage includes harvestable Cilician firs. Tropical coverage uses tropical trees; current lumber harvesting supports only Cilician firs, and the Flora panel states that limitation.

A spatial occupancy mask is built once per district. Candidate planting uses constant-time cell lookups and avoids roads, river banks, lot footprints, built quarries, Brickworks and coal mines, and retained flora. Lot definitions are cached for the duration of regional generation. Missing lot definitions abort generation instead of guessing a footprint. No per-tree route searches or repeated forest scans.

Generated trees have explicit provenance. Regeneration replaces generated standing trees, retains manually planted flora and harvested/fallen/stump trees, and preserves their state and identity. Plans are built without changing the region, one district per scheduled UI step. Canceling/detaching the dialog abandons the plan. Successful bulk commit invalidates the shared harvest index once per district. Save failure restores the original flora lists/settings in memory. Region saving now writes a temporary file and atomically replaces the destination, avoiding partially overwritten saves.

## Validation

34 NUnit cases passed inside the isolated Regions Review editor, covering generation, climate rules, deterministic density, clear footprints, preservation, ID uniqueness, rollback, harvest index invalidation, atomic save/reload, existing river generation/network behavior, terrain controls and region deletion. The review harness invokes test methods and parameterized cases, including setup/teardown. See tests.txt.

Live UI checks cover disabled Desert options, saving/reloading Mediterranean, selecting Wooded, canceling, and completed scheduled generation with reload. Both final menus were captured and visually checked. Fixtures use disposable regions and temporary saves; the user's Testy region was not populated with generated forest. Changes were applied as checked diff hunks, preserving review-only helpers. No main-project source edits or Unity control.

## Stress measurements

A disposable 28 × 20 region with 66 districts and 15 generated rivers/streams produced 291,132 trees:

- Generation CPU: 495.34 ms total; 6.41 ms median district step; 18.84 ms maximum step.
- Save: 1,477.81 ms; reload: 1,282.64 ms; save size: 183,900,548 bytes.
- Largest district: 4 × 4 region units, 8,786 trees; harvest index build plus 100 m query: 3.99 ms.

These measurements cover data generation, persistence and indexing, not rendering/frame rate or sustained simulation. Whole-region JSON persistence is a bottleneck at this density, particularly because district simulation can save frequently. No long-duration run was performed and the previously reported lockup is not proven resolved.

Final UI generation produced 2,212 trees in a disposable 2 × 2 district and successfully saved/reloaded through the actual scheduled Generate action. The isolated review was restored to Testy → regional Terrain → Flora. Final Unity compilation and `git diff --check` passed. Changes remain uncommitted on `lot-updates`, alongside the prior stone-delivery/economy work.
