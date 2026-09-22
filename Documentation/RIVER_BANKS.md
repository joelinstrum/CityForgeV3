# V6 varied neutral wide-river banks

Major rivers now use the versioned `BanksV6Varied` family: four distinct
shoreline compositions and one neutral submerged-gravel texture. Blue is no
longer baked into the bank artwork; the animated water surface supplies the
river color. The four compositions vary gravel-bar width, grass intrusion,
stone density and larger-rock placement.

The bank shader chooses seeded neighboring variants over irregular physical
reaches, smoothly crossfades between them, and samples each with a different
phase, direction and slight longitudinal scale. The result is stable across
reloads and district clipping without a hard texture switch. This adds two
texture samples on V06 major banks but no additional material or draw call.
Medium and smaller rivers retain V04. V05 and all earlier source art remain
unchanged for rollback.

Major-river V06 also uses a width-specific submerged handoff. The neutral bed
is no longer darkened to 80%; it renders at 94%, while the shoreline-to-bed
crossfade expands from 1.84 metres to 8.48 metres. The animated water gains
opacity across a correspondingly broader depth interval (0.42 edge opacity,
0.34 deep-water start and 0.58 softness), with an 0.82 near-submerged opacity.
Together these overlapping fades replace the purple-gray inner ribbon and its
hard cutoff with pale submerged stone that gradually yields to deep blue.
Medium rivers and streams retain their existing sharper mountain-water
calibration.

Exact prompts, generated-source identifiers, processing and hashes are in
`Documentation/ArtStudies/RiverBanksV06Varied/README.md`.

---

# V5 wide-river bank split

Major rivers (generated at 144–228 metres) now use the versioned
`BanksV5Wide` set. Its pale gravel remains related to V04, while submerged
stones use a restrained slate blue-gray/green-gray wash instead of the bright
cyan shallows that became a conspicuous rim at major-river scale. Selection is
based only on channel width: 100 metres and above uses V05 Wide.

Streams and medium rivers (up to 76 metres) deliberately retain the approved
V04 resources unchanged. This preserves their clear mountain-stream character
and keeps that brighter art available for a future tropical climate treatment.
The split adds no draw calls, no additional material per river, and no
per-frame work; the three resource paths are selected once during river mesh
rebuild. Major-river bank presentation also extends eight metres beyond the
physical channel and fades through that shoulder into the underlying terrain.
The shoulder samples the same world-anchored V05 terrain grass before its
opacity reaches zero, and multi-scale noise breaks up the fade contour. This
removes the differently tinted straight ribbon without changing channel
width, collision, construction clearance or simulation. V1–V04 remain
present and unchanged.

Exact prompts and source identifiers are in
`Documentation/ArtStudies/RiverBanksV05Wide/README.md`.

---

# V4 approved light shoreline

On September 21 the user approved a shoreline direction in which deep blue
water is the darkest value, clear cyan-blue shallows are lighter, and pale
silver-gray gravel forms the dry bank without a brown or dark wet outline.
`BanksV4` supplies three new, versioned derivatives: a natural shoreline, a
sparser open-gravel alternate, and a uniform pale submerged-gravel bed. Runtime
mapping, shader blending, bend variation, mesh geometry and water animation are
unchanged. V1–V3 remain available for rollback.

Exact generation prompts, output identifiers and mechanical processing are in
`Documentation/ArtStudies/RiverBanksV04/README.md`. The approved concept is
preserved beside that document.

---

# V3 refinement — less grass in the gravel

The user endorsed the V2 direction, then requested fewer grass fingers and less grass/rock repetition. V3 retains the continuous 48m mapping and existing geometry. Its dominant alternate artwork is now `BanksV3/open-gravel.png`, an imagegen edit of `BanksV2/shoreline-gravel.png` that removes the three conspicuous sedge tufts and most descending grass. V2 sources are preserved.

The open-gravel composition contributes 70–100% depending on smooth, deterministic reach noise and bend weight; the retained V2 painting adds restrained variation. Broad material shift and the outer fade also use irregular reach noise instead of regular sine waves. Texture repetition still exists, but the recurring grassy lobes are substantially reduced. No simulation, water, river geometry, or cache lifecycle changes.

