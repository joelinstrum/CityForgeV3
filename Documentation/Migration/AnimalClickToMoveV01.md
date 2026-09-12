# Animal click-to-move v01

Implemented in the Unity project, 2026-09-09.

In the lot editor, select an existing horse or bear, then click clear ground to issue a walking destination. Existing animals no longer drag/teleport when selected. New animal placement retains its initial placement drag. Other object hits retain selection priority. Active placement tools and camera pan retain their existing precedence.

A mint arrow appears at an accepted destination for 1.6 seconds and fades during its last half second. Rejected destinations display a red arrow. Animals turn with their walk clip, follow a route around static props/buildings, and idle on arrival. Selecting another destination replaces the current order. Bears still abandon orders when their existing musketman/tower threat detection triggers. Orders are temporary; positions update the normal lot data. This implementation wires input into the lot editor, not district-wide animal selection.

## Files

- LotWorldController.AnimalCommands.cs: orders, ground targeting, continuous obstacle clearance, A* route planning, locomotion, transient arrow.
- AnimalMovementQa.cs: disposable Game View fixture and menu-driven controller checks.
- integration.diff: exact changes to the four existing source files.
- *.before: pre-change backups. Do not rerun install.py over a modified project.

## Validation

Unity runtime/editor scripts compiled successfully. Tests ran in the normal docked Game View.

- routes.txt: obstacle detour found; occupied and outside-lot destinations rejected.
- programmatic-detour.txt: horse walked around a bench and arrived within 0.00044 m; walk and arrow observed; arrow subsequently hidden.
- selection-routing.txt: camera-projected horse selected via the production selection API; ground targeting handled and accepted through the production destination API.
- selection-command-movement.txt: selected horse walked to the requested target and idled within 0.00035 m.
- game-view.jpg: selected horse in the normal Game View.

Physical end-to-end mouse selection is NOT verified: Sky coordinate clicks did not reliably land in Unity's Game View, including unrelated UI controls. The selection and command tests above call controller APIs through editor test menus; they are not evidence of physical pointer event delivery. No game coordinate compensation was added for that automation issue. A user mouse check remains outstanding.

The disposable QA lot can be reopened while playing with City Forge > QA > Animals > Open Click To Move. The fixture currently emits an existing inactive-world coroutine error when opening; this predates the movement implementation. Saved user lots and canonical bear/horse assets were not overwritten.
