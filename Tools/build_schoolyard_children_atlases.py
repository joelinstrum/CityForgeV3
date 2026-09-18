"""Run with Blender -b --python; bake four source FBXs into directional automata strips.

Source ZIPs stay canonical. This script reads extracted copies in the temp build root.
Each atlas is eight directions wide and 24 frames tall: idle, walk, clap.
"""
import bpy
import glob
import math
import os
import sys
from mathutils import Vector

args = sys.argv[sys.argv.index('--') + 1:]
key, build_root, output_root = args
bpy.ops.wm.read_factory_settings(use_empty=True)

def import_child(name):
    before = set(bpy.data.objects)
    path = glob.glob(os.path.join(build_root, name, '*.fbx'))[0]
    bpy.ops.import_scene.fbx(filepath=path)
    added = set(bpy.data.objects) - before
    arm = next(obj for obj in added if obj.type == 'ARMATURE')
    mesh = next(obj for obj in added if obj.type == 'MESH')
    actions = {label: action for action in bpy.data.actions for label in ('idle', 'walk', 'clap')
               if action.name.lower().endswith('|' + label)}
    return arm, mesh, actions

source = {'18th-century-boy-2': '18th-century-boy-1',
          '18th-century-girl-1': '18th-century-girl-2'}.get(key, key)
if source != key:
    donor_arm, donor_mesh, actions = import_child(source)
    donor_arm.hide_render = True
    donor_mesh.hide_render = True
    arm, mesh, _ = import_child(key)
else:
    arm, mesh, actions = import_child(key)
assert all(name in actions for name in ('idle', 'walk', 'clap'))
arm.hide_render = True
mesh.hide_render = True

for direction in range(8):
    bpy.ops.object.select_all(action='DESELECT')
    arm.select_set(True)
    mesh.select_set(True)
    bpy.context.view_layer.objects.active = arm
    bpy.ops.object.duplicate(linked=False)
    copies = list(bpy.context.selected_objects)
    rig = next(obj for obj in copies if obj.type == 'ARMATURE')
    child = next(obj for obj in copies if obj.type == 'MESH')
    rig.name = key + '-rig-' + str(direction)
    child.name = key + '-mesh-' + str(direction)
    rig.hide_render = False
    child.hide_render = False
    pivot = bpy.data.objects.new(key + '-pivot-' + str(direction), None)
    bpy.context.collection.objects.link(pivot)
    rig.parent = pivot
    pivot.location.x = (direction - 3.5) * 1.2
    pivot.rotation_euler.z = direction * math.pi / 4
    for modifier in child.modifiers:
        if modifier.type == 'ARMATURE': modifier.object = rig
    rig.animation_data_create()

camera_data = bpy.data.cameras.new('Schoolyard Sprite Camera')
camera = bpy.data.objects.new('Schoolyard Sprite Camera', camera_data)
bpy.context.collection.objects.link(camera)
camera.location = (0, -20, 7)
camera.rotation_euler = (Vector((0, 0, .5)) - camera.location).to_track_quat('-Z', 'Y').to_euler()
camera_data.type = 'ORTHO'
camera_data.ortho_scale = 9.6
bpy.context.scene.camera = camera
for location, energy in (((-3, -5, 7), 1500), ((4, 3, 5), 900)):
    data = bpy.data.lights.new('Softbox', 'AREA')
    obj = bpy.data.objects.new('Softbox', data)
    bpy.context.collection.objects.link(obj)
    obj.location = location
    data.energy = energy
    data.shape = 'DISK'
    data.size = 8
scene = bpy.context.scene
scene.world = bpy.data.worlds.new('Neutral Ambient')
scene.world.color = (.7, .7, .7)
scene.render.engine = 'BLENDER_EEVEE'
scene.render.resolution_x = 768
scene.render.resolution_y = 128
scene.render.resolution_percentage = 100
scene.render.film_transparent = True
scene.render.image_settings.file_format = 'PNG'
scene.render.image_settings.color_mode = 'RGBA'
scene.render.image_settings.compression = 15
scene.render.filepath = os.path.join(output_root, key, 'frame.png')
scene.render.film_transparent = True
scene.view_settings.view_transform = 'Standard'
scene.view_settings.look = 'Medium High Contrast'
try: scene.eevee.taa_render_samples = 16
except AttributeError: pass
os.makedirs(os.path.dirname(scene.render.filepath), exist_ok=True)

for state_index, state in enumerate(('idle', 'walk', 'clap')):
    action = actions[state]
    for rig in (bpy.data.objects.get(key + '-rig-' + str(i)) for i in range(8)):
        rig.animation_data.action = action
        rig.animation_data.action_slot = action.slots[0]
    for frame_index in range(8):
        frame = 1 + round(frame_index * ((57 if state == 'walk' else 60) - 1) / 7)
        scene.frame_set(frame)
        scene.render.filepath = os.path.join(output_root, key,
            f'{state_index * 8 + frame_index:02d}.png')
        bpy.ops.render.render(write_still=True)
print('BAKED', key)
