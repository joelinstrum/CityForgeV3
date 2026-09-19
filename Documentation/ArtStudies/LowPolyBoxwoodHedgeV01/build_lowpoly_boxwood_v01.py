"""Create a 3 x 1 x 1 m low-poly clipped boxwood prototype in Blender 5.

Run with:
  Blender -b --factory-startup --python build_lowpoly_boxwood_v01.py

The source foliage image and this script are versioned independently from the
approved GeorgianClippedHedgesV01 asset. Only the selected hedge is exported.
"""

from pathlib import Path
import math
import random

import bpy
from mathutils import Vector


SOURCE = Path(__file__).resolve().parent
REPO = SOURCE.parents[2]
RUNTIME = (REPO / "Assets/CityForgeV3/Resources/CityForgeV3/Garden/"
           "LowPolyBoxwoodHedgeV01")
VALIDATION = REPO / "Documentation/Validation/low-poly-boxwood-hedge-v01"
RUNTIME.mkdir(parents=True, exist_ok=True)
VALIDATION.mkdir(parents=True, exist_ok=True)

bpy.ops.object.select_all(action="SELECT")
bpy.ops.object.delete(use_global=False)

vertices = []
faces = []
face_uvs = []
lookup = {}


def vertex(point):
    key = tuple(round(v, 6) for v in point)
    if key not in lookup:
        lookup[key] = len(vertices)
        vertices.append(point)
    return lookup[key]


def surface(rows, columns, point_at, outward, uv_at):
    grid = [[vertex(point_at(r / rows, c / columns))
             for c in range(columns + 1)] for r in range(rows + 1)]
    for r in range(rows):
        for c in range(columns):
            points = [grid[r][c], grid[r + 1][c],
                      grid[r + 1][c + 1], grid[r][c + 1]]
            a, b, d = (Vector(vertices[i]) for i in points[:3])
            if (b - a).cross(d - a).dot(Vector(outward)) < 0:
                points.reverse()
            faces.append(points)
            face_uvs.append([uv_at(vertices[i]) for i in points])


x_at = lambda t: -1.5 + 3.0 * t
y_at = lambda t: -0.5 + t
z_at = lambda t: t
scale = 2.9  # metres per foliage image; leaf clumps survive the Lot camera
surface(12, 4, lambda a, b: (x_at(a), y_at(b), 1.0), (0, 0, 1),
        lambda p: ((p[0] + 1.5) / scale, (p[1] + .5) / scale))
surface(12, 4, lambda a, b: (x_at(a), y_at(b), 0.0), (0, 0, -1),
        lambda p: ((p[0] + 1.5) / scale, (p[1] + .5) / scale))
for side in (-1, 1):
    surface(12, 4,
            lambda a, b, side=side: (x_at(a), side * .5, z_at(b)),
            (0, side, 0),
            lambda p: ((p[0] + 1.5) / scale, p[2] / scale))
    surface(4, 4,
            lambda a, b, side=side: (side * 1.5, y_at(a), z_at(b)),
            (side, 0, 0),
            lambda p: ((p[1] + .5) / scale, p[2] / scale))

mesh = bpy.data.meshes.new("Low-poly boxwood 3x1x1 mesh")
mesh.from_pydata(vertices, [], faces)
mesh.update()
uv_layer = mesh.uv_layers.new(name="Boxwood UV")
for polygon, uvs in zip(mesh.polygons, face_uvs):
    for loop_index, uv in zip(polygon.loop_indices, uvs):
        uv_layer.data[loop_index].uv = uv

hedge = bpy.data.objects.new("CF_LowPolyBoxwoodHedge_3x1_v01", mesh)
bpy.context.collection.objects.link(hedge)
bpy.context.view_layer.objects.active = hedge
hedge.select_set(True)

bevel = hedge.modifiers.new("Soft clipped corners", "BEVEL")
bevel.width = .095
bevel.segments = 1
bevel.limit_method = "ANGLE"
bevel.angle_limit = math.radians(28)
bpy.ops.object.modifier_apply(modifier=bevel.name)

random.seed(9182026)
mesh = hedge.data
mesh.update()
for point in mesh.vertices:
    x, y, z = point.co
    if z < .08:
        continue  # preserve a flat ground anchor
    irregularity = (0.013 * math.sin(7.1 * x + 9.2 * y + 2.3 * z)
                    + 0.009 * math.sin(13.7 * x - 6.4 * y + 5.1 * z)
                    + 0.005 * random.uniform(-1, 1))
    strength = min(1.0, (z - .08) / .27)
    point.co += point.normal * irregularity * strength
    if z > .77:
        point.co.z += (.010 * math.sin(3.6 * x + 2.2 * y)
                       + .005 * math.sin(8.5 * x - 4.1 * y))

# Keep the authored footprint exact despite the subtle surface displacement.
minimum = [min(v.co[i] for v in mesh.vertices) for i in range(3)]
maximum = [max(v.co[i] for v in mesh.vertices) for i in range(3)]
for point in mesh.vertices:
    point.co.x = (point.co.x - (minimum[0] + maximum[0]) * .5) * 3.0 / (
        maximum[0] - minimum[0])
    point.co.y = (point.co.y - (minimum[1] + maximum[1]) * .5) / (
        maximum[1] - minimum[1])
    point.co.z = (point.co.z - minimum[2]) / (maximum[2] - minimum[2])
