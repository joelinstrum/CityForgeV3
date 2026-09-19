# Tree placement and season-neutral lighting

An armed Flora tool now owns its first click. Existing tree canopies, selected
objects, and the prior deselect-first UI route no longer consume a planting
click. Repeated armed clicks plant repeatedly; Esc disarms the tool so existing
flora can be selected and moved. The ground anchor may overlap water, roads,
overlays, props, and other flora. Only the lot boundary and a building
footprint block a new tree. Hybrid buildings use their rotated package
footprint. 3D buildings use cached projected mesh contours, with a renderer
bounds fallback if the imported mesh cannot provide a contour. The 3D bounds
and contours are invalidated when building presentations rebuild, avoiding a
mesh scan on every pointer move. Previously saved tree placements remain
unchanged, including any that overlap buildings.

The common `SeasonLighting` ground, flora and building multipliers are now
season-neutral. Time-of-day and user environment controls still light the lot.
Spring/summer/autumn/winter can still select distinct authored artwork, garden
growth states and winter weather; these are visual content, not separate
lighting shades. No save schema, Undo, manual-only Save, or worker/labor code
changed.

Validation: `Validation/tree-placement-season-light-v01/`. Unity compiled and
an isolated Play-mode fixture checked first and second armed clicks over a
water area, disarmed selection, hybrid and 3D building exclusion, open-ground
placement, and equal base/flora/building lighting tint across four seasons.
The fixture was destroyed, global scene lighting restored, and no Save called.
Focused EditMode tests were updated and added, but cannot run while Joel's
Editor remains in Play mode. Physical pointer interactions and saved-file
reload remain to verify in the normal Lot Editor. Earlier forest/regional
suites do not validate this work.
