# Antique Brick stair smoothing and unmarked surface V02

An Antique Brick drag that makes two alternating cardinal stair steps in one
direction is replaced by diagonal links. Once established, each further
matching stair step extends the diagonal. Only redundant tiles placed in that
same drag are removed; their construction cost is refunded. A preexisting road,
branch, explicit diagonal connection, or lot blocking the corner prevents that
local replacement. No saved district is rewritten just by loading it.

All district Antique Brick tiles now use the cached procedural road mesh and
the canonical antique-brick surface texture. Straight and corner pieces no
longer sample the authored topology sprites that contain pale edge lines. The
brick source texture and topology artwork remain unchanged.

`route-review.png` shows a straight road followed by a staircase drawn in one
drag and locally smoothed into a continuous diagonal. It was rendered from an
unsaved 64 × 64 district in isolated Unity batch mode using
`AntiqueDiagonalRoadReview.cs.txt`. The image has no pale edge lines.

The focused EditMode suite passed 16/16 tests in `road-editmode-tests.xml`,
including continued smoothing, preservation of old tiles and branches, and
verification that straight brick roads sample the plain brick texture.

`dense-profile.txt` uses 2,304 Antique Brick road tiles and 30 diagonal links.
Thirty indexed edits took 1.918 ms; a per-edit full-index rebuild proxy took
9.309 ms. One world build took 296.643 ms. Twenty 800 × 600 batch renders had
a 5.650 ms median and 14.011 ms maximum. These short runs are only indicative;
the preceding V01 profile used the sprite renderer and had an 11.407 ms median
with a 215.561 ms cold spike. Unity batch reported zero for allocation and
draw-call counters, so those measures remain unavailable. Sustained live
dense-district profiling is still open. No disk progress save was made.