for polygon in mesh.polygons:
    polygon.use_smooth = True
mesh.update()

# Blender's bevel modifier interpolates the source UVs through its narrow
# corner faces. FBX/Unity then exposes those stretched slivers as bright
# foliage stripes. Reproject the finished mesh face by face at one scale.
uv_layer = mesh.uv_layers.active
for polygon in mesh.polygons:
    normal = polygon.normal
    for loop_index in polygon.loop_indices:
        point = mesh.vertices[mesh.loops[loop_index].vertex_index].co
        if abs(normal.z) >= max(abs(normal.x), abs(normal.y)):
            uv = ((point.x + 1.5) / scale, (point.y + .5) / scale)
        elif abs(normal.y) >= abs(normal.x):
            uv = ((point.x + 1.5) / scale, point.z / scale)
        else:
            uv = ((point.y + .5) / scale, point.z / scale)
        uv_layer.data[loop_index].uv = uv

image = bpy.data.images.load(str(SOURCE / "boxwood-foliage-v01.png"))
image.pack()
material = bpy.data.materials.new("CF dark boxwood foliage v01")
material.use_nodes = True
nodes = material.node_tree.nodes
principled = nodes.get("Principled BSDF")
principled.inputs["Roughness"].default_value = .88
texture = nodes.new("ShaderNodeTexImage")
texture.name = "Versioned boxwood foliage"
texture.image = image
texture.extension = "REPEAT"
material.node_tree.links.new(texture.outputs["Color"],
                             principled.inputs["Base Color"])
hedge.data.materials.append(material)

# Save a local prototype scene before adding render-only comparison art.
bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE / "CF_LowPolyBoxwoodHedge_3x1_v01.blend"))
bpy.ops.object.select_all(action="DESELECT")
hedge.select_set(True)
bpy.context.view_layer.objects.active = hedge
bpy.ops.export_scene.fbx(
    filepath=str(RUNTIME / "CF_LowPolyBoxwoodHedge_3x1_v01.fbx"),
    use_selection=True, object_types={"MESH"},
    use_mesh_modifiers=False, bake_space_transform=False,
    axis_forward="-Z", axis_up="Y", add_leaf_bones=False,
    path_mode="AUTO")

bpy.ops.mesh.primitive_plane_add(size=200, location=(0, 0, -.035))
floor = bpy.context.object
floor.name = "Render-only warm gray ground"
floor_mat = bpy.data.materials.new("Render ground")
floor_mat.diffuse_color = (.28, .26, .23, 1)
floor.data.materials.append(floor_mat)

world = bpy.context.scene.world
world.color = (.12, .16, .18)
world.use_nodes = True
world.node_tree.nodes["Background"].inputs["Color"].default_value = (.17, .22, .24, 1)
world.node_tree.nodes["Background"].inputs["Strength"].default_value = .5

light_data = bpy.data.lights.new("Broad garden light", "AREA")
light = bpy.data.objects.new("Broad garden light", light_data)
bpy.context.collection.objects.link(light)
light.location = (-3.0, -4.0, 5.0)
light_data.energy = 600
light_data.shape = "DISK"
light_data.size = 6.0

camera_data = bpy.data.cameras.new("Garden camera")
camera = bpy.data.objects.new("Garden camera", camera_data)
bpy.context.collection.objects.link(camera)
camera.location = (4.5, -6.5, 5.1)
target = Vector((0, 0, .50))
camera.rotation_euler = (target - camera.location).to_track_quat("-Z", "Y").to_euler()
camera_data.type = "ORTHO"
camera_data.ortho_scale = 5.4
bpy.context.scene.camera = camera
bpy.context.scene.render.engine = "CYCLES"
bpy.context.scene.cycles.samples = 24
bpy.context.scene.render.resolution_x = 1300
bpy.context.scene.render.resolution_y = 850
bpy.context.scene.render.resolution_percentage = 100
bpy.context.scene.render.image_settings.file_format = "PNG"
bpy.context.scene.render.filepath = str(VALIDATION / "blender-study.png")
bpy.ops.render.render(write_still=True)

triangles = sum(len(face.vertices) - 2 for face in mesh.polygons)
(VALIDATION / "mesh-report.txt").write_text(
    f"Blender low-poly boxwood prototype\n"
    f"Dimensions: 3.00 x 1.00 x 1.00 m (X length, Y depth, Z height)\n"
    f"Mesh vertices: {len(mesh.vertices)}\n"
    f"Mesh polygons: {len(mesh.polygons)}\n"
    f"Equivalent triangles: {triangles}\n"
    f"Foliage image: versioned boxwood-foliage-v01.png; exact runtime copy\n"
    f"FBX and Blend are separate new V01 assets; Georgian V01 untouched\n")
print((VALIDATION / "mesh-report.txt").read_text())
