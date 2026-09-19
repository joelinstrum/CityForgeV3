"""Repair Joe's hollow-window Town Center in Blender; preserve the Tripo source.

Blender --factory-startup -b --python Tools/build_town_center.py -- <project>
The exported mesh uses Blender Z-up; the Unity builder bakes its FBX transform.
"""
import bpy
import json
import sys
from pathlib import Path
from mathutils import Vector, Matrix

project = Path(sys.argv[sys.argv.index('--') + 1])
root = project / 'Assets/CityForgeV3/Resources/CityForgeV3/Buildings3D/TownCenterV01'
author = project / 'Authoring/Buildings/TownCenterV01'
author.mkdir(parents=True, exist_ok=True)
(root / 'Derived').mkdir(exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=str(next((root / 'Source').glob('*.fbx'))))
shell = next(o for o in bpy.context.scene.objects if o.type == 'MESH')
shell.name = 'TownCenter_OriginalShell'
shell.data.transform(shell.matrix_world)
shell.matrix_world.identity()
shell.data.transform(Matrix.Scale(14.5, 4))
source_material = shell.data.materials[0]
bsdf = next(n for n in source_material.node_tree.nodes if n.type == 'BSDF_PRINCIPLED')
for link in list(bsdf.inputs['Metallic'].links):
    source_material.node_tree.links.remove(link)
bsdf.inputs['Metallic'].default_value = 0
bsdf.inputs['Roughness'].default_value = .8
bsdf.inputs['Specular IOR Level'].default_value = .15

# Bake clean, unlit patches through the original UVs. This avoids stretching
# disconnected atlas islands across the reconstructed elevation.
def bake_patch(name, cy, cz, width, height, pixels):
    m=bpy.data.materials.new('Temporary source color bake');m.use_nodes=True
    m.node_tree.nodes.clear()
    tex=m.node_tree.nodes.new('ShaderNodeTexImage')
    tex.image=next(n.image for n in source_material.node_tree.nodes if n.type=='TEX_IMAGE' and n.image and n.image.filepath.endswith('_0.jpg'))
    emit=m.node_tree.nodes.new('ShaderNodeEmission');out=m.node_tree.nodes.new('ShaderNodeOutputMaterial')
    m.node_tree.links.new(tex.outputs['Color'],emit.inputs['Color']);m.node_tree.links.new(emit.outputs[0],out.inputs['Surface'])
    shell.data.materials[0]=m
    scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=8
    scene.view_settings.view_transform='Standard';scene.render.film_transparent=True
    scene.render.resolution_x=pixels;scene.render.resolution_y=round(pixels*height/width);scene.render.resolution_percentage=100
    camdata=bpy.data.cameras.new('Patch camera');cam=bpy.data.objects.new('Patch camera',camdata);scene.collection.objects.link(cam)
    cam.location=(20,cy,cz);cam.rotation_euler=(Vector((0,cy,cz))-cam.location).to_track_quat('-Z','Y').to_euler();camdata.type='ORTHO';camdata.ortho_scale=width;scene.camera=cam
    path=root/'Derived'/(name+'.png');scene.render.filepath=str(path);bpy.ops.render.render(write_still=True)
    shell.data.materials[0]=source_material;bpy.data.objects.remove(cam,do_unlink=True)
    result=bpy.data.materials.new('TC_'+name);result.use_nodes=True
    p=next(n for n in result.node_tree.nodes if n.type=='BSDF_PRINCIPLED');p.inputs['Roughness'].default_value=.9
    image=result.node_tree.nodes.new('ShaderNodeTexImage');image.image=bpy.data.images.load(str(path));result.node_tree.links.new(image.outputs['Color'],p.inputs['Base Color'])
    return result

siding=bake_patch('RearSiding',.6,4.02,3.0,.54,1536)
stone=bake_patch('RearStone',1.5,.48,1.4,.64,768)

