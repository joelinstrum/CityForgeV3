"""Pack rendered RGB garment masks with the same UVs as the strolling clip."""
import argparse
from pathlib import Path

from PIL import Image

parser = argparse.ArgumentParser()
parser.add_argument('render_dir', type=Path)
parser.add_argument('output_dir', type=Path)
args = parser.parse_args()
args.output_dir.mkdir(parents=True, exist_ok=True)
for facing in range(8):
    sheet = Image.new('RGB', (2048, 800))
    for frame in range(40):
        path = args.render_dir / f'direction-{facing}-frame-{frame:02d}.png'
        with Image.open(path) as image:
            if image.size != (256, 160):
                raise ValueError(f'Wrong frame size: {path}: {image.size}')
            sheet.paste(image.convert('RGB'),
                        ((frame % 8) * 256, (frame // 8) * 160))
    out = args.output_dir / f'garment-mask-facing-{facing}.png'
    sheet.save(out, optimize=True)
    print(out)
