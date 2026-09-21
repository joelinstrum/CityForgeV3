# River banks V05 Wide artwork lineage

Date: September 21, 2026

Tool: OpenAI built-in image generation in image-edit mode, followed only by
deterministic ImageMagick metadata/alpha normalization. The accepted V04
runtime files were the structural edit targets. The user's September 21
Regions Review screenshot established the problem: clear cyan V04 shallows
look attractive on streams and medium rivers but form an overly bright blue
rim when spread across a major river.

| Runtime file | Exact edit target | Selected generation output |
| --- | --- | --- |
| `shoreline-wide-muted.png` | `BanksV4/shoreline-light.png` | `exec-b0463415-cd07-45cd-b923-80d207e1835e.png` |
| `open-gravel-wide-muted.png` | `BanksV4/open-gravel-light.png` | `exec-0ff4b293-5a55-4da6-8477-32cbce437a0b.png` |
| `submerged-gravel-wide-muted.png` | `BanksV4/submerged-gravel-light.png` | `exec-b2f0e07f-727c-47ea-971a-a4ebff60efb1.png` |

All outputs are under built-in generation session
`01a0c06d-340b-7df3-9096-01a475be1842`. The shoreline images retained their
exact 2172×724 target dimensions and the submerged gravel retained its exact
1536×1024 target dimensions. No resizing, cropping, painting or color grading
followed generation. Runtime PNGs were normalized to opaque 8-bit RGB with:

```text
magick selected.png -alpha off -strip PNG24:runtime.png
```

Runtime SHA-256:

- `shoreline-wide-muted.png`: `9a0abc73e347dbfa08e7c5b95205aa4628375905fc9131859a2841d2481b54e9`
- `open-gravel-wide-muted.png`: `4a21c00aff3d6ed6f119b5c00be58c5ab1305d6e882f32c89e894488c10ee476`
- `submerged-gravel-wide-muted.png`: `3c229d066e3f26a9bf5b052357c38717c6ac211b870bc45a810621f515b6a5a8`

## Exact shoreline prompt

```text
Edit this exact CityForge top-down orthographic river shoreline texture into a separate wide/major-river bank variant. Preserve the exact 2172x724 panoramic layout, straight horizontal bank orientation, grass along the top, pale neutral gray gravel and stones in the middle, texture density, painterly realism, lighting direction, and edge-to-edge continuity. Change only the submerged lower portion: remove the luminous cyan, turquoise, royal-blue, tropical-blue cast. Replace it with restrained desaturated slate blue-gray water with a very subtle natural green-gray undertone. The submerged band should be only modestly lighter than deep river water and lower in saturation and contrast, so it will not create a bright colored rim on a very wide river. Keep submerged stones visible but subdued and less contrasty. No brown bank, no mud, no dark outline, no bright cyan, no white foam, no waves, no labels, no text, no frame. Produce a production-ready game texture.
```

## Exact open-gravel prompt

```text
Edit this exact CityForge top-down orthographic open-gravel river-edge texture into a separate wide/major-river bank variant. Preserve the exact 2172x724 panoramic layout, straight horizontal shoreline orientation, grass along the top, pale neutral gray gravel and stones, texture density, painterly realism, lighting direction, and edge-to-edge continuity. Change only the submerged lower portion: remove luminous cyan, turquoise, royal-blue, tropical-blue color. Replace it with restrained desaturated slate blue-gray shallow water with a very subtle natural green-gray undertone. The submerged region must be only modestly lighter than deep river water and lower in saturation and contrast, so it cannot read as a bright colored rim when stretched along a major river. Keep underwater gravel visible but subdued. No brown bank, no mud, no dark outline, no bright cyan, no white foam, no waves, no labels, no text, no frame. Production-ready game texture.
```

## Exact submerged-gravel prompt

```text
Edit this exact CityForge top-down orthographic submerged gravel texture into a separate wide/major-river shallow-bed variant. Preserve the exact 1536x1024 layout, uniform all-over pebble field, scale and density of stones, painterly realism, lighting direction, and seamless-looking edge continuity. Remove the luminous cyan, turquoise, royal-blue, tropical-blue wash. Recolor the water and underwater stones to restrained desaturated slate blue-gray with a very subtle natural green-gray undertone. Keep the gravel legible but subdued, flatter, and lower contrast. This texture will cover broad strips along very wide rivers, so it must not create a glowing blue border; it should sit quietly between pale dry gravel and deeper blue river water. No brown, no mud, no dark band, no cyan, no white foam, no waves, no shoreline or grass, no labels, no text, no frame. Production-ready game texture.
```
