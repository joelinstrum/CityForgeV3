# Interior quarry mining stations

Removed the two generated Working stone face cubes. Both miners now stand on the imported quarry interior floor, facing its existing stone ledges. Stations use quarry-local positions (-1.1,0.375,0) and (0.1,0.365,6), facing local -X, and therefore follow lot rotation. Canonical quarry meshes/materials unchanged; no permanent collision geometry added.

Unity compiled successfully. Temporary QA mesh raycasts measured zero floor gap at both stations, strike contact within 8cm of the existing rock surface, and invariant contact after a 90-degree quarry rotation. Both workers retain their staggered pickaxe animation. Interior-facing Game camera capture visually inspected; no generated outside cubes remain.
