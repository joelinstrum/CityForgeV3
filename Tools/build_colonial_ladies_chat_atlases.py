"""Bake two colonial ladies into one directional idle clip.

Run with Blender -b --python Tools/build_colonial_ladies_chat_atlases.py --
    <extracted canonical FBX> <output directory>. The red source texture is
    unchanged; the second dress color is a shader-only derivative.
"""
import bpy
import math
import os
import sys
from mathutils import Vector

fbx_path, output_dir = sys.argv[sys.argv.index('--') + 1:]
os.makedirs(output_dir, exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=fbx_path)
rig = next(obj for obj in bpy.context.scene.objects if obj.type == 'ARMATURE')
mesh = next(obj for obj in bpy.context.scene.objects if obj.type == 'MESH')
idle = next(action for action in bpy.data.actions if action.name.lower().endswith('idle'))
rig.animation_data_create()
rig.animation_data.action = idle
rig.animation_data.action_slot = idle.slots[0]

# FBX authored height is 0.967 units; calibrate both copies to 1.72 m.
scale = 1.72 / mesh.dimensions.z
left = bpy.data.objects.new('Red lady', None)
bpy.context.collection.objects.link(left)
left.location.x = -0.88
left.scale = (scale,) * 3
rig.parent = left
mesh.parent = left

right = bpy.data.objects.new('Blue lady', None)
bpy.context.collection.objects.link(right)
right.location.x = 0.88
right.scale = (scale,) * 3
blue_rig = rig.copy()
blue_rig.data = rig.data.copy()
bpy.context.collection.objects.link(blue_rig)
blue_rig.name = 'Blue lady rig'
blue_rig.parent = right
blue_rig.animation_data_clear()
blue_rig.animation_data_create()
blue_rig.animation_data.action = idle
blue_rig.animation_data.action_slot = idle.slots[0]
blue_mesh = mesh.copy()
blue_mesh.data = mesh.data.copy()
bpy.context.collection.objects.link(blue_mesh)
blue_mesh.name = 'Blue lady mesh'
blue_mesh.parent = right
for modifier in blue_mesh.modifiers:
    if modifier.type == 'ARMATURE':
        modifier.object = blue_rig

# Isolate dark crimson cloth by channel difference and low green. Skin,
# apron, bonnet, hair and shoes retain the canonical material colors.
material = mesh.data.materials[0].copy()
material.name = 'Colonial lady blue dress derivative'
blue_mesh.data.materials.clear()
blue_mesh.data.materials.append(material)
nodes = material.node_tree.nodes
links = material.node_tree.links
bsdf = next(node for node in nodes if node.type == 'BSDF_PRINCIPLED')
base_socket = bsdf.inputs['Base Color']
base_link = next(link for link in links if link.to_socket == base_socket)
source = base_link.from_socket
links.remove(base_link)
separate = nodes.new('ShaderNodeSeparateColor')
links.new(source, separate.inputs['Color'])
red_minus_green = nodes.new('ShaderNodeMath')
red_minus_green.operation = 'SUBTRACT'
links.new(separate.outputs['Red'], red_minus_green.inputs[0])
links.new(separate.outputs['Green'], red_minus_green.inputs[1])
red_gate = nodes.new('ShaderNodeMath')
red_gate.operation = 'GREATER_THAN'
red_gate.inputs[1].default_value = 0.12
links.new(red_minus_green.outputs[0], red_gate.inputs[0])
green_gate = nodes.new('ShaderNodeMath')
green_gate.operation = 'LESS_THAN'
green_gate.inputs[1].default_value = 0.12
links.new(separate.outputs['Green'], green_gate.inputs[0])
mask = nodes.new('ShaderNodeMath')
mask.operation = 'MULTIPLY'
links.new(red_gate.outputs[0], mask.inputs[0])
links.new(green_gate.outputs[0], mask.inputs[1])
blue = nodes.new('ShaderNodeHueSaturation')
blue.inputs['Hue'].default_value = 0.12
blue.inputs['Saturation'].default_value = 0.72
links.new(source, blue.inputs['Color'])
mix = nodes.new('ShaderNodeMixRGB')
links.new(mask.outputs[0], mix.inputs[0])
links.new(source, mix.inputs[1])
links.new(blue.outputs['Color'], mix.inputs[2])
links.new(mix.outputs[0], base_socket)

# Arm poses are solved in Blender and baked into the group frames. The
# rig is only an authoring input, never loaded by Unity.
def pose_target(name, parent, x, y, z):
    target = bpy.data.objects.new(name, None)
    bpy.context.collection.objects.link(target)
    target.parent = parent
    target.location = (x, y, z)
    target.hide_render = True
    return target


def constrain_wrist(armature, parent, side, wrist, elbow):
    target = pose_target(armature.name + side + ' wrist', parent, *wrist)
    pole = pose_target(armature.name + side + ' elbow', parent, *elbow)
    ik = armature.pose.bones[side + '_Forearm'].constraints.new('IK')
    ik.target = target
    ik.pole_target = pole
    ik.chain_count = 2
    ik.pole_angle = -math.pi / 2
    ik.use_stretch = False
    return ik

