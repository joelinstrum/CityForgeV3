# Delete Building — Regions Review

Added the Delete Building button to the single right-side selection panel for all saved lots, Brickworks, stone quarries and coal mines. Resource-backed owners register a demolition callback that preserves the deposit. Other entity types can opt in with WithBuildingDeletion; the UI has no entity-ID or type switches. Keyboard deletion uses the same callback. Quarry removal clears its active wagon/loading state while keeping its stone site, script, and previously credited inventory.

Unity compilation passed. Actual UI button checks passed for ordinary lot removal and quarry demolition, including resource identity/location preservation, save persistence, cleared selection and undo. Mine/Brickworks callbacks inspected; not separately exercised through UI.
