"""Trim the two houses' directional renders without changing 50 px/m scale."""
from pathlib import Path
from PIL import Image


root = Path(__file__).resolve().parents[1]
buildings = root / 'Assets/CityForgeV3/Resources/CityForgeV3/Buildings3D'
for name in ('ColonialHouseA', 'ColonialHouseB'):
    folder = buildings / name / 'FarViews'
    for index in range(8):
        path = folder / f'angle-{index}.png'
        with Image.open(path) as image:
            bounds = image.convert('RGBA').getchannel('A').getbbox()
            if bounds is None:
                raise RuntimeError(f'Empty far view: {path}')
            left, top, right, bottom = bounds
            crop = (max(0, left - 4), max(0, top - 4),
                    min(image.width, right + 4),
                    min(image.height, bottom + 4))
            image.crop(crop).save(path, optimize=True)
    # Direction 7 keeps each front door and one side visible in the library.
    source = folder / 'angle-7.png'
    (buildings / name / 'thumbnail.png').write_bytes(source.read_bytes())
