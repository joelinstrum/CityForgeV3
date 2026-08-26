# Simple Street Lamppost V01

- Source archive: `Authoring/Props/SimpleStreetLamppostV01/v01_source/street+lamp+3d+model.zip`.
- Runtime identity: `simple-street-lamppost-v01`, displayed as `Simple Street Lamp` in the Props Library under Street Lighting.
- Runtime model: `CityForgeV3/Props/SimpleStreetLamppostV01/SimpleStreetLamppostV01`, retaining the supplied 4,454 vertices and 8,976 triangles.
- Scale contract: normalized from the source mesh to 3.6 meters tall with a 0.55-by-0.55-meter placement footprint.
- Materials: supplied base color, normal, metallic, roughness, and combined roughness/metallic maps are preserved under the package's `Textures` directory. Runtime metallic/smoothness packing stores metallic in RGB and inverted roughness in alpha; this prevents the opaque alpha of the source JPG from making the black iron uniformly glossy and gray.
- Black-level correction: runtime uses the UV-registered `base-color-dark.png` derivative. Its selective contrast curve deepens iron texels while retaining the lantern glass; the canonical source base color remains unchanged.
- Night lighting: daytime emission and lighting are off. Evening and Night enable warm glass emission, one soft-shadow point light at the lantern head, and an 8.5-meter warm ground-light pool.
- Catalog preview: reproducibly rendered by `Authoring/Props/SimpleStreetLamppostV01/render_catalog_preview.py`.
- Emission derivative: generated from the supplied UV atlas using the built-in image-generation edit workflow, with the glass isolated as warm emission and non-emissive regions black.
