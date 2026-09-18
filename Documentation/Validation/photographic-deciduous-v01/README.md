# Photographic deciduous additions V01

New planting IDs: `mature-oak`, `american-elm`, and `shagbark-hickory`. Each has spring, summer, autumn and winter cutouts in `PhotographicDeciduousV01`, routed through the shared lot/district flora resolver and both planting menus. The previous `ashe`, `oak`, `oak-b`, and `vendor-hickory` images and paths remain intact. No save or labor code changed.

`resources.txt` is a fresh Unity Editor check for all12 loaded textures, alpha-measured trunk foot versus pivot, expected pixels-per-unit, library resource paths, and unchanged legacy Ashe/Oak paths. `game-review.txt` records an active lot-camera render of three temporary presentations per season. They were removed afterward and the lot JSON matched exactly. `lot-camera-{spring,summer,autumn,winter}.png` were visually inspected; summer shows the Oak upright beside existing flora. These captures use the lot's current lighting for every texture and the temporary presentations have no shadows, so they do not establish a complete seasonal-lighting or shadow review.

No Editor Play stop, save, commit, or fixture mutation. No performance improvement claimed. New resource textures share the existing cached billboard path; performance under many added placements was not profiled.
