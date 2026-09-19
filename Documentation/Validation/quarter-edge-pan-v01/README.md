# Quarter-screen district edge panning — September 19

The outer 25% at each screen edge starts district camera panning in that
direction. The intersections of those strips (four 25% x 25% corner regions)
and the center are neutral. At exact 25%/75% boundaries the edge is active,
except the corner intersections. Panning retains the existing zoom-relative
speed, unscaled timing, screen-direction projection and map clamping.

Pointer movement on the existing screen determines direction; no enlarged
invisible buttons cover world clicks. The four previous small edge buttons
are no longer composed. Menu chrome, buttons, text fields and object inspectors
suppress edge panning even outside the corner exclusions. Leaving the screen
or losing application focus clears the direction. A new pointer movement after
closing a dialog can rearm it.

Document modals and choice overlays block district pan polling and clear the
cached edge direction even while a text field owns focus. Region-map arrow
panning now observes the same modal guard. No camera changes occur from these
pan paths until the dialog is cleared. Simulation and persistence are unchanged.

23 isolated EditMode tests passed: 17 edge/boundary/corner/outside cases, two
modal variants (including existing-direction cancellation and unchanged pan
offset), existing pan direction/speed contracts, and both normal/test armed-Lot
pointer checks. Runtime compiled; `git diff --check` passed. Physical pointer
hovering in the live Game view remains for user review. No new per-frame district
scan, terrain rebuild, or render work is introduced. This is not a dense-district
or long-duration performance benchmark. No player saves, commits, pushes, or
review sync; worker/labor optimization untouched.
