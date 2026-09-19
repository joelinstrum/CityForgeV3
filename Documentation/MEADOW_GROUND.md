# District meadow V1

User request: bring default grass up to the riverbank artwork quality, using one composition across a 4 × 4 lot area and matching the September 16 grass reference. The generated square meadow uses olive grass, small broadleaf plants, tiny flowers and sparse earth/pebbles. Each composition is 40 × 40 metres (four 10m lots per axis).

Artwork: `Assets/CityForgeV3/Resources/CityForgeV3/Terrain/MeadowV01/meadow-4x4.png`. Generated using built-in image_gen; exact prompt is in `Documentation/Validation/meadow-v01/artwork-prompt.json`. Original source is `/Users/joelinstrum/.codex/generated_images/01a0aaee-bd35-7e13-b14e-21d0bcf81741/exec-8246bd33-4106-4409-bb69-c1fae526b2af.png`. Existing canonical default grass and riverbank artwork are preserved.

`DistrictGrassResource` selects the new artwork; `DistrictGrassTextureWorldSizeMeters` is 40. The district-only `MeadowGroundSurface` derives from the existing shadow receiver, retaining lighting, time-of-day tint, terrain relief and shadow behavior. Four offset samples blend smoothly between deterministic neighbouring compositions. Explicit texture gradients keep mip selection stable across hash cell boundaries. No per-lot objects or extra terrain meshes are created. Cost is four texture samples instead of one for the meadow surface. Existing grass decoration chunks and surface invalidation are unchanged.

Mountain terrain retains its separately calibrated original 5m grass and MountainGroundSurfaceV10 triplanar material. Ordinary flat and hilly district ground uses the new meadow; unrelated lot surfaces still use ShadowReceivingLotSurface. Regional flat biome colours are unchanged. District UI previews using DistrictGrassResource naturally pick up the new source.

Artwork importer retains repeat on both axes, trilinear filtering, mipmaps and anisotropy 8; this version uses uncompressed colour. A first single-sample repeat revealed a regular patch pattern in the wide Game view, so the final shader blends offset compositions to reduce that pattern.

Validation: 66 targeted EditMode tests passed at 17:23:56 UTC. Final integer-hash shader refinement was compiled and checked live via ShaderUtil and inspected in the actual non-maximized Unity Game view at closeup, bend and hilly terrain. The first sine-based hash produced visible precision artefacts on Metal and was replaced with an integer hash; final captures are in QA/MeadowV01. Shader requires target 3.5. Flat and hilly incremental/full surface-cache equivalence checks passed. No geometry or mouse-interaction changes; no new physical-pointer test claimed. QA fixture restored, three user save hashes unchanged, full pre-QA byte backups retained in /tmp/cityforge-meadow-save-backup.

## September 19 — smoother district zoom bands

Counting closest as zoom 1: zoom 1 now takes the former zoom 2 texture scale,
and zoom 2 takes the former zoom 3 scale. From zoom 3 outward, isotropic coarse
mip filtering smooths both base meadow and the flat-ground straw patches; hilly
meadow uses the same filter. Broad color variation remains. Camera zoom stops,
source artwork and mountains' separate material are unchanged. See
`Validation/meadow-zoom-smoothing-v01/` for six-stop scale checks, shader checks,
flat/hill before/after renders and validation limits. Existing ground materials
receive the new filtering value when zoom changes.
