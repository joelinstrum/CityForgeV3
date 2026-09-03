"""Derive Mixed Use Brick LOD1-LOD4 and neutral eight-angle LOD5 art.

The supplied FBX remains the immutable LOD0. Every derivative retains its UVs
and imported material identity.
"""

import bpy
import math
from pathlib import Path
from mathutils import Vector


ROOT = Path(__file__).resolve().parents[1]
ASSET = ROOT / "Assets/CityForgeV3/Resources/CityForgeV3/Buildings3D/Evaluation/MixedUseBrick"
SOURCE = ASSET / "Source/tripo_convert_c4f7afa3-bb8e-499d-9766-3880c50e7e58.fbx"
RATIOS = (0.55, 0.30, 0.15, 0.07)


def scene_meshes():
    return [obj for obj in bpy.context.scene.objects if obj.type == "MESH"]


def bounds(objects):
    points = [obj.matrix_world @ Vector(corner)
              for obj in objects for corner in obj.bound_box]
    low = Vector((min(p.x for p in points), min(p.y for p in points),
                  min(p.z for p in points)))
    high = Vector((max(p.x for p in points), max(p.y for p in points),
                   max(p.z for p in points)))
    return low, high


bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=str(SOURCE))
source_meshes = scene_meshes()
if not source_meshes:
    raise RuntimeError("Mixed-use LOD0 contains no meshes")

for level, ratio in enumerate(RATIOS, start=1):
    bpy.ops.object.select_all(action="DESELECT")
    derived = []
    for original in source_meshes:
        duplicate = original.copy()
        duplicate.data = original.data.copy()
        duplicate.name = f"MixedUseBrick_Level{level}"
        bpy.context.collection.objects.link(duplicate)
        duplicate.select_set(True)
        bpy.context.view_layer.objects.active = duplicate
        modifier = duplicate.modifiers.new(
            name=f"CityForge LOD{level} UV-Preserving Decimation",
            type="DECIMATE")
        modifier.ratio = ratio
        modifier.use_collapse_triangulate = True
        bpy.ops.object.modifier_apply(modifier=modifier.name)
        derived.append(duplicate)
    target = ASSET / f"LOD{level}/MixedUseBrick_LOD{level}.fbx"
    bpy.ops.export_scene.fbx(
        filepath=str(target), use_selection=True, object_types={"MESH"},
        apply_unit_scale=True, bake_space_transform=False,
        add_leaf_bones=False, embed_textures=False,
        path_mode="RELATIVE", use_mesh_modifiers=True)
    for duplicate in derived:
        bpy.data.objects.remove(duplicate, do_unlink=True)

low, high = bounds(source_meshes)
center = (low + high) * 0.5
height = high.z - low.z
radius = max(high.x - low.x, high.y - low.y) * 3.0
canvas = max(height * 1.30, high.x - low.x, high.y - low.y) * 1.20

# Neutral, shadowless studio lighting keeps LOD5 compatible with runtime
# time-of-day tinting. Source FBX materials and texture UVs remain untouched.
bpy.context.scene.render.engine = "BLENDER_EEVEE"
bpy.context.scene.render.film_transparent = True
bpy.context.scene.render.resolution_x = 1024
bpy.context.scene.render.resolution_y = 1024
bpy.context.scene.render.resolution_percentage = 100
bpy.context.scene.render.image_settings.file_format = "PNG"
bpy.context.scene.view_settings.look = "AgX - Medium High Contrast"
bpy.context.scene.world = bpy.data.worlds.new("CityForge Neutral LOD World")
bpy.context.scene.world.color = (0.055, 0.055, 0.055)

bpy.ops.object.light_add(type="AREA", location=(center.x - 2.2,
                                                center.y - 2.5,
                                                high.z + 2.5))
key = bpy.context.object
key.data.energy = 650
key.data.size = 4.0
bpy.ops.object.light_add(type="AREA", location=(center.x + 2.0,
                                                center.y + 1.8,
                                                center.z + 1.2))
fill = bpy.context.object
fill.data.energy = 260
fill.data.size = 3.0

bpy.ops.object.camera_add()
camera = bpy.context.object
camera.data.type = "ORTHO"
camera.data.ortho_scale = canvas
bpy.context.scene.camera = camera
target = Vector((center.x, center.y, low.z + height * 0.48))
elevation = math.radians(22.0)

for index in range(8):
    degrees = index * 45
    azimuth = math.radians(degrees)
    horizontal = radius * math.cos(elevation)
    camera.location = target + Vector((
        math.sin(azimuth) * horizontal,
        -math.cos(azimuth) * horizontal,
        radius * math.sin(elevation),
    ))
    camera.rotation_euler = (target - camera.location).to_track_quat(
        "-Z", "Y").to_euler()
    bpy.context.scene.render.filepath = str(
        ASSET / f"LOD5/mixed-use-brick-angle-{index}-{degrees:03d}-v01.png")
    bpy.ops.render.render(write_still=True)

print("Generated Mixed Use Brick LOD1-LOD4 and eight LOD5 billboard views; LOD0 unchanged.")
