# Targeted building deletion

Cause: deletion cleared the world composition key and called Show, triggering DistrictWorldController.Build for the entire district. Building-only deletion now removes its hosted lot or invokes its registered presentation refresh, updates the composition key, clears the selection panel and invalidates labor navigation. Roads/rivers/flora deletion retains its prior refresh path. Coal demolition refreshes the coal presentation layer without regenerating resources or terrain.

Unity compiled successfully. Review fixture UI deletion handler: ordinary lot 0.8821ms, quarry 43.7124ms (includes synchronous saving). These are handler timings, not full end-to-end frame measurements. Camera identity preserved in both; unrelated lot identity preserved for ordinary lot. Save/resource preservation and undo checks passed.
