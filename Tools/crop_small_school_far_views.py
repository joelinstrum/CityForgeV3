"""Trim the eight Blender renders while retaining their 50 px/m scale."""
from pathlib import Path
from PIL import Image

root = Path(__file__).resolve().parents[1]
views = root / "Assets/CityForgeV3/Resources/CityForgeV3/Buildings3D/SmallSchoolV01/FarViews"
for index in range(8):
    path = views / f"angle-{index}.png"
    with Image.open(path) as image:
        bounds = image.getchannel("A").getbbox()
        if bounds is None:
            raise RuntimeError(f"Empty schoolhouse view {index}")
        left, top, right, bottom = bounds
        bounds = (max(0, left - 4), max(0, top - 4),
                  min(image.width, right + 4), min(image.height, bottom + 4))
        image.crop(bounds).save(path)
