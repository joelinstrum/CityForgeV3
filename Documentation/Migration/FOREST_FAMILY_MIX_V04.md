# Forest family mix V04 seasonal derivatives

Date: September 22, 2026

Destination:
`Assets/CityForgeV3/Resources/CityForgeV3/Flora/ForestClustersFamilyMixV04/`

V04 adds autumn and winter derivatives for the accepted V03 compact and large
deciduous/mountain cluster silhouettes. It replaces the runtime fallback to V01
seasonal compositions, whose trunks were arranged in a single row. Summer and
spring remain on V03, tropical clusters remain summer-only, and all V01/V03
source artwork remains unchanged.

The eight PNGs were produced with the built-in image-generation editor from
their matching V03 summer targets. Every prompt required the exact tree count,
species mix, trunk positions, depth staggering, diamond-like arrangement,
silhouette, camera, square canvas, and transparent background to remain fixed.

Autumn prompt: change only foliage to a restrained natural early-to-mid autumn
palette of muted ochre, russet, brick red, dull gold, and remaining olive green;
keep evergreens subdued green; avoid neon color, oversaturation, uniformly
orange foliage, row alignment, ground, shadows, text, borders, and watermarks.

Winter prompt: change only seasonal vegetation; make deciduous trees naturally
leafless with fine branching and keep evergreen needles restrained cold green;
avoid snow, frost, opaque leafy canopies, row alignment, ground, shadows, text,
borders, and watermarks.

Generated-source lineage:

- `forest-deciduous-compact-autumn.png` — `exec-4955b102-b105-41d3-92be-7424bd155833.png`
- `forest-deciduous-large-autumn.png` — `exec-b09295f2-3e17-4207-b5ca-0b484337c847.png`
- `forest-mountain-compact-autumn.png` — `exec-2d04c0dd-6988-48f3-ac57-17f701328298.png`
- `forest-mountain-large-autumn.png` — `exec-133aaaf7-6993-4f43-965e-52dde005b46b.png`
- `forest-deciduous-compact-winter.png` — `exec-39d2288b-9769-47f5-b5af-e27462bb8afb.png`
- `forest-deciduous-large-winter.png` — `exec-55340b55-fe5c-4077-97e2-16a497847e39.png`
- `forest-mountain-compact-winter.png` — `exec-2c67cf30-7525-474b-92be-73d887f267c5.png`
- `forest-mountain-large-winter.png` — `exec-16a5d037-a4a6-422d-a3b3-c77a297dffe4.png`

All derivatives are 1254 × 1254 RGBA PNGs. Unity import settings are copied
from V03 with new GUIDs, mipmaps, preserved alpha coverage, readable pixels,
clamped wrapping, and the existing 2048 maximum texture size.
