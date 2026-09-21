# World lighting contract

City Forge has one environment-lighting owner per rendered world.

- A district owns its directional sun, Unity ambient settings, and the shared
  City Forge shader lighting values.
- A standalone Lot Editor owns its preview environment only while it is not
  district-hosted.
- A district-hosted Lot must not change `RenderSettings`, the shared sun, or
  shared shader lighting. It may update only its local presentation state.
- Opaque meshes use Unity's normal lit-material path. Their base color is never
  routed through emission to compensate for scene lighting.
- Custom artwork shaders read `_CFWorldAmbientColor`, `_CFWorldSunColor`, and
  `_CFWorldLightDirection`, published once when the world time changes.
- Camera-facing artwork receives ambient plus the full directional intensity
  because its quad normal is not a physical surface normal. The calibrated
  shared values retain highlight headroom; noon currently totals approximately
  `(0.96, 0.970, 0.978)` before texture multiplication.
- Custom artwork illumination is hue-preservingly scaled only when its brightest
  channel would exceed the shared 0.98 display-white point. The calibrated sun
  budget keeps native 3D lighting in the same range; it does not add emission.
- Hybrid directional building bases use one shared daylight exposure with a
  highlight shoulder. Their registered shade and genuine night-light overlays
  remain separate, and dusk/full-night artwork is not daylight-lifted.
- Native 3D building materials use one shared daylight-only indirect-diffuse
  scale. This keeps camera-facing shaded facades as readable and colorful as
  directional-render buildings without changing source albedo, direct
  highlights, terrain, or artwork. Evening and night use the neutral scale.
- Roads, rivers, Lot ground, decals, flora, and garden artwork do not have local
  night tints, light floors, sun directions, or time-of-day brightness controls.
- Emission and local lights are opt-in effects for actual emitters: windows,
  lanterns, streetlamps, torches, vehicle headlights, and fire.

The shared values are uniforms, not per-object material updates. Changing the
time of day therefore does not require a district scan to recolor ordinary
surfaces. Existing local presentation updates for explicit night emitters remain
incremental and do not rebuild a district.

## Invalidation boundaries

`DistrictWorldController.ApplyRegionEnvironment` republishes the contract when
the district time changes or its environment is initialized. A standalone
`LotWorldController` republishes it after updating its own preview sun. Loading,
district entry, and editor preview construction already pass through one of
those boundaries. No persistence is involved.
