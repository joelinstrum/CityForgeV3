# Oak Trees Flora V01

Source package: `/Users/joelinstrum/Downloads/flora/oak-trees`

## Published billboards

| Flora ID | Display name | Height | Seasonal artwork |
| --- | --- | ---: | --- |
| `canyon-live-oak-a` | Canyon Live Oak A | 14 m | Green spring/summer, yellow autumn, bare winter |
| `canyon-live-oak-b` | Canyon Live Oak B | 13.25 m | Green spring/summer, yellow autumn, bare winter |
| `angel-oak-spanish-moss` | Angel Oak with Spanish Moss | 20.5 m | Evergreen artwork with restored moss alpha |

The billboards use the CityForge transparent 768 px render contract. Their
runtime pivots preserve two pixels beneath the visible root silhouette: close
enough to remove the floating gap while retaining the complete rounded base.
Oak billboard alpha is remapped from 5%-30% to 0%-100% after rendering. This
removes accumulated card transparency while preserving holes and soft edges.

## Deferred sources

- The Angel Oak maps and material library were supplied in a follow-up. They
  restore the FBX bark, foliage, and Spanish moss presentation.
- The later `ANGEL-OAK` package confirmed the authoritative V-Ray material
  assignment and contains geometry matching the earlier Corona FBX.
- The older sprawling OBJ was also repaired with the supplied maps, but its
  locked-camera billboard hides the trunk and reads as a floating canopy, so
  it remains an authoring-only evaluation rather than a published Flora item.
- `Blender-Quercus virginiana.rar` is complete, but its extremely broad,
  low canopy hides the trunk from the locked CityForge billboard camera and
  reads as a floating shrub. It was rendered for evaluation but not published.
