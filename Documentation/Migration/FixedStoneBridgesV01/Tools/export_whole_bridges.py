"""Whole supplied meshes: one uniform size calibration, no cuts or vertex deformation."""
import bpy,json,shutil,zipfile
from pathlib import Path
from mathutils import Vector
root=Path('/Users/joelinstrum/dev/CityForge-workspaces/quarry-and-performance-updates')
source=Path('/Users/joelinstrum/Downloads/buildings/bridges')
staging=Path('/tmp/cityforge-fixed-bridges')
width=None
for archive,folder,package in [('stone+bridge+3d+model.zip','short','StoneOriginalV01'),('stone+arch+bridge+3d+model-long.zip','long','StoneLongOriginalV01')]:
 extracted=staging/folder;extracted.mkdir(parents=True,exist_ok=True)
 with zipfile.ZipFile(source/archive) as z:z.extractall(extracted)
 bpy.ops.wm.read_factory_settings(use_empty=True)
 bpy.ops.import_scene.fbx(filepath=str(next(extracted.glob('*.fbx'))))
 obj=next(o for o in bpy.context.scene.objects if o.type=='MESH');mesh=obj.data.copy();mesh.transform(obj.matrix_world)
 lo=min(v.co.x for v in mesh.vertices);hi=max(v.co.x for v in mesh.vertices)
 ymin=min(v.co.y for v in mesh.vertices);ymax=max(v.co.y for v in mesh.vertices)
 scale=35 if width is None else width/(ymax-ymin)
 width=(ymax-ymin)*scale
 inv=obj.matrix_world.inverted();heights=[]
 for i in range(257):
  x=lo+(hi-lo)*(.001+.998*i/256)
  hit,p,_,_=obj.ray_cast(inv@Vector((x,(ymin+ymax)/2,2)),inv.to_3x3()@Vector((0,0,-1)))
  heights.append((obj.matrix_world@p).z*scale if hit else None)
 valid=[i for i,h in enumerate(heights) if h is not None]
 for i,h in enumerate(heights):
  if h is None:heights[i]=heights[min(valid,key=lambda j:abs(j-i))]
 datum=heights[0];heights=[h-datum for h in heights]
 mesh.calc_loop_triangles();uv=mesh.uv_layers.active.data;verts=[];norms=[];uvs=[];triangles=[];lookup={}
 for tri in mesh.loop_triangles:
  indices=[]
  for li in tri.loops:
   v=mesh.vertices[mesh.loops[li].vertex_index].co;n=mesh.corner_normals[li].vector;u=uv[li].uv
   key=tuple(round(c,8) for c in (*v,*n,*u))
   if key not in lookup:
    lookup[key]=len(verts);verts.append(dict(x=-(v.y-(ymin+ymax)/2)*scale,y=v.z*scale-datum,z=(v.x-lo)*scale))
    norms.append(dict(x=-n.y,y=n.z,z=n.x));uvs.append(dict(x=u.x,y=u.y))
   indices.append(lookup[key])
  # The Unity axis mapping reverses handedness.
  triangles.extend(indices[::-1])
 out=root/'Assets/CityForgeV3/Resources/CityForgeV3/Bridges'/package;out.mkdir(parents=True,exist_ok=True)
 data=dict(fixedModel=True,fixedLength=(hi-lo)*scale,halfWidth=width/2,leftEndDeck=0,rightEndDeck=heights[-1],deckHeights=heights,
  modules=[dict(name='Whole',vertices=verts,normals=norms,uv=uvs,triangles=triangles)])
 (out/'modules.json').write_text(json.dumps(data,separators=(',',':')))
 albedo=next(extracted.rglob('*_0.jpg'));shutil.copy2(albedo,out/'albedo.jpg')
 # Render the untouched imported source model for its catalog image.
 center=Vector(((lo+hi)/2,0,.03));camdata=bpy.data.cameras.new('Review');cam=bpy.data.objects.new('Review',camdata);bpy.context.collection.objects.link(cam)
 cam.location=center+Vector((-.8,-1,.65));cam.rotation_euler=(center-cam.location).to_track_quat('-Z','Y').to_euler();camdata.type='ORTHO';camdata.ortho_scale=1.15
 scene=bpy.context.scene;scene.camera=cam;scene.render.engine='CYCLES';scene.cycles.samples=24
 scene.world=bpy.data.worlds.new('World');scene.world.use_nodes=True;scene.world.node_tree.nodes['Background'].inputs[0].default_value=(.25,.25,.25,1)
 light=bpy.data.lights.new('Softbox','AREA');light.energy=80;light.shape='DISK';light.size=2
 lamp=bpy.data.objects.new('Softbox',light);bpy.context.collection.objects.link(lamp);lamp.location=(-.3,-.5,1.5)
 scene.render.resolution_x=1000;scene.render.resolution_y=600;scene.render.resolution_percentage=100;scene.render.image_settings.file_format='PNG';scene.render.film_transparent=True
 scene.render.filepath=str(out/'preview.png');bpy.ops.render.render(write_still=True)
 print('FIXED',package,'length',data['fixedLength'],'width',width,'endRise',heights[-1],'triangles',len(triangles)//3)
