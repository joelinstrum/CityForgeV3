# Art Museum Production LOD Pilot

## Source lineage

Imported unchanged from `/Users/joelinstrum/Downloads/buildings/art-museum`
into `ArtMuseumProduction/`. Original archives and the appearance reference are
retained in `Source/`.

| Source | SHA-256 |
|---|---|
| ArtMuseum-LOD0.zip | `47752fb7a41bc49b894376155a4d5b3caf56f535c4b601efa77388a9e45479b8` |
| ArtMuseum-LOD1.zip | `24b44f85912b27f21d61b752d4939f2256ff9ee8876be90595eeb49ff3e1cf84` |
| ArtMuseum-LOD2.zip | `ee7a9bd4bdf1dea306fedc59b5f448ae1675d2e76c2b87cc74917bd330692294` |
| ArtMuseum-LOD3.zip | `46c389b79c17ceb60a72eed397a05887679d3a070cab4ad1f5bcc2396b426046` |
| ArtMuseum.png | `bc8cae9f78f64271d8e44b2384f0351135251f6d1fb365d66d8c870a1567b315` |

## Unity measurements

Measured after import in Unity 6.1.12f1. Each level has one renderer, one
submesh, and one material slot.

| Level | Triangles | Vertices | Imported bounds (x, y, z) | Center |
|---|---:|---:|---|---|
| LOD0 | 220,023 | 157,895 | 0.71, 0.80, 0.97 | 0.00, 0.40, 0.00 |
| LOD1 | 17,883 | 17,028 | 0.73, 0.83, 0.97 | 0.00, 0.41, 0.00 |
| LOD2 | 14,046 | 14,489 | 0.70, 0.81, 0.98 | 0.00, 0.41, 0.00 |
| LOD3 | 6,832 | 8,113 | 0.68, 0.78, 0.97 | 0.00, 0.39, 0.00 |

The bounds and centers pass the package's 5% tolerance. The imported source is
normalized to roughly one metre, so the package applies one common 40x scale,
yielding an approximately 28.4 x 38.8 m footprint and 32 m height. No source
mesh is modified.

LOD1 does not meet the original ~80K target and is close to LOD2 in cost. Keep
its authored label, but accept its transition only after visual comparison; it
may be redundant if it does not preserve visibly more architecture than LOD2.
LOD3 is slightly above the 3K–5K target but is suitable for evaluation.

## Runtime configuration

| Level | Initial screen height | Visible mesh | Shadow mesh |
|---|---:|---|---|
| LOD0 | 0.60 | supplied LOD0 | supplied LOD2 |
| LOD1 | 0.30 | supplied LOD1 | supplied LOD2 |
| LOD2 | 0.12 | supplied LOD2 | supplied LOD3 |
| LOD3 | 0.035 | supplied LOD3 | supplied LOD3 |
| LOD4 | 0.01 | pending impostor | intended LOD3/hybrid caster |

Cross-fade width starts at 0.12. The thresholds remain provisional until fixed
camera comparisons confirm that façade sculpture, roof silhouette, color,
pivot, and ground contact do not pop.

### Material correction

The source base-color and normal textures were present, but Unity's embedded FBX
material interpreted the Tripo packed metallic/roughness channels incorrectly,
making the stone appear nearly black and metallic. Each LOD now uses an explicit
CityForge material binding its own supplied base-color and normal texture. Stone
is treated as dielectric (`metallic = 0`) with restrained smoothness. Source
textures remain unchanged.

The authored low-detail shadow caster overlaps the visible mesh. The visible
museum therefore does not receive that mismatched caster's self-shadow; it
still receives the active directional and ambient lighting, while the cheaper
caster continues to shadow the lot and nearby objects. This prevents the
façade from collapsing to black.

The projected-ground-shadow clone must update only renderers using
`ProjectedBuildingMeshShadow`. Disabled beauty renderers inside that clone
retain references to the real LOD materials; writing the shadow `_Color` to
those shared materials black-tints every visible atlas. Runtime filtering now
isolates the shadow parameters, and the supplied warm-stone, slate-roof,
window, and red-banner colors remain visible on all four LODs.

### Lot manipulation

Click the visible museum from any Lot Editor tool to select it; selection is
shown by one clean light-blue outer-volume outline rather than highlighting the
many imported child meshes. Drag across the lot ground to move it. Left arrow
rotates clockwise and right arrow rotates counter-clockwise in 45-degree steps;
up/down are consumed while the museum is selected, so the lot cannot pan.
Clicking empty space clears the outline and disables movement until the museum
is selected again. Position and rotation are stored in `PlacedBuilding3D` and
survive the normal save/load path.

## Remaining art

- CityForge-calibrated multi-heading LOD4 billboard/impostor;
- final visual approval of the unusually aggressive LOD0-to-LOD1 reduction;
- optional cheaper shadow-only mesh if LOD3 shadow cost proves too high;
- simple collision proxy.
