# Hitchcock mansion v01 source lineage

- Source: `/Users/joelinstrum/Downloads/buildings/props/psycho/HitchcockMansion.zip`
- Source archive SHA-256: `b9ccb42269bd9fdafbe95ae5adbc794ef49f61a19a3cc54a033f801158a1abbc`
- Source type: Tripo FBX with one fused mesh and external base-color, normal, roughness, metallic, and packed RM maps.
- Authoring intake: `Authoring/Buildings/IncomingTripo/hitchcock-mansion/v01_source/`
- Runtime identity: `cityforge.v3.residential.hitchcock_mansion_tripo_01`
- Runtime package: `CityForgeV3/Buildings/HitchcockMansionTripoV01/building-package`
- Scale: normalized to 17.0 m total height for the initial historic-residential review; centered dimensions are `17.710129 x 18.542833 x 17.0 m`.
- Footprint: 2x2 lots, with origin at foundation center on the ground plane.
- Artwork: four 1280x1280 neutral facings at 40 pixels per meter plus a centered top-down plan render.
- Shadow: source-derived voxel-remesh silhouette with 15,839 vertices and 30,152 polygons, exported as `CF_PROXY_BUILDING_GENERATED` for V2 projected-mesh shadows.
- Materials: the supplied dark slate, painted clapboard, historic trim, and brick chimney PBR appearance is preserved; no generic replacement material was introduced.
- Night status: four transparent placeholders pending individually traced window masks.
- Publication status: active-catalog review asset; final in-game scale, entrance orientation, nighttime window masks, and rotation QA remain review gates.

## Production 3D LOD package

- Package root: `Assets/CityForgeV3/Resources/CityForgeV3/Buildings3D/HitchcockMansionProduction/`
- LOD0: immutable supplied FBX, 44,044 triangles.
- LOD1: UV-preserving 40,000-triangle review derivative.
- LOD2: UV-preserving 20,000-triangle review derivative.
- LOD3: UV-preserving 15,999-triangle review derivative.
- LOD4: UV-preserving 12,000-triangle review derivative.
- LOD5: eight transparent 1024x1024 neutral billboard views at 45-degree intervals.
- Far-LOD constraint: lower 5K/2.5K attempts were rejected because the fused source topology collapsed the porch and mansard roof into spikes. The documented 12K floor is the lowest clean automated derivative from this source.
- Runtime: schema-v2 `Building3DPackage`, adjustable cross-fade thresholds, LOD2/LOD4 mesh shadow casters, simple box collision, shared PBR material, and eight-angle billboard selector.
