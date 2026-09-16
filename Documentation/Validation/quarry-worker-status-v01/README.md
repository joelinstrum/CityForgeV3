# Quarry worker animation and status

The existing QuarryWorkerPresentation animates staggered pickaxe swings while the simulation is running, wages are paid, the site is enabled and its phase is mining. Full carts deliberately stop mining. Read-only inspection of the Regions Review Testy/District 9 save found both built quarries full (4/4) with no reachable Brickworks.

Added a generic selectable status provider and a live inspector status label, showing quarry state, unpaid/paused conditions and cart load. Updates every 500ms while attached without rebuilding the panel/world. No production rates or save contents changed to force mining.

Unity compiled in the isolated review copy. Two miners' pick positions and cycle phases advanced across actual Unity frames; forcing the fixture full stopped the animation; the inspector updated to the waiting reason and 4/4 cart. Screenshot visually reviewed after editor restart. User save data was not modified by fixtures.
