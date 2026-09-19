# Fixed stone bridge validation

Unity 6000.1.12f1; isolated feature worktree; no control of the separate main Unity editor and no region saves.

93/93 focused EditMode tests passed, including fixed model length/serialization, all four diagonal orientations, bounded sampling, and existing road/river/bridge/labor/undo regression fixtures.

`FixedStoneBridgeQa.Run` verified actual procedural rivers and the real crossing modal:

| River width | Original 34.33 m model | Long 50.37 m model |
| --- | --- | --- |
| 24 m | Offered | Offered |
| 42 m | Hidden | Offered |
| 64 m | Hidden | Hidden |

Every available model was assembled without changing any exported vertex or triangle. The full short model has 7,006 exported split vertices and 9,336 triangles; the long model has 13,581 exported split vertices and 18,605 triangles. Runtime length matches source-calibrated length within 1 mm. Both approach travel joins are continuous within 2 cm. Build, in-memory undo, and serialized reload passed for both new styles.

[Original model on 24 m river](stone-original-24.png) and [long model on 42 m river](stone-long-42.png). Approach roads are separate geometry and do not alter either model.

The 25-long-bridge synthetic fixture used 75 renderers. Thirty synchronous camera submissions measured 0.71 ms median / 1.12 ms maximum; 100,000 indexed travel queries took 56.00 ms with zero sampled managed heap growth. These are short CPU measurements, not GPU/full-frame or long-duration stability results. The previous V03 run recorded 1.05/1.48 ms submissions and 72.01 ms travel queries; runs include machine-load variation. Shared materials and bounded queries remain in use. No whole-district scan/redraw was added. Dense mixed-district profiling remains open.

Fit checks sample only the proposed crossing and approaches at bounded stations; the diagonal test requires fewer than 1,000 surface calls for its fixture. Fixed fitting is performed when the chooser is composed, not each frame. Existing modular stone records are retained for backward compatibility but cannot be selected for new construction. Replace an old crossing manually to use an original complete model.
