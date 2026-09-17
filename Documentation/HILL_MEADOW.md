# Hill meadow V1

User reference: September 16 11:06:26 screenshot, lighter/thinner hilltop grass, richer lows, soft slope colour variation, small embedded stones and earth patches. Implemented as a continuous terrain material, independent of lot boundaries. Existing hill geometry and road/river clearances are retained. Detail stones and plants are painted texture, not new gameplay objects.

## Artwork and material

New versioned artwork: `Assets/CityForgeV3/Resources/CityForgeV3/Terrain/HillsV01/crest-meadow-4x4.png`, generated with built-in image_gen using the approved MeadowV01 image as style reference. Source and exact prompt: `Validation/hills-v01/artwork-prompt.json`. Original generated image and approved meadow/banks preserved. Repeat, mipmaps, trilinear, anisotropy8, uncompressed colour; 1254px square. Authored for a broad 4×4-lot composition, hill mapping enlarged to50m for readability (base meadow remains40m).

MeadowGroundSurface's HILL_MEADOW local variant adds a four-sample offset hill composition over the existing four-sample meadow. Height, slope and smoothly interpolated irregular fields control wear/crest blending. Broad110m and smaller28m colour fields are calculated per existing terrain vertex, avoiding per-pixel noise work. Lows receive a subtle richer tint and crests a lighter tint. River/road level corridors fade to the existing meadow as elevation approaches zero. No new renderers or meshes. Existing hill-overlay chunks are retained.

ConfigureMountainGroundMaterial configures keyword/height/texture on build and existing terrain refresh boundaries. Height range uses the same1–60m ordinary-hill clamp. Flat districts disable the variant; MountainGroundSurfaceV10 retains its separate texture and material contract. Saved data, undo, worker/labor logic and surface invalidation are unchanged. No automatic save added.

## Validation and cost

Actual normal Game-view before/after captures use identical camera and geometry; previews: QA/HillsV01. Inspected the hill crests, slopes, river transition, and isolated saved Little River Bend with2,947flora/1lot converted to ordinary hills only in scratch memory. Live shader/resource and surface-cache no-op retention/full-rebuild equivalence checks passed. Fixture restored without Save. No physical mouse gesture test claimed.

Final dense-district short profile:120frames after45warmup, material off/on median13.38/17.86ms, p9532.89/40.55ms;677draws and6,292,775triangles in both states. These are editor-wide observations, not isolated GPU timings or long-duration guarantees. Extra texture sampling has a material cost (about4.5ms median in this sample) despite unchanged geometry/draw count; broad noise moved to vertices, but this run did not establish a frame-time improvement from that optimization. No new per-frame CPU work or managed allocations are introduced by the terrain material configuration. Ordinary-hill pixels use8texture samples vs4; flat/mountain variants unchanged.

During iteration a shader parameter named texture was rejected as a Metal keyword; renamed meadowSampler and revalidated compilation. An incomplete paused-editor profile and a profile of the failed shader were discarded. QA play now clears EditorApplication.isPaused, and resume is available through the project-specific bridge.

Fresh targeted EditMode results: 77/77 passed at 2026-09-16 21:35:28Z.

### September16 — preserve meadow hue on hills

User likes the lighter crest art but questioned the different green. Ordinary hills already inherit MeadowV01 as their base; the HillMeadow fragment function additionally multiplied RGB by (.91,1.005,.95) in low areas, introducing a green bias. Replaced both low/crest colour multipliers with neutral scalar brightness (.96 to1.055), retaining the lighter crest texture, height/slope blending, and broad irregular variation. No texture replacements or hill-overlay changes. Live shader/material and surface-cache checks passed; windowed Game-view capture inspected at QA/RiverBanks/Pine-Ridge-214720651.png. Fixture restored without Save.
