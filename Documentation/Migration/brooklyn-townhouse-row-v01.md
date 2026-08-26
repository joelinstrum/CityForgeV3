# Brooklyn townhouse row v01 source lineage

- Source: `/Users/joelinstrum/Downloads/buildings/brooklyn-townhomes/townhouse+row+3d+model.zip`
- Source type: Tripo FBX with one fused mesh and external PBR textures.
- Tree cleanup: the unwanted left tree was removed as a discrete mesh island plus a localized trunk remnant; the fused right tree was isolated as a 13,417-vertex above-ground component after excluding the shared ground slab. The outer masonry, stoops, sidewalk, ivy, and window boxes were preserved and checked in a front-corner render.
- Review scale: uniform 22x source correction, provisionally aligned to an approximately 2.4m raised residential entrance. Corrected metric dimensions are recorded in the runtime manifest.
- Runtime package: `CityForgeV3/Buildings/BrooklynTownhouseRowTripoV01/building-package`, registered as `Brooklyn Row` under Residential.
- Lighting status: neutral facings use the current +0.65 review exposure. Night files are transparent placeholders pending individually traced window masks.
- Publication status: active-catalog review asset; brick appearance remains source-authored and requires comparison against the City Forge v79 brick standard before production-ready approval.
