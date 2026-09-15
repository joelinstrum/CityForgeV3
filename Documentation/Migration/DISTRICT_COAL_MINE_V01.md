# District coal mine V01 — September 13, 2026

District toolbar: INDUSTRY → COAL MINE · SITE → BUILD COAL MINE. This is a native district facility, outside the Lot Editor. Each coal deposit can have one mine; building replaces its three decorative coal sprites. VIEW SITE focuses it; REMOVE MINE restores the cluster. Ctrl+Z undoes placement/removal through the existing district snapshot history. No construction charge, extraction workers, stockpile output, or depletion is implemented yet.

## Model and spatial contract
Supplied archive: Downloads/buildings/Stone Mine/wooden+mine+shed+3d+model.zip (SHA256 cf0f6538f362675728decacc8ab63325e825c62fd4df7782628382f18b348356). Source is one assembled 2128-vertex, 4484-triangle mesh with shed, entrance, roof, rails and props. No newly assembled architectural components.

Versioned authoring lineage lives in CityForgeMCP/Authoring/Buildings/DistrictCoalMine/v01: v01_source immutable extraction/lossless Blend → v02_metric registered Blend → v03_native FBX and generated spatial.json. Height 4m, rendered width6.3802m/depth9.3071m. Foundation origin from low-band trimmed vertex bounds. Native front/rails +Z, rear -Z. Original base-color and normal maps copied unchanged; neutral matte imported-building material contract. Native geometry uses imported lighting, no flora brightness edits.

DistrictCoalMine samples the terrain gradient and rotates the rear uphill. The downhill rail tip is grounded and the rear seats into the slope. This does not cut a tunnel in the terrain. MineBuilt and MineYawDegrees persist on the existing resource record. Developed resource locations are retained through hill edits; undeveloped terrain still uses its existing reseeding rules.

## Validation
Unity compiled and prepared the prefab; metric height assertion passed. Actual Industry button submit (focused button + Return), site BUILD click, and physical Ctrl+Z exercised. Mouse automation on the toolbar reported a stale center-of-game pointer position despite requested coordinates; hit-test confirmed the toolbar button at its actual bounds, and keyboard activation opened it. No speculative global input changes were made.

Actual Little River Bend disk reload passed: one rendered mine, persisted yaw, rear samples uphill, duplicate/flat-site rejection, mine retained under terrain-edit fixture. Existing hills reload check passed with770flora,1river,1lot and37.44m peak. Main editor left playing in normal docked Game view focused on the built first mine; second coal site remains available. Screenshots and run records: CityForgeMCP/artifacts/buildings/coal-mine-v01. Joe's mine visual approval is pending; he accepted the preceding coal clusters.
