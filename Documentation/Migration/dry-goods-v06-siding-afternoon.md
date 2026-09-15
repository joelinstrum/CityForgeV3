# Dry Goods v06 — siding and afternoon lighting

Installed derivative of approved v04 / v05, with stable catalog ID `dry-goods-v05` resolving to the new DryGoodsV06 package. Canonical sources and v05 package retained.

The shell bake previously allocated only 1.48% of atlas area to UV islands. Reducing Smart UV margin from .006 to .0005 raises allocation to 29.04%, giving 4.431x linear texture resolution in the same 4096 maps. Rebaked color, normal and roughness from the original approved wood shader and physical source coordinates. Isolated source/baked pilot renders inspected before assembly; fine grain remains subtle at district viewing distance. No new siding color/material design.

Hosted afternoon sunlight now follows the world-space projected shadow ray. District afternoon temporarily suppresses unowned root-level scene directional lights; previous enabled states restore on other presets and district shutdown. Night settings, window intensities, door bindings and all non-shell texture maps are unchanged.

Validation: six focused EditMode tests pass (four door regressions, afternoon ray alignment, scene-light restoration). Existing v06 runtime report passes room/day/night/isolation, door rotation and disk reload checks. Actual district built from a read-only saved Dry Goods lot in a temporary review region: afternoon and night inspected in the normal docked Game view. Captures show correct shaded right facade and accepted lit windows; review framing clips a roof edge and is not a presentation render. Original user saves were not rewritten. No populated-city performance or new physical mouse door verification claimed.

Artifacts: `DG_Siding_Pilot_v06.blend`, `siding-pilot-close.png`, `source-siding-close.png`, `DG_DryGoods_GameExport_v06.blend`, `DryGoodsV06.fbx`, texture maps, export manifests, scripts, revision-tests.xml, runtime reports, unity-district-afternoon.jpg and unity-district-night.jpg.

Material contract: Standard shader; DG_BakeUV export UV; original DG_SourceUV / DG_Generated / DG_Object / DG_Random coordinates and attributes preserved for source baking. 4096 shell color/normal/roughness; metallic RGB zero and smoothness alpha 1-roughness. Other maps copied without alteration from v05.

Joel's v05 evaluation was positive for the building/night, with siding and afternoon corrections requested. v06 acceptance is pending his review.