red_ik = []
blue_ik = []
for side, sign in (('L', 1), ('R', -1)):
    red_ik.append(constrain_wrist(rig, left, side,
        (sign * .09, -.14, .62), (sign * .26, -.08, .65)))
    blue_ik.append(constrain_wrist(blue_rig, right, side,
        (sign * .04, -.14, .66 if side == 'L' else .64),
        (sign * .24, -.08, .66)))

# Keep both pairs of feet planted. The source "idle" action contains a
# stepping beat; only its upper-body motion belongs in this conversation.
bpy.context.scene.frame_set(1)
lower_names = [bone.name for bone in rig.pose.bones
               if bone.name in {'Root', 'Hip', 'Pelvis'} or
               any(part in bone.name for part in
                   ('Thigh', 'Calf', 'Foot', 'ToeBase'))]
planted = {name: (rig.pose.bones[name].location.copy(),
                  rig.pose.bones[name].rotation_quaternion.copy(),
                  rig.pose.bones[name].scale.copy())
           for name in lower_names}


# Constraints are evaluated after the imported action, so the planted
# lower-body pose survives Blender's render-time dependency graph update.
planted_rig = rig.copy()
planted_rig.data = rig.data.copy()
bpy.context.collection.objects.link(planted_rig)
planted_rig.name = 'Planted lower-body reference'
planted_rig.animation_data_clear()
planted_rig.hide_render = True
for name, (location, rotation, bone_scale) in planted.items():
    bone = planted_rig.pose.bones[name]
    bone.location = location
    bone.rotation_quaternion = rotation
    bone.scale = bone_scale
    for armature in (rig, blue_rig):
        lock = armature.pose.bones[name].constraints.new('COPY_TRANSFORMS')
        lock.target = planted_rig
        lock.subtarget = name
        lock.target_space = 'LOCAL'
        lock.owner_space = 'LOCAL'
        lock.mix_mode = 'REPLACE'

# Offset the blue lady's underlying body idle in an NLA strip. Her folded
# arms release briefly in the middle of the loop, then fold again.
blue_rig.animation_data.action = None
track = blue_rig.animation_data.nla_tracks.new()
strip = track.strips.new('Offset idle', -80, idle)
strip.action_slot = idle.slots[0]
strip.repeat = 2

camera_data = bpy.data.cameras.new('Shared automata camera')
camera = bpy.data.objects.new('Shared automata camera', camera_data)
bpy.context.collection.objects.link(camera)
camera.location = (0, -10, 5.2)
camera.rotation_euler = (Vector((0, 0, .75)) - camera.location).to_track_quat('-Z', 'Y').to_euler()
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
scene.render.resolution_x = 896 if os.environ.get('COLONIAL_DEBUG_RES') else 224
scene.render.resolution_y = 512 if os.environ.get('COLONIAL_DEBUG_RES') else 128
scene.render.resolution_percentage = 100
scene.render.film_transparent = True
scene.render.image_settings.file_format = 'PNG'
scene.render.image_settings.color_mode = 'RGBA'
scene.render.image_settings.compression = 15
scene.view_settings.view_transform = 'Standard'
scene.view_settings.look = 'Medium High Contrast'

if os.environ.get('COLONIAL_ONLY_RED'):
    blue_mesh.hide_render = True
if os.environ.get('COLONIAL_ONLY_BLUE'):
    mesh.hide_render = True

# Facing 0 sees both faces at a three-quarter angle. Later directions
# rotate the whole paired scene; the ladies continue facing each other.
for direction in ([int(os.environ["COLONIAL_PREVIEW_DIRECTION"])]
                  if os.environ.get("COLONIAL_PREVIEW_DIRECTION") else
                  range(1 if os.environ.get("COLONIAL_PREVIEW") else 8)):
    yaw = -direction * math.tau / 8
    for pivot, x, y in ((left, -0.88, -0.25),
                        (right, 0.88, 0.25)):
        pivot.location.x = x * math.cos(yaw) - y * math.sin(yaw)
        pivot.location.y = x * math.sin(yaw) + y * math.cos(yaw)
    left.rotation_euler.z = math.radians(68) + yaw
    right.rotation_euler.z = math.radians(-68) + yaw
    frames = ([int(os.environ["COLONIAL_PREVIEW_FRAME"])]
              if os.environ.get("COLONIAL_PREVIEW_FRAME") else
              range(1 if os.environ.get("COLONIAL_PREVIEW") else 16))
    for frame_index in frames:
        # Each lady has a distinct upper-body idle phase. IK holds the red
        # hands at her waist; the blue arms open briefly then fold again.
        scene.frame_set(1 + round(frame_index * 368 / 16))
        openness = max(0.0, 1.0 - abs(frame_index - 7.5) / 2.5)
        for constraint in blue_ik:
            constraint.influence = 1.0 - openness
        # A slight opposite turn keeps their attention on one another.
        right.rotation_euler.z = math.radians(-68) + yaw + 0.035 * math.sin(frame_index * math.tau / 16 + 1.3)
        left.rotation_euler.z = math.radians(68) + yaw + 0.028 * math.sin(frame_index * math.tau / 16)
        scene.render.filepath = os.path.join(output_dir, f'direction-{direction}-frame-{frame_index:02d}.png')
        bpy.ops.render.render(write_still=True)
print('BAKED 8 facings x 16 frames', output_dir)
