# Town Center V01

Joe supplied `/Users/joelinstrum/Downloads/buildings/town-center/town+center+3d+model.zip`
(SHA-256 `32177a2d9df625c29a803b1da1c98226b4980a6e01fa9899ef73eae55c9c0b83`).
All six extracted files are preserved unchanged under
`Assets/CityForgeV3/Resources/CityForgeV3/Buildings3D/TownCenterV01/Source`.
The supplied daytime and nighttime images are visual references, not instructions.
No legacy project content was ported.

## Blender derivative

`Tools/build_town_center.py` imports the source into an isolated Blender process,
applies a 14.5 scale to establish approximately 10.54 m height, and retains its
original shell, UVs and textures. The authored footprint, including porch,
steps and hanging sign, is approximately 14.2 × 11.64 m.

The deleted rear elevation is rebuilt with a thick wall, corner and horizontal
timbers, and a stone base. Its siding and stone textures are unlit orthographic
bakes of intact portions of the supplied model. The derivative has separate
window sashes, transparent panes, attic inserts, floors, a rear interior lining,
simple shelves/desks, and lantern glass/housings. It does not reconstruct a fully
furnished, navigable interior or add an opening door.

Full and reduced FBXs and the two new texture bakes are under `Derived`.
The full Blender master is `Authoring/Buildings/TownCenterV01/TownCenter_Repaired.blend`
(local authoring files are ignored by the existing repository policy).
Run the Blender script with `-- <project path>` to reproduce these assets.
`TownCenterBuilder.Build` imports the derivative, bakes FBX transforms into Unity
meshes, and builds the materials, two visual LODs, package, and catalog prefab.
Unity's FBX coordinate contract maps Blender `(x,y,z)` to `(-x,z,-y)` here.

## Lighting and interior activity

The catalog identity is `town-center-v01`, under Buildings → Civics. One building
changes lighting with the existing district/Lot presets: day 0, evening .65,
night 1. Shared materials remain unchanged; `BuildingNightLighting` uses per-instance
property blocks for interior/attic/lantern emission. Five bounded, shadowless
porch/sign lights supply nearby spill. There is no per-window realtime light.

The upper room reuses the existing Gentleman and Lady Strolling Automata clip:
two people, a 20-second walk/pause/return loop, eight facings, authored at 2 fps.
It is a pre-rendered decorative pair, not district population, labor, or indoor
pathfinding. The gentleman's existing costume is Victorian. Source animation
lineage and texture costs are recorded in `couple-stroll-v01.md`.

The indoor renderer stays at its room anchor, faces the active camera, and uses
depth testing/writing so walls, floors and sash bars occlude it. It deliberately
omits the outdoor player's ground-clearance offset. Visibility varies with the
walking phase and viewing angle; it does not promise both people in every window.

`BuildingInteriorAutomata` checks only its cached shell bounds, camera and fixed
local light bindings. Distant/offscreen buildings stop clip advancement and
disable local lights. The far mesh has no actor renderer or lights. Shadow copies
disable the interior actor object, preventing duplicate animation. Shared atlas
and sprite caches are reused with the existing outdoor player, without changing
outdoor animation behavior. No district scan, redraw, navigation change, worker
optimization change or progress save was added.

The read-only bundled Lot `town-center-civic-v01` composes this building on a
2 × 2 Civics Lot. It appears under **Civic → Browse Civic Lots**, requires a
road, costs $2,500, contributes no population, and also supplies the Town Center
founder choice. It is a project resource rather than a player save; placement
still uses the normal explicit district action and manual persistence contract.

Validation and measured limits: `Documentation/Validation/town-center-v01/README.md`.
