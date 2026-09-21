# River banks V04 artwork lineage

Date: September 21, 2026

Tool: OpenAI built-in image generation in precise-object-edit mode, followed
only by deterministic ImageMagick metadata/alpha normalization. The approved
visual target is `approved-light-shoreline-concept.png`, generated from the
user's September 21 Regions Review screenshot and explicitly approved by the
user. It establishes the value rule: deep water is darkest; clear cyan-blue
shallows and pale gravel become progressively lighter; there is no brown or
dark wet outline.

| Runtime file | Structural edit target | Selected generation output |
| --- | --- | --- |
| `shoreline-light.png` | `BanksV2/shoreline.png` | `exec-5d76dc22-4ff6-49f7-8b54-ad8d4c3484fa.png` |
| `open-gravel-light.png` | `BanksV3/open-gravel.png` | `exec-d67a34bd-9b2e-4556-b637-449754bbe13e.png` |
| `submerged-gravel-light.png` | `BanksV1/inside-gravel.png` | `exec-c9eb4c80-c0fb-41bd-9e32-cbb9178a5ca9.png` |

All selected outputs are under built-in generation session
`01a0c06d-340b-7df3-9096-01a475be1842`. The first two retained the exact
2172×724 dimensions of their targets. The submerged gravel retained the exact
1536×1024 dimensions of its target. No resizing, cropping, painting or color
grading followed generation. Runtime PNGs were normalized to opaque 8-bit RGB
with this mechanical command:

```text
magick selected.png -alpha off -strip PNG24:runtime.png
```

Runtime SHA-256:

- `shoreline-light.png`: `a20994599164c2f28e6c87b9d2a42e7d6e10cd758d338c1352a2579f7bfe7c6b`
- `open-gravel-light.png`: `a24514c350041278aa2e0d6481b26854c94160c2d811d45258947cd14fd17f5e`
- `submerged-gravel-light.png`: `e4150127ba71d40d5ccb4697a32ddc393c574f45cb541c6144f74d894f13cb50`

## Shoreline prompt

```text
Use case: precise-object-edit
Asset type: CityForge V3 production riverbank texture, light shoreline V04
Input images: Image 1 is the exact edit target and required wide top-down bank composition; Image 2 is the approved color and value reference only
Primary request: transform Image 1 into the approved light natural shoreline while preserving its wide orthographic composition, scale, and vertical material progression. The upper edge remains irregular natural green grass. The middle becomes pale cool-gray, silver-gray, and light beige river pebbles with a few weathered light-gray rounded stones. The lower submerged area becomes visibly LIGHTER than deep river water: pale stones seen through clear cyan-blue/blue-green shallows. Remove the dark bottom zone completely.
Style/medium: realistic-painterly isometric strategy-game ground texture matching CityForge
Composition/framing: full-bleed extra-wide strip, top-down, grass at top transitioning through dry pale gravel into luminous submerged pebbles at bottom; horizontally seamless with natural continuation across left and right edges; no focal point
Lighting/mood: neutral soft daylight with restrained stone highlights, no cast shadows
Color palette: cool pale gray, silver, subdued beige, soft moss green, clear cyan-blue shallows
Constraints: preserve Image 1 dimensions/aspect and bank scale; no continuous brown, black, charcoal, purple, muddy, or dark-gray band anywhere; no dark wet rim; no reddish or orange soil; no water body texture, foam, waves, buildings, people, boats, text, watermark, frame, or hard border. Keep grass fingers irregular but restrained. Opposite horizontal edges must tile inconspicuously.
```

## Open-gravel prompt

```text
Use case: precise-object-edit
Asset type: CityForge V3 production riverbank texture, light open-gravel alternate V04
Input images: Image 1 is the exact edit target and required sparse-grass wide bank composition; Image 2 is the approved color and value reference only
Primary request: transform Image 1 into a light, open, natural gravel shoreline while preserving its top-down extra-wide composition and deliberately sparse vegetation. Keep a narrow irregular grass boundary at the top, a broad field of pale cool-gray and silver river pebbles through the middle, and luminous pale submerged stones under clear cyan-blue/blue-green shallows at the bottom. Completely remove the existing brown dry soil and dark submerged region.
Style/medium: realistic-painterly isometric strategy-game ground texture matching CityForge
Composition/framing: full-bleed extra-wide strip, top-down; horizontally seamless with natural continuation at left and right; fewer grass fingers and fewer prominent sedge clumps than the other shoreline variant; varied open gravel pockets without rows or repeated stamps
Lighting/mood: neutral soft daylight, restrained highlights, no cast shadows
Color palette: cool pale gray, silver, quiet light beige, restrained moss green, clear cyan-blue shallows
Constraints: preserve Image 1 dimensions/aspect, material scale, and low vegetation density; no continuous brown, black, charcoal, purple, muddy, or dark-gray band; no dark wet rim; no reddish/orange soil; no water surface waves, foam, buildings, people, boats, text, watermark, frame, or hard border. Opposite horizontal edges must tile inconspicuously.
```

## Submerged-gravel prompt

```text
Use case: precise-object-edit
Asset type: CityForge V3 production submerged riverbed texture, pale gravel V04
Input images: Image 1 supplies pebble scale and realistic-painterly material detail; Image 2 supplies the approved light cyan-blue shallow-water value and color target
Primary request: create a uniform top-down field of pale cool-gray and silver river pebbles visible through clear, softly luminous cyan-blue shallow water. Remove all grass, exposed brown earth, dark mud, dark wet stones, and vertical bank progression. The full image should be a consistent submerged gravel bed suitable for sampling anywhere beneath the river edge.
Style/medium: realistic-painterly isometric strategy-game ground texture matching CityForge
Composition/framing: orthographic top-down, full-bleed, uniform fine-to-medium rounded pebbles with restrained size variation, no horizon, no focal cluster; inconspicuously seamless across left/right and top/bottom edges
Lighting/mood: diffuse daylight filtered through clear shallow water, soft pale stone highlights, no directional cast shadows
Color palette: pale silver-gray, cool light gray, quiet light beige, restrained moss hints, transparent cyan-blue water cast
Constraints: preserve Image 1 dimensions/aspect and pebble scale; no grass, plants, soil, banks, dark zones, continuous bands, black/charcoal/purple/brown mud, water surface waves, foam, caustic stripes, large boulders, objects, text, watermark, or frame. Keep overall luminance clearly higher than deep blue water.
```
