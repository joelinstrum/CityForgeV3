# Next Work Prompt: Production 3D Buildings and LOD Pipeline

Continue CityForge V3's experiment replacing or supplementing building billboards with real 3D buildings. Work incrementally and preserve the existing billboard/hybrid system until the 3D pipeline has been visually and performance validated.

## Current state

- The **3D Buildings** lot contains aligned brownstone test buildings.
- Real 3D building meshes drive detailed projected ground shadows, including stairs and other silhouette features.
- Hidden mesh-identical casters support shadows on ordinary 3D objects, flora, and billboard receivers.
- Projected building shadows use shared stencil coverage so overlaps do not become darker.
- Morning and afternoon use CityForge's documented visual compass; preserve those directions.
- The lush-grass base texture is intentionally protected from lighting color shifts.
- Do not use special QA shortcuts as proof of runtime behavior. Test through the normal Unity-window game flow and load the saved **3D Buildings** lot.

## Production LOD target

- **LOD0:** approximately 250,000 triangles — extremely close inspection.
- **LOD1:** approximately 80,000 triangles — normal close gameplay.
- **LOD2:** approximately 20,000 triangles — typical city view.
- **LOD3:** approximately 3,000–5,000 triangles — distant geometry.
- **LOD4:** billboard/impostor — very distant.

The original high-resolution model must remain untouched. Do not automatically decimate production meshes and assume the result is acceptable. Lower LOD geometry must be supplied or explicitly approved by the user after visual review. Past automatic reductions lost important architectural quality.

## Work to perform

1. Define a versioned `Building3DPackage` contract containing stable asset ID, source provenance, orientation, scale, pivot, footprint, material bindings, LOD meshes, billboard views, collision proxy, and shadow meshes.
2. Import one user-approved building as the pilot without modifying its source file.
3. Normalize orientation, scale, ground contact, front direction, and pivot consistently across every LOD.
4. Configure a Unity `LODGroup` with screen-relative transitions and cross-fades. Make thresholds adjustable rather than embedding unexplained constants.
5. Keep draw calls and material slots visible in diagnostics. Prefer shared materials and texture atlases for related building families.
6. Use simple collision primitives. Do not use the beauty mesh for collision.
7. Create separate near and far shadow LOD slots. They should preserve stairs, rooflines, chimneys, and major setbacks without requiring the full LOD0 mesh.
8. Generate the LOD4 billboard from CityForge's exact approved camera headings, scale, pivot, color treatment, and lighting.
9. Support a hybrid mode where the billboard is visible but hidden 3D geometry still supplies spatial relationships, occlusion, and shadows.
10. Add a dense performance lot and diagnostics for visible triangles, shadow triangles, draw calls, material count, texture memory, CPU frame time, and GPU frame time.

## Required QA

- Compare every transition at CityForge's supported zoom levels and camera headings.
- Confirm there is no visible scale, pivot, orientation, material, or ground-contact jump between LODs.
- Confirm stairs and roof details produce appropriate shadows at close range.
- Confirm overlapping shadows do not double in opacity.
- Verify both the Unity Editor and a fresh player build through the normal game flow.
- Capture labeled screenshots of each LOD and transition distance.
- Report measured performance rather than judging feasibility from polygon count alone.

Do not broadly revise lighting, grass rendering, camera angle, or the existing billboard system during this pass. If a source LOD is visually unacceptable, stop at that asset and request an artist-approved replacement instead of hiding the problem with material or lighting changes.