def material(name, color, emission=0):
    m = bpy.data.materials.new(name)
    m.diffuse_color = (*color, 1)
    m.use_nodes = True
    p = next(n for n in m.node_tree.nodes if n.type == 'BSDF_PRINCIPLED')
    p.inputs['Base Color'].default_value = (*color, 1)
    p.inputs['Roughness'].default_value = .78
    if emission:
        p.inputs['Emission Color'].default_value = (1, .47, .17, 1)
        p.inputs['Emission Strength'].default_value = emission
    return m

trim = material('TC_WeatheredWood', (.24, .17, .10))
interior = material('TC_Interior', (.28, .20, .12))
glass = material('TC_Glass', (.15, .20, .22))
glow = material('TC_LanternGlass', (.55, .55, .45))
metal = material('TC_Iron', (.035,.04,.035))
groups = {'Wood': [], 'Interior': [], 'Glass': [], 'LanternGlass': [], 'Iron': [], 'RearSiding': [], 'RearStone': []}

def box(name, center, size, mat, group):
    bpy.ops.mesh.primitive_cube_add(size=1, location=center)
    o = bpy.context.object
    o.name = name
    o.scale = size
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    o.data.materials.append(mat)
    groups[group].append(o)
    return o

# Reconstruct a closed, thick rear wall with source-derived siding/stone.
def rear_panel(name,z,height,mat,tw,th):
    o=box(name,(-.36,4.24,z),(10.5,.12,height),mat,name)
    for poly in o.data.polygons:
        for i in poly.loop_indices:
            v=o.data.vertices[o.data.loops[i].vertex_index].co
            o.data.uv_layers.active.data[i].uv=(v.x/tw,v.z/th)
    return o
rear_panel('RearSiding',3.875,5.75,siding,3.0,.54)
rear_panel('RearStone',.53,.94,stone,1.4,.64)

# Enclosed rooms: low interior ceiling prevents a view through the roof;
# floors and a rear lining give the windows physically occluding depth.
box('Rear interior lining',(-.36,4.13,3.8),(10.46,.20,5.9),interior,'Interior')
for z in (1.12,4.08,6.77):
    box('Floor or ceiling',(-.36,.95,z),(10.0,6.1,.12),interior,'Interior')
for x in (-5.58,4.84):
    box('Rear corner post',(x,4.33,3.88),(.18,.17,5.8),trim,'Wood')
for z in (1.15,4.10,6.64):
    box('Rear horizontal timber',(-.36,4.35,z),(10.5,.14,.14),trim,'Wood')
box('Rear foundation backing',(-.36,4.08,.52),(10.5,.30,1.02),interior,'Interior')

windows=[]
def window(label, center, width, height, side=False):
    x,y,z=center
    # Entire sash is just inside the existing irregular reveal. Narrow timber
    # muntins remain physical geometry; clear panes reveal the occupied room.
    def piece(name,u,v,w,h,depth,mat,group):
        pos=(x,y+u,z+v) if side else (x+u,y,z+v)
        dims=(depth,w,h) if side else (w,depth,h)
        return box(label+' '+name,pos,dims,mat,group)
    for u in (-width/2,width/2):piece('jamb',u,0,.055,height+.10,.085,trim,'Wood')
    for v in (-height/2,0,height/2):piece('rail',0,v,width,.055,.085,trim,'Wood')
    for u in (-width/6,width/6):piece('muntin',u,0,.025,height,.055,trim,'Wood')
    for v in (-height/4,height/4):piece('muntin',0,v,width,.025,.055,trim,'Wood')
    piece('clear pane',0,0,width,height,.012,glass,'Glass')
    windows.append(dict(name=label,center=[-x,z,-y],width=width,height=height,side=side))

for i,x in enumerate((-3.36,-.51,2.42)):
    window('Front upper '+str(i),(x,-2.25,5.70),1.04,1.45)
for i,x in enumerate((-3.68,2.44)):
    window('Front lower '+str(i),(x,-2.25,2.69),1.02,1.70)
for side,x in [('Right',4.75),('Left',-5.48)]:
    for i,y in enumerate((-.55,1.98)):
        window(side+' upper '+str(i),(x,y,5.70),.91,1.43,True)
        window(side+' lower '+str(i),(x,y,2.69),.92,1.69,True)
