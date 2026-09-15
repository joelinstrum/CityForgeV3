# District building selection and rotation

- Use Select and click a lot, Stone Quarry, or Brickworks. A blue boundary identifies the selection; the inspector shows its facing and rotation buttons.
- Rotate one selected building in 90-degree steps with the buttons, R (clockwise), or Shift+R (counter-clockwise). Empty land clears selection. Rectangle selection also includes industrial buildings.
- The editor's hidden resource menu still uses R when no rotatable building is selected. Pending Brickworks placement retains its existing R control.
- Industry rotation validates terrain, district bounds, lots, roads, and other industrial footprints before committing. Rotation is saved and supports district undo.
- A parked quarry wagon rotates with the loading bay. A traveling wagon retains its district position and headings; returning wagons target the rotated bay. Rotating a Brickworks redirects incoming or unloading wagons to its new receiving position without crediting cargo twice.
- Industry rotation data changes live in `DistrictIndustryRotation`; validation, picking and outlines live in the district world presentation; UI reuses the selected-lot panel styles. No artwork was changed or ported.

Validation: 62 EditMode tests passed, including five rotation cases covering parked/traveling wagons, receiver changes, cargo preservation and save/reload. Live isolated-district checks exercised screen picking, both industrial inspectors, button rotation and undo. R was visually verified on the quarry. Evidence is in `QA/DistrictRotation/`.
