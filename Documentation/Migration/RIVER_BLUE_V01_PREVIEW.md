# River blue V01 preview

Date: September 20, 2026

`RiverBlueV01` is a versioned river-surface art study derived from the supplied
`river-blue-concept.png` color and material reference. It adds a 1254×1254 RGB
teal-blue looping surface and a separate 1254×1254 RGBA pale crest atlas.

Only `RiverWaterTextureResource` and `RiverWhitecapTextureResource` now select
the V01 assets. River geometry, UV scale, flow vectors, two-phase advection,
wave distortion, depth/opacity gradient, time-of-day lighting, whitecap pulse,
materials, shaders, draw calls and rebuild behavior are unchanged.

The prior unversioned water and whitecap textures remain in place for rollback.
Exact source lineage, prompts, rejected output and processing are recorded in
`Documentation/ArtStudies/RiverBlueV01/prompts-and-lineage.md`.