Built-in image_gen source: `exec-d6f1b940-b08d-4bf6-bdc9-6ae1e3598398.png` in the existing generated-images directory. Exact prompt and lineage: `Documentation/Validation/river-banks-v03/artwork-prompt.json`.

---

# Current direction — V2 continuous shoreline artwork

The user rejected V1 on September 16: the in-game result was a repetitive flat strip and did not match the reference. The V1 implementation notes below are historical and superseded where they conflict with this section.

V2 maps complete shoreline compositions along physical river distance. A composition spans **48 metres**, across multiple lots, with a **16-metre cross-bank coordinate scale**. Lot boundaries do not enter material selection or UVs. The original bank meshes and elevations are retained. UV2.x carries signed bend; UV2.y carries cumulative distance / 48 plus a bank-side phase. UV0.y carries cross-bank distance relative to the outer bed edge. Junction and border clipping interpolate both channels.

Two wide generated paintings contain coherent grass fingers, gravel patches, recognizable stones and sedge clumps. A slowly changing blend and signed curvature vary their contribution. Wrap edges crossfade offset samples; the whole wet-to-grass composition remains intact instead of extracting and mirroring narrow strips. The submerged floor transitions to existing fine gravel in district coordinates, preventing clamped bottom pixels from stretching into underwater streaks. A feathered outer margin reveals the underlying terrain.

Runtime artwork is in `Assets/CityForgeV3/Resources/CityForgeV3/Water/River/BanksV2/`:
- `shoreline.png`: built-in image_gen source `exec-068405d0-5d73-466b-a43e-76e719001fdb.png`.
- `shoreline-gravel.png`: built-in image_gen source `exec-ddada7b3-8e00-4520-a9ff-570616343113.png`, corrected from rejected two-panel output `exec-5ebac755-0fe0-45c7-8527-4ef02917f0ca.png`.

Sources remain in `/Users/joelinstrum/.codex/generated_images/01a0aaee-bd35-7e13-b14e-21d0bcf81741/`. Exact prompts and tool provenance: `Documentation/Validation/river-banks-v02/artwork-prompts.json`. V1 art is preserved at its original paths.

This is a richer textured surface, not new three-dimensional rock or reed geometry. Tight bends can still compress the continuous bank coordinates. It does not introduce an eroded physical shoreline, water-depth simulation, or changes to the river editing model. Visual acceptance remains the user's decision; technical tests do not establish reference fidelity.

---

# Riverbank materials V1

River banks use three compatible grass/soil/pebble texture strips. The standard strip blends toward loose gravel on the inside of bends and exposed earth on the outside. This is a presentation rule, not an erosion or watershed simulation.

## Runtime contract

- Runtime images: `Assets/CityForgeV3/Resources/CityForgeV3/Water/River/BanksV1/` (`grass-pebbles.png`, `inside-gravel.png`, `outside-earth.png`). These are lossless copies of generated originals; previous river artwork is preserved.
- `RiverBankAppearance` calculates signed bend weights over a symmetric physical-distance window, clamped to 24–120 metres and scaled by channel width. Collinear point subdivision does not change a weight at an existing position. Source endpoints receive neutral weights. Reversing centerline order reverses the sign; multiplying by bank side preserves the inside/outside classification.
- `DistrictWorldController.BuildRiver` retains existing bank vertices, elevations, channel width, water geometry and surface sampling. All five cross-section bands use the same material treatment so there is no old/new texture boundary beneath the water. Existing UV0 is retained for the legacy fallback. UV2.x carries the signed bank weight through existing junction subtraction and border clipping.
- `RiverBankSurface` samples fixed wet gravel, dry soil/gravel and grass areas of the artwork in district-space metres (12m U period, mirrored V within each material band). It blends their colors by height relative to the actual waterline; it does not shift texture coordinates with bank height, which caused visible streaks in an earlier candidate. The shader blends the three variants by signed bend strength, crossfades offset samples across horizontal repeat boundaries, and adds small district-anchored variation to the grass transition. The outer edge fades into the actual terrain. This is shader seam suppression, not a claim that generated source pixels are intrinsically seamless.
- The new grass edge stays visible at all district zoom levels; existing legacy grass-edge visibility remains the fallback when the new base texture is absent. Mipmaps, trilinear filtering and anisotropy come from the existing river texture importer.
- The bank shader retains camera-projected open-border underside clipping. Water animation, flow direction, river editing, model serialization, surface invalidation, roads and workers are unchanged.
- No material variant is selected by district name or source-point index. Extreme self-overlap remains subject to existing river geometry/Repair limitations.

