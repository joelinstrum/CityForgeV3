# River banks V06 varied artwork lineage

Date: September 21, 2026

Tool: OpenAI built-in image generation in image-edit mode, followed only by
deterministic ImageMagick alpha/metadata normalization. The V05 wide natural
shoreline was the edit target for all four bank compositions. The V05
submerged gravel was the target for the neutral riverbed. No accepted V05 or
earlier file was overwritten.

| Runtime file | Selected generation output |
| --- | --- |
| `bank-01-neutral.png` | `exec-9bb8846b-63f4-43f5-acb5-6085c3c5afc2.png` |
| `bank-02-bars.png` | `exec-af36092d-a38e-4922-85e4-647afd2e48ec.png` |
| `bank-03-open.png` | `exec-45c7fa0d-38a6-46a1-817b-bfd508fee9d4.png` |
| `bank-04-cobbles.png` | `exec-1d34370e-db0e-4b05-9685-b7ee731987c9.png` |
| `submerged-neutral.png` | `exec-53492527-57e6-4b32-afb4-cb8aa91b7ba5.png` |

All outputs are in built-in generation session
`01a0c06d-340b-7df3-9096-01a475be1842`. Bank images retain the target's exact
2172×724 runtime dimensions; submerged gravel retains 1536×1024. Because three
selected bank outputs differed from the target by one or two pixels, all four
were normalized to the exact production dimensions. Runtime conversion was:

```text
magick selected.png -resize '2172x724!' -alpha off -strip PNG24:runtime.png
```

Runtime SHA-256:

- `bank-01-neutral.png`: `e01831d75bb52551e52effd93c2b96efacd4f63f03fc2a59f1e91fe6da4a495e`
- `bank-02-bars.png`: `adb6c4e127fcd133e3cd63c11320a50c91be21047aaa4ea094542c7436b80359`
- `bank-03-open.png`: `78a9b3512c16437ee923964b21f314e00b715e9b99bb061f8fd59703857efe07`
- `bank-04-cobbles.png`: `db3deb2c50bc8221b42e1f39642cd86cad2148c0ed6acbe183bf5873d882bf53`
- `submerged-neutral.png`: `2f31a9487fe89d6c1da1b1814721690b2bc5817fb5aed8af4a0cdb62ea8bb488`

## Exact shared bank prompt

Each bank call used this text followed by its listed variant directive:

```text
Use case: precise-object-edit
Asset type: CityForge V3 production wide-river shoreline texture variant
Input image: structural/style reference and edit target. Preserve exact 2172x724 dimensions, top-down orthographic view, painterly realism, material scale, neutral daylight, grass palette, pale silver-gray gravel, neutral wet stones, and left/right material heights for horizontal repeat.
Primary request: REDESIGN THE LARGE-SCALE GRASS/GRAVEL SHORELINE SILHOUETTE. Do not preserve the source's four evenly spaced scalloped grass cutouts. Remove that repeated pattern completely. Remove all baked blue, cyan, turquoise, royal-blue, and blue-gray water color; lower wet stones are neutral gray with only a faint green-gray undertone.
Constraints: full-bleed single bank strip; grass above, dry gravel middle, neutral wet gravel below; no alpha, text, frame, whole-river scene, foam, waves, mud, dark outline, regular scallops, repeated bays, or evenly spaced features.
```

Variant directives:

```text
Variant 1 — DIRECT GRASS EDGE: Grass meets the gravel along one mostly continuous, gently irregular horizontal edge. No bays, no deep cutouts, no grass fingers, no repeating lobes. Boundary movement stays shallow, organic, and within about 8 percent of image height. Use scattered tiny tufts only.

Variant 2 — ONE LONG GRAVEL BAY: Create exactly one broad extended exposed-gravel cutout centered in the image, spanning roughly 55 percent of the total width. The gravel pushes upward into the meadow as one continuous shallow basin with softly irregular sides. Outside that single bay, grass meets gravel directly. No second, third, or fourth cutout.

Variant 3 — ONE GRASS PENINSULA: Make the grass/gravel boundary mostly open and gently uneven, with exactly one broad asymmetric grass peninsula descending into the gravel left of center. The peninsula is long and soft-edged, not a narrow finger. No repeated coves or evenly spaced lobes anywhere else.

Variant 4 — TWO UNEQUAL BREAKS: Create only two widely separated shoreline events: one short shallow gravel opening near the left quarter, and one much longer irregular gravel opening across the right half. All remaining stretches have grass meeting gravel directly. The two openings must differ strongly in width and depth; no repeating four-part rhythm.
```

## Exact submerged-gravel prompt

```text
Use case: precise-object-edit
Asset type: CityForge V3 production submerged riverbed texture
Input image: edit target. Preserve exact 1536x1024 dimensions, uniform all-over pebble field, top-down orthographic view, stone scale and density, painterly realism, flat neutral daylight, and seamless-looking edge continuity.
Primary request: Remove all baked blue, cyan, turquoise, royal-blue, and blue-gray water color. Recolor the entire image as neutral wet river stones seen through clear colorless shallow water: cool charcoal-gray and soft neutral gray pebbles with only a very faint natural green-gray undertone. Reduce saturation and contrast slightly. The actual animated water surface will supply all blue color in game.
Constraints: no shoreline, no grass, no alpha, no text, no frame; preserve varied pebble sizes and sparse rounded stones.
Avoid: colored water wash, blue ribbon, cyan cast, brown mud, dark vignette, foam, waves, highlights suggesting a water surface.
```
