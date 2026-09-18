"""Bake two Victorian gentlemen as one eight-facing Automata conversation.

Blender -b --python Tools/build_victorian_gentlemen_chat_atlases.py --
    <existing rigged gentleman FBX> <runtime dark base color> <output dir>
The canonical FBX and texture are read only. A warm-brown coat and the second
performance exist only in this derived bake; Unity loads group atlases only.
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
mesh = next(obj for obj in bpy.context.scene.objects if obj.type == 'MESH' and
            any(mod.type == 'ARMATURE' for mod in obj.modifiers))
for obj in list(bpy.context.scene.objects):
    if obj not in (rig, mesh):
        bpy.data.objects.remove(obj, do_unlink=True)

idle = next(action for action in bpy.data.actions
            if action.name.endswith('preset:biped:idle') and '|' in action.name)
fold = next(action for action in bpy.data.actions
            if action.name.endswith('preset:biped:fold_arms') and '|' in action.name)
rig.animation_data_create()
rig.animation_data.action = idle
rig.animation_data.action_slot = idle.slots[0]

# Both figures use the ported 10K rig; the second is duplicated for authoring.
# Calibrate against the rendered mesh, keeping a planted floor and 4 m group.
scale = 1.78 / mesh.dimensions.z
left = bpy.data.objects.new('Charcoal gentleman', None)
bpy.context.collection.objects.link(left)
left.scale = (scale,) * 3
rig.parent = left
mesh.parent = left

right = bpy.data.objects.new('Brown gentleman', None)
bpy.context.collection.objects.link(right)
right.scale = (scale,) * 3
brown_rig = rig.copy()
brown_rig.data = rig.data.copy()
bpy.context.collection.objects.link(brown_rig)
brown_rig.name = 'Brown gentleman rig'
brown_rig.parent = right
brown_rig.animation_data_clear()
brown_rig.animation_data_create()
track = brown_rig.animation_data.nla_tracks.new()
strip = track.strips.new('Folded arms conversation', 1, fold)
strip.action_slot = fold.slots[0]
strip.scale = 368 / 410

brown_mesh = mesh.copy()
brown_mesh.data = mesh.data.copy()
bpy.context.collection.objects.link(brown_mesh)
brown_mesh.name = 'Brown gentleman mesh'
brown_mesh.parent = right
for modifier in brown_mesh.modifiers:
    if modifier.type == 'ARMATURE':
        modifier.object = brown_rig

source_material = mesh.data.materials[0]
dark = bpy.data.images.load(texture_path, check_existing=True)
dark.colorspace_settings.name = 'sRGB'


def set_base_texture(material):
    material.use_nodes = True
    nodes = material.node_tree.nodes
    links = material.node_tree.links
    bsdf = next(node for node in nodes if node.type == 'BSDF_PRINCIPLED')
    socket = bsdf.inputs['Base Color']
    for link in list(socket.links):
        links.remove(link)
    image = nodes.new('ShaderNodeTexImage')
    image.image = dark
    links.new(image.outputs['Color'], socket)
    return image, bsdf


charcoal_material = source_material.copy()
charcoal_material.name = 'Charcoal source cloth'
mesh.data.materials.clear()
mesh.data.materials.append(charcoal_material)
set_base_texture(charcoal_material)

brown_material = source_material.copy()
brown_material.name = 'Warm brown cloth derivative'
brown_mesh.data.materials.clear()
brown_mesh.data.materials.append(brown_material)
image, bsdf = set_base_texture(brown_material)
nodes = brown_material.node_tree.nodes
links = brown_material.node_tree.links
links.remove(bsdf.inputs['Base Color'].links[0])
lightness = nodes.new('ShaderNodeRGBToBW')
links.new(image.outputs['Color'], lightness.inputs['Color'])
dark_gate = nodes.new('ShaderNodeMath')
dark_gate.operation = 'LESS_THAN'
dark_gate.inputs[1].default_value = 0.72
links.new(lightness.outputs[0], dark_gate.inputs[0])
channels = nodes.new('ShaderNodeSeparateColor')
links.new(image.outputs['Color'], channels.inputs['Color'])
skin_difference = nodes.new('ShaderNodeMath')
skin_difference.operation = 'SUBTRACT'
links.new(channels.outputs['Red'], skin_difference.inputs[0])
links.new(channels.outputs['Green'], skin_difference.inputs[1])
cloth_gate = nodes.new('ShaderNodeMath')
cloth_gate.operation = 'LESS_THAN'
cloth_gate.inputs[1].default_value = 0.16
links.new(skin_difference.outputs[0], cloth_gate.inputs[0])
mask = nodes.new('ShaderNodeMath')
mask.operation = 'MULTIPLY'
links.new(dark_gate.outputs[0], mask.inputs[0])
links.new(cloth_gate.outputs[0], mask.inputs[1])
strength = nodes.new('ShaderNodeMath')
strength.operation = 'MULTIPLY'
strength.inputs[1].default_value = 0.75
links.new(mask.outputs[0], strength.inputs[0])
mix = nodes.new('ShaderNodeMixRGB')
mix.blend_type = 'MIX'
mix.inputs[2].default_value = (0.10, 0.040, 0.024, 1)
links.new(strength.outputs[0], mix.inputs[0])
links.new(image.outputs['Color'], mix.inputs[1])
links.new(mix.outputs[0], bsdf.inputs['Base Color'])

# The source actions move the pelvis and feet. Lock each lower-body bone at
# its standing pose through render-time constraints, as with the ladies clip.
bpy.context.scene.frame_set(1)
lower_names = [bone.name for bone in rig.pose.bones
               if bone.name in {'Root', 'Hip', 'Pelvis'} or
               any(part in bone.name for part in
                   ('Thigh', 'Calf', 'Foot', 'ToeBase'))]
planted = {name: (rig.pose.bones[name].location.copy(),
                  rig.pose.bones[name].rotation_quaternion.copy(),
                  rig.pose.bones[name].scale.copy()) for name in lower_names}
reference = rig.copy()
reference.data = rig.data.copy()
bpy.context.collection.objects.link(reference)
reference.name = 'Planted standing reference'
reference.animation_data_clear()
reference.hide_render = True
for name, (location, rotation, bone_scale) in planted.items():
    bone = reference.pose.bones[name]
    bone.location = location
    bone.rotation_quaternion = rotation
    bone.scale = bone_scale
    for armature in (rig, brown_rig):
        lock = armature.pose.bones[name].constraints.new('COPY_TRANSFORMS')
        lock.target = reference
        lock.subtarget = name
        lock.target_space = 'LOCAL'
        lock.owner_space = 'LOCAL'
        lock.mix_mode = 'REPLACE'

for armature, pivot in ((rig, left), (brown_rig, right)):
    target = bpy.data.objects.new(armature.name + ' attentive gaze', None)
    bpy.context.collection.objects.link(target)
    target.parent = pivot
    target.rotation_euler.x = math.radians(16)
    target.hide_render = True
    gaze = armature.pose.bones['Head'].constraints.new('COPY_ROTATION')
    gaze.target = target
    gaze.target_space = 'LOCAL'
    gaze.owner_space = 'LOCAL'
    gaze.mix_mode = 'ADD'

# A brief raised-hand gesture belongs to the charcoal gentleman. The brown
# gentleman performs the supplied fold-arms action, staggered in time.
gesture_target = bpy.data.objects.new('Speaking hand target', None)
bpy.context.collection.objects.link(gesture_target)
gesture_target.parent = left
gesture_target.hide_render = True
gesture_pole = bpy.data.objects.new('Speaking elbow pole', None)
bpy.context.collection.objects.link(gesture_pole)
gesture_pole.parent = left
gesture_pole.location = (0.28, -0.12, 0.73)
gesture_pole.hide_render = True
gesture = rig.pose.bones['L_Forearm'].constraints.new('IK')
gesture.target = gesture_target
gesture.pole_target = gesture_pole
gesture.chain_count = 2
gesture.pole_angle = -math.pi / 2
gesture.use_stretch = False

camera_data = bpy.data.cameras.new('Shared Automata camera')
camera = bpy.data.objects.new('Shared Automata camera', camera_data)
bpy.context.collection.objects.link(camera)
camera.location = (0, -10, 5.2)
camera.rotation_euler = (Vector((0, 0, .84)) - camera.location).to_track_quat('-Z', 'Y').to_euler()
camera_data.type = 'ORTHO'
camera_data.ortho_scale = 128 / 26
bpy.context.scene.camera = camera
for location, energy, size in (((-4, -5, 7), 1200, 6), ((3, 3, 6), 900, 7)):
    data = bpy.data.lights.new('Softbox', 'AREA')
    obj = bpy.data.objects.new('Softbox', data)
    bpy.context.collection.objects.link(obj)
    obj.location = location
    data.energy = energy
    data.shape = 'DISK'
    data.size = size
scene = bpy.context.scene
scene.world = bpy.data.worlds.new('Neutral ambient')
scene.world.color = (.7, .7, .7)
scene.render.engine = 'BLENDER_EEVEE'
scene.render.resolution_x = 896 if os.environ.get('GENTLEMEN_DEBUG_RES') else 224
scene.render.resolution_y = 512 if os.environ.get('GENTLEMEN_DEBUG_RES') else 128
scene.render.resolution_percentage = 100
scene.render.film_transparent = True
scene.render.image_settings.file_format = 'PNG'
scene.render.image_settings.color_mode = 'RGBA'
scene.render.image_settings.compression = 15
scene.view_settings.view_transform = 'Standard'
scene.view_settings.look = 'Medium High Contrast'

directions = ([int(os.environ['GENTLEMEN_PREVIEW_DIRECTION'])]
              if os.environ.get('GENTLEMEN_PREVIEW_DIRECTION') else
              range(1 if os.environ.get('GENTLEMEN_PREVIEW') else 8))
frames = ([int(value) for value in os.environ['GENTLEMEN_PREVIEW_FRAMES'].split(',')]
          if os.environ.get('GENTLEMEN_PREVIEW_FRAMES') else
          [int(os.environ['GENTLEMEN_PREVIEW_FRAME'])]
          if os.environ.get('GENTLEMEN_PREVIEW_FRAME') else
          range(1 if os.environ.get('GENTLEMEN_PREVIEW') else 16))
for direction in directions:
    yaw = -direction * math.tau / 8
    for pivot, x, y in ((left, -0.90, -0.18), (right, 0.90, 0.18)):
        pivot.location.x = x * math.cos(yaw) - y * math.sin(yaw)
        pivot.location.y = x * math.sin(yaw) + y * math.cos(yaw)
    for frame_index in frames:
        scene.frame_set(1 + round(frame_index * 368 / 16))
        beat = max(0.0, 1.0 - abs(frame_index - 6.5) / 2.5)
        gesture_target.location = (0.14, -0.19, 0.50 + 0.18 * beat)
        gesture.influence = 0.65 * beat
        left.rotation_euler.z = math.radians(68) + yaw + 0.025 * math.sin(frame_index * math.tau / 16)
        right.rotation_euler.z = math.radians(-68) + yaw + 0.03 * math.sin(frame_index * math.tau / 16 + 1.5)
        scene.render.filepath = os.path.join(output_dir,
            f'direction-{direction}-frame-{frame_index:02d}.png')
        bpy.ops.render.render(write_still=True)
print('BAKED', len(directions), 'facings x', len(frames), 'frames', output_dir)
