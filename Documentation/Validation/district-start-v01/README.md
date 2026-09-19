# District startup and region names — September 19

The date/status in the district header is clickable. Unstarted places say
**Not started** and open Start District / Start Town choices. Hover or keyboard
focus changes the caption beneath the choices. Starting a district initializes
its existing Founded/year/population clock fields without requiring a founder
building. No new schema field or save migration is needed. Starting a town later
preserves its clock and content; the header remains the entry point.

Start Town offers Fort and City Center. City Center retains the legacy
city-charter-house ID but has no Lot asset, so it is disabled with an explanation.
Fort is enabled only when its existing fortress-lot content resolves. No empty
founder can start a town. Town designation is applied on successful placement.
Unstarted places have no region name/marker, placeholder tooltip, or inspector
name; existing stored names are preserved. Region layer toggles still apply.

Validation: six focused EditMode cases passed in a separate temporary Unity
project, using a distinct company/product identity and no player save files.
All current runtime code compiled there. Tests cover modal choices without data
mutation, later-town availability, calendar preservation, existing content,
in-memory JSON round trip, and all four started/designation label combinations.
`git diff --check` passed. Tests do not establish physical-pointer behavior or
visual layout. Fort placement, disk reload, dense rendering, allocations and
frame spikes remain unverified; no long-duration stability claim.

Starting a district updates only the existing header/status widgets. Town
placement now uses AddPlacedLot rather than rebuilding all presentations.
Existing placement composition-key calculation and undo JSON still visit the
whole district once at this explicit placement boundary; this remaining cost
was flagged to Joe and has not been profiled here. Routine HUD updates add no
new district scan. Worker/labor optimization is unchanged.

No player Save, commit, push, or isolated-review sync was performed. Joe's
explicit no-commit instruction overrides the normal committed review handoff.

## Naming confirmation follow-up

Both start choices now open a prefilled name field with Cancel and OK. Blank
names disable OK; confirmed names are trimmed. Cancel leaves name and startup
state unchanged. District Info stages name and designation edits until OK,
replacing its X. Confirmation updates the existing upper-right heading and name
field directly, without rebuilding district presentations. Only explicit Save
persists progress.

Eight focused EditMode cases passed in the isolated scratch project, including
the actual naming dialog callbacks for district and town, Cancel, blank-name
validation, immediate heading updates, retained heading identity, and district
versus town startup state. The naming cases use a temporary EditorWindow panel
so real UI Toolkit change events run. Earlier detached/headless fixture attempts
failed because fields had no panel / graphics device; the final run enabled
graphics in the scratch project and passed all eight. Compilation and
`git diff --check` passed. No player editor was controlled or player save written.
Physical-pointer interaction and visual layout remain for in-game review.
