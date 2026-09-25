# Shadow District soft tree shadow prototype

September 25, 2026. Validated in an isolated Unity 6000.1.12f1 project using a
read-only copy of the player's saved `Large Region Test` / `Shadow DIstrict`
tile (`city-022`). The tile has 20 trees on a 35 m hill. The open Unity editor
and the original save were untouched.

The study is opt-in only for a district named `Shadow DIstrict`. Other playable
districts retain the shadow-free performance pass. It uses the existing
`broad-canopy` soft alpha texture on 8 × 8 terrain-following grids, sized and
rotated from the district sun. Existing flora batching combines the 20 shadow
meshes into seven spatial shadow batches. Tree billboard renderers remain
non-casters; the district directional light's realtime shadows stay disabled.
One sampled shadow mesh spans 5.016 m of terrain elevation in the saved tile,
confirming that its vertices follow the hillside rather than a flat plane.

The saved-district fixture passed. In isolated EditMode rendering, the complete
morning, noon, and afternoon shadow refreshes took 4.39, 5.98, and 3.03 ms
in one run, respectively, over eight budgeted calls each. A repeat reached
9.78 ms for noon. The focused canopy suite passed
26/26, including a synthetic hilly 20-tree regression. These are EditMode CPU
measurements, not Game view frame-time or GPU measurements. No dense-forest
scaling conclusion should be drawn from this 20-tree prototype.

Captured views at zoom 1 and zoom 2 are included for visual comparison. The
current mask is intentionally soft; its opacity and footprint should be
finessed against the live editor view before adopting the method more widely.
