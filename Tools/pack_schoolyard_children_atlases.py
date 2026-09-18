"""Pack Blender's eight-facing frame strips into four 768 x 3072 atlases."""
from pathlib import Path
from PIL import Image
import sys

render_root = Path(sys.argv[1])
output_root = Path(sys.argv[2])
output_root.mkdir(parents=True, exist_ok=True)
names = ('18th-century-boy-1', '18th-century-boy-2',
         '18th-century-girl-1', '18th-century-girl-2')
for name in names:
    atlas = Image.new('RGBA', (768, 3072))
    for row in range(24):
        path = render_root / name / f'{row:02d}.png'
        strip = Image.open(path).convert('RGBA')
        assert strip.size == (768, 128), path
        atlas.paste(strip, (0, row * 128))
    atlas.save(output_root / f'{name}.png', optimize=True)
    print(name, 'atlas', atlas.size,
          'alpha', atlas.getchannel('A').getextrema())
