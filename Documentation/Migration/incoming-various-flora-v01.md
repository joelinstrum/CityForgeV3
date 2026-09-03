# Incoming Various Flora V01

- Source folder: `/Users/joelinstrum/Downloads/flora/various`.
- Canonical extracted sources: `Authoring/Flora/IncomingVarious/*/v01_source/raw/`.
- Reproducible renderer: `Authoring/Flora/IncomingVarious/render_incoming_various_billboards_v01.py`.
- Billboard contract: orthographic camera at the locked CityForge 28-degree
  elevation and -55.5-degree azimuth, transparent 768-pixel RGBA output,
  normalized group transforms, deterministic diffuse/opacity material
  conversion, and no baked ground shadow.
- Runtime artwork: `CityForgeV3/Flora/LegacyTreesV01/{id}-{season}`.

## Published identities

| ID | Display name | Height | Seasonal treatment |
| --- | --- | ---: | --- |
| `hart-tongue-fern` | Hart's-tongue Fern | 0.55 m | Evergreen artwork |
| `japanese-painted-fern` | Japanese Painted Fern | 0.8 m | Evergreen artwork |
| `male-fern` | Male Fern | 1.0 m | Evergreen artwork |
| `soft-shield-fern` | Soft Shield Fern | 0.9 m | Evergreen artwork |
| `eucalyptus-robusta-a` | Eucalyptus Robusta A | 24 m | Evergreen artwork |
| `eucalyptus-robusta-b` | Eucalyptus Robusta B | 27 m | Evergreen artwork |
| `silver-maple-a` | Silver Maple A | 18 m | Green spring/summer, yellow autumn, bare winter |
| `silver-maple-b` | Silver Maple B | 15.5 m | Green spring/summer, yellow autumn, bare winter |

Each identity has a per-asset pixels-per-unit contract derived from the final
opaque billboard height. This preserves real-world scale while retaining
enough source pixels for small ground plants.

## Deferred source package

`Blender.zip` contains two Adiantum forms and two Cytisus forms. Its legacy
scene produces an opaque veil in Blender 5 even after external textures,
diffuse/opacity materials, compositor state, and world state are rebuilt.
Those four candidates remain authoring-only and are intentionally not
registered until their mesh/material assignments receive a focused repair.

The swamp-water archive in the same download folder is outside this flora
batch and was not extracted or registered here.
