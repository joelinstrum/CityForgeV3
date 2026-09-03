"""Build Hitchcock Mansion LOD1-LOD4 review meshes and eight LOD5 views.

The supplied FBX remains the immutable LOD0. Derivatives keep the same source
space, material slots, UVs, pivot, and export axis contract.
"""

import hashlib
import json
import math
from pathlib import Path

import bpy
from bpy_extras.object_utils import world_to_camera_view
from mathutils import Vector


ROOT = Path(__file__).resolve().parents[1]
ASSET = ROOT / "Assets/CityForgeV3/Resources/CityForgeV3/Buildings3D/HitchcockMansionProduction"
SOURCE = ASSET / "Source/tripo_convert_2d190be9-ee5d-4766-b295-7a6beeb3eb9d.fbx"
# This fused Tripo mesh develops catastrophic porch and mansard spikes below
# roughly 12K triangles. These budgets are the lowest clean review candidates.
TARGET_TRIANGLES = (40000, 20000, 16000, 12000)
TARGET_HEIGHT_METERS = 17.0


def scene_meshes():
    return [obj for obj in bpy.context.scene.objects if obj.type == "MESH"]


def triangles(objects):
    return sum(
        max(0, len(polygon.vertices) - 2)
        for obj in objects
        for polygon in obj.data.polygons
    )


def bounds(objects):
    points = [
        obj.matrix_world @ Vector(corner)
        for obj in objects
        for corner in obj.bound_box
    ]
    low = Vector(tuple(min(point[index] for point in points) for index in range(3)))
    high = Vector(tuple(max(point[index] for point in points) for index in range(3)))
    return low, high


def vec(value):
    return [round(float(component), 6) for component in value]


def sha256(path):
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(block)
    return digest.hexdigest()


def look_at(obj, target):
    obj.rotation_euler = (Vector(target) - obj.location).to_track_quat("-Z", "Y").to_euler()


def set_visible(objects, visible):
    for obj in objects:
        obj.hide_render = not visible
        obj.hide_viewport = not visible


def render(scene, path, size):
    scene.render.resolution_x = size
    scene.render.resolution_y = size
    scene.render.resolution_percentage = 100
    scene.render.filepath = str(path)
    bpy.ops.render.render(write_still=True)


bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.context.scene.unit_settings.system = "METRIC"
bpy.context.scene.unit_settings.length_unit = "METERS"
bpy.context.scene.unit_settings.scale_length = 1.0
bpy.ops.import_scene.fbx(filepath=str(SOURCE), use_image_search=True)
source_meshes = scene_meshes()
if not source_meshes:
    raise RuntimeError("Hitchcock Mansion LOD0 contains no mesh objects")

source_triangles = triangles(source_meshes)
source_low, source_high = bounds(source_meshes)
source_size = source_high - source_low
scale_to_meters = TARGET_HEIGHT_METERS / source_size.z

lod_records = [
    {
        "level": 0,
        "role": "immutable supplied source",
        "triangles": source_triangles,
        "reductionRatio": 1.0,
        "sourceSpaceBoundsDimensions": vec(source_size),
        "sourceSpaceBoundsCenter": vec((source_low + source_high) * 0.5),
        "runtimeScale": round(scale_to_meters, 9),
        "fbx": SOURCE.relative_to(ASSET).as_posix(),
    }
]

lod_mesh_sets = [source_meshes]
for level, target_triangles in enumerate(TARGET_TRIANGLES, start=1):
    ratio = min(0.92, max(0.01, target_triangles / source_triangles))
    bpy.ops.object.select_all(action="DESELECT")
    derived = []
    for original in source_meshes:
        duplicate = original.copy()
        duplicate.data = original.data.copy()
        duplicate.name = f"HitchcockMansion_LOD{level}"
        bpy.context.collection.objects.link(duplicate)
        duplicate.select_set(True)
        bpy.context.view_layer.objects.active = duplicate
        modifier = duplicate.modifiers.new(
            name=f"CityForge LOD{level} UV-Preserving Decimation",
            type="DECIMATE",
        )
        modifier.ratio = ratio
        modifier.use_collapse_triangulate = True
        bpy.ops.object.modifier_apply(modifier=modifier.name)
        derived.append(duplicate)
    target = ASSET / f"LOD{level}/HitchcockMansion_LOD{level}.fbx"
    bpy.ops.export_scene.fbx(
        filepath=str(target),
        use_selection=True,
        object_types={"MESH"},
        global_scale=1.0,
        apply_unit_scale=False,
        bake_space_transform=False,
        axis_forward="-Z",
        axis_up="Y",
        add_leaf_bones=False,
        embed_textures=False,
        path_mode="RELATIVE",
        use_mesh_modifiers=True,
    )
    low, high = bounds(derived)
    lod_records.append(
        {
            "level": level,
            "role": "UV-preserving review derivative",
            "triangles": triangles(derived),
            "targetTriangles": target_triangles,
            "reductionRatio": round(ratio, 6),
            "sourceSpaceBoundsDimensions": vec(high - low),
            "sourceSpaceBoundsCenter": vec((low + high) * 0.5),
            "runtimeScale": round(scale_to_meters, 9),
            "fbx": target.relative_to(ASSET).as_posix(),
        }
    )
    lod_mesh_sets.append(derived)
    set_visible(derived, False)

