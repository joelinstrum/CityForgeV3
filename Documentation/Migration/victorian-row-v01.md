# Victorian row v01 source lineage

- Source: `/Users/joelinstrum/Downloads/buildings/victorian-row/victorian+townhouse+3d+model.zip`
- Source type: Tripo-generated textured FBX with external PBR maps.
- Source status: read-only; extracted into the V3 authoring intake folder without modifying the download.
- Intake package: `Authoring/Buildings/IncomingTripo/victorian-row/v01_source/`
- Intended use: mid-wealth residential row building.
- Era classification: `Industrial Age` (`industrial`), pending visual review.
- Runtime package: `Assets/CityForgeV3/Resources/CityForgeV3/Buildings/VictorianRowTripoV01/`
- Runtime status: published as a review package with four neutral facings, a plan image, and a semantic proxy.
- Blender intake scale: the initial `9.794m x 3.942m x 5.606m` review import read substantially undersized beside the Founder’s Cabin. A 3× V02 review proved roughly 30% too large by comparative door scale. The accepted V03 review is uniformly 2.1× the intake at `20.5674m x 8.2782m x 11.7726m`; V01 and V02 proxies remain preserved.
- Registration correction: V03 uses the camera-projected foundation-center pivot (`0.5, 0.6510669` top-origin) and `persisted-pivot` registration so runtime alpha-bound fitting cannot pull the billboard behind its semantic foundation.
- Lighting derivative: `victorian-row-v02-lighting.blend` and `render_v02_lighting.py` produce four brighter neutral facings at AgX exposure +0.5 with increased key/fill illumination. The original review renders remain preserved.
- Primitive correction: the V03 semantic foundation, wall, roof, entrance anchor, and shadow envelope were reduced to 90% in the horizontal plane after in-game review. Height and registered artwork remain unchanged.
- Night-overlay correction: the initial review package incorrectly referenced each full neutral render as its night overlay, causing the entire row to be composited twice and glow. The four night paths now use transparent registered placeholders pending a dedicated window-mask pass.
- Catalog validation: the package loads through `HybridBuildingPackageRegistry` and resolves under `Residential` / `Mid-Wealth` with the short label `Victorian Row`.
- Remaining review work: separate night/day shade passes, mesh optimization, and final visual approval.
