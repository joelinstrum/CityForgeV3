# Distant district clouds

Cloud bodies appear at LOD4 and LOD5Billboard, the two farthest district zooms. Ground shadows remain enabled at all six levels, independently of body visibility. This adds no saved fields or automatic writes.

## Rendering choice

City Forge uses the Built-in Render Pipeline (`m_CustomRenderPipeline: 0`, no HDRP package). Unity's suggested [volumetric-cloud instructions](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@17.0/manual/create-realistic-clouds-volumetric-clouds.html) require HDRP. No rendering-pipeline conversion was performed. This implementation uses textured, camera-facing cloud billboards with projected ground shadows; it is not raymarched/physically based volumetric weather and cannot be flown through. Baked internal cloud lighting receives the existing time-of-day artwork tint. Night reduces ground shadows. No new weather simulation or controls.

## Bounded work and lifecycle

`DistrictCloudLayer` creates at most two clouds in one 8-vertex mesh, plus one shadow renderer sharing the existing terrain mesh. There are two materials and two renderers. Sizes vary, alternate silhouettes are used, and some UVs are mirrored. CPU evaluation of two weather slots drives both layers from the same position/opacity arrays. Base drift is (1.65, 0.525) m/s multiplied by max(1, shortest district span / 640m), preserving visible travel across district sizes. Clouds fade near wrap boundaries. Camera-facing mesh orientation is set at district construction (the district camera has fixed orientation). Cloud altitude is established above the configured hill height at construction. Only the body renderer is disabled below LOD4; zoom does not recreate meshes or touch the district model. Two independent weather schedules (113/149 seconds) randomize positions and active duration, with 10-second arrival and 12-second departure fades. Usually one or two clouds are present, occasionally neither. A private random seed is created on district construction; zoom does not change it. Cloud silhouette alpha is feathered using coarse mip coverage, preserving interior artwork detail.

At every zoom, LateUpdate evaluates two positions/opacities from elapsed real time and writes the same cached two-element vector array to two materials and checks the terrain mesh reference, adopting a replacement after terrain rebuild. No district scans, terrain sampling, CPU animation mesh uploads, or per-frame material creation. Owned body mesh/materials are released on destruction; the borrowed terrain mesh is not destroyed. Shadow projection adds one full terrain geometry pass and two filtered alpha lookups per shaded pixel at every zoom. This is bounded rendering work, not a district presentation rebuild. Very large terrain meshes still increase its GPU geometry cost. Shadows are approximate and do not use physical sun-ray integration or cast through building volumes.

## Artwork

Built-in image_gen produced two true-alpha RGBA images, installed under `Assets/CityForgeV3/Resources/CityForgeV3/Weather/CloudsV01/`:
- `cumulus.png`, source `exec-ef2cd0ba-6ea3-4b1a-b255-abae4475adf7.png`.
- `cumulus-wisps.png`, source `exec-a418fd8d-a4f2-414f-9835-b6973a356c8b.png`.

Originals remain in `/Users/joelinstrum/.codex/generated_images/01a0aaee-bd35-7e13-b14e-21d0bcf81741/`. Prompts: `Documentation/Validation/distant-clouds-v01/artwork-prompts.json`. Import uses alpha transparency, clamp, mipmaps, trilinear filtering and anisotropy8. No canonical art overwritten.

## Validation

73 targeted EditMode tests passed fresh at18:35:04 UTC, including all six zoom stops, resource/shader loading and mesh reuse. Live checks passed on synthetic flat and isolated saved Little River Bend (2,947flora,1lot); serialized district state and mesh identities were unchanged by zoom tests. Actual normal, non-maximized Game-view captures inspected at LOD5 and LOD4. Fixture restored; no explicit Save was invoked. No physical mouse gesture test was needed or claimed.

Final short editor profile,120sample frames after45warmup frames per state: clouds off median3.35ms/p954.02ms, clouds on median3.67ms/p954.48ms.10,000unchanged cloud zoom calls took0.820ms with0reported managed bytes. Full-frame draw counts/triangles are in frame-profile.txt; UnityStats includes variable editor rendering and UI, so these are not clean per-camera GPU counters. No GPU timing or long-duration stability claim. These measurements describe the original six-cloud prototype; see the later validation below for the sparse revision. Evidence: Documentation/Validation/distant-clouds-v01; previews: QA/CloudsV01.

## QA bridge isolation

The merged Review checkout also ran the old global command poller. A test request was observed being consumed there. The editor bridge now uses the project directory name in command/result/test filenames under `Path.GetTempPath()`. Use `python Tools/Unity/map-layers-command.py COMMAND...` for this checkout; the tool aborts on a command exception. On this Mac, the OS temp directory is under `/var/folders/`, not `/tmp`. Test results are `cityforge-CityForge_-_V3-map-layers-tests.xml` there. Do not use the old global `/tmp/map-layers-command.py` while multiple checkouts are running. No Review project files were edited by this change.

## Sparse-cloud revision validation — 2026-09-16

73 targeted tests passed fresh at 19:06:12 UTC. Live checks on an isolated saved Little River Bend (2,947 flora, one lot) confirmed bodies only at LOD4/5, shadows enabled at all six levels, unchanged district data and mesh identity, and shader compilation. Normal Game-view captures inspected. Fixture restored without Save.

