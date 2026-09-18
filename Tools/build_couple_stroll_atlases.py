"""Bake a source-rigged lady and gentleman into one eight-facing stroll clip.

Blender -b --python Tools/build_couple_stroll_atlases.py --
    <canonical lady FBX> <existing gentleman FBX> <gentleman dark texture>
    <output directory>

The source rigs and textures are read only. Unity receives group atlases,
never per-character scripts or a live skeleton.
"""
import math
import os
import sys

import bpy
from mathutils import Vector

mask_mode = os.environ.get('COUPLE_MASK_MODE') == '1'


lady_path, gentleman_path, gentleman_texture, output_dir = \
    sys.argv[sys.argv.index('--') + 1:]
os.makedirs(output_dir, exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)


def import_person(path, label, height, x):
    existing = set(bpy.context.scene.objects)
    old_actions = set(bpy.data.actions)
    bpy.ops.import_scene.fbx(filepath=path)
    created = set(bpy.context.scene.objects) - existing
    rig = next(obj for obj in created if obj.type == 'ARMATURE')
    mesh = next(obj for obj in created if obj.type == 'MESH' and
                any(mod.type == 'ARMATURE' for mod in obj.modifiers))
    actions = {action.name.split(':')[-1]: action
               for action in set(bpy.data.actions) - old_actions
               if 'preset:biped:' in action.name}
    for obj in created:
        if obj not in (rig, mesh):
            bpy.data.objects.remove(obj, do_unlink=True)
    pivot = bpy.data.objects.new(label + ' route pivot', None)
    bpy.context.collection.objects.link(pivot)
    pivot.scale = (height / mesh.dimensions.z,) * 3
    pivot.location.x = x
    rig.parent = pivot
    mesh.parent = pivot
    rig.animation_data_create()
    return pivot, rig, mesh, actions


gentleman, man_rig, man_mesh, man_actions = import_person(
    gentleman_path, 'Gentleman', 1.78, -0.55)
lady, lady_rig, lady_mesh, lady_actions = import_person(
    lady_path, 'Lady', 1.72, 0.55)
assert {'walk', 'idle', 'look_around'} <= set(man_actions)
assert {'walk', 'idle'} <= set(lady_actions)

# Use the same charcoal cloth selected for the earlier gentleman clip.
man_material = man_mesh.data.materials[0].copy()
man_material.name = 'Source-derived charcoal gentleman'
man_mesh.data.materials.clear()
man_mesh.data.materials.append(man_material)
man_material.use_nodes = True
bsdf = next(node for node in man_material.node_tree.nodes
            if node.type == 'BSDF_PRINCIPLED')
base = bsdf.inputs['Base Color']
links = man_material.node_tree.links
for link in list(base.links):
    links.remove(link)
source_image = bpy.data.images.load(gentleman_texture, check_existing=True)
source_image.colorspace_settings.name = 'sRGB'
source_node = man_material.node_tree.nodes.new('ShaderNodeTexImage')
source_node.image = source_image
links.new(source_node.outputs['Color'], base)

if mask_mode:
    # These UV masks are derived from the source textures, not from screen
    # positions. The ordinary mesh depth test therefore keeps overlapping
    # sleeves, faces and garments in the correct order in every direction.
    mask_dir = os.environ['COUPLE_UV_MASK_DIR']
    for mesh, channel, filename in (
            (lady_mesh, 0, 'lady-dress-uv-mask.png'),
            (man_mesh, 1, 'gentleman-cloth-uv-mask.png')):
        material = bpy.data.materials.new(mesh.name + ' recolor mask')
        material.use_nodes = True
        nodes = material.node_tree.nodes
        nodes.clear()
        image = nodes.new('ShaderNodeTexImage')
        image.image = bpy.data.images.load(os.path.join(mask_dir, filename))
        image.interpolation = 'Closest'
        emission = nodes.new('ShaderNodeEmission')
        separate = nodes.new('ShaderNodeSeparateColor')
        combine = nodes.new('ShaderNodeCombineColor')
        material.node_tree.links.new(image.outputs['Color'], separate.inputs[0])
        material.node_tree.links.new(separate.outputs['Red'],
                                     combine.inputs[channel])
        material.node_tree.links.new(combine.outputs['Color'],
                                     emission.inputs['Color'])
        output = nodes.new('ShaderNodeOutputMaterial')
        material.node_tree.links.new(emission.outputs[0], output.inputs[0])
        mesh.data.materials.clear()
        mesh.data.materials.append(material)

scene = bpy.context.scene


def planted_pause(rig, idle):
    """Hold feet during look/turn beats, while walk uses the source gait."""
    rig.animation_data.action = idle
    rig.animation_data.action_slot = idle.slots[0]
    scene.frame_set(1)
    reference = rig.copy()
    reference.data = rig.data.copy()
    bpy.context.collection.objects.link(reference)
    reference.name = rig.name + ' planted reference'
    reference.animation_data_clear()
    reference.hide_render = True
    locks = []
    for bone in rig.pose.bones:
        if bone.name in {'Root', 'Hip', 'Pelvis'} or any(
                part in bone.name for part in ('Thigh', 'Calf', 'Foot', 'ToeBase')):
            target = reference.pose.bones[bone.name]
            target.location = bone.location.copy()
            target.rotation_quaternion = bone.rotation_quaternion.copy()
            target.scale = bone.scale.copy()
            lock = bone.constraints.new('COPY_TRANSFORMS')
            lock.target = reference
            lock.subtarget = bone.name
            lock.target_space = 'LOCAL'
            lock.owner_space = 'LOCAL'
            lock.mix_mode = 'REPLACE'
            lock.influence = 0
            locks.append(lock)
    rig.animation_data.action = None
    return locks


