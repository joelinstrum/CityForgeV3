# Bridge joints and graded approaches — V03

Unity 6000.1.12f1; feature/district-bridges. The original left and right stone ends remain unchanged. Adjacent complete middle sections alternate longitudinal direction so identical cut faces meet. An odd module count preserves the original end connectors. Fit is distributed across the modules; intermediate spans may compress or lengthen them because the count must be odd. Source deck undulation remains visible.

## Verification

- 88/88 focused EditMode tests passed. The new approach test rejects occupied grass shoulders outside the road width and confirms those shoulders do not become the bridge travel corridor.
- Unity graphics QA passed construction/cancel/undo/reload and both bridge styles.
- A 120 m assembly with seven center modules checks every neighboring cut-face vertex in both directions: all matched within 2 mm, including both original ends. Travel elevations sampled 1 mm before/after each seam differ by less than 1 cm.
- Both bank approaches have fixed 72-vertex combined earth geometry, a road-width crest, and grass shoulders falling toward the original local ground. The placement corridor reserves up to 7.5 m on each side at the banks. The earth is a local bridge-owned terrain overlay using the district's shared grass material; it does not edit the saved heightfield or repaint the district. Removal/undo removes it with the bridge. No autosave was added.

[Close-up of module joints](stone-seams-close.png), [graded banks](stone.png), [long-span fixture](stone-repeated.png). The long-span fixture deliberately extends over dry ground to exercise the module count; its underlying road is diagnostic setup, not a newly painted underwater road.

## Performance limits

The earth adds one renderer per bridge, bringing the total from two to three. It uses the existing shared ground material and is built only with the bridge/preview. No scans or whole-district redraws were added. See report.txt: 25 bridges use 75 renderers, versus the prior 50. Median camera submission in this run was 1.05 ms (prior run 0.68 ms), maximum 1.48 ms (prior 1.11 ms); 100,000 indexed travel queries took 72.01 ms (prior 43.56 ms). These short runs were not controlled benchmarks and include machine-load variation. Batch UnityStats reports zero, so actual draw calls, GPU time and full frame time remain unverified. Dense mixed-district and long-duration profiling remain open.

Existing edit-composition scans were not changed: the measured 20,000-flora/10,000-road fixture remains an 85.24 ms edit-boundary spike. This is a pre-existing limitation recorded in V01, not bridge travel or recurring geometry work.
