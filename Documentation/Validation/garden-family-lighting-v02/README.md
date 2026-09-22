# Garden-family lighting validation v02

Date: September 21, 2026

## Scope

The rejected picket-only material change is fully reverted. The aged-picket
material retains its authored `(0.78, 0.76, 0.68)` tint. No Lot, prop ID, or
individual material selects a lighting correction.

`GardenPropPBR` now applies one 1.3 daylight exposure to its already-lit result,
then hue-preservingly bounds only values above the world's 0.98 display-white
point. This affects every native mesh using the garden shader. Morning, Noon,
and Afternoon use 1.3; Evening and Night use 1.0. The shader's emission output
remains zero.

Both `DistrictWorldController` and standalone `LotWorldController` publish the
same garden exposure at their existing environment transition boundaries. This
is one global uniform write, with no district scan, Lot walk, material walk,
redraw, rebuild, or per-frame work.

## Validation

An isolated Unity 6000.1.12f1 fixture passed 43/43 focused world-lighting,
garden, district-afternoon, and flora-batch checks. The suite verifies the
daylight/night exposure values, global publication, white-point use,
non-emissive shader contract, garden shader assignment, and the independent
first-paint flora-shadow regression.

Graphics-enabled captures compare neutral exposure against the shared daylight
response across full-cottage and mixed pickets, boxwood, clipped hedges, and
square/rectangular Georgian beds. Native meshes lift coherently; billboard
flowers/grass and the ground do not change, and highlights retain detail.

- `garden-family-neutral.png`
- `garden-family-daylight.png`

Unity reported no C# or shader errors. The fixture did not load or save player
Lot, district, or region content. The open editor and
CityForge-Regions-Review were not driven.
