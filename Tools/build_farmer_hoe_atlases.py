"""Bake the canonical farmer's hoe/walk actions into a contained lot automata.

Run: Blender -b --python Tools/build_farmer_hoe_atlases.py -- FBX TEXTURE OUTPUT
Set FARMER_PREVIEW=1 for the first frame only.
"""
import math
import os
import sys

import bpy
from mathutils import Vector


fbx_path, texture_path, output_dir = sys.argv[sys.argv.index('--') + 1:]
os.makedirs(output_dir, exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=fbx_path)
rig = next(obj for obj in bpy.context.scene.objects if obj.type == 'ARMATURE')
mesh = next(obj for obj in bpy.context.scene.objects if obj.type == 'MESH')
actions = {action.name.rsplit('_', 1)[-1].lower(): action
           for action in bpy.data.actions}
hoe, walk = actions['hoe'], actions['walk']
rig.animation_data_create()

material = bpy.data.materials.new('Canonical farmer base color')
material.use_nodes = True
material.node_tree.nodes.get('Principled BSDF').inputs['Roughness'].default_value = .85
texture = material.node_tree.nodes.new('ShaderNodeTexImage')
texture.image = bpy.data.images.load(texture_path)
material.node_tree.links.new(texture.outputs['Color'],
    material.node_tree.nodes.get('Principled BSDF').inputs['Base Color'])
mesh.data.materials.clear()
mesh.data.materials.append(material)

# One immutable 3 x 3 m scene. The two stations and the whole body stay well
# inside the tile; motion is baked into frames, not driven per district tick.
scale = 1.72 / mesh.dimensions.z
pivot = bpy.data.objects.new('Farmer footprint', None)
bpy.context.collection.objects.link(pivot)
pivot.scale = (scale,) * 3
rig.parent = pivot
mesh.parent = pivot

camera_data = bpy.data.cameras.new('Shared automata camera')
camera = bpy.data.objects.new('Shared automata camera', camera_data)
bpy.context.collection.objects.link(camera)
camera.location = (0, -10, 5.2)
camera.rotation_euler = (Vector((0, 0, .75)) - camera.location).to_track_quat('-Z', 'Y').to_euler()
camera_data.type = 'ORTHO'
camera_data.ortho_scale = 3.0
bpy.context.scene.camera = camera
for location, energy, size in (((-4, -5, 7), 1000, 5), ((3, 3, 6), 650, 6)):
    data = bpy.data.lights.new('Softbox', 'AREA')
    obj = bpy.data.objects.new('Softbox', data)
    bpy.context.collection.objects.link(obj)
    obj.location = location
    data.energy = energy
    data.shape = 'DISK'
    data.size = size

scene = bpy.context.scene
scene.world = bpy.data.worlds.new('Neutral ambient')
scene.world.color = (.65, .65, .65)
scene.render.engine = 'BLENDER_EEVEE'
scene.render.resolution_x = 192
scene.render.resolution_y = 192
scene.render.resolution_percentage = 100
scene.render.film_transparent = True
scene.render.image_settings.file_format = 'PNG'
scene.render.image_settings.color_mode = 'RGBA'
scene.render.image_settings.compression = 15
scene.view_settings.view_transform = 'Standard'
scene.view_settings.look = 'Medium High Contrast'

for direction in range(1 if os.environ.get('FARMER_PREVIEW') else 8):
    yaw = -direction * math.tau / 8
    for frame in range(1 if os.environ.get('FARMER_PREVIEW') else 32):
        segment = frame % 16
        at_second_station = frame >= 16
        if segment < 12:
            action = hoe
            source_frame = 1 + segment * 72 / 12
            x, y = ((.43, .32) if at_second_station else (-.43, -.32))
            heading = math.radians(25 if at_second_station else -20)
        else:
            action = walk
            step = (segment - 12) / 4
            if at_second_station:
                step = 1 - step
            x = -.43 + .86 * step
            y = -.32 + .64 * step
            source_frame = 1 + (segment - 12) * 30 / 4
            heading = math.radians(-25 if at_second_station else 155)
        rig.animation_data.action = action
        rig.animation_data.action_slot = action.slots[0]
        scene.frame_set(1)
        action_frame = source_frame
        # Render the sampled source action without keying scene/object motion.
        rig.animation_data.action_extrapolation = 'HOLD'
        scene.frame_set(round(action_frame))
        pivot.location = (x * math.cos(yaw) - y * math.sin(yaw),
                          x * math.sin(yaw) + y * math.cos(yaw), 0)
        pivot.rotation_euler.z = heading + yaw
        scene.render.filepath = os.path.join(output_dir,
            f'direction-{direction}-frame-{frame:02d}.png')
        bpy.ops.render.render(write_still=True)
print('Baked farmer hoe automata:', output_dir)
