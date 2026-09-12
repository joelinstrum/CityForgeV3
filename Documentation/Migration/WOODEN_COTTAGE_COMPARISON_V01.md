# Wooden cottage comparison v01

Library: **3D Building Library → Residential → Wooden Cottage**. ID `wooden-cottage-v01`.
Replaces the visible UV saltbox comparison card. UV source retained as hidden legacy content so existing saved references remain loadable; no saved lots modified. Lit saltbox remains available.

Original supplied archive: `wooden+cottage+3d+model.zip`. All six imported source files are byte-identical. FBX has one mesh, 29,636 polygons, UVMap, and one material. Blender inspection identified image0 as color and image2 as normal, plus roughness/metallic connections. Direct FBX import uses Unity's native material interpretation, without color overrides or added night lighting. Confirmed normal texture is explicitly imported as NormalMap (linear), rather than a color image. Source files are unchanged.

Uniform height normalization9.5m matches the saltbox for this appearance experiment; it is not an assertion of the cottage's real-world dimensions. Catalog pitch−90/yaw90 corrects Tripo source axes. Texture/material fidelity beyond the native FBX interpretation has not been independently calibrated to Tripo lighting.

Normal windowed Game-view comparison inspected: cottage foreground, lit saltbox background, same camera and noon lighting. Cottage roof shingles and trim read finer/less rounded, but are still quite bright under game lighting. Original-material Blender thumbnail retained as reference. No global lighting or source hue changes made. User visual preference pending.

QA: City Forge → QA → Saltbox → Open Cottage Comparison, in Play mode. Fixture is unsaved. Artifacts include mesh/material inspection, source hashes, thumbnail, catalog entry, and actual Game-view screenshot.
