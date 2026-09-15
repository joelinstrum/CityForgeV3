# District selectable-object contract

All new placed presentations must register with `DistrictWorldController.RegisterSelectable` after constructing their visible geometry. Do not add an object-specific branch to pointer handlers or build a second picking system.

The registration supplies:

- Stable `DistrictSelectionRef`: existing Flora/Lot/Road/River identity, or Entity with a namespaced ID (e.g. `brickworks:<saved-id>`). Keep the same ID across rebuilds and saves.
- Root GameObject and visible renderers. Exclude ground shadows, placement guides, traveling vehicles and independently managed workers. A child-renderer change requires re-registration.
- Display title/description, and optional `DistrictSelectionAction` callbacks. An action returns an empty string on success or a useful reason on rejection. Validate before mutating; update saved model fields and presentation together. Do not save, rebuild the whole district, or open UI in these callbacks.

The shared component owns ray picking, projected marquee bounds and world bounds. The world discovers active, configured components only, so destroyed or replaced presentations cannot remain in a separate stale hit list. Discovery happens on interaction, not on pan/zoom or each frame.

The shared inspector resolves identity before an action, initializes existing undo history, invokes the action, saves through `SaveDistrictEdit`, refreshes selection highlighting and updates the district composition key. No quarry/brickworks/mine branches occur in the picker or inspector.

An object may expose no rotation action if rotation is invalid by design. Coal mines explain their slope-fixed orientation. Quarries and Brickworks validate clearance and delivery state before rotating. Entity move/delete operations are not inferred from a transform: the current UI explicitly directs users to supported actions/management rather than silently ignoring or destructively moving them.

Migration: quarries, Brickworks and coal mines use the shared inspector; trees use its visible-geometry picking while retaining existing group editing. All hosted lots automatically opt into the shared inspector in AddLot, regardless of lot category. Their single lot adapter binds existing validated rotation and lot metadata. World-surface pointer-down checks inspectors before any active category/placement tool; UI controls remain excluded. Roads/rivers retain their specialized footprint/polyline picking in the shared collection pipeline, preserving river connectivity restrictions. This is an adapter boundary, not permission to introduce new type-specific pointer branches.

Acceptance for every new presentation: visible-body pick, marquee inclusion, deselection, inactive/destroyed exclusion; supported action validates and persists its model; undo restores it; unsupported actions are absent; overlapping picks choose nearest. Include a temporary unknown-type registration test to ensure the core requires no changes. Do not give traveling subobjects their parent's selectable geometry unintentionally.

## Presentation update: right-side inspector
The shared inspector now renders exclusively in the existing right-side Selected Lot/Selected Object panel. Never create a selection modal. RefreshSelectedObjectPanel replaces that single panel in place after pick/action/clear, preserving active menu and camera. Existing action registrations are unchanged. World clicks inspect before active tools; clicks inside the panel must never reach world-tool handlers. Empty land clears inspected lot/entity selection.

## Rotation advisories
Rotation is user-controlled: clearance, overlap, water access and active delivery diagnostics are warnings after applying the orientation, not authorization gates. DistrictActionResult separates successful changes (with optional message) from failures such as a missing object. Shared panel saves successful warned changes and displays diagnostics. Do not reintroduce placement validation as a rotation blocker. Initial construction validation remains separate. Away quarry wagons retain their world pose; navigation is invalidated and return destination updated.


## Building deletion
All saved lots receive Delete Building in the shared panel. Other building owners attach `WithBuildingDeletion(Action, preservesResource)` to their selectable registration. The callback updates simulation data only; the common delete path owns undo, saving, clearing selection and rebuilding presentation. Resource-backed callbacks preserve the deposit and remove only construction/operational state. Both keyboard and panel deletion use this capability. Future saved lot types need no extra UI code.

Building-only deletion now refreshes incrementally. Owners with presentation caches pass `refresh:` to `WithBuildingDeletion`; simple roots default to deactivate/destroy. Hosted lots are removed from world registries automatically. No full district rebuild occurs for building-only deletion.

All shared deletion kinds now update incrementally, including mixed selections. Flora removes roots and registry entries; roads update changed cells plus neighbors; local rivers refresh their dependent water/terrain presentation without recreating the district. Deletion keeps the composition key synchronized rather than calling Show with an invalidated world.
