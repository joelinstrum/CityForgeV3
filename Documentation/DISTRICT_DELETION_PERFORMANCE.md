# District deletion and navigation performance

Small selection deletions now remove only their live presentations. Tree and lot deletion preserves terrain, other trees/lots, camera and UI screen. Roads refresh their road layer; river edits retain their terrain/water refresh because they change the ground surface. No whole-district loading screen is requested by deletion. The selected inspector is updated in place; selection changes also avoid rebuilding the screen. Save and undo remain supported.

Labor navigation reuses the district presentation composition key, which spatial edit paths already update, instead of reconstructing a string for every tree, lot, road and river on each lookup (three lookups per running frame). Navigation still rebuilds when the district or spatial key changes; deletion explicitly invalidates it. Empty timber crews no longer construct a road navigation graph each frame, and quarry delivery avoids scanning district geometry when no delivery phase is active.

Live isolated fixture: tree deletion including save took 2.19 ms. Terrain mesh references, the remaining tree object and screen instance were preserved, no district loader appeared, and undo restored the tree. A synthetic 5,000-tree CPU benchmark measured 583.31 ms for 30 former composition-string checks versus 0.004 ms for cached navigation checks. This measures the removed hot path, not whole-game FPS. See QA/DistrictDeletion/verified.txt.

## Main integration (2026-09-15)

Main's shared selection system now owns incremental deletion, including resource-preserving industrial demolition and refreshing only changed road cells and their neighbors. Cached labor navigation and the idle timber/quarry fast paths remain enabled. The old separate deletion presentation helper was retired.
