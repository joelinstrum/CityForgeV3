# Region map direct entry and repeat action

Region district tiles enter their district directly on click. The redundant
bottom-right `Enter selected district` action has been removed from the region
intent dock.

After a successful generated-river action, that dock exposes a contextual
`Regenerate Rivers` action. It repeats the saved river counts and options with
a fresh seed, preserving the existing building-conflict checks and rollback
behavior. Failed generation does not replace the prior repeat action, while
removing rivers, starting a manual region stroke, applying climate or flora,
or regenerating the district layout clears it. The repeat choice is session UI
state and does not introduce autosaving or new persisted region data.

Validation passed 9/9 focused map-chrome EditMode tests and 83/83 broader
river, terrain-menu, map-chrome and shoreline regression tests. The focused
click test invokes the actual district tile button and verifies that the
district editor replaces the region map.
