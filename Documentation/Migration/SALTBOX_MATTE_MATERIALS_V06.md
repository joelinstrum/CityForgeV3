# Saltbox matte material correction v06

The supplied Tripo/in-game comparison exposed white washout on the roof and weak olive color on shutters. Correct base-color images were assigned and imported as sRGB, metallic was already zero. Unity Standard nevertheless had both dielectric specular highlights and glossy environment reflections enabled.

Disabled `_SpecularHighlights` and `_GlossyReflections`, enabling their corresponding OFF shader keywords, on the saltbox's 41 material slots. Original color images, white material tint, UVs, roughness/smoothness, meshes and all 13 pane emission controls are unchanged. No global scene exposure, lighting or other buildings were modified.

Versioned before/after material snapshots accompany this report; canonical Blender sources remain untouched. Existing package paths/IDs stay stable so previously placed saltboxes receive the fix. The package builder now reapplies matte settings during rebuild.

Normal windowed Game-view noon and night inspections passed. The same roof region decreased from average RGB (160,144,127) to (119,104,88), removing the white reflection contribution without recoloring the source atlas. Night pane glow remains visible. User visual acceptance against the Tripo reference is pending.
