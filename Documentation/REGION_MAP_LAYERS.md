# Region map layers

MAP LAYERS sits immediately before TERRAIN in the region header. Five independent checkboxes control Towns & Cities, District Names & Borders, Rivers, Topography, and Transportation. District Names & Borders sits directly below Towns & Cities and defaults off; the other layers default on. District labels/borders can be shown independently of town labels. Choices are stored in the region and persisted by Save Region. Old saves also default district names and borders off.

Visibility updates existing UI elements without rebuilding the map or changing scroll position. District hit targets remain available when town/city labels are hidden. Clicking outside the anchored menu or pressing Escape closes it.

Topography uses a bounded preview texture (maximum 1024 pixels per axis) generated from saved district elevation, with shaded relief and grassland/desert/forest/snow land-cover colors. The elevation preview uses a coarser sample spacing; gameplay elevation's default spacing is unchanged. The texture is released when the map detaches. Biome data defaults to grassland for existing regions; this change does not add a biome painting tool.

Transportation draws existing district roads as thin local roads and saved regional `TransportRoutes` as outlined gold highways or dark rail lines with cross ties. Route points use the region's existing X/Y map units. Terrain → Roads → Create a national pike now draws and names saved highway routes. Rail authoring is not present yet; no artificial routes are added to user regions. The QA fixture uses synthetic routes and terrain to verify all overlay types.

Validation: 18 EditMode tests passed covering independent layer visibility, preserved UI elements, old-save defaults and serialization, bounded relief previews, terrain sampling and existing river behavior. Live isolated-map checks verified all five toggles, dropdown opening, scroll/relief preservation, and actual Toggle submit input hiding topography while keeping the other layers. Screenshots and test output: QA/RegionMapLayers (local artifacts).

## Orientation correction

The region plane reflects its local Y axis and rotates -45 degrees, then compresses vertically. This matches the existing district camera: positive world X projects upper-right and positive Z upper-left. The former +45-degree UI rotation mirrored screen Y relative to the district view. All map layers, borders, and pencil input share the corrected plane; saved coordinates remain unchanged. Place and road labels counter-reflect to remain readable.

Validation: 35 targeted EditMode tests pass. `RegionMapOrientationTests` compares the actual styled UI plane against the actual district camera pose, verifies pointer coordinate round trips, and checks text orientation. Live pencil naming and save/reload also passed after the correction.

## Physical district proportions

The region map uses equal ground-axis scales (with a Y reflection for orientation) and vertical projection sin(20 degrees), matching the district camera. The former reciprocal scales distorted rectangular districts: in a 28-by-20 region, a 4-by-2 district looked nearly square. Both views now show the same geometry up to zoom and translation. Horizontal labels cancel the projection. The orientation test compares district corners and interior feature points against the real camera transform, as well as pointer conversion and label alignment.

## Horizontal labels

Town, district, and road names now use shared screen-aligned anchors. Three nested transforms cancel ground scaling/reflection, rotation, and vertical compression in order, preserving horizontal, unstretched text on rectangular regions. Ground geometry and pointer conversion are unchanged. Verified visually in Unity; all 35 targeted tests passed, including identity label axes in the actual UI transform.

## River pencil tools

Terrain → Rivers provides Create Major River and Create Small River in place of the direction selector. Click and drag the blue pencil, then release to save. Major uses a thicker preview and a deep 64m channel with thin banks; Small uses a thinner preview and a shallow 18m channel. Escape or Cancel exits without saving. Automatic generation retains its amount options and uses default varied flow; it preserves hand-drawn rivers.

The shared pencil commits only after release. River paths are stored in region coordinates and clipped into district sections. Covered road tiles are removed to leave water gaps. Building conflicts prevent placement, and save failures restore the original lists.

Validation: 37 targeted Unity tests passed, including profiles, clipping, regeneration preservation, persistence, map orientation and road behavior. Live isolated-fixture checks passed for both river buttons, pointer input and save/reload, followed by the road pencil regression check.

Automatic river generation now has a cardinal V01 baseline: district rivers
are straight two-point channels on the 10-meter district lattice, while region
trunks and tributaries use only the north/south and east/west grid axes.
Tributaries meet trunks at exact right angles. Flow direction, river amounts,
width/depth, clipping and persistence remain intact. Hand-drawn and
district-shaped rivers keep their authored geometry. Controlled curves and
additional generated variety are intentionally deferred until this alignment
is accepted in the isolated Regions Review workspace.

