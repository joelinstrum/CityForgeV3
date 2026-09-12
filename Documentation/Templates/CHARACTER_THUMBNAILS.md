# 3D character library thumbnails

The library uses transparent renders of the actual runtime models, including
assembled horses, harnesses, drivers and cargo. Do not substitute generic art.

## Generate or update

1. Add a new prop ID to `CharacterThumbnailBuilder.Ids` in Unity's
   `Assets/Editor/CharacterThumbnailBuilder.cs` and a card in
   `CityForgeApp.OpenCharactersModal`.
2. Enter Play mode and open a lot. Run **City Forge → Characters → Render Library
   Thumbnails**. This creates temporary presentations in isolated preview scenes;
   it does not place or save objects in the lot.
3. Inspect every PNG for framing, pose, texture, transparency and correct team
   composition. The first four IDs use upper-body framing; other IDs show the
   complete model. Adjust framing when adding unusually shaped models.
4. PNGs are generated under
   `Assets/CityForgeV3/Resources/CityForgeV3/UI/CharacterThumbnails/<prop-id>.png`.
   The importer uses alpha transparency, sRGB, no mipmaps, uncompressed textures,
   and a 512-pixel maximum. Capture reads the actual render texture dimensions
   because Retina preview textures can be twice the requested size.
5. Open the ordinary Lot Editor character library and inspect the top and bottom
   of its scroll view. Click a portrait and confirm it arms the corresponding
   placement preview. The whole portrait/name button is selectable.

## QA helpers

**Open Portrait Library** loads saved Track in memory and reports missing images
and buttons in `QA/CharacterThumbnails/library.txt`. **Show Vehicle Portraits**
scrolls to the bottom. **Check Portrait Selection** invokes the cavalry card's
actual callback, checks its ID and modal dismissal, clears the preview and
reopens the library; it does not place or save anything. This callback check
supplements normal Game-view visual inspection.

Render source: `CreatePropPresentation`, idle pose sampled at 23% of the clip,
neutral two-light preview, contact-shadow geometry excluded. Harness geometry
is updated before capture. Existing source models and materials are preserved.

Initial batch: 13 entries (four people, Kong, bear, horse, mounted cavalry,
standalone carriage, single-horse carriage, lumber wagon, two-horse covered
wagon, single-horse food wagon). Evidence and renderer source are in
`artifacts/ui/character-thumbnails/v01/` in CityForgeMCP.

Trapper addition: fourteenth entry `mounted-trapper-v01`, full horse/rider/cargo
framing. Source and checks: `artifacts/animals/trapper/v01/`.
