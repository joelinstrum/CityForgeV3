# Defined forest-cluster ground shadows v01

The SimCity 4 screenshot supplied by Joe is a visual reference for a darker, more legible tree-shadow footprint, not a source asset. No artwork or code was copied from it.

Previously each leafed forest cluster used one projected elliptical canopy fan fading continuously from its center to its edge. The updated mesh keeps the same anchored, sun-directed footprint but adds an inner ring at 76% of its radius. Its opacity stays defined through that ring, then fades over the final 24%. Leafed canopy strength rises slightly from 0.72 to 0.78; the small trunk-contact patch rises from 0.34 to 0.38. Winter keeps its prior soft, lighter 42-vertex footprint. Individual trees, other flora, and their shadow material are unchanged.

The leafed cluster mesh grows from 42 to 66 vertices, without adding a renderer, material, draw pass, per-frame scan, or presentation rebuild. It is updated when a cluster is added, a season changes, or lighting/elevation changes through existing bounded work. In an isolated Unity 6.1 EditMode fixture, all 31 forest/flora tests passed. A 384-cluster season transition retained 32 flora mesh batches and 97 four-cluster slices; maximum measured slice was 15.99 ms and p95 was 13.68 ms in the final headless run. The prior short headless run with the softer shadow reported 14.28 ms maximum and 13.32 ms p95. These runs are not a controlled GPU/frame-time comparison; Joe's dense live district remains the visual and performance acceptance check.

`Documentation/Previews/forest-cluster-defined-shadows-v01.png` is a close-zoom afternoon render of nine synthetic summer deciduous clusters from the isolated fixture. The preview is visual QA only, and Joe's Unity editor was not driven.
