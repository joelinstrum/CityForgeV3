#!/usr/bin/env python3
"""Create a review worksheet of likely illuminated windows from QA renders.

This is deliberately a discovery tool, not an automatic final-mask generator.
It finds compact warm/high-value regions, groups nearby panes, and records
numbered candidates. An artist or importer approves candidate IDs before a
Blender ray-projection stage creates shared emissive geometry.
"""

from __future__ import annotations

import argparse
import colorsys
import json
from collections import deque
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont


def candidate_mask(image: Image.Image) -> list[list[bool]]:
    rgba = image.convert("RGBA")
    pixels = rgba.load()
    mask = [[False] * rgba.width for _ in range(rgba.height)]
    for y in range(rgba.height):
        for x in range(rgba.width):
            red, green, blue, alpha = pixels[x, y]
            hue, saturation, value = colorsys.rgb_to_hsv(
                red / 255.0, green / 255.0, blue / 255.0)
            mask[y][x] = (alpha > 32 and 0.045 < hue < 0.17 and
                          saturation > 0.30 and value > 0.43 and
                          green > blue * 1.18)
    # Join mullion-separated highlights belonging to one window without
    # merging windows on adjacent bays or floors.
    expanded = [[False] * rgba.width for _ in range(rgba.height)]
    for y in range(rgba.height):
        for x in range(rgba.width):
            if not mask[y][x]:
                continue
            for yy in range(max(0, y - 3), min(rgba.height, y + 4)):
                for xx in range(max(0, x - 2), min(rgba.width, x + 3)):
                    expanded[yy][xx] = True
    return expanded


def components(mask: list[list[bool]]) -> list[tuple[int, int, int, int, int]]:
    height, width = len(mask), len(mask[0])
    seen = [[False] * width for _ in range(height)]
    found = []
    for y in range(height):
        for x in range(width):
            if not mask[y][x] or seen[y][x]:
                continue
            queue = deque([(x, y)])
            seen[y][x] = True
            low_x = high_x = x
            low_y = high_y = y
            area = 0
            while queue:
                px, py = queue.popleft()
                area += 1
                low_x, high_x = min(low_x, px), max(high_x, px)
                low_y, high_y = min(low_y, py), max(high_y, py)
                for nx, ny in ((px - 1, py), (px + 1, py),
                               (px, py - 1), (px, py + 1)):
                    if (0 <= nx < width and 0 <= ny < height and
                            mask[ny][nx] and not seen[ny][nx]):
                        seen[ny][nx] = True
                        queue.append((nx, ny))
            box_width = high_x - low_x + 1
            box_height = high_y - low_y + 1
            if (120 <= area <= 2400 and 8 <= box_width <= 42 and
                    18 <= box_height <= 90 and box_height >= box_width * 1.20):
                found.append((low_x, low_y, high_x + 1, high_y + 1, area))
    return sorted(found, key=lambda item: (item[1], item[0]))


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("images", nargs="+", type=Path)
    parser.add_argument("--output-dir", required=True, type=Path)
    parser.add_argument("--selection-rate", type=float, default=0.45)
    args = parser.parse_args()
    args.output_dir.mkdir(parents=True, exist_ok=True)
    records = []
    next_id = 1
    for path in args.images:
        image = Image.open(path).convert("RGBA")
        boxes = components(candidate_mask(image))
        annotated = image.convert("RGB")
        draw = ImageDraw.Draw(annotated)
        for box in boxes:
            candidate_id = f"W{next_id:03d}"
            x0, y0, x1, y1, area = box
            # Stable irregular selection: consistent between repeated runs,
            # intentionally avoiding the every-window checkerboard effect.
            selected = ((next_id * 37 + 11) % 100) < round(
                max(0.0, min(1.0, args.selection_rate)) * 100)
            color = (255, 196, 48) if selected else (55, 205, 255)
            draw.rectangle((x0, y0, x1, y1), outline=color, width=2)
            draw.rectangle((x0, max(0, y0 - 13), x0 + 34, y0), fill=(8, 12, 18))
            draw.text((x0 + 2, max(0, y0 - 12)), candidate_id,
                      fill=color, font=ImageFont.load_default())
            records.append({
                "id": candidate_id,
                "view": path.stem,
                "source": str(path.resolve()),
                "pixelBounds": [x0, y0, x1, y1],
                "selectedByDefault": selected,
                "status": "candidate",
                "detectionAreaPixels": area,
            })
            next_id += 1
        annotated.save(args.output_dir / f"{path.stem}-window-candidates.png")
    report = {
        "schema": "cityforge-window-lighting-candidates-v1",
        "method": "warm-region discovery; human approval required",
        "selectionRate": args.selection_rate,
        "candidateCount": len(records),
        "defaultSelectedCount": sum(1 for item in records
                                    if item["selectedByDefault"]),
        "candidates": records,
    }
    (args.output_dir / "window-lighting-candidates.json").write_text(
        json.dumps(report, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({key: report[key] for key in (
        "schema", "candidateCount", "defaultSelectedCount")}, indent=2))
    return 0 if records else 2


if __name__ == "__main__":
    raise SystemExit(main())
