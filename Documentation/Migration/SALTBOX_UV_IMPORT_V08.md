# Saltbox UV comparison v08

Replaces the visible **Saltbox — Original Import** card with **Saltbox — UV Import**, under 3D Building Library → Residential. The lit saltbox remains available. The old comparison source is retained only as hidden legacy content so saved lots referencing it can still load; no user-owned save was edited.

Imported `uv-wooden+house+3d+model.zip` directly. Its FBX contains 28 mesh parts with UVMap layers and one shared material/4096×4096 base-color atlas, unlike the earlier 28-texture export. Both archive files are byte-identical to the imported source. No Blender re-export, added lighting, pane edits or material color adjustments. Unity texture maximum raised to4096 with sRGB enabled to retain the full shared atlas.

Catalog ID `new-england-saltbox-uv-v08`; resource folder `Buildings3D/SaltboxUVV08`. This export uses the standard Tripo source-axis correction (pitch−90°, yaw90°); 9.5m uniform height normalization matches the lit saltbox. Source FBX unchanged.

Normal windowed Unity Game view checked: UV import in foreground, lit saltbox in background, both upright and textured at matching scale. The UV import remains pale under existing Unity lighting; no claim that the new UV atlas fixes the discrepancy. QA menu: City Forge → QA → Saltbox → Open UV Comparison. User visual acceptance pending.