scene = bpy.context.scene
scene.render.engine = "BLENDER_EEVEE"
scene.render.film_transparent = True
scene.render.image_settings.file_format = "PNG"
scene.render.image_settings.color_mode = "RGBA"
scene.view_settings.look = "AgX - Medium High Contrast"
scene.view_settings.exposure = 0.65
world = bpy.data.worlds.new("CityForge Hitchcock Neutral World")
world.use_nodes = True
world.node_tree.nodes["Background"].inputs["Color"].default_value = (0.22, 0.24, 0.27, 1)
world.node_tree.nodes["Background"].inputs["Strength"].default_value = 0.82
scene.world = world

bpy.ops.object.light_add(type="AREA", location=(2.2, -2.5, 3.0))
key = bpy.context.object
key.data.energy = 950
key.data.size = 1.5
bpy.ops.object.light_add(type="AREA", location=(-2.0, 0.6, 1.8))
fill = bpy.context.object
fill.data.energy = 520
fill.data.size = 1.2

bpy.ops.object.camera_add()
camera = bpy.context.object
camera.data.type = "ORTHO"
scene.camera = camera
center = (source_low + source_high) * 0.5
radius = max(source_size) * 3.0
camera.data.ortho_scale = max(source_size) * 1.42
target = Vector((center.x, center.y, source_low.z + source_size.z * 0.46))
elevation = math.radians(22.0)

# One identical three-quarter frame per geometry level for transition review.
qa_azimuth = math.radians(315)
for meshes in lod_mesh_sets:
    set_visible(meshes, False)
for level, meshes in enumerate(lod_mesh_sets):
    set_visible(meshes, True)
    horizontal = radius * math.cos(elevation)
    camera.location = target + Vector(
        (math.sin(qa_azimuth) * horizontal,
         -math.cos(qa_azimuth) * horizontal,
         radius * math.sin(elevation))
    )
    look_at(camera, target)
    render(scene, ASSET / f"QA/lod{level}-front-three-quarter.png", 800)
    set_visible(meshes, False)

# LOD5 uses LOD2 to preserve fine silhouette details in its baked imagery.
set_visible(lod_mesh_sets[2], True)
billboards = []
for index in range(8):
    degrees = index * 45
    azimuth = math.radians(degrees)
    horizontal = radius * math.cos(elevation)
    camera.location = target + Vector(
        (math.sin(azimuth) * horizontal,
         -math.cos(azimuth) * horizontal,
         radius * math.sin(elevation))
    )
    look_at(camera, target)
    path = ASSET / f"LOD5/hitchcock-mansion-angle-{index}-{degrees:03d}-v01.png"
    render(scene, path, 1024)
    pivot = world_to_camera_view(scene, camera, Vector((center.x, center.y, source_low.z)))
    billboards.append(
        {
            "index": index,
            "yawDegrees": degrees,
            "file": path.relative_to(ASSET).as_posix(),
            "pivotTopOrigin": [round(pivot.x, 9), round(1 - pivot.y, 9)],
        }
    )

manifest = {
    "schema": "cityforge-building-lod-package-v2",
    "assetId": "hitchcock-mansion-v01",
    "source": SOURCE.relative_to(ROOT).as_posix(),
    "sourceSha256": sha256(SOURCE),
    "sourceArchivePreserved": True,
    "canonicalSourceModified": False,
    "coordinateSystem": "source-space-with-uniform-runtime-meter-scale",
    "originPolicy": "source-pivot-with-package-centering-offset",
    "targetHeightMeters": TARGET_HEIGHT_METERS,
    "runtimeScale": round(scale_to_meters, 9),
    "lods": lod_records,
    "billboardLevel": 5,
    "billboards": billboards,
    "qa": {
        "status": "generated-review-candidates",
        "cameraElevationDegrees": 22,
        "transparentBackground": True,
        "bakedGroundShadow": False,
        "geometryComparisonFrames": [
            f"QA/lod{level}-front-three-quarter.png" for level in range(5)
        ],
    },
}
(ASSET / "Source/manifest.json").write_text(json.dumps(manifest, indent=2) + "\n")
print(json.dumps(manifest, indent=2))
