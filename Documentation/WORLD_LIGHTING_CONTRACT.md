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
- Native 3D building and garden-prop materials use one shared daylight-only
  indirect-diffuse scale. Garden meshes additionally share one family-wide
  daylight exposure followed by the world's hue-preserving 0.98 white-point
  shoulder. This keeps small shaded meshes readable and colorful without
  changing any asset's source albedo, direct lighting, terrain, or billboard
  art. Evening and night use neutral scales.
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
`LotWorldController` republishes the same representation-family values after
updating its own preview sun. Loading,
district entry, and editor preview construction already pass through one of
those boundaries. District construction publishes the saved preset before any
terrain, flora shadow, or Lot presentation derives lighting state; applying the
same preset again does not schedule a redundant flora-shadow transition. No
persistence is involved.
