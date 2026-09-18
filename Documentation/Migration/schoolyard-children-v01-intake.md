# Schoolyard children V01 — pre-rendered Automata port

The canonical source is `/Users/joelinstrum/Downloads/3d characters/18th-century-schoolchildren/`. Four ZIPs and four PNG previews were copied byte-for-byte to `Documentation/Migration/SourceArchives/SchoolyardChildrenV01/`. The previews are static T-pose references. Source and derived SHA-256 hashes are in `schoolyard-children-v01-source-manifest.json`; the archived files were checked against their originals.

| ZIP | FBX animation actions | Mesh vertices |
| --- | --- | ---: |
| `18th-century-boy-1.zip` | idle, walk, run, clap | 2,396 |
| `18th-century-boy-2.zip` | none | 2,379 |
| `18th-century-girl-1.zip` | none | 2,382 |
| `18th-century-girl-2.zip` | idle, walk, clap | 2,418 |

Blender 5.1.2 imports all four with 41 identically named bones. The bind poses differ, so boy 1's idle/walk/clap actions were applied to boy 2 and girl 2's to girl 1 for the derived bake. The run action is not used. `Tools/build_schoolyard_children_atlases.py` renders intermediate directional pose strips; `Tools/pack_schoolyard_children_atlases.py` packs those into four intermediate character atlases. `Tools/compose_schoolyard_group_atlases.py` then pre-composes five appearances (boy 1 twice), their shadows, walking paths, idle moments, and claps into one 32-frame loop for each of eight directions. The intermediate character atlases are build inputs, not runtime assets. The eight cropped 1792 × 512 group atlases and thumbnail are under `Assets/CityForgeV3/Resources/CityForgeV3/Automata/SchoolyardChildrenV01/Group/`.

The Lot Editor's Automata library contains **Group of 18th Century Children** (`group-18th-century-children-v01`). One placement occupies an 8 × 8 m footprint. The user selects the entire group, with a visible ground outline, then moves, rotates, deletes, or undoes it as one lot object. Placement stops after one click so a subsequent click can select an existing group. The saved data holds only the clip ID, position, and quarter-turn rotation. It does not store individual children or animation state. No automatic disk save was added.

`automata-catalog.json` defines the clip's name, thumbnail, footprint, atlas dimensions, facing count, and playback speed. `AutomataClipCatalog` loads these definitions, and the shared `AutomataClipPlayer` displays one sprite frame for the whole group. It advances a frame index and chooses the nearest pre-rendered direction for the camera. There is no per-child script, runtime pathfinding, pose selection, or skeletal animation. Another scene, such as business people talking, can use the same player by adding atlas frames and a catalog entry. Shared atlases and sprites are cached across instances; the selection outline is shared and drawn only for the selected group. Updates are culled by visibility and character LOD, and placement is capped at eight groups per lot.

The former runtime presentation used five character and five shadow renderers per group. The flipbook uses one active scene renderer; its second renderer is enabled only for selection. Cropping transparent margins reduced runtime atlas pixels from 9.44 million to 7.34 million and PNG bytes from 6.24 MB to 2.29 MB, despite adding eight group-facing sheets. These are asset and renderer counts, not measured draw calls or frame-time results.

The Automata presentation stays separate from props, avoiding the existing full prop-presentation rebuild on a local edit. Its edit notification avoids the generic lot-wide ID repair scan because new placements already receive unique IDs. Restoration touches at most eight Automata presentations. The pre-rendered scene follows the terrain at its center; independent foot placement on uneven ground is outside this flipbook contract, so a flat schoolyard is the intended placement surface.

Validation: the targeted Unity EditMode test passed, and two unsaved offscreen captures show the selected group beside the active 3D schoolhouse at different frames. Evidence and remaining limits are in `../Validation/schoolyard-children-v01/README.md`. Live Testy District 9 review, night lighting, rotated lots, representative dense-district CPU/allocations/draw calls/frame-time spikes, and long-duration stability remain open. No lot, district, or region was saved during this port.
