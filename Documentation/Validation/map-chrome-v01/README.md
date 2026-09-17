# Region / district map chrome V01

Implemented on `lot-updates`; installed selectively into CityForge-Regions-Review. The main CityForge - V3 project was not modified or controlled. Existing uncommitted statistics work remains intact. No player save was written; Testy's save timestamp remains September 16, 2026 15:45:20.

## Behavior

- Shared navy/gold tokens, serif headings, reusable icon buttons, and one cached 16-cell illustrated atlas.
- Region: left terrain/rivers/flora/climate/roads shortcuts, map layers, Save/Menu/Statistics, district selection inspector and explicit Enter District. Selecting a tile updates its inspector and old/new tile borders without rebuilding the map.
- District: shared header, cached treasury/population/food/lumber/stone/brick totals, season and Pause/Play controls, terrain/builder palettes, industry/labor/resources access, and a scrollable object inspector with saved lot preview.
- Tool category/mode switches (including B/T keys) recompose only the bounded palette. Labels replace placeholder tool glyphs; category icons use the atlas. Existing tool availability remains unchanged.
- District Info, selected object, and founding prompts share the right-side space without duplicate overlapping panels.
- Scoped modal styles apply to region/district dialogs; lot-editor visuals are unchanged.
- Save remains explicit, retaining Saved/Save Failed feedback with independent icon and caption.
- Removed the old twice-per-second all-quarry warning scan/banner. Selected-object warning callbacks retain operational diagnostics.

## Validation

- 33 targeted checks: 4 map chrome regressions plus 29 lot stats, district simulation/economy, site preparation and timber regressions.
- Map tests preserve region map/header object identities across district selection; preserve district viewport/header during local palette changes; verify caption updates retain their icon, and verify region shortcuts open the requested tab.
- Added assertion that the regional rail stays above the map in hierarchy order.
- Unity compilation and `git diff --check` passed.
- Live review: Testy District 9, 9,951 flora; region and district layouts, illustrated category atlas, local mode switching, saved-lot inspector/preview. Region Game view screenshots may include Unity's existing editor-only 'No cameras rendering' overlay because that screen is UI Toolkit; it is not a game UI panel.

## Performance evidence and limits

Same new UI, two update strategies measured in the isolated Unity Editor:

- Former whole-screen recomposition strategy: 20 updates, 893.7151 ms total (~44.69 ms/update).
- Local palette strategy: 20 updates, 50.3414 ms total (~2.52 ms/update).
- 1,000 fixed HUD refreshes: 134.7423 ms total (~0.135 ms/refresh). Normal refresh frequency is twice per second.
- Local palette updates retained the screen, world controller, and world Transform count.
- Short 180-frame Editor sample: median 6.73 ms, p95 16.86 ms, maximum 24.68 ms; reported draw calls averaged 440.39 (439–517). This includes the existing rendered district and Editor overhead, not just UI.
- The managed allocation API returned zero even for allocating UI reconstruction; that counter is not trustworthy in this Editor session and is not evidence of zero allocation. Palette reconstruction allocates UI elements on user tool changes. HUD refresh formats a fixed number of labels.
- No before/after full-game draw-call capture or long-duration stability proof. The timing comparison isolates the update strategy with the same new UI, not old-vs-new overall rendering. One cached atlas is shared by the new medallion images; no new world renderers were added.
