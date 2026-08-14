# Brick road artwork V48

The active road package now uses Joel's coherent brick-road artwork authored on
2026-07-28. Straight, corner, T-junction, and four-way tiles come from the same
source family, so their roadway mouths, curbs, brick scale, and color align.

The previous `CobblestoneV1` package remains in Resources as preserved legacy
artwork. It is no longer the production manifest because its generated corner
used a narrower roadway mouth than its straight tile.

## Contract

- Tile: 10 x 10 meters
- Roadway width: 3.8 meters
- Straight base ports: north / south
- Corner base ports: south / west, matching the authored bitmap
- T-junction base ports: east / south / west
- Four-way ports: all directions
- Endpoint: pending; the editor disables its button until matching authored art
  is available

Source files are copied, not moved or overwritten, from
`/Users/joelinstrum/Downloads/images/textures/brick-road`.
