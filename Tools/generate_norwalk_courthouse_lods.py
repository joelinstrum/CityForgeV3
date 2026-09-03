"""Generate derivative Norwalk courthouse meshes without modifying LOD0."""

import bpy
import os
import sys


def argument(name):
    args = sys.argv[sys.argv.index("--") + 1:]
    return args[args.index(name) + 1]


source = argument("--source")
destination = argument("--destination")
ratios = (0.50, 0.25, 0.10, 0.04)

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=source)
mesh_objects = [obj for obj in bpy.context.scene.objects if obj.type == "MESH"]
if not mesh_objects:
    raise RuntimeError("Norwalk source contains no mesh objects")

for level, ratio in enumerate(ratios, start=1):
    bpy.ops.object.select_all(action="DESELECT")
    duplicates = []
    for source_object in mesh_objects:
        duplicate = source_object.copy()
        duplicate.data = source_object.data.copy()
        bpy.context.collection.objects.link(duplicate)
        duplicate.select_set(True)
        duplicates.append(duplicate)
        bpy.context.view_layer.objects.active = duplicate
        modifier = duplicate.modifiers.new(
            name=f"CityForge LOD{level} Decimation", type="DECIMATE")
        modifier.ratio = ratio
        modifier.use_collapse_triangulate = True
        bpy.ops.object.modifier_apply(modifier=modifier.name)

    level_directory = os.path.join(destination, f"LOD{level}")
    os.makedirs(level_directory, exist_ok=True)
    bpy.ops.export_scene.fbx(
        filepath=os.path.join(level_directory,
                              f"NorwalkCourthouse_LOD{level}.fbx"),
        use_selection=True,
        object_types={"MESH"},
        apply_unit_scale=True,
        bake_space_transform=False,
        add_leaf_bones=False,
        path_mode="RELATIVE",
        embed_textures=False,
        use_mesh_modifiers=True,
    )
    for duplicate in duplicates:
        bpy.data.objects.remove(duplicate, do_unlink=True)

print("Generated Norwalk LOD1-LOD4; source LOD0 was not modified.")
