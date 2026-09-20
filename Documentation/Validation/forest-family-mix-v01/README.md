# Forest family mix V01 validation

Validated September 19, 2026 in a cloned Unity 6000.1.12f1 project. The open
CityForge V3 editor and all player saves remained untouched.

- 47/47 forest generation, persistence/rollback, harvesting, paint, batching,
  artwork, seasonal-resolution and shadow tests passed headlessly.
- 3/3 Region Terrain UI, serialization, and percentage-control tests passed with graphics enabled. A prior headless UI
  attempt produced Unity's expected `No graphic device` view-initialization log;
  it was rerun with graphics and passed.
- The 14 selected transparent PNGs were inspected together as a contact sheet.
  Dominant deciduous and mountain families visibly include a cross-family tree;
  compact and large tropical compositions visibly mix palms and broadleaf trees.
- `git diff --check` passed before commit.

The performance-sensitive generation path builds one `DistrictElevation` sample
grid per district, then performs five constant-time height samples and a bounded
8m-cell occupancy query per candidate. No routine UI refresh, simulation tick,
camera update, or panning path was added. Each clump remains one renderer and one
spatial-batch member; shadows remain one mesh with five or nine proxies.

Evidence:

- `forest-expanded-results.xml` and `forest-expanded.log`
- `terrain-ui-results.xml` and `terrain-ui.log`
- `forest-targeted-results.xml` and `forest-targeted.log`
- `prompts-and-lineage.md`
