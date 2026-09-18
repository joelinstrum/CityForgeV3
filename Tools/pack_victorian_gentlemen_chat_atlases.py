"""Pack the baked paired gentleman frames for the shared AutomataClipPlayer."""
import argparse
from pathlib import Path

from PIL import Image


parser = argparse.ArgumentParser()
parser.add_argument('render_dir', type=Path)
parser.add_argument('output_dir', type=Path)
args = parser.parse_args()
args.output_dir.mkdir(parents=True, exist_ok=True)
clip_id = 'victorian-gentlemen-chatting-v01'
for facing in range(8):
    sheet = Image.new('RGBA', (224 * 8, 128 * 2))
    for frame in range(16):
        path = args.render_dir / f'direction-{facing}-frame-{frame:02d}.png'
        with Image.open(path) as image:
            if image.size != (224, 128):
                raise ValueError(f'Wrong frame size: {path}: {image.size}')
            sheet.paste(image.convert('RGBA'),
                        ((frame % 8) * 224, (frame // 8) * 128))
    out = args.output_dir / f'{clip_id}-facing-{facing}.png'
    sheet.save(out, optimize=True)
    print(out)
with Image.open(args.render_dir / 'direction-0-frame-00.png') as image:
    image.save(args.output_dir / 'thumbnail.png', optimize=True)
