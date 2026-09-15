# Quarry and performance checkpoint

Branch starts at origin/main e6d8cfacd07f4020977bf59897546a5618ab8ed6. Captures the tested Regions Review game source and required assets, including the earlier quarry/resource, terrain, regional river, and labor foundation absent from main. Main Unity working directory and index are untouched.

Changes include shared object selection in the right-side inspector, advisory rotation conflicts, doubled Brickworks dimensions, resource-preserving demolition, incremental deletion for all selection kinds, zoom improvements, and regional river work. Detailed validation records accompany this file.

Excluded: Unity Library/Temp/logs, copied user saves, temporary review launcher/command poller and an unreferenced default animator controller. Restored standard persistentDataPath and game product name in this checkout; diagnostic paths normalized to their original project locations. Runtime logic otherwise matches the compiled review copy. Unity was not launched on this commit checkout.
