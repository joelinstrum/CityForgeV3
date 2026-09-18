"""Bake one repeatable five-child scene into eight directional flipbook atlases.

Inputs are the four intermediate character atlases produced by the Blender
and packing scripts. No actor movement or pose selection runs in Unity.
"""
from pathlib import Path
from PIL import Image, ImageDraw
import argparse
import math

FRAME_W, FRAME_H = 256, 192
OUTPUT_CROP = (16, 64, 240, 192)
OUTPUT_W, OUTPUT_H = 224, 128
CELL_W, CELL_H = 96, 128
FRAME_COUNT, FACING_COUNT, FPS = 32, 8, 8
PIXELS_PER_METER = 32
CHARACTER_SCALE = 1.5 * PIXELS_PER_METER / 98.46
BASE_POINTS = ((-2.6, -1.4), (-2.1, 1.7), (0.7, 2.7),
               (2.7, 0.6), (1.2, -2.4))
EXCURSIONS = ((1.05, 0.45), (0.55, -0.85), (-0.85, -0.45),
              (-0.8, 0.4), (0.4, 0.9))
PHASES = (0, 6, 13, 20, 26)
NAMES = ('18th-century-boy-1', '18th-century-boy-2',
         '18th-century-girl-1', '18th-century-girl-2',
         '18th-century-boy-1')


def actor_state(actor, frame):
    phase = (frame + PHASES[actor]) % FRAME_COUNT
    bx, bz = BASE_POINTS[actor]
    dx, dz = EXCURSIONS[actor]
    if phase < 12:
        amount, action, heading = phase / 12, 1, (dx, dz)
    elif phase < 18:
        amount, action, heading = 1, 2, (-bx - dx, -bz - dz)
    elif phase < 20:
        amount, action, heading = 1, 0, (-bx - dx, -bz - dz)
    else:
        amount, action, heading = (32 - phase) / 12, 1, (-dx, -dz)
    return (bx + dx * amount, bz + dz * amount), action, heading, phase % 8


def rotate(x, z, angle):
    c, s = math.cos(angle), math.sin(angle)
    return x * c - z * s, x * s + z * c


def paste_actor(canvas, atlas, actor, frame, direction, position, action,
                heading, animation_frame):
    x, z = position
    hx, hz = heading
    angle = -direction * math.tau / FACING_COUNT
    x, z = rotate(x, z, angle)
    hx, hz = rotate(hx, hz, angle)
    facing = round(math.atan2(hx, -hz) / (math.tau / FACING_COUNT)) % FACING_COUNT
    row = action * 8 + animation_frame
    part = atlas.crop((facing * CELL_W, row * CELL_H,
                       (facing + 1) * CELL_W, (row + 1) * CELL_H))
    size = (round(CELL_W * CHARACTER_SCALE),
            round(CELL_H * CHARACTER_SCALE))
    part = part.resize(size, Image.Resampling.LANCZOS)
    foot_x = FRAME_W / 2 + x * PIXELS_PER_METER
    foot_y = 143 - z * 10.2
    left = round(foot_x - size[0] * .5)
    top = round(foot_y - size[1] * .86)
    canvas.alpha_composite(part, (left, top))


def render_frame(atlases, direction, frame):
    canvas = Image.new('RGBA', (FRAME_W, FRAME_H))
    states = []
    angle = -direction * math.tau / FACING_COUNT
    for actor in range(5):
        position, action, heading, animation_frame = actor_state(actor, frame)
        _, depth = rotate(*position, angle)
        states.append((depth, actor, position, action, heading, animation_frame))
    shadow = Image.new('RGBA', canvas.size)
    draw = ImageDraw.Draw(shadow)
    for _, _, position, _, _, _ in states:
        x, z = rotate(*position, angle)
        sx, sy = FRAME_W / 2 + x * PIXELS_PER_METER, 143 - z * 10.2
        draw.ellipse((sx - 11, sy - 3, sx + 11, sy + 3),
                     fill=(18, 14, 10, 70))
    canvas.alpha_composite(shadow)
    for _, actor, position, action, heading, animation_frame in sorted(
            states, reverse=True):
        paste_actor(canvas, atlases[NAMES[actor]], actor, frame, direction,
                    position, action, heading, animation_frame)
    return canvas.crop(OUTPUT_CROP)


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('intermediate_atlas_dir', type=Path)
    parser.add_argument('output_dir', type=Path)
    args = parser.parse_args()
    args.output_dir.mkdir(parents=True, exist_ok=True)
    atlases = {name: Image.open(args.intermediate_atlas_dir /
                                f'{name}.png').convert('RGBA')
               for name in set(NAMES)}
    for direction in range(FACING_COUNT):
        sheet = Image.new('RGBA', (OUTPUT_W * 8, OUTPUT_H * 4))
        for frame in range(FRAME_COUNT):
            image = render_frame(atlases, direction, frame)
            sheet.paste(image, ((frame % 8) * OUTPUT_W,
                                (frame // 8) * OUTPUT_H))
        path = args.output_dir / f'group-18th-century-children-v01-facing-{direction}.png'
        sheet.save(path, optimize=True)
        print(path.name, sheet.size)
    render_frame(atlases, 0, 0).save(args.output_dir / 'thumbnail.png')


if __name__ == '__main__':
    main()
