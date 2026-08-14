# Migration 0001 — initial assets

## Reused

| Asset | Source | V3 destination | Reason |
|---|---|---|---|
| City Forge splash | `City Forge - Foundations Next/BaseGame/ui/art/splash/city-forge-splash.png` | `Assets/CityForgeV3/Resources/CityForgeV3/Art/city-forge-splash.png` | Explicitly approved visual |
| Main-menu labels | Existing product vocabulary | Runtime screen composition | Explicitly approved vocabulary |
| Upbeat main-menu background, logo, five illustrated menu buttons, settings icon, and statistics icon | `City Forge - Foundations Next/BaseGame/ui/art/main-menu/upbeat/` | `Assets/CityForgeV3/Resources/CityForgeV3/Art/MainMenu/` | Joe explicitly approved the complete current menu screen and icons for V3 |

## Rebuilt

- Unity project and settings
- application bootstrap
- navigation and screen state
- main-menu interaction and reusable image-button composition
- new UI buttons and interaction states
- lot-editor layout
- 3D lot scene and camera

## Deliberately excluded

- legacy UI runtime code;
- legacy USS, prefabs, and unapproved sliced frames and panels;
- old lot renderer and its pivot workarounds;
- bulk content definitions.

## Follow-on hybrid derivative

The first approved building port is recorded separately in
`Documentation/Contracts/FIVE_BAY_HYBRID_V01.json`. It reuses the exact v28
runtime pixels descended from the canonical v79 Federal Georgian Five-Bay
asset. The source files remain unchanged; V3 owns new copies, a centered metric
proxy, and new presentation code.
