# Gilded Age mansion v01 source lineage

- Source: `/Users/joelinstrum/Downloads/historic+mansion+3d+model.zip`
- Source type: Tripo FBX with one fused mesh and external PBR textures.
- Authoring intake: `Authoring/Buildings/IncomingTripo/gilded-age-mansion/v01_source/`
- Geometry: approximately 721,650 polygons; retained as authoring-only source geometry.
- Initial scale: the raw model measured approximately `0.966 x 0.979 x 0.642m`; the provisional 10x intake produced `9.657 x 9.787 x 6.422m` but left the formal entrance only about 1.25m tall.
- Door-calibrated scale: the main formal entrance is normalized to 2.2m, applying a 1.76x correction to the provisional intake. The corrected building measures `16.997 x 17.225 x 11.302m`, uses 34.090909 pixels per meter, and occupies a 2x2 lot footprint.
- Visual mass correction: in-game comparison still read undersized after door calibration, so the complete package received a further uniform 1.25x correction. It now measures `21.246 x 21.531 x 14.128m`, uses 27.272727 pixels per meter, and occupies a 3x3 lot footprint. This correction is intentionally documented separately from the 2.2m door reference.
- Second visual mass correction: a subsequent in-game comparison still read undersized, so the complete package received another uniform 1.25x correction. It now measures `26.558 x 26.914 x 17.660m`, uses 21.818182 pixels per meter, and remains within its 3x3 lot footprint. The cumulative visual correction beyond the door-derived scale is 1.5625x.
- Architecture: broad two-story limestone mansion with parapet, hipped roof, multiple chimneys, formal entrance, side porch, and fenced grounds. Source masonry appearance is preserved.
- Runtime package: `CityForgeV3/Buildings/GildedAgeMansionTripoV01/building-package`, registered as `Gilded Mansion` under Residential.
- Runtime assets: four 1024x1024 neutral facings, centered plan render, and a semantic primitive reduced to 90% in the horizontal plane.
- Lighting status: neutral facings use the current +0.65 review exposure. Night files are transparent placeholders pending individually traced window masks.
- Publication status: active-catalog review asset.
