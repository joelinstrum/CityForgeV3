# New England barn prop v01

Source: `/Users/joelinstrum/Downloads/buildings/New England Farm 2/barn-2.zip`.
All six supplied files are preserved byte-for-byte under
`Assets/CityForgeV3/Resources/CityForgeV3/Props/Agriculture/NewEnglandBarnV01/Source`.

Available in Lot Editor → Props → Agriculture as **New England Barn**
(`new-england-barn-v01`). The prop library already has category sections;
Agriculture extends that existing UI. This is a decorative prop, with no
production, employees, or era restriction. Existing prop serialization applies.

One source mesh, 8,596 vertices and 17,419 triangles, one material.
Instance orientation corrects Z-up with -90° pitch; shared normalization sets
height to 9.8m, footprint approximately 11.35 × 14.08m (uniformly enlarged
40% following the farmhouse comparison). Source geometry is unchanged.
Original `_0` albedo and `_2` normal atlas are bound explicitly; timber uses a
matte nonmetallic material. Other supplied maps remain preserved.

The library thumbnail is a derivative rendered in a disposable Blender scene.
Shared depth, shadow, and wet reflection helpers now retain the visible model’s
local rotation along with its scale and position. No additional render passes
or per-frame work were added. Existing static-prop presentation uses three
auxiliary mesh passes; this intake is not a dense-prop performance benchmark.

Validation and source hashes: `Documentation/Validation/new-england-barn-v01/`.
