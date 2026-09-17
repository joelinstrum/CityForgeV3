# Region and district UI concepts — v01

Design mockups only. No runtime UI, saves, or lot-editor changes.

## Shared visual direction

Use the existing opening menu's midnight navy panels, fine gold borders, ivory typography, and illustrated medallion icons. Simplify ornament at gameplay scale. Keep short text labels beside or below icons; selected tools receive a consistent highlight.

Both screens share a location/navigation header, explicit Save action, Statistics access, left tool palette, and one contextual right panel. Region information is about the selected district; district information is about the selected object. Tool settings should reuse the contextual panel when appropriate. Detailed statistics open on demand rather than permanently consuming the map.

## Region concept

Show district boundaries and selection clearly. Offer region terrain, rivers, flora, and climate tools. The selected-district panel leads with status, climate, era, and Enter District. Regional edits must make their region-wide scope explicit in the relevant tool panel.

## District concept

Show local construction and management tools, compact cached resource totals, seasonal controls, and the selected building's existing operational details. Warnings belong in this same inspector. The illustrated Brickworks panel demonstrates hierarchy; mockup quantities and progress indicators are illustrative, not verified live data or newly implemented functionality.

## Implementation boundaries

- Keep the lot editor as-is.
- Preserve manual saving only.
- Reuse tokenized components across both screens.
- Icons in these concepts are visual direction, not extracted production assets. Author final icons separately at UI sizes, with consistent padding and states.
- Bind existing cached simulation totals to individual labels; avoid scans, whole-screen recomposition, or world repaints on routine updates.
- Preserve existing tools and navigation during implementation; these mockups show representative states, not every menu.
- Map artwork is illustrative and does not specify a world-rendering redesign.

Generated using the built-in image-generation tool. Exact prompts are in prompts.md. No Unity project was controlled for this design work.
