# Stone bridge original-end derivative — V02

Historical V02 record. See the sibling V03 documentation for matched module joints and graded earth approaches.


This replaces the reflected and flattened StoneV01 runtime package. The canonical artwork is Joe's `stone+bridge+3d+model.zip` at `/Users/joelinstrum/Downloads/buildings/bridges/`, SHA-256 `edc4d94123782898fd5cb39376e825482bea335759f803170a2ca37afe84e7b9`. The unchanged imported review scene is `stone-bridge-review-v01/bridge-review.blend` beside it. The old derivative remains available in Git history; no canonical file was edited.

`Tools/export_stone_original_ends.py` cuts the source's single 9,336-triangle mesh at source X = −0.18 and +0.19. The left and right pieces retain their original vertices, UVs, texture, arches, parapets, and different profiles. An exporter check found 2,096 left and 1,387 right original source vertices at their exact positions after rigid coordinate conversion, with less than 0.0001 m error. Cut faces are tessellated. The source albedo is copied byte for byte. Runtime output is `Assets/CityForgeV3/Resources/CityForgeV3/Bridges/StoneV02/`.

Each bridge places one left end, one right end, and as many complete center modules as the crossing requires. The runtime rounds to the nearest whole number of center arches and shares only the small residual fit across them; it never turns the entire middle into one elongated arch. The ends receive a rigid vertical translation so their differently sloped source endpoints meet the approach roads; their geometry is not flattened or stretched. Every repeated center module removes the source section's endpoint slope so adjacent copies meet cleanly. A sampled source deck profile repeats with the geometry and supplies the stone travel elevation. The bridge planner uses the nearest dry bank anchors that allow eight-meter approaches. A typical 30 m river produces about a 50 m crossing, close to the source bridge's 34.3 m body plus approaches.

The bridge's supports retain their source depth. They are not extended to the riverbed; very deep channels may leave the original feet above the bed. Bridges already saved with long V01 bank endpoints retain those endpoints until removed and rebuilt. District persistence remains manual.
