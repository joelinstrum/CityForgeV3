# Plane UK 3D flora pilot — v01

Imported 2026-09-11. User requested Flora categories and 3D trees at zoom levels 1 and 2.

## Source and lineage
Canonical source is `/Users/joelinstrum/Downloads/flora/HybridPlaneUKTrees/Blender.rar` plus `Textures.rar`. Originals are unchanged. Read-only source inspection lives in sibling `../inspection`. `build.py` produces separate registered derivative Blends, FBXs, and matching billboard images under this directory. Source UVs and texture pixels are preserved. No AI-generated foliage textures.

A uses the standalone green branch/cap/leaf assembly ending 001; B uses London_Plane_Fall02. Spring and summer share green foliage; autumn swaps to source autumn textures; winter removes leaf faces from the same woody skeleton. Winter is not scaled to compensate for missing leaves. No wind animation is supplied.

## Runtime contract
Resource root: `CityForgeV3/Flora/PlaneUK3DV01/`.
Stable saved IDs: `plane-uk-3d-a`, `plane-uk-3d-b`.
Prefabs: `Tree-{a,b}-{summer,autumn,winter}`. Do not use `Plane-` prefab names: they collide case-insensitively with FBX resource names.
Billboards: `plane-{a,b}-{summer,autumn,winter}`; 512 px, PPU 512/18, pivot (0.5, 0.22307235).
Full summer tree height: 12 m before existing placed-flora size variation.

A: 8,662 leafy triangles / 4,648 winter triangles.
B: 7,273 leafy triangles / 4,237 winter triangles.
Leafy prefabs have four shared materials; winter has two. Original bark/cap/leaf color and normal maps are mapped explicitly. Color maps are sRGB, normal maps linear. Custom two-sided alpha-cutout shader uses metallic 0, smoothness 0.1, and shared instancing-enabled materials. Instancing support is not a measured draw-call or FPS guarantee.

`PlaneUkFloraPresentation` keeps the existing sprite as save/selection anchor and adds a mesh presentation wrapper at zoom Detail/Inspection (user 1/2). Farther out, the wrapper is inactive and the matching sprite is visible. The wrapper counters billboard camera rotation; its child retains FBX axis conversion and import scale. Never set the imported mesh root rotation to identity: that lays this FBX sideways. A trunk capsule provides near-view selection. Existing flora placement, row planting, and persistence IDs are reused.

Flora modal: Trees, Shrubs (including ferns and hedges), Rocks (empty until assets are added), 3D, Agriculture (corn retained). New tree cards appear under 3D. Existing billboards remain available for comparison.

## Build and verification
Unity editor menu: City Forge > Flora > Build Plane UK Trees. Builder configures only this versioned asset directory; it is not a global importer. `integrate.py` is a historical one-time integration patch and MUST NOT be rerun against an already integrated controller.

Live verification used Unity's normal docked Game view and a disposable unsaved comparison lot. Corrected FBX orientation after initial visual check. Final view shows both upright 3D trees beside the existing London Plane billboard, with visible ground shadows and textured foliage.

Verified:
- Builder produced all six prefabs; shader validation passed.
- Zoom 1 and 2: both mesh presentations active. Zoom 3: neither active (billboard fallback).
- Navigation-submit events exercised all five real tab callbacks successfully.
- Tree card callback armed Plane UK A and closed the modal.
- Normal AddFlora placement path succeeded through its existing QA entry.

Evidence: assets.txt, lod.txt, library.txt, comparison-windowed.png, flora-modal-windowed.png.
Limitations: automated physical mouse clicks in docked Game view did not trigger runtime UI during this session; callback/planting checks above passed independently. No dense-forest FPS benchmark, physical drag/row test, or save/reload round-trip was measured. Zoom changes are discrete, not crossfaded. Distant billboards use one fixed source viewing direction.
