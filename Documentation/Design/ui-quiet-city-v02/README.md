# Quiet city UI vision — V02

Original board: design proposal, preserved unchanged. The quiet layout is now implemented; see `../../Validation/quiet-ui-v02/README.md` for behavior, screenshots, and checks.

The city occupies the screen. Idle UI consists of a slim icon-and-number resource strip and two small primary actions: Build and Terrain. Terrain is a provisional interpretation of the repeated word “building” in the request. Small Save and Menu affordances sit within the top strip.

Population uses three faces (man, woman, child). Resource names and units appear on hover or keyboard focus; for example, `LUMBER: 3 tons`. Visible values are illustrative, not a proposal to change inventory units or conversion rules.

Build opens a compact contextual category tray. Selecting a lot dismisses tool trays and shows a small summary card. Further details expand only on request. Clicking empty terrain or pressing Escape returns to the quiet state. Tooltips must not be the only accessible names, and compact artwork should retain forgiving click targets and support UI scaling.

The board uses current gameplay as the world reference and the earlier concept for navy/brass styling only. Generated scenery is illustrative, not a renderer change. Canonical references remain unchanged.

Implementation should reuse the current cached icons and local UI updates. Opening a tray, selection, and refreshing resource values must not rebuild world presentation or enumerate district collections. Save remains explicit and manual.

See `prompts.md` for the exact generation prompt. Generated board: `vision-board.png`.

Review notes: the generator enlarged the two primary buttons beyond the requested scale; implementation should target roughly 36–44 logical pixels with adequate hit areas. It placed the lumber tooltip on a Build tray item rather than the top counter; resource inventory tooltips belong on the top counters. Build tray category art is a placeholder, not a final category taxonomy. The framing and scenario crops are illustrative rather than pixel-exact layouts.
