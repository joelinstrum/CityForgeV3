# Lot placement followed by camera pan — 2026-09-16

Placement already preserves the exact camera transform. However, both hand and arrow pans called ApplyCameraFacing afterward, reconstructing the camera from the current building package (hybrid) or the native-3D basis once a first 3D building existed. This let an unchanged placement view snap on the next pan. The failure also affected a camera pose retained after switching building packages.

PanCameraInScreenPlane now applies only the clamped pan-offset delta to the actual camera position. It retains current rotation and orthographic size, plus the existing presentation alignment/depth updates. Explicit orbit and load/refit operations are unchanged. No default-angle migration, save changes, or extra presentation rebuilds were added.

Validation in the isolated review editor, while out of Play Mode:
- Four new cases failed before the fix and passed afterward: native/hybrid placement followed by hand/arrow panning, with a non-default camera pose.
- Seven existing cases passed: four arrow directions, repeated vertical-pan camera depth, first-3D placement stability, and opening the 3D workspace plus placement.
- Assertions cover immediate placement stability, pan rotation/zoom continuity, exact clamped translation, and no forward-depth drift.
- Unity compilation and git diff whitespace checks passed. This confirms the placement-then-pan path; an independent rotation at the exact drop event has not been reproduced.

The runtime patch was three-way merged into Regions Review with its helper changes preserved and its previous file backed up under RecoveryBackups/lot-camera-pan-fix. No user lots or regions were loaded or saved by these fixture tests. The review editor was already stopped and was left stopped. The main Unity project was untouched.
