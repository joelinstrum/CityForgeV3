# StreetTree3D V01

- Canonical source: `Authoring/Flora/SpeedTree/StreetTreeNarrow/v01_source/CF_Flora_StreetTreeRounded_A_v01.spm`.
- Mesh derivative: `Authoring/Flora/SpeedTree/StreetTreeNarrow/v01_export/CF_Flora_StreetTreeRounded_A_v01.fbx` with the exported SpeedTree textures and material description.
- The SpeedTree `Roots` and `Root Twigs` generators are deleted in the canonical source. Billboard generation therefore disables the legacy emergency root-plane cut.
- The rounded derivative also disables the narrow tree's synthetic lower-foliage duplication so its clear urban trunk remains visible.
- Billboard derivative: `Authoring/Flora/SpeedTree/StreetTreeNarrow/CF_Flora_StreetTreeRounded_billboard_v01.blend`, normalized to 12 meters tall and rendered with the City Forge locked 28-degree elevation and -55.5-degree front-right azimuth.
- Runtime artwork: `CityForgeV3/Flora/LegacyTreesV01/street-tree-3d-{season}` at 465x564 RGBA.
- Runtime identity: `street-tree-3d`, displayed as `StreetTree3D` in the Flora Library.