120-frame samples after 45 warmup frames: farthest off/on median 5.86/7.47 ms, p95 18.66/18.17 ms; closest off/on median 7.35/7.87 ms, p95 20.88/19.64 ms. Closest shadow pass added one reported draw and approximately 262k triangles. 10,000 unchanged zoom calls: 0.156–0.242 ms, zero reported managed bytes. Editor-wide timings are noisy and not isolated GPU timings; no long-duration claim. The retained full terrain shadow pass now runs at every zoom; two alpha lookups replace six. Evidence: `Validation/distant-clouds-v02`.

### 2026-09-16 — higher clouds and closer far zooms

Raised cloud center altitude from terrain height + 80m + 0.18×cloud size to terrain height + 140m + 0.25×cloud size. LOD5 orthographic framing now equals former LOD4 (fullFit); LOD4 is 0.75×fullFit, a 25% smaller view span. Camera radii now 2400m/1800m respectively, retaining mountain near-plane safeguards. LOD0–3 unchanged; bodies still LOD4/5 and shadows all levels. Live dense-copy visibility/mesh/model check passed; flat actual Game-view captures inspected at both far stops. QA fixture restored without Save.

### 2026-09-16 — closer LOD3

User requested LOD3 40% closer. Its orthographic half-span is now fullFit × 0.42 (formerly × 0.70); other zoom settings and cloud behavior unchanged. Unity recompiled and live six-stop cloud visibility/mesh/model checks passed on the isolated dense saved district; fixture restored without Save.

### 2026-09-16 — final 10% adjustment to LOD3/4

Reduced LOD3/4 orthographic view spans another 10%: fullFit multipliers now 0.378 and 0.675 respectively. Other stops unchanged. Existing cloud drift remains 1.1 m/s on X and 0.35 m/s on Z (~1.15 m/s total), shared by bodies and shadows. This originally used scaled shader time; the later motion correction below supersedes that behavior. Unity compiled and live six-stop visibility/mesh/model check passed on isolated dense copy.

### 2026-09-16 — 50% faster cloud drift

Increased shared body/shadow drift vector from (1.1, 0.35) to (1.65, 0.525) metres per second. Appearance/rest timing and fade durations remain unchanged. Unity shader refreshed; live cloud shader/zoom/mesh/model checks passed on an isolated dense saved district. Fixture restored without Save.

### 2026-09-16 — verified cloud motion correction

User correctly reported clouds fading without moving. Two real Game-view captures 10.867 seconds apart on an isolated saved Little River Bend showed exactly zero displacement of the same cloud (normalized image correlation 1.0). Earlier visibility/compilation tests did not establish motion, and the earlier separated images could include cloud respawns.

Replaced the shader built-in `_Time.y` dependency with `_CloudTime`, explicitly written to both cached materials each LateUpdate from elapsed `Time.realtimeSinceStartupAsDouble`. Two constant-time uniform writes, no mesh updates or district scans. Cosmetic weather now continues through simulation pause; zoom does not reset it. Wind remains (1.65,0.525) m/s. The exact underlying built-in shader-time failure was not isolated; do not claim a specific Unity/Metal defect.

Same-camera, same-silhouette capture comparison after correction: 14 pixels right and 9 pixels upward in 13.298 seconds, normalized correlation 0.99942. Both material clocks advanced together from 0.8805783 to 14.17375. The before/after measurements distinguish translation from fading or respawning. Live visibility/mesh/model/shader checks passed; scratch fixture restored without Save. Short 2,947-flora district profile: off/on median 7.71/8.27ms, p95 17.97/20.87ms, editor-wide and noisy. Unchanged zoom calls: 10,000 in 0.211ms, zero reported bytes. Evidence in Validation/distant-clouds-motion.

### 2026-09-16 — CPU positions and district-scaled motion

User again reports stationary clouds after the clock fix; exact viewed district/zoom still unknown (asked asynchronously). Do not claim the reported view has been reproduced/resolved. Computer Use selected the separate Review editor, which has no cloud source; no Review edits.

Replaced shader weather calculation with CPU EvaluateMotion for exactly two slots. Cached two-element Vector4 position/opacity array is uploaded to body and shadow materials each LateUpdate. No allocations, object scans or mesh uploads in that path. Integer hash replaces GPU sine hash; double elapsed time preserves scheduling. Base drift multiplied by max(1,min(width,depth)/640) so large-map movement remains perceptible. Real-time motion continues while simulation is paused.

76 tests passed fresh at 19:56:18 UTC, including same-lifetime continuous movement at 640,2560,7680m. Live isolated saved Little River Bend checks pass; both material motion arrays equal. Actual windowed Game-view pair 195658667 / 195707905 shows same cloud translated (19,-13) pixels in 9.238s, normalized correlation 0.99808 despite fading. Original images under QA/RiverBanks. Dense 2947-flora profile off/on median7.89/8.16ms p9520.18/20.59ms; editor-wide short sample only. 10000 unchanged zoom calls0.790ms/0reported bytes. Fixture restored without Save. Evidence in Documentation/Validation/distant-clouds-cpu-motion. Changes remain uncommitted.
