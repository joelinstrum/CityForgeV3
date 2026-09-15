import bpy,json,sys,math
from pathlib import Path
from mathutils import Vector
repo=Path(sys.argv[sys.argv.index('--')+1]);src=repo/'Authoring/Buildings/BrickworksV01/Source/extracted';out=repo/'Assets/CityForgeV3/Resources/CityForgeV3/Industry/BrickworksV01';out.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.fbx(filepath=str(next(src.glob('*.fbx'))))
objects=[o for o in bpy.context.scene.objects if o.type=='MESH'];print('MODELS',[(o.name,list(o.dimensions),len(o.data.polygons)) for o in objects])
roles={}
for o in objects:
 for m in o.data.materials:
  if not m or not m.use_nodes:continue
  for node in m.node_tree.nodes:
   if node.type=='BSDF_PRINCIPLED':
    for key in ['Base Color','Normal','Roughness']:
     inp=node.inputs.get(key)
     if inp and inp.is_linked:
      n=inp.links[0].from_node
      if n.type=='NORMAL_MAP':n=n.inputs['Color'].links[0].from_node
      if n.type=='TEX_IMAGE':roles[key]=bpy.path.abspath(n.image.filepath)
print('TEXTURES',roles)
coords=[o.matrix_world@v.co for o in objects for v in o.data.vertices];lo=Vector([min(v[i] for v in coords) for i in range(3)]);hi=Vector([max(v[i] for v in coords) for i in range(3)])
# This compact yard occupies a 24m footprint, preserving the supplied proportions.
scale=24/max(hi.x-lo.x,hi.y-lo.y);center=Vector(((lo.x+hi.x)/2,(lo.y+hi.y)/2,lo.z))
for o in objects:
 for v in o.data.vertices:v.co=(o.matrix_world@v.co-center)*scale
 o.matrix_world.identity()
bpy.context.view_layer.update();bpy.ops.object.select_all(action='DESELECT')
for o in objects:o.select_set(True)
bpy.context.view_layer.objects.active=objects[0]
bpy.ops.wm.save_as_mainfile(filepath=str(repo/'Authoring/Buildings/BrickworksV01/Brickworks_metric_v01.blend'))
bpy.ops.export_scene.fbx(filepath=str(out/'Brickworks.fbx'),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',bake_anim=False,add_leaf_bones=False)
import shutil
for role,name in [('Base Color','BaseColor.jpg'),('Normal','Normal.jpg'),('Roughness','Roughness.jpg')]:
 if role in roles:shutil.copy2(roles[role],out/name)
(out/'spatial.json').write_text(json.dumps({'source':'Authoring/Buildings/BrickworksV01/Source','coordinateSystem':'blender-metric-origin-centered','originPolicy':'foundation-center-ground','rotationAnchor':[0,0,0],'size':list((hi-lo)*scale),'textureRoles':roles},indent=2))
# Four neutral material review angles and a plan; runtime uses the 3D model directly.
scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=12;scene.render.resolution_x=600;scene.render.resolution_y=600;scene.render.resolution_percentage=100;scene.render.film_transparent=True
scene.world=bpy.data.worlds.new('Review world');scene.world.color=(.45,.45,.45)
light=bpy.data.objects.new('Review softbox',bpy.data.lights.new('Review softbox','AREA'));scene.collection.objects.link(light);light.location=(0,0,25);light.data.energy=2200;light.data.shape='DISK';light.data.size=20
camera=bpy.data.objects.new('Review camera',bpy.data.cameras.new('Review camera'));scene.collection.objects.link(camera);scene.camera=camera;camera.data.type='ORTHO';camera.data.ortho_scale=34
review=repo/'Authoring/Buildings/BrickworksV01/Review';review.mkdir(exist_ok=True)
target=Vector((0,0,(hi.z-lo.z)*scale*.35))
for name,pos in [('front-left',(-30,-34,27)),('front-right',(30,-34,27)),('rear-left',(-30,34,27)),('rear-right',(30,34,27)),('plan',(0,0,45))]:
 camera.location=pos;camera.rotation_euler=(target-camera.location).to_track_quat('-Z','Y').to_euler();scene.render.filepath=str(review/(name+'.png'));bpy.ops.render.render(write_still=True)
print('BRICKWORKS_READY',list((hi-lo)*scale))
