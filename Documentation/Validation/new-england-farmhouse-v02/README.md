# New England Farmhouse intake validation

See [migration record](../../Migration/new-england-farmhouse-v02.md) for the import contract and era behavior.

- `tests.txt`: seven passing Unity cases, after the final entrance orientation correction.
- `source-hashes.json`: original archive and per-file SHA-256; all six runtime source files match archive bytes.
- `source-metrics.json`: source mesh/material inspection in a separate Blender process.
- `unity-comparison.jpg`: actual Unity Game view; new farmhouse on the right, existing Founders farmhouse/cabin on the left.
- `FarmhouseIntakeReview.cs.txt`: isolated editor test/comparison helper. Its world-X placements are labelled left/right in the helper text; screen positions are reversed by the camera.

Unity compilation and tracked diff checks passed. Native placement checked at 9m height and ground contact within 3cm; camera position/rotation unchanged. No user save writes, main-project control, source reduction, or performance-soak claims.
