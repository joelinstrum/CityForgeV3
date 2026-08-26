# City Forge V3

City Forge V3 is a clean Unity 6 project for a constrained-camera 3D/2D
hybrid city builder. The current vertical slice establishes:

- the approved City Forge splash artwork;
- a new reusable UI system built with UI Toolkit;
- a main menu where only Lot Editor is active;
- a true 3D lot workspace with an isometric camera and grid;
- an explicit boundary between simulation, spatial proxies, and presentation.

Open the project with Unity `6000.0.69f1` and enter Play Mode from the sample
scene. Runtime bootstrap code constructs the current prototype without relying
on legacy scenes or UI prefabs.

See [V3 architecture](Documentation/Architecture/V3_FOUNDATION.md) and the
[Lot Editor coordinate contract](Documentation/Architecture/LOT_EDITOR_COORDINATES.md).
The [initial migration record](Documentation/Migration/0001_INITIAL_ASSETS.md)
documents the original asset boundary.
