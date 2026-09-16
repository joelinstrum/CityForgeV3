# Shared lumberjack harvest grid

DistrictHarvestIndex is a weakly owned runtime cache per district, built lazily when active workers first need it. It uses 32m spatial buckets and an ID dictionary. Nearby searches respect the crew harvest radius (100m for legacy crews). Worker claims prevent duplicate assignments; assigned fallen trees remain accessible for collection, and unclaimed fallen wood can be recovered. Stumps are removed from candidate buckets.

Plant, move, type change, manual harvest and delete UI paths update entries. Collection updates the index directly. List replacement/count changes trigger fallback rebuilding after bulk edits; undo explicitly invalidates the index. Reloaded districts build fresh caches from saved flora. The cache is not serialized.

Workers request at most two paths total per simulation tick, stagger retries after about two seconds, and advance past unsuccessful candidates on subsequent attempts. Failed return paths have the same budget and retry delay. Labor grid searches stop after 4096 visited cells (direct clear walking segments still bypass grid search); extremely long obstructed routes may therefore be considered unreachable.

Validation: 13 timber tests passed in the isolated Unity editor, covering incremental edits, cache reuse, reload, bounded retries and progression past blocked candidates, harvesting and exactly-once cargo delivery. Unity compilation and git diff --check passed. No long-duration performance soak completed.
