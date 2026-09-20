# Expanded Lot Settings catalog

The Main/gear tool now composes a 600 px-wide, 430 px-minimum catalog, replacing
the shared 390 px context-panel presentation. Four large cards expose General,
Stats, Bonus, and Lot Behaviors in one place, with a dedicated Close action.
Stats and Bonus remain available as top-level tool-rail buttons.

The panel is presentation-only. It creates no district query, presentation
rebuild, or persistence operation. Each card routes to its existing settings
modal, preserving draft/apply behavior and explicit manual Lot saving.

Focused isolated EditMode results are recorded in `tests.xml`. The suite covers
the expanded catalog contract, every category card, the top-level Bonus entry,
all ten Bonus resource fields, resource serialization defaults, and one-time
placement behavior. No player Lot, district, or region was saved.
