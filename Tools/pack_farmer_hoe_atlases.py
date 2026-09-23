"""Pack the contained farmer renders into shared eight-facing clip atlases."""
import argparse
from pathlib import Path
from PIL import Image

parser = argparse.ArgumentParser()
parser.add_argument('render_dir', type=Path)
parser.add_argument('output_dir', type=Path)
args = parser.parse_args()
args.output_dir.mkdir(parents=True, exist_ok=True)
for facing in range(8):
    sheet = Image.new('RGBA', (192 * 8, 192 * 4))
    for frame in range(32):
        path = args.render_dir / f'direction-{facing}-frame-{frame:02d}.png'
        with Image.open(path) as image:
            if image.size != (192, 192):
                raise ValueError(f'Wrong frame size: {path}: {image.size}')
            sheet.paste(image.convert('RGBA'),
                        ((frame % 8) * 192, (frame // 8) * 192))
    sheet.save(args.output_dir /
               f'founders-farmer-hoeing-v01-facing-{facing}.png', optimize=True)
with Image.open(args.render_dir / 'direction-0-frame-00.png') as image:
    image.save(args.output_dir / 'thumbnail.png', optimize=True)
