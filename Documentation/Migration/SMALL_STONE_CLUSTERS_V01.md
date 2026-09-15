# Small stone clusters — 2026-09-12

Added two supplied PNGs to the shared Lot/District flora Stones catalog:

- Small Stones: `stone-cluster-4`, from Downloads/flora/stones/stone-cluster-4.png.
- Small Pebbles: `pebbles`, from Downloads/flora/stones/pebbles.png.

Resources live in CityForgeV3/Flora/StonesV01. PNG copies are byte-for-byte
identical to the supplied RGBA sources; dark hidden RGB is not a painted backdrop.
Importer settings match existing stone-cluster-2, with unique metadata GUIDs.
Both use the full sheet at the established 4 m cluster width, existing near-bottom
pivot, stone rotation/submergence and shared persistence behavior. The individual
pieces remain small. Menu previews use representative sprite rectangles from the
same PNGs; placed sprites retain the entire source composition. Existing entries,
accepted district decals and tree appearance are unchanged.

Verified Unity compilation/import, source SHA-256 equality, both menu cards,
and both placed sprites in a disposable Lot in normal docked Game view.
The initial 2.2 m test footprint made Small Stones too faint; final previews use
4 m and a closer QA camera. District menu wiring shares the same catalog/card
factory; district placement and save/reload were not separately exercised.
Joe visual acceptance pending. Full project tests were not run for this asset addition.

Evidence and source hashes: CityForgeMCP/artifacts/flora/small-stones-v01/.
Editor review: City Forge > Flora > Repairs > Small Stones / Small Pebbles;
Show Stones Menu opens the real library only inside this disposable QA session.
