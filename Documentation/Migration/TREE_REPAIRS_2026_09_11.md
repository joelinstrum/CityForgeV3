# Tree repair pass — 2026-09-11

Joe's tree-by-tree screenshot review is the authority for this pass. Canonical PNGs and source Blends are preserved. New rendered derivatives live in Resources/CityForgeV3/Flora/TreeRepairsV01. Stable flora IDs are retained, so existing lots use corrected presentations on reload.

| Tree | Change |
|---|---|
| Date Palm | Pivot moved onto visible rounded trunk base; closes transparent lower margin. |
| Street Tree | Removed narrow-street-tree from the Flora catalog. Existing saved placements remain loadable. |
| Streettree3d | Re-rendered the rounded source with full framing; strengthened alpha; retained green/autumn/leafless snowy winter treatment. Still a billboard despite its historic display name. |
| Eucalyptus Robusta A/B | 30% smaller; per-tree exposure 1.65; B contact pivot lowered to visible trunk base. |
| Silver Maple A | Contact pivot aligned with central trunk and visible base. |
| Angel Oak | Original textured source converted to near-view 3D, same saved ID; moved to 3D category. Original billboard remains farther away. Exposed root fan sunk 0.65m. |
| Red Maple | Seasonal selection/contact anchors moved from outlying root tips to main trunk. |
| Cilician Fir | Half size; full silhouette re-rendered; refraction/attribute material graph replaced with original color/opacity maps and diffuse shading, plus exposure 2.2 in game. |
| Snowy Fraser Fir | Full pointed crown and lower silhouette re-rendered; corrected pivot; original overall displayed height preserved. |
| Classic Balsam Fir | Half size; re-rendered full crown and rounded lower trunk. |
| Hickory | Half size. |
| Willow | 30% smaller. |
| Cypress Oak | 30% smaller, exposure 1.4, contact anchor corrected to preserve rounded visible base. |
| Oregon Ash and Wide Oregon Ash | 30% smaller, exposure 1.5. |

## Contracts
FloraTreeRepairs contains per-family size/pivot/exposure values. Requested reductions are relative to previous opaque billboard height, not padded canvas size. PPU increases by 1/scale for unchanged artwork; re-renders derive PPU from old and new opaque extents. Existing seeded per-placement size variation remains.
LitShadowReceivingSprite adds default-neutral per-renderer exposure/opacity parameters. Unlisted sprites retain values of 1. No global time-of-day, terrain, or ambient-light settings were changed for production. Source image pixels remain unchanged except newly rendered 3D derivatives.

Angel Oak original source: Authoring/Flora/OakTrees/angel-oak-spanish-moss/angel-oak-spanish-moss-billboard-v01.blend and its v01_source/raw/Map JPEGs. Texture JPEGs are copied byte-for-byte, including separate opacity maps. The original is 2,087,570 triangles; the accepted-for-preview derivative retains full leaf/moss cards and reduces woody geometry to 1386481 triangles. This is a HEAVY single-tree prototype, not approved for dense forests; no FPS benchmark has been made. Uses the same zoom 1/2 mesh, farther billboard rule as Plane UK. Reduced experiments were rejected when they lost canopy quality.

Reproducible scripts: render.py (fir and Streettree3d framing), street-seasons.py (Streettree3d seasonal pass after render.py), angel.py (mesh export), AngelOakBuilder.cs (Unity prefab/material mapping). stage.py/install.py are historical one-time integration aids with concurrent-file checks, not scripts to rerun on an already integrated project.

## Verification
Background Blender renders visually checked for full crowns and base silhouettes. Unity compiled and generated prefab/materials successfully. Live QA uses native docked Game view, disposable unsaved lots, and City Forge > Flora > Repairs menus. Evidence PNGs accompany this document. Physical automated mouse clicks did not reliably activate runtime selection; fixture uses the existing selection presentation directly. Existing saves were not overwritten. Dense-forest performance and manual drag/row interactions are not benchmarked in this pass.