Remove Rivers sits beside the two drawing buttons. It clears all generated, hand-drawn, and district-local rivers across the region and saves immediately. Both region paths and district sections are cleared so reload cannot recreate them. Failed saves restore the original lists.

River sizes now include Small (18m, shallow), Large (64m, deep), and Major (128m, deep). The former Major button is now Large; the new Major is twice its width. The blue-pencil preview and saved map line both show Major at twice the Large width. Existing saved rivers retain their widths.

River buttons are ordered Major, Large, Small, then Remove Rivers. River input now filters sub-channel jitter, tight reversals and self-crossing/near-overlapping returns within the current stroke. Two corner-smoothing passes preserve endpoints and broad bends; the live preview and saved geometry use the same smoothing. Each channel keeps its selected width. Separate rivers can still meet or overlap to form tributaries; this does not reshape existing saved rivers.

The river pencil stays active after each saved stroke until Escape/Cancel. Both ends of the preview snap to nearby existing river centerlines (generated, hand-drawn or district-local). Snap distance includes both channel half-widths plus 16m tolerance. The preview and saved geometry share the same endpoints; each stroke retains its selected width. To connect two rivers, draw a connecting stroke from one to the other.

## District river shaping

District Water → River Tools replaces district generation. Shape River drags an existing bend or interior endpoint with a soft falloff; nearby endpoints snap onto other channels. Soften River brushes away sharp bends. Erase River cuts out brush-sized portions, splitting channels when needed. Radius is adjustable from 40–400m. A blue/red brush and centerline draft preview update during the gesture; release saves and refreshes river geometry, and Escape cancels. District undo restores edits.

Shaping preserves width/depth and anchors border crossings, with movement fading near the border to retain alignment with neighbors. District-authored river lists are saved as local overrides so automatic region regeneration cannot restore erased sections or overwrite shaped channels. Remove Rivers clears these overrides along with all river geometry. Building conflicts reject shaping.

At district boundaries, channel geometry extends beyond the crossing and is clipped to the district rectangle after junction merging. Water and all bank bands end flush with the border rather than leaving a diagonal cap or overhang.

Grassland and the underlying map plane use RGB (73, 89, 36), sampled from the mean sRGB color of the canonical default district grass texture. This preserves its olive green without adding tiled texture detail to the cartographic map. Other biome colors remain distinct.

District Water now shows labeled Select Water, Shape River, Soften River and Erase River buttons directly in the flyout, with the brush-radius slider below. No secondary river-tools menu is needed.

## Fixed-width district redraw (supersedes the Shape brush behavior above)

Shape River now requires pressing on an existing river. Mouse-down locks that channel's width and depth; dragging records a bounded 512-point path and displays only pooled, untextured bank outlines. No sculpt-model calculation, terrain sampling, mesh rebuilding or save occurs during pointer motion. The Water panel displays the locked width instead of a shape-radius slider.

On release, the selected reach between the stroke's nearest start/end positions along the original channel is replaced by the smoothed drawn path. Its untouched upstream/downstream portions and other rivers remain in place. Trace along the reach to replace it; a click or perpendicular tug with no progress along the original river does not commit. Width stays fixed. Textured water/banks and terrain refresh once after saving. Soften/Erase also defer their model work until release.

Validation: 46 targeted tests passed, plus live Shape/Erase/undo checks. The live checks explicitly assert unchanged river data and mesh instance IDs during a drag, then saved edits and clipped geometry after mouse-up.

Brush defaults are now 80m with a 20–200m adjustment range (halved). River-surface queries use a 128m spatial grid of channel segments, with precomputed lengths and along-river distances, so ground decoration only checks nearby segments. River refresh avoids rebuilding decoration twice when elevation is refreshed. Editor timings separately report mesh, terrain and decoration work.

## Incremental district surfaces

River editing now shares district surface invalidation with roads and lot footprints. Unchanged height samples, decoration chunks and road presentations are retained. See [district surface cache](DISTRICT_SURFACE_CACHE.md) for implementation and validation.
