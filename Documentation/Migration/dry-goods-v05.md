# Dry Goods v05 — native Unity import

Imported into CityForge V3 as `dry-goods-v05`, in **3D Buildings → Commercial → Dry Goods Store**.

The approved authoring master is still `../v04-assembly/DG_DryGoods_Animated_v04.blend`. This directory is a separate game-export derivative; it does not replace that master.

## Delivered behavior

- Complete 7.8 × 6 m exterior, 12.9 m high, with brown cedar shingles, aged wooden siding, blue trim, chimneys, sign, display goods, steps and glazing.
- Sixteen independently controlled upper windows plus four storefront lights. Five ground-level rear/side windows remain dark. Day is off; Evening uses the game's 0.65 transition and Night uses 1.0.
- Selected native buildings with a door expose **OPEN DOOR / CLOSE DOOR**. The front door swings 90 degrees around its jamb over one second, can reverse smoothly, and its open/closed target is saved with the placed building.
- Shadow copies disable their duplicate lights and follow the visible door.

## Export/material contract

`export.py` evaluates approved collection instances and modifiers, preserves original Generated/Object/Random coordinates in attributes, and bakes procedural color, tangent normal and roughness to explicit `DG_BakeUV` atlases. Five groups: Shell/Roof 4096, Storefront/WindowFrames 2048, Door 1024. `pad_roof_atlas.py` extends valid edge texels into unpainted roof gutters without changing valid colors. `refresh_baked_preview.py` reloads corrected file textures into the export Blend.

`install.py` packs roughness into metallic/smoothness alpha, copies assets and the builder to Unity, and idempotently adds the Commercial catalog entry. In Unity, **City Forge → 3D Buildings → Create Dry Goods Package** rebuilds the versioned package while preserving generated asset GUIDs. Stop Play before rebuilding.

Re-export order: export → pad roof → refresh preview → install → Unity package builder. Running export alone replaces padded texture maps.

Unity uses Standard materials with baked color/normal/metallic-smoothness maps, separately blended store/door glass, and per-renderer property blocks for window emission. Four storefront point lights prefer per-pixel rendering; intensity 8, display range 2.5 m, entrance/lantern range 2.8 m. Upper spills remain intensity 0.08, range 1.1 m.

FBX centimeter/axis transforms are baked into persisted native Unity meshes. All visible mesh transforms are unit scale and the native hinge's up axis is Unity Y. Foundation origin is centered at ground level; the catalog front yaw is 90 degrees. The FBX retains its authored door action; the runtime controller uses the native hinge and matching eased swing.

## Verification and limits

`door-tests.xml`: four passing focused tests (pivot/angle, smooth reversal, save compatibility, shadow following).

`runtime-report-checks.txt`: 25 controls, 20 active night lights, day all off, independent room isolation, one-second door motion, eight rotations returning to the initial orientation, and a disk save/reload restoring the open door. Disposable save evidence is under `qa-lot/`, outside Joel's saved-lot folder.

`unity-*.jpg`: actual normal, docked Unity Game view captures. Day, night, open/closed door, front and rear lighting and rotated building were inspected. No standalone player, Scene view, fabricated screenshot, or offscreen camera was substituted. `baked_material_preview.png` is the separate Blender material review.

Physical mouse attempts were made in the docked Game view, but delivered pointer positions were inconsistent with the requested positions. Consequently **the actual Open/Close Door button's mouse interaction is not yet verified**. Runtime controller and save/reload checks do pass. Joel's in-game visual acceptance remains pending.

This is one detailed LOD: 29 visible meshes and 388,234 imported triangles. It is suitable for this first building experiment; distant LODs, simplified shadow/collision meshes and a populated-city performance pass are not included. Existing CityForge ground/flora/projected-shadow infrastructure is retained. No standalone app build was made.
