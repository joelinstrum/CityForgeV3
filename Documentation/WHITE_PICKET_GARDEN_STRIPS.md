# White picket garden strips

Garden/Parks now offers nine straight white-picket plantings, each one rotatable `PlacedProp`. The fence is exactly **2 m long** and was built specifically for these gardens as a cached low-poly mesh. Eleven pickets vary subtly in height and lean between two smaller posts, avoiding the rigid, oversized appearance of the standalone fence. Its matte warm-ivory material uses newly authored aged wood with visible grain, cracked paint, gray exposed wood and restrained green organic staining. The existing standalone White Picket Fence prop remains unchanged.

The overall planting footprint is **2.2 × 1.8 m** to leave room on both sides. Natural Grass covers the footprint. A dense photographic cottage border overlaps both sides of the fence with clematis, coneflowers, daisies, black-eyed Susans, delphinium, sedum, hostas, ferns and violet groundcover. Style-specific roses or purple flowers add variation, and a muted grass border grounds the piece.

- **White Picket Roses** — pink rose bushes on both sides with a low flower row at each outer edge.
- **White Picket Cottage** — tall purple flowers behind, lower purple flowers in front, and pink flowers along both outer edges.
- **White Picket Mixed** — roses on both sides, a tall purple accent behind, and low pink flower rows.
- **White Picket Coneflowers** — pink coneflower clumps with delphinium and leafy groundcover.
- **White Picket Daisies** — white daisy masses with ferns and lower cottage flowers.
- **White Picket Black-Eyed Susans** — golden flowers with sedum, daisies and groundcover.
- **White Picket Hosta & Fern** — a lower shade-garden arrangement with a restrained purple accent.
- **White Picket Clematis** — flowering vines grow through the pickets above hostas and ferns.
- **White Picket Full Cottage** — the densest mixed border, with roses and purple flower spikes layered over the complete cottage planting.

The new aged fence texture and dense planting cutout live under `Garden/AgedWhitePicketV01`. Their SHA-256 hashes are `1ef870d23ead9043fad02167622a1adb12c04affb8c5be7d1335824fad6de2a0` and `466bfd83f8a7f3829ad48098551ccaea0e8615a1d028ab4885ebc49cfe26b24c`. The combinations retain their stable Garden IDs and use the existing selection, quarter-turn rotation, Undo, in-memory serialization and manual-only Save paths. Seasonal changes hide summer flowers in winter while retaining grass, fence and winter rose foliage. No worker/labor code or player save was modified.

[Revised isolated Unity preview](Validation/white-picket-garden-v02/isolated-preview.png) shows all three on supplied cobblestone. [Focused EditMode results](Validation/white-picket-garden-v02/test-results.xml) passed **3/3** for the exact 2 m fence, aged material, grass source, front/rear plant placement, quarter-turn footprint, winter and preview appearance, and session JSON round trip. Physical UI placement, selection/Undo and disk save/reload remain for hands-on QA.

[Nine-variant family preview](Validation/white-picket-garden-v03/nine-variants.png) shows the original three plus six additional planting mixes. The v03 focused run passed **4/4**, covering all nine IDs and the Garden Library layout. The Library now places its catalog grid in a vertical `ScrollView`; the title, guidance and Done action remain outside it, so Done stays fixed at the bottom of the visible modal while the cards scroll.
