# Antique Brick district diagonal roads V01

Drag the Antique Brick Road tool across diagonal district cells to draw NE, SE,
SW, or NW links. A route can turn between a cardinal and diagonal direction.
The links are explicit saved data, so roads that merely touch at corners do not
connect. Existing district road saves retain their cardinal layout; the new
field defaults to zero. Deleting or replacing a linked tile removes its two
ends. Road and delivery navigation updates use direct cell lookups, while
district loading, undo, and bulk moves rebuild their index.

The diagonal presentation uses a shared Antique Brick material and a small
mesh for each distinct port arrangement. Mesh arms meet halfway between road
centers, including across tile corners. The existing brick texture remains
canonical; no source artwork was changed. The generated noon review image
`route-review.png` shows a cardinal-to-NE-to-cardinal route without gaps. It
was captured from an unsaved 64 × 64 cell district in isolated Unity batch mode
using `AntiqueDiagonalRoadReview.cs.txt`. The curb color override also removes
the pale template strip in this capture.

Focused EditMode results: `road-editmode-tests.xml` passed 13/13 placement,
all-direction routing, saved-link, deletion, delivery, and mesh checks.
`delivery-editmode-tests.xml` passed 11/11 Brickworks regressions. Unity
compiled the runtime and shader without errors. No progress save was made.

`dense-profile.txt` records a 2,304-road fixture with 30 diagonal links. The
new 30-edit indexed path took 0.867 ms, versus 9.175 ms for a full-index
rebuild proxy. One batch render build took 387.231 ms. Twenty 800 × 600
camera renders had 11.407 ms median and one 215.561 ms spike. Batch mode
reported zero for allocation and draw-call counters, so those numbers are
unavailable rather than measured zeros. This short capture does not establish
live dense-district frame time or long-duration stability. Delivery route
construction still snapshots the district road list once per trip, not per
frame. Live editor review remains to be completed in the isolated review copy.
