# River blue V01 prompts and lineage

Date: September 20, 2026

Tool: OpenAI built-in image generation, followed by documented lossless/mechanical
texture preparation with ImageMagick 7.

The user-supplied color and material reference was
`/Users/joelinstrum/Downloads/textures/river-blue/river-blue-concept.png`.
A canonical copy is stored beside this document as `river-blue-concept.png`.
The existing `Water/River/river-texture.png` and `white-cap-river.png` assets
remain intact; V01 is a new versioned runtime set.

| File | Selected tool output | Processing |
| --- | --- | --- |
| `river-texture.png` | `exec-0722b7a1-29ce-4263-a3de-f9e642c013fc.png` | Selected after the half-tile seam-healing edit described below. |
| `white-cap-river.png` | `exec-7849bd97-320b-43fb-a48b-a2951612c0de.png` | RGB normalized to constant pale ivory `#e7ece8`; generated alpha preserved exactly. |

The first crest attempt, `exec-555db9e9-e27a-4730-b54f-e2deaba7787a.png`,
was rejected because it was too dense and retained saturated cyan artifacts.

## Base-water generation prompt

```text
Use case: stylized-concept
Asset type: seamless looping RGB base-color texture for City Forge V3 animated river surfaces
Input image: the supplied river-blue concept is the color, material, scale, and look-and-feel reference; create a new production texture from it
Primary request: create a square, perfectly seamless top-down river-water texture with the same rich natural teal-blue body color, translucent green-blue undertones, fine ripples, and restrained pale glints as the reference. It must read as inland moving river water rather than open-ocean swell.
Style/medium: realistic painterly-photographic game texture matching the reference
Composition/framing: orthographic top-down, uniform full-bleed texture, evenly distributed fine directional ripples and small wavelets, no focal point; opposite edges must tile invisibly in both axes
Lighting/mood: neutral daylight encoded as subtle surface highlights only; no directional scene shadow
Color palette: preserve the reference's deep teal-blue and blue-green range; visibly bluer than gray water, natural rather than saturated cyan
Materials/textures: fine overlapping river ripples and small narrow crest glints; retain broad calm blue areas so a separate foam atlas can remain legible
Constraints: seamless in both X and Y; full square coverage; no alpha; no banks, rocks, plants, riverbed objects, shore, horizon, sky reflection scene, large ocean waves, breaking surf, broad white foam patches, text, watermark, frame, vignette, obvious repeated motifs, or edge discontinuities.
```

The initial selected base output was
`exec-889da2ce-38f1-435f-b79f-d15afc1d2992.png`. It was rolled exactly half
its width and height (`magick input.png -roll +627+627 offset.png`) so the
runtime tile boundary came from naturally adjacent interior pixels. The visible
center cross was then healed with this exact edit prompt:

```text
Use case: precise-object-edit
Asset type: seamless looping RGB base-color texture for City Forge V3 animated river surfaces
Input image: edit target; it is a half-tile-offset river texture whose original outer boundaries have been relocated to a visible vertical seam and horizontal seam through the exact center
Primary request: heal only the visible central vertical and horizontal seams so the ripples, teal-blue color, value, and fine wave highlights flow naturally across the center with no cross, line, tonal jump, or mirrored patch.
Constraints: preserve the square dimensions, RGB full-coverage format, exact rich teal-blue palette, top-down river-water appearance, fine ripple scale, overall highlight density, and all outer-edge pixels and outer-edge continuity. Do not crop, rotate, reframe, recolor, relight, add foam patches, add objects, add a horizon, or change the outer 15% border. The result must remain an inconspicuous seamless tile when repeated in both axes; no text or watermark.
```

## Crest generation prompt

```text
Use case: stylized-concept
Asset type: seamless transparent RGBA crest-and-foam atlas for City Forge V3 animated river surfaces
Input images: Image 1 is the required teal-blue water style and wave-scale reference. Image 2 is the existing transparent crest atlas and is only a density, spacing, and production-format reference; replace its colored artifacts and do not copy its exact marks.
Primary request: create a new square atlas of many scattered, slender river-wave crests and broken foam filaments that visually belong to Image 1. Use varied short arcs, narrow branching glints, intermittent lace-like whitewater, and a few medium elongated crest groups. The marks will move and pulse over a separately rendered water base.
Style/medium: realistic painterly-photographic river foam and specular crest detail
Composition/framing: genuinely transparent square canvas; evenly distributed isolated crest clusters with generous transparent negative space; texture must tile invisibly across opposite edges in both axes; mostly fine-to-medium marks, no dominant focal cluster
Lighting/mood: softly luminous daylight crests
Color palette: restrained warm ivory, pale silver-blue, and desaturated blue-white sampled from Image 1 highlights; absolutely no saturated cyan, electric blue, royal blue, or dark blue fill
Materials/textures: feathered translucent fringes, brighter narrow cores, clean natural foam breakup
Constraints: genuine RGBA transparency outside crest pixels; seamless in X and Y; no opaque or black background; no water-color base layer; no solid blue shadows beneath crests; no broad ocean breakers, surf, shoreline, rocks, banks, spray clouds, objects, text, watermark, frame, hard cutout edges, halos, obvious rows, or repeated stamp patterns.
```

## Crest-density correction prompt

```text
Use case: precise-object-edit
Asset type: transparent RGBA crest-and-foam atlas for City Forge V3 animated river surfaces
Input images: Image 1 is the edit target. Image 2 is the required water highlight style/color reference.
Primary request: radically reduce only the crest density in Image 1. Keep approximately 12–18 isolated slender crest clusters total, separated by broad genuinely transparent areas covering at least 75% of the canvas. Convert every surviving crest to restrained warm ivory, pale silver, or very desaturated blue-white matching Image 2's highlights.
Composition/framing: retain varied short arcs and a few medium broken branching filaments; distribute them irregularly without rows or one dominant group; keep a generous fully transparent margin around all four outer edges.
Constraints: preserve genuine RGBA transparency. Remove all saturated cyan, turquoise, green, electric blue, royal blue, dark blue fill, and gray water slabs. No opaque or black background, no water base layer, no broad foam blankets, no ocean breakers, no objects, no text, no watermark, no frame, no colored halo. Change only density, isolation, and crest coloration; retain fine feathered alpha edges.
```

The correction preserved good alpha silhouettes but left color contamination in
fully or partly transparent RGB. Runtime crest brightness uses RGB luminance
multiplied by alpha, so the RGB was normalized mechanically while retaining the
generated alpha channel byte-for-byte:

```text
magick -size 1254x1254 xc:'#e7ece8' ( selected.png -alpha extract ) \
  -alpha off -compose CopyOpacity -composite PNG32:white-cap-river.png
```
