# Macro grass V01 validation

The generated runtime texture is 4096 × 4096 RGB with no alpha. Opposite one-pixel borders compare at RMSE `0 (0)` horizontally and vertically. The Unity importer retains 4096 resolution, Repeat wrap mode, trilinear filtering, anisotropic filtering, and mipmaps.

The focused Unity 6000.1.12f1 EditMode test `CityForgeV3.Tests.UiFoundationTests.DistrictDefaultGrassUsesBroadWorldSpaceTextureDensity` passed on September 19, 2026. It loaded the runtime texture, verified its dimensions, mip chain and wrap mode, verified the 75 m scale at all six district zoom levels, verified the default lot option uses world-space UVs, and checked all three affected shaders for compile errors and the shared world-size property. Unity exited with code 0 and logged no C# or shader errors.

Image generation used the built-in image-generation workflow with the user-supplied village image as palette and ground-density reference only. Exact prompt and runtime lineage are recorded in `Documentation/ArtStudies/MacroGrassV01/README.md`.

The change does not add worn-building masks, road-edge transitions, or sibling macro variants. Those remain separate follow-up systems. Final in-game palette and variation strength require visual acceptance in the isolated review editor.
