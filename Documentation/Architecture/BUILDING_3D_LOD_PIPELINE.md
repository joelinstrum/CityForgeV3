# Production 3D Building LOD Pipeline

## Status

Schema version 1 and the brownstone pilot are implemented. The pilot is an
integration checkpoint, not a completed five-level art package. Its original
22K FBX is referenced unchanged as LOD2. LOD0, LOD1, LOD3, the impostor,
collision proxy, and independently authored shadow LODs remain required art.

The existing lighting, camera, lush-grass receiver, building orientation, and
mesh-projected/native shadow paths remain authoritative. The normal saved-lot
rebuild now loads the brownstone production prefab.

## Package contract

`Building3DPackage` is a versioned `ScriptableObject` with:

- stable asset ID and source provenance;
- authored scale, pivot offset, front yaw, footprint, and bounds tolerance;
- independently assigned visual and shadow prefabs per representation;
- adjustable screen-relative thresholds and triangle targets;
- optional simple collision prefab;
- cross-fade controls and hybrid impostor/shadow intent.

`Building3DPackageInstance` builds Unity's supported `LODGroup`, preserves a
single package transform across levels, and can attach a separate ShadowsOnly
caster to any level. It never edits or decimates an input mesh.

The custom inspector reports missing levels, unsupported schema versions,
non-descending thresholds, invalid scale, absent renderers, inconsistent bounds
or centers (pivot drift), and material-binding differences. Missing authored
levels are warnings so a partial integration package can be reviewed honestly.

## Initial CityForge thresholds

These are starting points for fixed-camera comparison, not art approval:

| Level | Screen-relative height | Triangle target | Intended use |
|---|---:|---:|---|
| LOD0 | 0.60 | ~250,000 | extreme inspection |
| LOD1 | 0.30 | ~80,000 | close gameplay |
| LOD2 | 0.12 | ~20,000 | typical city view |
| LOD3 | 0.035 | 3,000–5,000 | distant geometry |
| LOD4 | 0.012 | ~1,500–3,000 | very distant geometry |
| LOD5 | 0.002 | eight-angle billboard/impostor | extreme distance |

The current partial pilot contains only the 0.12 LOD2 entry. Add levels in
descending threshold order. Validate each boundary at fixed camera position and
heading before accepting it; do not tune global lighting to conceal a mismatch.

## Preparing a new building

1. Keep the canonical source outside the derived LOD folders and record its DCC,
   version, author, units, up axis, front direction, and license/provenance.
2. Export each approved LOD independently with identical origin, units, yaw, and
   ground plane. Do not apply automatic reduction without explicit approval.
3. Reuse consistent material bindings and textures. Differences must be
   deliberate and visually compared, especially roof/cornice and masonry color.
4. Supply a simple collision proxy; never use a beauty mesh as collision.
5. Supply near and far shadow meshes that preserve stairs, chimneys, rooflines,
   setbacks, and major façade depth. Assign them separately from visual meshes.
6. Render the LOD5 impostor from CityForge's eight approved 45-degree headings, pivot,
   color treatment, and lighting. Keep a low-cost shadow mesh active beneath it
   when `KeepShadowMeshWithImpostor` is enabled.
7. Run inspector validation, edit-mode tests, and the normal Unity Game workflow.
   Load the saved **3D Buildings** lot instead of relying on a synthetic scene.

### Repeatable intake

Run the Python intake before opening Unity:

```sh
python3 scripts/building_lod_intake.py \
  /path/to/BuildingSource \
  Assets/CityForgeV3/Resources/CityForgeV3/Buildings3D/BuildingProduction
```

Use `--inspect` for a read-only preflight and `--force` to update an existing
package without deleting unrelated files. Progress is machine-readable and can
be advanced with repeatable flags such as:

```sh
--progress unityImport=complete --progress transitionQa=in_progress
```

`Tools/Import Building LOD Package.command` is a Finder/terminal wrapper around
the same script. Python owns ZIP safety, hashes, provenance, folder staging, and
progress tracking. Unity editor code owns model metrics, object references,
materials, prefabs, `LODGroup`, shadow renderers, and impostor rendering because
those operations require Unity's importer and renderer.

Validate the completed authoring manifest before constructing the Unity prefab:

```sh
python3 scripts/validate_building_lod_manifest.py \
  Assets/CityForgeV3/Resources/CityForgeV3/Buildings3D/BuildingProduction/Source/manifest.json \
  --package-root Assets/CityForgeV3/Resources/CityForgeV3/Buildings3D/BuildingProduction \
  --output Assets/CityForgeV3/Resources/CityForgeV3/Buildings3D/BuildingProduction/Source/unity-intake-contract.json
```

This fails on pivot, rotation, scale, material-slot, bounds, export-axis,
texture, FBX, or eight-view billboard mismatches. The generated contract records
the expected meter bounds. Unity must derive one uniform package scale from
imported LOD0, apply it once at the shared `Representations` root, and call
`LODGroup.RecalculateBounds()`; never encode per-LOD runtime scale or rotation
repairs.

## Shadow contract

Morning sun is east and shadows travel west; afternoon is the exact opposite;
noon uses a short contact shadow. Visual LOD changes must not create a second
caster. The existing projected mesh shadow retains its shared stencil coverage,
so overlapping shadows do not accumulate extra darkness. Package shadow prefabs
are ShadowsOnly and should be authored cheaper meshes, not bounds rectangles.

## Performance measurement protocol

Record each representation's triangles, vertices, renderers, submeshes,
materials, texture memory, batches, and shadow triangles from the package
inspector/profiler. Profile 1, 16, 64, and 128 instances at each representative
zoom using the Unity Profiler and Frame Timing Manager in the normal Game view.
Capture median and 95th-percentile CPU/GPU frame time after warm-up, plus memory
and shadow pass cost. Hardware, resolution, quality tier, Unity version, camera
state, and time of day must accompany results.

No dense-instance performance claim is recorded yet because only one of five
coordinated brownstone representations exists. Measurements taken by duplicating
the same 22K mesh into fake levels would not validate the intended pipeline.

## Brownstone pilot layout

`BrownstoneProduction/` contains the package asset and the standard `Source`,
`LOD0`, `LOD1`, `LOD2`, `LOD3`, `Impostor`, `Materials`, `Textures`, and `Prefabs`
folders. The LOD2 package reference deliberately points to the existing original
FBX rather than copying or modifying it.

## Required manual art

- approved ~250K LOD0 brownstone;
- approved ~80K LOD1 brownstone;
- approved 3K–5K LOD3 brownstone;
- approved multi-heading LOD4 impostor with matching color/pivot;
- near and far mesh shadow casters;
- simple collision proxy;
- final shared material/texture binding decision for all levels.
