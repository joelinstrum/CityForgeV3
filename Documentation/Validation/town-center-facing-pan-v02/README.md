# Town Center hosted facing and farthest-pan correction

Validated September 20, 2026 in a disposable Unity 6000.1.12f1 copy. The live
editor and all player persistence remained untouched.

`tests.xml` records 11/11 passing focused EditMode cases: the exact saved camera
quaternion from Joe's Town Center, all four established legacy diagonal-label
mappings both with and without matching camera transforms, invalid/top-down
fallbacks, and all six district pan multipliers. The Town Center transform
resolves to district turn 3; the farthest
`LOD5Billboard` multiplier is 0.04725, exactly 35% above 0.035. Zooms 0–4 are
unchanged.

`district-facing.png` is a graphics-enabled capture of the actual player Lot
JSON loaded through the district-hosted `LotWorldController`, with the computed
turn plus the normal 180-degree host offset under the fixed district camera.
The entrance and Town Center sign face the camera, the flower fence is on the
near-left edge, and both trees are behind/right, matching the authored Lot
composition. No Lot or district Save action was invoked.

Placement orientation remains a one-time calculation for the pending Lot.
Panning reads one constant multiplier for the current zoom. Neither change adds
a district enumeration, presentation rebuild, or per-frame allocation.
