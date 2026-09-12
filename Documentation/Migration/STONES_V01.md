# Stone families — 2026-09-11

Source: /Users/joelinstrum/Downloads/flora/stones, two original transparent RGBA PNGs. Runtime copies are byte-for-byte identical. Do not infer opacity from the RGB preview: hidden RGB contains brown/black colors. Image-generation attempts were rejected and are not used.

Shared StoneFloraCatalog defines Mostly Large, Large and Small, Mostly Small. Lot Flora Rocks tab renamed Stones; District Flora has Trees/Stones tabs with identical family cards and same placement IDs. Existing lot/district persistence handles these flora IDs. No random-tree pool contamination.

Mostly Large uses upper-left cluster; Large and Small uses upper-right slab with surrounding small stones; Mostly Small uses smaller-stones sheet at 2.2m width. Larger families 4m width. Atlas regions use existing PNG alpha, authored near-bottom pivots; sources not cropped or repainted. Trees' projected shadow geometry is skipped for low stone sprites. Existing camera-facing flora and terrain depth apply.

Verification: compiled; all three families displayed in disposable lots in normal docked Unity Game view. No original artwork changes. District uses same sprite factory and resource IDs; District physical placement/save roundtrip not exercised this turn. Final region/pivot refinement needs Joe's visual review. Historical scripts are not idempotent; use installed code as current truth. Sprite.OverrideGeometry rejected by Unity and removed entirely.
