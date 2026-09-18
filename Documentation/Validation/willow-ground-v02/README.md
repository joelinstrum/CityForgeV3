# Willow ground contact correction

The approved Willow PNGs already have pivots at their opaque trunk feet (summer105px above bottom, alpha>128). Changing the pivot would move the selection root relative to the artwork.

Lot presentations of `vendor-willow` now sink by0.16m visually, while saved placement and selection remain at the original ground point. A narrow ground fade buries the pictured roots; projected shadow source receives the same0.16m compensation. Other flora is unchanged.

Unity refreshed and compiled. Read-only `willow-ground-review` updated only the live Willow presentation, confirmed shadow sprite parity and unchanged lot JSON, and captured the normal Game view in `game-view.png`. The capture was inspected: trunk base meets terrain and the square surrounds its base. No Save, fixture write, or commit. This does not claim a performance change.
