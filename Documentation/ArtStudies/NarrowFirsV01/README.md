# Narrow fir silhouettes V01

CityForge's district forest and placed firs share `ForestTrueAngleFirAtlasV01/fir-trees.png` and its matching winter atlas. Each atlas already contains six narrow silhouettes in slots 1, 3, 5, 8, 11, and 13. The other nine slots have wider crowns. The cleanup pass excludes slot 5 because its summer slice contains a detached foliage island, leaving five clean narrow silhouettes in use.

`ForestTrueAngleCluster` now selects only the clean narrow slices for fir pieces in compact and large forest clusters, including the firs mixed into deciduous clusters. Individually placed Cilician, balsam, Fraser, and blue spruce records also select narrow slices. Saved flora identities, generated positions, season handling, and source textures are unchanged. The fir batch root and individual shadow width follow the narrower art.

The summer atlas's visible alpha bounds for the selected slices are 203–254 px wide. Wider slices previously used by the same renderer reach 342 px. The matching winter atlas uses the same narrow slot positions. This pass changes silhouette only; a darker fir palette is a separate art decision.
