# Lot Editor coordinate and direction contract

## Character travel headings

The Lot Editor uses Unity yaw headings for cardinal character travel. These
values are calibrated and approved; do not rotate them to match the apparent
isometric screen angle:

| Direction | Heading |
| --- | ---: |
| North | 0° |
| East | 90° |
| South | 180° |
| West | 270° |

Combined arrow keys use the exact halfway headings:

| Direction | Heading |
| --- | ---: |
| North-east | 45° |
| South-east | 135° |
| South-west | 225° |
| North-west | 315° |

`LotWorldController.CharacterDirectionForArrowInput` is the runtime authority
for this mapping. Changing camera azimuth, building artwork registration, or
model forward orientation must not silently alter these travel headings.

## Sun and shadow compass contract

The same world headings govern directional lighting. Screen-left and
screen-right are never substitutes for compass directions because their
appearance changes when the isometric camera rotates.

| Time | Sun position | Light rays and cast shadows |
| --- | --- | --- |
| Morning | East (90°) | West (270°) |
| Noon | High in the south (180°) | Northward, compact but readable shadow |
| Afternoon | West (270°) | East (90°) |

`TimeOfDayLighting.SunRotation` is the runtime authority. Unity directional
lights point in the direction their rays travel, so the light transform uses
the sun-position azimuth plus 180°, followed by the Lot Editor's calibrated
90° counter-clockwise visual-compass correction. This correction is required because raw Unity
X/Z headings otherwise display as south/north in the isometric lot view when
the player expects west/east. Camera registration and model-facing offsets
must never add any further rotation to this world-space light direction.

## Building profile and height

Top Down controls a building's X/Z placement. Profile is an orthographic
elevation view of the selected native 3D building; its two axes show the
east-west and north-south terrain cross-sections. The gold line samples lot
terrain, or the district's actual terrain/riverbed when hosted. The optional
blue line uses a nearby authored water area or district river water elevation
when available. Otherwise it is an adjustable preview reference, clearly
labelled as such and not saved as a water body.

`PlacedBuilding3D.ElevationOffsetMeters` is the building's saved Y offset from
the lot datum; absent fields in older saves mean zero. Profile dragging and
the inspector buttons change only the selected building, its existing shadow
copies, and its selection outline. Snap Base to Ground samples the terrain at
the building center; use the profile lines to check edges on a slope. Camera
and preview-line changes are editorial only. Progress is written only by the
explicit Lot Save action. No per-frame district scan or redraw is required.
