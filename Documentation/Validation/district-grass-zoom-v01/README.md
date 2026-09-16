# District grass zoom calibration

The default meadow material now uses the next camera band's apparent texture
size at the two closest district zoom levels: LOD0 matches former LOD1 and
LOD1 matches former LOD2. LOD2 and farther retain the 40m source coverage.
The ratios use actual orthographic camera sizes, not UI grid zoom scales.
Mountain grass and authored lot textures retain their existing calibration.

Only the existing ground material texture scale changes. Identical scale writes
are skipped. No texture loads, shader changes, mesh rebuilding, district scans,
new draw calls, or extra texture samples are added to zoom handling. Texture
patterns shift at zoom boundaries. GPU timing was not benchmarked; no material
performance change is expected from this uniform adjustment.

Unity batch compilation passed. A temporary validation runner checked apparent
texture size against the prior target camera band at all six zoom levels; all
passed. Live review state was captured and restored without a progress save.
