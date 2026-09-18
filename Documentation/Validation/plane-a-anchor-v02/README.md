# Plane A planting anchor correction

The prior fixes alternated between canvas-bottom pivot 0 and Plane B's .065
pivot, then added a selection-only canvas-bottom offset. Neither was measured
against Plane A's visible trunk. Plane A's opaque foot (alpha >128/255) is 19
pixels above the bottom for spring/summer, 20 for autumn, and 18 for winter on
the 1536-pixel canvas. Runtime now uses those normalized values. Plane B is
unchanged. The selection-only offset has been removed: sprite, shadow and
selection share the standard placement origin.

`review.txt` records fresh Unity validation of all four alpha-defined feet.
`game-view.png` is the actual normal Game View inspected with Plane A selected
beside Plane B and the bench. The visible trunk is at y=.02; the standard
selection surface at y=.06 differs by about two screen pixels. No PNG edits,
scale changes or saved-placement changes were made for this correction.

QA command: `plane-anchor-review` on the project-scoped bridge. It refreshes
only the visible Plane A and its shadow with the production pivot, displays
its selection square, and captures the current Game View. This supports
inspection after hot reload, which can retain old scene sprites while resetting
private Lot Editor state. It does not Save or stop Play mode. The existing
Plane A EditMode asset test now checks actual alpha pixels against the pivot;
the EditMode suite was not rerun this turn to avoid stopping the user's lot.
