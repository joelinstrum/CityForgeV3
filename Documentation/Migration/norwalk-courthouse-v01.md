# Norwalk Courthouse V01

- Source: `/Users/joelinstrum/Downloads/buildings/norwalk-tower/clock+tower+3d+model.zip`.
- Source intake: `Authoring/Buildings/IncomingTripo/norwalk-courthouse/v01_source/`; the 220,084-vertex / 440,850-polygon FBX remains authoring-only.
- Brick standard: reviewed against the Federal-Georgian Five-Bay v79 reference copied from the read-only predecessor project. The supplied masonry PBR maps are retained because their brick scale, mortar relief, restrained variation, and non-plastic response remain compatible with that standard.
- Scale contract: the initial `22.143930 x 22.075224 x 29.000000m` intake read undersized in-game. The complete spatial and artwork contract received a user-requested 17.5% visual correction and now measures `26.019118 x 25.938388 x 34.075000m`. Runtime occupancy remains 3x3.
- Runtime package: `CityForgeV3/Buildings/NorwalkCourthouseTripoV01/building-package`, registered as `Norwalk Courthouse` under Civic / Government.
- Runtime assets: four 1024x1024 neutral facings, centered top-down plan, and `semantic-primitive-v02.fbx`: a source-derived voxel-remesh silhouette retaining the courthouse body, clock tower, belfry columns, and dome. The box-only V01 primitive was rejected after in-game review.
- Default orientation: corrected to one clockwise front-facing quarter turn so the formal entrance is shown in the initial export view.
- Shadow regression guard: package validation now requires `projectionMode: projected-mesh`, the `CF_PROXY_BUILDING_GENERATED` object, more than 1,000 silhouette vertices, and a proxy height over 30m. This directly carries forward the successful New England church contract.
- Intake standard promotion: this package is the first `cityforge-v3-hybrid-building-package-v2` asset. V2 makes the church-derived projected-mesh declaration and generated proxy object mandatory for all future building imports while leaving existing V1 packages readable.
- Night lighting: four transparent placeholders are registered. No window illumination is authored until the user supplies the in-game overlay markup.
- Review status: four neutral facings and four close perspective corners inspected; no floating geometry, missing texture dependency, or broken tower silhouette observed. This remains an in-game visual review checkpoint rather than final approval.
