# Dirt path overlays V01

Imported four user-supplied PNGs from `/Users/joelinstrum/Downloads/overlays/dirt-path` into `CityForgeV3/LotTextures/DirtPathV01`.

Lot Editor → Overlays now includes Dirt Path — Straight, Curve, T-Junction, and Cross. Stable IDs are `dirt-path-straight`, `dirt-path-curve`, `dirt-path-tee`, and `dirt-path-cross`. Each uses the existing 10 × 10 metre overlay footprint, alpha-blended ground receiver, painting, quarter-turn rotation, selection, deletion, and saved placement data. These are visual overlays; no new road or pedestrian routing logic is introduced.

All source PNG bytes are preserved. The curve's original filename is `dirth-path-curve.png`; only the runtime filename corrects that typo. Unity metadata enables sRGB color, mipmaps, transparent-edge handling, clamped UV edges, and no importer power-of-two rescaling. Normal runtime quality/mipmap settings still apply.

Registered in both the runtime overlay options and the descriptive texture catalog. Existing overlay IDs and the first-option fallback are preserved. Reused the existing renderer; no new UI panel or update loop.

Validation and hashes: `Documentation/Validation/dirt-path-overlays-v01/`. Updated the existing persistence test for 12 overlays and the previously stale eight-base-texture count. Its second placement now uses a known cell instead of a stale hardcoded screen coordinate. Original Downloads files and the main Unity project are unchanged.

## Curve artwork replacement — September 16, 2026

Replaced the curve with the updated `dirth-path-curve.png` supplied at the same
Downloads path. Stable overlay ID, runtime path, Unity GUID, and importer settings
are unchanged, so existing placements receive the revised artwork. Synced the
image to Regions Review. Previous artwork is archived as
`Documentation/Validation/dirt-path-overlays-v01/original-curve.png`; updated
source hashes are in `curve-replacement.txt`. No source image was edited.
