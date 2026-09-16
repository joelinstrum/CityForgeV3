# Confined whole-lot dragging

Drag an inspected lot directly in the district view. The entire presentation translates continuously inside its original enclosing grid-cell rectangle. The visible footprint stops at the rectangle edge. Escape or pointer cancellation restores the prior offset; release saves one edit. Repeated gestures use the persisted original rectangle, so they cannot walk a lot across the district.

`DistrictLotNudge` stores selection identity, fixed cell bounds, and district-space offset. Existing lots, quarries, Brickworks and coal mines restore offsets when rebuilt. Resource coordinates and occupied grid indices remain unchanged. Demolition clears the offset and preserves resource deposits. Existing lots that fill an axis of their cell rectangle have no movement available along that axis.

`CityForgeApp.LotNudge.cs` shares input handling across lot types and editor categories. Dragging changes only the selected root transform; simulation pauses during the gesture, undo waits until release, and navigation is invalidated afterward. A quarry cart already making a delivery retains its world position while its home moves.

Validation runs in the isolated CityForge-Regions-Review editor, with synthetic fixture saves under ReviewScratch. The main Unity project is untouched.

## Results

Unity 6000.1.12f1 compiled successfully. Quarry assembly, fixed boundaries, repeat-drag confinement, cancel restoration and serialization checks passed. UI Toolkit pointer down/move/up checks passed for drygoods, queen-anne, 18thcenturychurch and quarry. All four presentation roots stayed intact through dragging; rebuilding restored their saved positions. Five undo entries comprise fixture quarry construction plus four drag releases. The check includes dragging far beyond the viewport. See accompanying reports and Review helper command blocks.
