# National pike drawing

Region → Terrain → Roads → Create a national pike closes the terrain menu and arms a map drawing overlay. A pencil indicator follows the pointer over the region. Hold the left mouse button to trace a road; release it to enter a name. Save Road commits the route and saves the region. Draw Again replaces the draft; Cancel / Escape discards it. A click or very short drag stays in drawing mode instead of creating a road.

The stroke is transformed through the map's rotated/compressed UI plane into region map units. Pointer capture preserves the gesture through boundary crossings; points clamp to region edges. Scrolling is suppressed during the drag. Drafts are bounded to 4,096 samples and never enter saved data before naming succeeds. Empty names are rejected. Save failure retains the draft and reports the error.

Named pikes are saved highway routes in `RegionSaveData.TransportRoutes`, appear on the Transportation layer, and receive a map label. Drawing enables that layer. Saving now rasterizes the stroke onto the shared 10m district grid. Cardinal steps connect through district boundaries, existing road materials are retained, and neighboring tiles receive straight/corner/T/cross topology. Preparation is separate from applying the edits; a failed save restores all changed road lists. The map uses a tan route line and the districts use the generated artwork.

Road tiles intersecting river water (including their footprint) are skipped. The route resumes on dry land on the far bank; the map also leaves a visible water gap. No bridge or vehicle connection spans that gap. New district Dirt Road placements use the same package. Existing saved DirtRoadV1 tiles retain their original art.

The region era defaults to Founders. Surface policy is dirt for Founders/Industrial, early concrete for Discovery, and blacktop for Modern. This pass supplies the dirt artwork; dedicated later-era highway geometry/art and era advancement UI remain future work. No old map-only pikes are automatically backfilled.

Validation: 34 EditMode tests passed, including water gaps, cross-district continuity, junction material preservation, era surface policy, package resources and shader compilation. Live isolated-map checks exercised the Roads button, menu dismissal, actual pointer down/drag/up events through the rotated map, click-only rejection, naming on release, empty-name rejection, Save Road button input, persisted name/geometry, map label, and draft cancellation. The live pencil save produced 397 dirt road tiles; reloading verified their package and that all remained outside river water. The QA save uses a temporary directory. Evidence: `QA/NationalPike/`.

Artwork lineage and exact prompts: [NATIONAL_PIKE_DIRT_ART.md](NATIONAL_PIKE_DIRT_ART.md). Unity material preview: `QA/NationalPike/dirt-pieces.png`.