# Attic windows were not actually hollowed: retain their source panes and
# overlay a shallow lit recess, as this floor has no walking occupants.
attic=material('TC_AtticGlow',(.045,.045,.035))
groups['AtticGlow']=[]
box('Front attic pane',(-.64,-2.70,7.85),(.72,.025,1.02),attic,'AtticGlow')
box('Right attic pane',(4.91,1.10,7.85),(.025,.65,1.03),attic,'AtticGlow')
box('Left attic pane',(-5.66,1.10,7.85),(.025,.65,1.03),attic,'AtticGlow')
for label,center,w,h,side in [('Attic front',(-.64,-2.73,7.85),.72,1.02,False),('Attic right',(4.94,1.10,7.85),.65,1.03,True),('Attic left',(-5.69,1.10,7.85),.65,1.03,True)]:
    window(label,center,w,h,side)

# Shallow counters and shelves make the rooms read as occupied civic rooms.
for x in (-3.5,2.3):
    box('Office desk',(x,2.4,4.87),(1.4,.7,.10),trim,'Wood')
    for dx in (-.55,.55):box('Desk legs',(x+dx,2.4,4.47),(.08,.5,.75),trim,'Wood')
for z in (1.5,2.2,2.9,4.5,5.2,5.9):
    box('Rear shelf',(-.36,3.83,z),(7.7,.38,.06),trim,'Wood')

# New porch lanterns are separate, editable assemblies. Retain the supplied
# lantern bodies and fit glow over the existing central glass only.
lamps=[(-.14,-4.32,3.52),(6.84,2.67,6.02),(5.08,2.86,3.10),(-5.30,-4.12,3.30),(4.50,-4.12,3.30)]
for i,p in enumerate(lamps):
    box('Lantern luminous glass '+str(i),p,(.22,.20,.28),glow,'LanternGlass')
    x,y,z=p
    for dx in (-.125,.125):
        for dy in (-.115,.115):box('Iron lantern corner',(x+dx,y+dy,z),(.025,.025,.33),metal,'Iron')
    box('Iron lantern base',(x,y,z-.17),(.29,.27,.04),metal,'Iron')
    box('Iron lantern cap',(x,y,z+.17),(.32,.30,.07),metal,'Iron')
    if i>=3:
        box('Porch hanging chain',(x,y,z+.47),(.025,.025,.55),metal,'Iron')

def join(objects,name):
    bpy.ops.object.select_all(action='DESELECT')
    for o in objects:o.select_set(True)
    bpy.context.view_layer.objects.active=objects[0];bpy.ops.object.join()
    o=bpy.context.object;o.name=name
    bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
    return o
shell=join([shell],'TC_Shell')
objects=[shell]+[join(items,'TC_'+name) for name,items in groups.items() if items]
manifest=dict(height=10.539,footprint=[14.20,11.64],windows=windows,
              lamps=[[-x,z,-y] for x,y,z in lamps],sourceScale=14.5,
              interiorAnchor=[.45,4.9,.7])
(root/'manifest.json').write_text(json.dumps(manifest,indent=2)+'\n')
bpy.ops.object.select_all(action='DESELECT')
for o in objects:o.select_set(True)
bpy.ops.export_scene.fbx(filepath=str(root/'Derived/TownCenter.fbx'),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',apply_unit_scale=True,bake_anim=False,path_mode='RELATIVE')
bpy.ops.wm.save_as_mainfile(filepath=str(author/'TownCenter_Repaired.blend'))
print('TOWN_CENTER_EXPORT',len(objects),sum(len(o.data.polygons) for o in objects))
# Derive a distant shell from the repaired mesh; source and master stay intact.
bpy.ops.object.select_all(action='DESELECT')
shell.select_set(True);bpy.context.view_layer.objects.active=shell
mod=shell.modifiers.new('Distant shell reduction','DECIMATE');mod.ratio=.27
bpy.ops.object.modifier_apply(modifier=mod.name)
bpy.ops.export_scene.fbx(filepath=str(root/'Derived/TownCenterFarShell.fbx'),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',apply_unit_scale=True,bake_anim=False,path_mode='RELATIVE')
