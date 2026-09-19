# Natural Grass Garden V01 check

`windowed-soil-comparison.png` shows four temporary Garden grass presentations over a temporary dark soil plane. The plane was added only to make the soft patch edges visible; it is not a new lot base or saved asset. Capture used the active Lot Camera in the normal windowed Unity Editor Game view. The presentations and plane were removed after capture, and the camera size was restored.

The active lot's serialized JSON was identical before and after, and no Save was called. The Play-mode runtime check passed for the four IDs, shared source texture, 0.40 m edge fade, rotation footprint swap and isolated session round trip. Unity compiled without new errors. The new EditMode test file is present but was not run while the active editor was in Play mode.

Physical mouse placement, selection, Undo, direct brick overlay interaction, disk save/reload and dense repeated-placement performance remain unverified. Forest 39/39 and regional 83/83 suites predate this work.
