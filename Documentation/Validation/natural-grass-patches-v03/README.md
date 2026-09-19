# Natural Grass full to the border

The active Lot Camera capture `lot-camera-full-to-border.png` shows a square and circular Natural Grass piece over the pink lot surface. Both have grass filled to the muted rim, without the pale strip visible in V02. The rectangle shader path now stays fully opaque across its quad; the circular shader path clips only beyond the outer radius, beneath the ring.

The editor refreshed and compiled. This was a read-only camera capture: the active Lot session JSON matched before and after, the camera target was restored, and no Save was called. The temporary Editor QA code was removed afterward. This does not verify physical placement/selection/Undo or saved-file reload. The forest 39/39 and regional 83/83 suites are unrelated.
