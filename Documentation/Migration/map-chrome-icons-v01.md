# Map chrome icons V01

New artwork generated for City Forge V3; no legacy UI or artwork was ported.

- Canonical generated atlas: `Assets/CityForgeV3/Resources/CityForgeV3/Art/MapChromeV01/medallions.png` (16 icons, four columns/four rows).
- Original generator output is preserved at `/Users/joelinstrum/.codex/generated_images/01a0aaee-2502-7b03-8ccf-098ea344671d/exec-2dd21d5e-661e-4b49-96c4-76a84d5fca85.png`.
- Built-in image-generation tool, not the CLI. The workspace atlas is an unchanged copy. UI Toolkit Image UVs select each cell; no image derivatives or duplicate textures are created.
- Visual reference: existing V3 MainMenu navy/gold illustrated button art and approved region/district concepts in `Documentation/Design/ui-regions-districts-v01/`.
- Atlas is loaded once and cached by CfMapChrome. Text remains live UI text, independent from artwork, with labels and hover help.

## Exact generation prompt

Create a production game UI icon atlas, exactly 4 columns by 4 rows of equal square cells, square 2048x2048 image. Every cell 512x512, each centered icon fits inside central 380x380 pixels with generous identical padding. Absolutely NO text, letters, labels, grid lines or numbers. Uniform solid deep navy background #081c30 across every cell. Each icon is an exquisite hand-painted dimensional miniature contained in a circular thin antique gold rim, blue-black enamel inner medallion, subtle cyan glint. Match a refined historical city-building game's navy-and-gold menu. Strong readable silhouettes at 48px; no fine clutter or fantasy decoration. EXACT ORDER left-to-right top-to-bottom: Row1: pointing ivory-gloved hand (select), green mountain with gray summit (terrain), winding blue stream through green land (rivers), mature green oak tree (flora). Row2: sun peeking over soft white cloud (climate), curving tan dirt road with grassy middle (roads), New England wooden farmhouse (lots), red brick workshop chimney (industry). Row3: brown bound ledger book (manage/save), ascending three gold bar chart columns (statistics), gold mechanical cog (menu/settings), classical civic building with columns (civic). Row4: park bench and small tree (parks), small steam locomotive (transit), water tower (utilities), three colored rectangular land parcels (zoning). Render all sixteen equally sized circular medallions in precisely aligned equal grid cells. No drop shadows outside cell centers. This is a real asset atlas to be displayed with cell UV cropping.
