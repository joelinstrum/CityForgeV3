# White garden paint and first-frame flora-shadow validation

Date: September 21, 2026

## White garden paint

The aged-white fence texture already supplies wood grain, gray wear, and paint
variation. Its cached material additionally multiplied every pixel by
`(0.78, 0.76, 0.68)`, preventing nominally white paint from reading white in
district shade even after the garden meshes joined the shared indirect-lighting
path. The one family material now uses `(0.98, 0.97, 0.92)`. This affects every
composed aged-picket garden and is not stored in, or selected by, an individual
Lot.

A graphics-enabled isolated Noon capture used the production full-cottage
picket composition and `GardenPropPBR`. The fence reads white/ivory while the
source wear, flower colors, and grass remain visible. Unity reported no shader
or compiler errors.

## Stable first flora paint

`RebuildEntireDistrict` formerly built flora with the controller's temporary
default sun before applying the district's saved time preset. `SetTimeOfDay`
then queued all individual flora shadows and their spatial batches for bounded
replacement, producing a visible change after first paint.

The rebuild now publishes the saved environment immediately after camera and sun
creation. Terrain, flora shadow geometry, batches, and Lots consequently derive
their initial presentation from the final preset. Reapplying that same preset
does not queue another flora transition. A genuine later preset change still
uses the established eight-renderer frame budget and one-cell scheduled batch
rebuild; there is no new per-frame scan or full redraw.

The fresh isolated EditMode run passed 43/43 world-lighting,
district-afternoon, picket-garden, and flora-batch checks. The regression test
builds a Morning district, confirms the first shadow batch contains the exact
normalized Morning sun ray, and confirms there is no pending time-of-day work.
It then changes to Afternoon and confirms the transition remains staged across
multiple bounded slices.

The fixture did not load or save player Lot, district, or region content. The
open City Forge V3 editor was not driven or restarted, and
CityForge-Regions-Review was not used.