## Artwork lineage

Tool: built-in `image_gen`, September 16, 2026. Joel supplied a visual reference and approved the first grass-to-gravel study as the direction. This does not constitute acceptance of the finished Unity result.

The base texture is the unmodified `Authoring/RiverBanks/v01/riverbank-transition-study.png`; its exact prompt is in `RIVER_BANK_MATERIAL_STUDY.md`. Authoring is ignored by Git; the identical runtime copy above is versioned. Two additional variants were generated by editing that base, preserving palette, material scale and top/bottom ordering. No existing canonical art was overwritten.

### Inside-gravel exact prompt

Edit this riverbank material into a compatible GRAVEL DEPOSIT variant for inside river bends. Keep exact overhead view, pixel-scale detail, muted palette, flat diffuse lighting, image dimensions, horizontal orientation and top-to-bottom grass to dry shore to damp riverbed material ordering. Keep topmost 15% olive grass and bottommost 15% dark damp riverbed consistent with source. Change middle: larger naturally irregular areas of fine tan-gray gravel and small rounded pebbles, grass transition recedes upwards by about 12% of image height, a few sparse low grass clumps but no big boulders. Fine gravel rather than uniform large cobbles. Left and right boundary bands match heights for horizontal repeat. No blue water, no text, no borders, no shadows, no illustration of a whole river. This is one continuous game diffuse texture strip for wrapping along arbitrary shorelines.

### Outside-earth exact prompt

Edit this riverbank diffuse texture into a compatible EXPOSED EARTH variant for outside river bends. Preserve directly overhead orthographic view, flat neutral lighting, muted palette, same image size and fine material scale. Retain horizontal grass-top to earth-middle to damp pebble-bed-bottom ordering. Topmost 15% stays muted olive grass matching source; bottommost 15% retains dark damp pebble bed. Change middle band to naturally worn tan-brown earth with fine silt, embedded small pebbles, subtle root fibers and grass tufts at its irregular upper boundary, fewer loose stones than source. No cliff face, no perspective, no large rocks. Left/right transition heights match for horizontal repeating strip. No water, no text, no border, no directional shadows. One continuous game material strip wrapping an arbitrary shoreline, not a scene.

## Validation

66 targeted EditMode tests passed at 2026-09-16 16:30:45 UTC. New tests cover bend sampling under point subdivision, direction reversal, mirroring, degenerate paths, clipped weight interpolation and imported assets/shader.

Inspected actual windowed Unity Game-view captures at detail, close and district zoom: an S-bend with a shallow tributary, its junction and border crossing, plus isolated saved City 042/051 copies. Capture refuses a maximized Game view. No substitute render camera was used. Visual inspection found and corrected submerged old/new texture boundaries and height-driven UV streaking before the final images.

Both saved-layout copies passed two Shape → Repair cycles through production completion methods, with width/depth retention, save/reload equivalence and repair undo. The live synthetic fixture also passed Shape/Erase UI Toolkit pointer gestures, no data/cache/mesh changes during drag, exactly one cache commit at completion, retained unaffected decoration and undo/save checks. These pointer events came through the QA bridge; they are not a claim of a fresh physical OS-mouse gesture test. No interaction code was changed.

Material-only refresh preserved saved data and surface-cache revision; clipped meshes stayed within district bounds and rebuilt deterministically. The shared surface-cache full-rebuild comparison and deferred road/object retention checks passed. The three V3 user save JSON files were verified byte-for-byte unchanged by SHA-256. Fixtures were restored; V3 remains in Play Mode on the original QA return screen (title screen). The separate Regions Review editor and workers/labor code were untouched.

Reports: `Documentation/Validation/river-banks-v01/`. Local screenshots: `QA/RiverBanks/final-bend.png` and `final-closeup.png` (QA is intentionally Git-ignored). The full local capture sequence includes intermediate rejected candidates; use the named final images.

This remains a texture/material pass: no new 3D reeds, protruding rocks, physical erosion or bank widening was introduced. Existing extreme topology limitations remain. Joel's final visual acceptance is pending.
