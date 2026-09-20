# Town Center bundled Civics Lot validation

`town-center-civic-v01` is a read-only project resource, not a file in the
player's `CityForge/Lots` directory. It composes `town-center-v01` at the center
of a 2 × 2 Civics Lot and uses the existing Town Center thumbnail. Normal
district placement costs $2,500 and requires a road; the Lot adds no residents.

The ordinary **Build → Civic → Browse Civic Lots** browser discovers the Lot
through `LotContentCatalog`. The Town Center card in the town-founding flow uses
the same definition, retaining the existing founder population override and
500-food reserve. Loading the bundled manifest happens at the catalog's existing
cache boundary and does not add a per-frame scan, district rebuild, or save.

Focused isolated Unity EditMode validation covers resource discovery, stable
identity, Civic classification, thumbnail resolution, cost and requirements,
runtime Lot composition, the normal Civic browser, the founder card, founder
food behavior, and the earlier Town Center lighting/LOD checks. Results are in
`tests.xml`. The fixture never invoked Lot, district, or region Save.
