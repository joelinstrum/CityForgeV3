# Dark Cobblestone lot surface V01

**Superseded September 18:** Joel rejected this generated texture. The active
Base and Overlay option now uses his supplied `cobblestone-texture.png`; see
`STONE_FOUNTAIN_AND_COBBLESTONE.md`. The saved ID remains `dark-cobblestone-v01`
so existing lots resolve. This document and the generated ArtStudy record the
earlier attempt only.

The Lot Editor now offers **Dark Cobblestone** in both Base and Overlays under saved ID `dark-cobblestone-v01`. Both use the same 1254 × 1254 authored resource at `CityForgeV3/LotTextures/DarkCobblestoneV01/dark-cobblestone`. A 10 m base repeat matches one 10 × 10 m overlay tile. The base ends at the lot line; an overlay can continue into the one-tile exterior ring. Dark Cobblestone does not create a pedestrian route. Existing base, overlay, selection, Undo and manual Save paths are reused; existing saved IDs and the approved gray road material remain unchanged.

The source, exact built-in imagegen prompt and byte-identical runtime lineage are in `ArtStudies/DarkCobblestoneV01/`. The Unity importer retains the full 1254-pixel non-power-of-two texture. An isolated 30 × 30 m Lot fixture rendered the actual base and overlay paths at the Lot Camera angle, and a session JSON round trip passed. The overlay screenshot was captured without its temporary yellow selection highlight. The fixture and Editor QA code were removed, and no Save was called. The new focused EditMode test is written but was not run because the user's Unity Editor was in Play mode; stopping it could lose unsaved state. Physical UI placement and disk save/reload remain open. Earlier forest/regional suites are unrelated.