man_locks = planted_pause(man_rig, man_actions['idle'])
lady_locks = planted_pause(lady_rig, lady_actions['idle'])


def sample_pose(rig, action, source_frame, locks, walking):
    # Freeze the evaluated pose after sampling so the two people can use
    # different gait phases in the same output frame.
    rig.animation_data.action = action
    rig.animation_data.action_slot = action.slots[0]
    scene.frame_set(source_frame)
    poses = [(bone, bone.location.copy(), bone.rotation_quaternion.copy(),
              bone.rotation_euler.copy(), bone.scale.copy())
             for bone in rig.pose.bones]
    rig.animation_data.action = None
    for bone, location, quaternion, euler, scale in poses:
        bone.location = location
        bone.rotation_quaternion = quaternion
        bone.rotation_euler = euler
        bone.scale = scale
    for lock in locks:
        lock.influence = 0 if walking else 1


def performance(frame):
    """20-second loop at 2 fps, with exact 2 m and 4 m waypoints."""
    if frame <= 5:
        return 2 * frame / 5, 0, 'walk', frame
    if frame <= 9:
        return 2, 0, 'look', frame - 6
    if frame <= 15:
        return 2 + 2 * (frame - 10) / 5, 0, 'walk', frame - 10
    if frame <= 19:
        return 4, 0, 'look', frame - 16
    if frame <= 21:
        return 4, math.pi * (frame - 19) / 2, 'turn', frame - 20
    if frame <= 35:
        return 4 - 4 * (frame - 22) / 13, math.pi, 'walk', frame - 22
    return 0, math.pi * (39 - frame) / 4, 'turn', frame - 36


camera_data = bpy.data.cameras.new('Shared Automata camera')
camera = bpy.data.objects.new('Shared Automata camera', camera_data)
bpy.context.collection.objects.link(camera)
camera.location = (0, -11, 6)
camera.rotation_euler = (Vector((0, 0, .9)) - camera.location).to_track_quat(
    '-Z', 'Y').to_euler()
camera_data.type = 'ORTHO'
camera_data.ortho_scale = 256 / 32  # Blender uses the horizontal span: 32 px/m.
scene.camera = camera
for position, energy, size in (((-4, -5, 7), 1200, 6),
                               ((3, 3, 6), 900, 7)):
    data = bpy.data.lights.new('Softbox', 'AREA')
    light = bpy.data.objects.new('Softbox', data)
    bpy.context.collection.objects.link(light)
    light.location = position
    data.energy = energy
    data.shape = 'DISK'
    data.size = size
scene.world = bpy.data.worlds.new('Neutral ambient')
scene.world.color = (.7, .7, .7)
scene.render.engine = 'BLENDER_EEVEE'
scene.render.resolution_x = 256
scene.render.resolution_y = 160
scene.render.resolution_percentage = 100
scene.render.film_transparent = True
scene.render.image_settings.file_format = 'PNG'
scene.render.image_settings.color_mode = 'RGBA'
scene.render.image_settings.compression = 15
scene.view_settings.view_transform = 'Standard'
scene.view_settings.look = 'Medium High Contrast'
if mask_mode:
    scene.view_settings.look = 'Medium High Contrast'

directions = ([int(os.environ['COUPLE_PREVIEW_DIRECTION'])]
              if os.environ.get('COUPLE_PREVIEW_DIRECTION') else range(8))
frames = ([int(value) for value in os.environ['COUPLE_PREVIEW_FRAMES'].split(',')]
          if os.environ.get('COUPLE_PREVIEW_FRAMES') else range(40))
for direction in directions:
    yaw = -direction * math.tau / 8
    for frame in frames:
        distance, heading, phase, phase_frame = performance(frame)
        walking = phase == 'walk'
        # The source walk loops in place. Advance the two pivots along one
        # shared four-metre path; their gait phases differ by twelve frames.
        traveled = 8 - distance if frame >= 22 else distance
        source_walk = 1 + (round(traveled * 35) % 56)
        if walking:
            man_action, man_frame = man_actions['walk'], source_walk
            lady_action = lady_actions['walk']
            lady_frame = 1 + ((source_walk + 11) % 56)
        else:
            man_action = man_actions['look_around'] if phase == 'look' \
                else man_actions['idle']
            man_frame = 1 + (phase_frame * 75 if phase == 'look'
                             else phase_frame * 20)
            lady_action = lady_actions['idle']
            lady_frame = 1 + phase_frame * 41
        sample_pose(man_rig, man_action, man_frame, man_locks, walking)
        sample_pose(lady_rig, lady_action, lady_frame, lady_locks, walking)
        # Subtle, asynchronous attention shifts while stopped.
        man_look = (0.24 * math.sin((phase_frame + 1) * 1.5)
                    if phase == 'look' else 0)
        lady_look = (-0.20 * math.sin((phase_frame + 1) * 1.2)
                     if phase == 'look' else 0)
        # Keep the couple close enough to brush shoulders. The small
        # fore-aft offset leaves both silhouettes readable in side views.
        for pivot, x, lag, glance in ((gentleman, -0.22, -0.12, man_look),
                                      (lady, 0.22, 0.12, lady_look)):
            y = 2 - distance + lag
            pivot.location.x = x * math.cos(yaw) - y * math.sin(yaw)
            pivot.location.y = x * math.sin(yaw) + y * math.cos(yaw)
            pivot.rotation_euler.z = yaw + heading + glance
        scene.render.filepath = os.path.join(output_dir,
            f'direction-{direction}-frame-{frame:02d}.png')
        bpy.ops.render.render(write_still=True)
print('BAKED', len(directions), 'facings x', len(frames), 'frames', output_dir)
