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
