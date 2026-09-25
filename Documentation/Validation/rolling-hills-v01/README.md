# Rolling hills V01 — September 25, 2026

## Intent and scope

The reference is Joe's `hill-goal-fixed.png`: broad overlapping rolling forms,
long slopes, restrained light/dark contrast, and substantial nearly flat land.
Existing `MacroGrassV05` remains the district grass source and color. No
per-hill tint, painted shade, dirt, or rock layer is used to create relief.

## Implementation

- Non-mountain relief now places 2–5 broad, rotated elliptical forms instead of
  3–14 compact domes. Later forms overlap two spread anchors, with a smooth
  cubic shoulder. The existing
  deterministic seed, height control, road/lot/river clearances, edge leveling,
  mesh normals, collider, and height sampler remain shared.
- The rolling-hill shader mixes a rotated, offset sample of the same approved
  grass source with the world-anchored base sample. Its broad blend field changes
  texture placement, not hill brightness or hue. Distant procedural grass grain
  is disabled on rolling hills. A shared 3x horizontal normal response makes
  small physical slopes readable under the existing sun and ambient contract;
  it does not change grass albedo. Flat grass and mountain artwork are unchanged.
- The crest-meadow color mask and dry grass/earth overlay are removed from
  rolling hills. Mountain ground and its overlay remain on their prior path.
- All changes occur during district construction or a terrain edit, or in the
  shared terrain shader. No frame, tick, time-change, save, or all-object update
  path was added. The removed overlay avoids its chunk meshes and draw calls on
  rolling-hill districts. The shader has one additional grass sample per pixel
  versus flat ground.

## Validation

The isolated Unity 6000.1.12f1 EditMode run passed 6/6 focused terrain tests
([results](elevation-tests.xml)). Three 35 m, 70% coverage seeds on a 1.28 km
district had 14,570–19,654 of 46,656 interior samples below 1 m, peak relief
31.68–32.08 m, and maximum adjacent 5 m steps of 1.21–1.52 m. Road, river,
edge, deterministic reload, and mesh/sampler checks passed.
The surface cache, map preview, river appearance, and far grass zoom regression
filter passed 19/19 ([results](regression-tests.xml)).

Synthetic 1400×1000 captures use the actual world-lighting preset values and
the approved grass asset on one isolated terrain mesh: [Morning](rolling-hills-morning.png),
[Noon](rolling-hills-noon.png), [Afternoon](rolling-hills-afternoon.png),
[Evening](rolling-hills-evening.png), and [Night](rolling-hills-night.png).
They are visual checks of terrain lighting, not a full district Game view.
The shadowed sides stay connected to the geometry; no crest color bands or
separate dirt patches are visible. Evening and Night remain very dark under
their existing world-lighting contract.

In eight warm runs constructing the same 66,049-sample synthetic terrain, the
prior generator averaged 36.98 ms and the new one 27.02 ms; both retained
266,752 managed bytes per build. A read-only copy of `Large Region Test` /
`Shadow DIstrict` (5,353 flora records, one river, 263,169 terrain samples)
averaged 198.50 ms before and 153.85 ms after, with about 1.27 MB retained
per build. This measures construction CPU and retained memory, not frame time.
The isolated single-mesh captures recorded 0.09–0.18 ms `Camera.Render` CPU
per call across presets. They do not measure GPU time, full-district draw calls,
frame spikes, or long-duration stability. The new grass blend adds one texture
sample; the rolling-hill overlay renderers are removed. Dense in-play graphics
profiling remains the limit of this check.

The live City Forge V3 editor and player save were untouched. No save action
was invoked in the fixture.
