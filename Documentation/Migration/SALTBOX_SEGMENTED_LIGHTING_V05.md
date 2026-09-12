# New England Saltbox — Lot Editor v05

Catalog: **3D Buildings → Residential → New England Saltbox**.
Asset ID: `new-england-saltbox-v05`.

Derived from the accepted `v04-room-controls/Saltbox_Independent_Window_Lighting_v04.blend`. The original archive, FBX, Blender sources and 28 texture images remain unchanged. Unity receives a separate FBX with 28 visible mesh objects and 38,825 triangles; hidden reference windows and Blender review lights are excluded. All 28 copied color textures match the source bytes. Height is provisionally 9.5 m, footprint 9.72 × 10.36 m; no change to source geometry scale.

Unity package: `Assets/CityForgeV3/Resources/CityForgeV3/Buildings3D/SaltboxV05/`. One detailed mesh representation preserves the reviewed pane topology at all usual editor distances. Further performance LODs have not been authored.

All non-pane slots retain their original color texture and UVs, mapped to opaque Unity Standard materials (metallic 0, smoothness 0.1). Thirteen separate pane materials retain the same underlying texture and add the approved warm emission. Each `CF_Lighting/Window_*` node has its own `BuildingNightLighting`, with an explicit renderer/material-slot reference and a small point spill light (Unity runtime equivalent to the Blender area-light preview). `SetRoomLit(bool)` controls the room; `Building3DPackageInstance.SetNightAmount` supplies the lot's day/night factor. Both emission and spill use the room switch. The existing default remains enabled for other buildings.

Default lit rooms: Front_02, Front_Upper_01, Right_01, Rear_Upper_02. Day = off; Evening = 0.65 brightness; Night = full. This is a fixed initial occupancy pattern, not a gameplay schedule. Individual room choices are runtime controls, not yet separate saved-lot fields or editor UI controls.

Pane materials must keep `_EMISSION` enabled and a positive authored emission color. Unity's Standard material validation strips the keyword when the authored color is black; the per-instance property block handles day/off instead. The original texture is visible with emission zero. Embedded attic and unsegmented side windows remain unmodified, as in the accepted Blender derivative.

Rebuild: run `export.py` in background Blender, copy the FBX and unchanged textures to the package Source/Textures folders, then **City Forge → 3D Buildings → Create Saltbox Package**. The builder preserves material/prefab GUIDs across rebuilds. `rooms.json` preserves individual room IDs, material slots and anchor positions.

QA: normal windowed Unity Editor Game view. **City Forge → QA → Open Saltbox Lighting Lot** opens an unsaved fixture; no user lot is overwritten. **Check Saltbox Room Controls** verifies each of the 13 pane property blocks and spill lights in isolation. Day/night screenshots and runtime/source validation files accompany this report. User visual acceptance in Unity is pending.
