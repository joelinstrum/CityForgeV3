# Tree follow-up repairs — 2026-09-11

Joe approved the first-pass Date Palm, Streettree3d, Eucalyptus A, Silver Maple A, Red Maple, Classic Balsam Fir, Cypress Oak, and both Oregon Ashes. Preserve their appearance and the approved scales.

## Follow-up
- Cilician Fir and Snowy Fraser Fir: bypass the lowest-alpha-band trunk detector. Dense low foliage made it pick a left branch. Use each re-render's authored pivot for selection and trunk hit testing; flipped variants remain centered.
- Eucalyptus B: per-renderer elliptical alpha edge rounds the lowest seven pixels of the trunk. All original texture files remain unchanged. Exposure and scale retained.
- Willow: saturation 1.4 and exposure 1.12, neutral defaults for other trees. Approved 0.7 scale retained.
- Hickory removed from Flora picker. Existing saved IDs remain loadable.
- Angel Oak entry explicitly says (3D), in the 3D category. Existing real mesh confirmed at zoom levels 1/2, billboard at 3. Distant billboard pivot moved to the root collar (480,310 in Unity bottom-up coordinates) to bury dangling roots and remove hover. No new mesh export: reuse previous preserved canopy mesh.

## Verification
Unity compiled updated runtime and sprite shader with no new errors. Inspected changed trees using disposable lots in the normal docked Game view. Screenshots beside this document show centered fir markers, Eucalyptus base, Willow, and near/far Angel Oak. Close previews intentionally focus on the base; some crowns extend behind the toolbar. Angel LOD check: zoom1 mesh1; zoom2 mesh1; zoom3 mesh0. No user lot overwritten.

Before snapshots and installed code copies are in this directory. install.py is historical and not idempotent; do not rerun it against integrated files.
