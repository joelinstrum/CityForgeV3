"""Derive reusable garment UV masks from the two archived source textures.

These masks are only authoring inputs. The Blender bake projects them through
the original rigs into depth-correct directional atlas masks.
"""
import argparse
from pathlib import Path

import numpy as np
from PIL import Image


def ramp(value, low, high):
    return np.clip((value - low) / (high - low), 0, 1)


parser = argparse.ArgumentParser()
parser.add_argument('lady_texture', type=Path)
parser.add_argument('gentleman_texture', type=Path)
parser.add_argument('output_dir', type=Path)
args = parser.parse_args()
args.output_dir.mkdir(parents=True, exist_ok=True)

with Image.open(args.lady_texture) as image:
    rgb = np.asarray(image.convert('RGB'), dtype=np.float32) / 255
    red, green, blue = rgb.transpose(2, 0, 1)
    # Burgundy cloth is distinct from the cream lace, skin and brown hair.
    dress = ramp(red / (green + .03), 1.9, 2.5) * \
        ramp(red / (blue + .03), 1.8, 2.3) * \
        ramp(red, .12, .20)
    Image.fromarray(np.uint8(np.round(dress * 255)), 'L').save(
        args.output_dir / 'lady-dress-uv-mask.png')

with Image.open(args.gentleman_texture) as image:
    rgb = np.asarray(image.convert('RGB'), dtype=np.float32) / 255
    red, green, blue = rgb.transpose(2, 0, 1)
    brightness = np.max(rgb, axis=2)
    chroma = brightness - np.min(rgb, axis=2)
    # Charcoal fabric is dark and neutral. Exclude orange skin, brown hair,
    # white shirt and brass details. Shoes may share the fabric color.
    cloth = (1 - ramp(brightness, .25, .43)) * \
        (1 - ramp(chroma, .055, .13)) * \
        (1 - ramp(red - green, .035, .085))
    Image.fromarray(np.uint8(np.round(cloth * 255)), 'L').save(
        args.output_dir / 'gentleman-cloth-uv-mask.png')
