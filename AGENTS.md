# City Forge V3

City Forge V3 is a clean successor to City Forge - Foundations Next.

- Treat the previous project as read-only reference material.
- Port assets deliberately and record every port in `Documentation/Migration/`.
- Do not copy legacy UI code, prefabs, styles, or screen layouts.
- Build UI from tokens, reusable components, patterns, and composed screens.
- Keep simulation, spatial representation, and presentation independent.
- Hybrid buildings must pair a 3D spatial proxy with calibrated directional
  renders generated from a shared camera and foundation contract.
- Preserve canonical source artwork. Derivatives receive new paths and lineage.

## District performance rules

- Treat deep collection counts and scans per frame, simulation tick, or routine
  UI refresh as a code smell. Never enumerate every object in a district just
  to obtain a count, total, status, or change-detection key during normal play.
  Use maintained counters, cached aggregates, direct ID lookups, and shared
  spatial indices. Reading an existing constant-time collection `Count` is fine;
  `Count(predicate)`, repeated filtering/summing, and serializing the district
  to detect changes require scrutiny.
- Never compare every district object with every other object. Bound queries by
  spatial buckets, nearby graph nodes, or an explicit work budget. Stop once
  enough matches are found, and stagger expensive retries.
- Outside loading/restoration and explicitly requested bulk operations, full
  district redraws, presentation rebuilds, and UI recomposition should be
  exceptional. Panning, zooming, selection, worker updates, harvesting, and small
  edits must update only affected objects, UI elements, or local render batches.
  Saving should serialize state without rebuilding its presentation.
- Reuse shared systems across existing and future buildings. Cache repeated
  sprites, textures, materials, and geometry; batch repeated static rendering.
  Shared textures alone do not guarantee low draw-call counts. Keep animation
  and selection working when batching or caching presentations.
- Maintain caches and counters incrementally on add, remove, move, and state
  changes. Define their invalidation/rebuild boundaries for bulk edits, undo,
  district changes, and reload. Avoid rebuilding a whole district index or
  navigation graph for an unrelated local change.
- Flag costly exceptions to Joe before implementing them, including costly
  existing paths that the proposed change would retain. Explain what triggers
  the work, how often it runs, how much data it touches, and why it is needed.
  Suggest concrete alternatives such as an incremental update, cached total,
  spatial lookup, local batch rebuild, or staged work. Record any justified
  exception and its measured cost; do not silently ship a full scan or redraw.
- Validate performance-sensitive changes with representative dense districts.
  Measure the relevant CPU time, allocations, draw calls, and frame-time spikes
  before and after. Report test size and remaining limits; short profiling does
  not establish long-duration stability.
- District and region persistence is manual. Only an explicit Save action may
  write progress to disk. Do not add timer-based, edit-triggered, navigation,
  or quit autosaves. Keep in-memory undo independent from disk persistence.

## Active City Forge V3 testing handoff

Joe performs interactive testing in the Unity editor opened directly on
`/Users/joelinstrum/dev/CityForge - V3`. Leave that editor open so it can import
validated changes; do not restart or drive it unless Joe explicitly asks. Use
temporary isolated project fixtures for destructive or headless QA. Do not sync
to, restart, or otherwise use `/Users/joelinstrum/dev/CityForge-Regions-Review`.
Finish validated work by committing it locally. Keep Lot, district, and region
persistence manual, never write player progress automatically, and do not push
or merge as part of this workflow.
