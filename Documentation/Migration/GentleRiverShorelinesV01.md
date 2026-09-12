# Gentle independent river shorelines — 2026-09-09

Only the generated water surface geometry changed. The original river path, terrain, riverbed bands/textures, water texture, shader and material setup are preserved.

Each side uses two smooth long sine waves with independent phase and frequency. Base wavelength is max(160 m, 7 x original water width). Half-width stays within 88–100% of the old water half-width, so broad expansions never exceed the existing water/bank envelope. Minima correspond to about 68.64% of the dirt outer distance; maxima remain at the old 78%, within the existing lower sloped-bank band.

Water-only subdivisions are spaced at clamp(width x 0.12, 2 m, 8 m). Centers and normals interpolate the exact original bank cross-sections; no new path or bank curve is fitted. The five existing edge-fade/depth rows and world-space texture mapping are retained. 32-bit indices are used only if the denser water mesh exceeds 65,535 vertices.

Validation: generated river fixture in normal docked Game view; 481 water rows versus 33 original path rows. Left width range 0.884926–0.999918; right 0.880054–0.997447; maximum left/right difference 0.100025; zero original-envelope violations. Runtime/editor/EditMode assemblies compiled during the change. A source comparison verified byte-for-byte unchanged code before water generation (including all bank construction), and unchanged code from mesh data assignment onward (all texture/material setup and other terrain methods). Game view screenshot: game-view.jpg.

The test uses the existing transient generated-river QA fixture and writes no saved region/lot. Runtime regeneration uses the stored river path unchanged. Editor menu: City Forge > QA > River > Check Gentle Shorelines. Original source snapshot and exact integration.diff are retained here.
