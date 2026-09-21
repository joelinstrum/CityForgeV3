# Hosted Lot lighting and Town Center catalog validation

Date: September 20, 2026

The user reference captures showed the same authored Town Center looking vivid
in the standalone Lot Editor and dull after district hosting. The district
version can rotate a native building so its visible facade receives mostly
indirect light. Validation therefore exercises a shared building-family
indirect-diffuse calibration, not a Town Center or per-Lot override.

The final contract publishes `_CFNativeSurfaceIndirectScale` once through
`ApplyRegionEnvironment`: 2.5 for Morning, Noon, and Afternoon, and 1.0 for
Evening and Night. The native building shader scales only `UnityGI` indirect
diffuse. Direct light, specular highlights, source albedo, terrain, hybrid
artwork, and all ordinary-surface emission remain unchanged. Window and lamp
emission retain their existing nighttime controls.

A graphics-enabled isolated fixture rendered the real Town Center at all five
presets. Morning, Noon, and Afternoon retained lively wood and shutter color,
readable white stone, and highlight detail while the comparison ground stayed
unchanged. Evening and Night remained on the prior neutral path. Unity reported
no shader compilation errors. A rejected albedo-exposure experiment produced
colored highlight artifacts and was not retained.

The bundled `town-center-civic-v01` is marked hidden. Catalog construction keeps
it in the direct stable-ID lookup for older placed instances but excludes it
from `LotContentCatalog.All`, the founder browser, and the Civic build browser.
The user-authored District Town Center is consequently the sole visible choice.

The final isolated EditMode run passed 32/32 assertions: the full eight-test
content-catalog architecture suite plus the focused world-lighting, hosted-Lot,
Town Center, and UI checks. A preceding final-code run passed the narrower 25/25
selection as well. The fixture did not load or write player Lot, district, or
region content. The open City Forge V3 editor was not driven or restarted, and
CityForge-Regions-Review was not used.
