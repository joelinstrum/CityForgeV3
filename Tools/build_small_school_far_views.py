"""Render source-derived schoolhouse billboards for the two distant district zooms."""
import math
import zipfile
from pathlib import Path

import bpy
from mathutils import Vector


ROOT = Path(__file__).resolve().parents[1]
source_dir = ROOT / "Authoring/Buildings/SmallSchoolV01/Source"
if not list(source_dir.glob("*.fbx")):
    source_dir.mkdir(parents=True, exist_ok=True)
    with zipfile.ZipFile(ROOT / "Documentation/Migration/SourceArchives/SmallSchoolV01/small-school.zip") as archive:
        archive.extractall(source_dir)
SOURCE = next(source_dir.glob("*.fbx"))
OUT = ROOT / "Assets/CityForgeV3/Resources/CityForgeV3/Buildings3D/SmallSchoolV01/FarViews"
OUT.mkdir(parents=True, exist_ok=True)

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=str(SOURCE))
model = next(obj for obj in bpy.context.scene.objects if obj.type == "MESH")
corners = [model.matrix_world @ Vector(corner) for corner in model.bound_box]
low = Vector(tuple(min(point[axis] for point in corners) for axis in range(3)))
high = Vector(tuple(max(point[axis] for point in corners) for axis in range(3)))
factor = 11.0 / (high.z - low.z)
center = Vector(((low.x + high.x) / 2, (low.y + high.y) / 2, low.z))
for vertex in model.data.vertices:
    vertex.co = (model.matrix_world @ vertex.co - center) * factor
model.matrix_world.identity()

scene = bpy.context.scene
scene.render.engine = "BLENDER_EEVEE"
scene.render.resolution_x = scene.render.resolution_y = 1024
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = "PNG"
scene.render.image_settings.color_mode = "RGBA"
scene.render.film_transparent = True
scene.world = bpy.data.worlds.new("Neutral ambient")
scene.world.use_nodes = True
scene.world.node_tree.nodes["Background"].inputs["Color"].default_value = (.78, .82, .88, 1)
scene.world.node_tree.nodes["Background"].inputs["Strength"].default_value = .8
scene.view_settings.view_transform = "AgX"
sun_data = bpy.data.lights.new("Neutral sun", "SUN")
sun_data.energy = 2.2
sun = bpy.data.objects.new("Neutral sun", sun_data)
scene.collection.objects.link(sun)
sun.rotation_euler = (math.radians(35), math.radians(-30), math.radians(-35))
camera_data = bpy.data.cameras.new("Shared distant camera")
camera = bpy.data.objects.new("Shared distant camera", camera_data)
scene.collection.objects.link(camera)
camera_data.type = "ORTHO"
camera_data.ortho_scale = 20.48
scene.camera = camera
target = Vector((0, 0, 4.3))
for index in range(8):
    azimuth = math.radians(index * 45)
    camera.location = target + Vector((
        math.cos(azimuth) * math.cos(math.radians(28)),
        math.sin(azimuth) * math.cos(math.radians(28)),
        math.sin(math.radians(28)),
    )) * 25
    camera.rotation_euler = (target - camera.location).to_track_quat("-Z", "Y").to_euler()
    scene.render.filepath = str(OUT / f"angle-{index}.png")
    bpy.ops.render.render(write_still=True)
