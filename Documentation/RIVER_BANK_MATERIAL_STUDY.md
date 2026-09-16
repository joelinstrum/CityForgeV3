# River bank material study — September 16, 2026

Status: initial artwork exploration retained for lineage. The implemented successor and validation are documented in [RIVER_BANKS.md](RIVER_BANKS.md).

Reference: Joel's `Screenshot 2026-09-16 at 9.05.37 AM.png`, supplied in conversation. The visual target is an irregular grass/dirt/pebble shoreline with a visible shallow bed. Its tile labels describe the reference, not required implementation instructions.

## Proposed rendering approach

Retain continuous banks following the authored centerline. `DistrictWorldController.BuildRiver` already builds cross-section bands and `AddRiverBand` maps U by traveled metres and V across the bank, with Repeat U / Clamp V. Use this existing contract rather than discrete fixed-angle river tiles.

Create compatible grass/soil, gravel, and exposed-earth material variants. Blend their weights smoothly using signed curvature measured over a physical distance, separately for each bank. Ordinary reaches use grass/soil; inside bends can expose more gravel; outside bends can expose more earth. These are visual rules, not erosion or hydrology simulation. Avoid selecting textures independently per source segment: resampling or Shape/Repair would otherwise change the appearance abruptly.

Keep blue water and foam out of bank artwork so existing animated water remains independent. Fade the outer bank into actual terrain material; baked grass alone cannot guarantee a terrain color match. Match material scale and transition heights across variants. Initial prototype should change material appearance only; any geometric bank widening must also respect shared surface sampling and clipping.

Preserve authored width/depth, border crossings, junction subtraction, flow vectors, save/reload, undo, and deferred surface refresh. Workers/labor are outside scope.

## Initial generated study

Candidate: `../Authoring/RiverBanks/v01/riverbank-transition-study.png`.

Tool: built-in image_gen. Original output is retained without pixel edits. Candidate is not a verified seamless texture. Before runtime use, inspect horizontal repetition, grass matching, pebble scale, UV compression, and close/far zoom in the normal Unity Game view. Validate both bend directions, straight reaches, junctions, border crossings, shallow/deep rivers, and repeated Shape → Repair on isolated copies of real saved layouts.

### Exact generation prompt

Use case: stylized-concept. Asset type: game terrain riverbank diffuse texture study for City Forge V3, one continuous texture, no atlas or panels. Create a high-quality naturalistic orthographic directly overhead material texture, wide landscape 3:2 composition. Entire image is ground, no horizon, no perspective. Across the image from top to bottom: muted olive meadow grass occupying top 35%, irregular sparse grass and roots mingling into dry earthy tan-gray fine gravel in central 35%, darker damp pebble and sandy silt riverbed occupying bottom 30%. The bands run horizontally across full image and can be wrapped along an arbitrary river shoreline. Organic uneven interpenetrating transitions, small scattered rounded pebbles and a few slightly larger stones, fine grass detail, subtle varied patches of exposed earth, restrained color and consistent material scale. Top grass approximate muted olive RGB 73,89,36. Visually grounded strategy-game environmental art with realistic material detail. Bottom is exposed wet riverbed texture with NO painted water; animated water will be rendered separately. Flat neutral diffuse illumination, no cast shadows, no directional highlights, no ambient vignette, no outlines. All ground fills image edge to edge. Left/right edges should match as a horizontal repeating strip, with consistent heights of transition bands at both ends. Do not paint bends, a full river, blue water, text, labels, arrows, borders, UI, or a mockup. This is a first material study, not a claimed validated seamless production asset.
