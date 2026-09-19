# Foundation garden beds

The Garden/Parks Lot Editor library now offers two shallow **2 × 1 m** house-front plantings:

- **Foundation Hedge & Flowers** — one clipped dark-green hedge along the house side, with a low flower row facing the walk.
- **Framed Foundation Flowers** — short hedge sections at both ends, three low shrubs behind a wider flower row.

Both use the established Georgian garden flower/shrub photographic cards, overhead planting image, clipped 3D hedge mesh and dark leaf material. The canopy image is cropped to its planted area so flowers fill the bed rather than stopping short of the edge. A low muted rim and dark earth ground the footprint. Local +Z is the house side; placement can be rotated in quarter turns. Winter hides flowers and the summer overhead image while showing winter shrubs and darkened hedge foliage.

## Cottage foundation additions

Five more **2 × 1 m** arrangements draw on the reference's roses, rounded bushes, purple flower spikes, and old picket fencing:

- **Open Rose Bushes** — three pink rose bushes without fencing, for direct placement against a house or walk.
- **Picket Rose & Shrubs** — one central rose, two rounded boxwood shrubs, purple accents and low pickets.
- **Picket Cottage Flowers** — a taller purple flower mass behind a lower pink flower row, enclosed by pickets.
- **Picket Rose Pair** — two roses around a single rounded shrub, enclosed by pickets.
- **Picket Rounded Shrubs** — three textured low-poly globes with purple accents and pink flowers, enclosed by pickets.

The new upright rose and purple phlox cutouts were generated as transparent photographic sprite assets in `Resources/CityForgeV3/Garden/FoundationPlantingsV01`. Source images are retained in Codex's generated image store; the project copies have SHA-256 `ea64a931edd4896ef476a531b208e0731850b8de081c4c7ac2b2748bb16b4f1d` (rose) and `d21b66362666f0cdb1e16b055513551a7eb0571f7ca668084ecc5eff37f5bbf0` (purple phlox). The rounded bushes use a cached low-poly bumpy sphere and the previously approved boxwood foliage texture. The weathered picket is a single cached 3D mesh per bed, with a lower front rail so the flowers remain visible. Four of the five additions have this picket enclosure.

[Five-piece isolated preview](Validation/foundation-garden-v02/isolated-preview.png) shows all additions on supplied cobblestone. Focused EditMode results are in [test-results.xml](Validation/foundation-garden-v02/test-results.xml): **3/3** checks pass for all seven foundation prop IDs, resources, fence footprint, winter appearance, quarter-turn dimensions and in-memory save-data round trip. This remains an isolated render and fixture check; interactive pointer placement, selection/Undo, disk save/reload and a dense Garden scene still need hands-on QA. No player save was touched.

Each arrangement is one `PlacedProp` with a new stable ID. Existing Lot placement, selection, rotation, Undo, in-memory serialization and manual-only Save paths are reused; no save migration or worker/labor changes are needed. Existing Garden pieces and source images were not changed.

[Isolated visual preview](Validation/foundation-garden-v01/isolated-preview.png) uses the supplied cobblestone surface and was captured in a temporary Unity render scene. Focused EditMode checks passed **2/2** for presentation resources, bounded footprint, quarter-turn dimensions, winter flowers, and prop ID/rotation JSON round trip. The temporary capture code was removed. Physical pointer placement, selection/Undo and saved-file reload in the interactive Lot Editor remain for hands-on QA; no player save was touched.
