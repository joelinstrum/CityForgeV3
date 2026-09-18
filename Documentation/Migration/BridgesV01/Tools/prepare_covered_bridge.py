import bpy,bmesh,math,json
from pathlib import Path
from mathutils import Vector
root=Path('/Users/joelinstrum/Downloads/buildings/bridges/covered-wooden-bridge-review-v01')
out=root.parent/'covered-wooden-bridge-modular-v01';out.mkdir(exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(root/'bridge-review.blend'))
source=next(o for o in bpy.context.scene.objects if o.type=='MESH')
SCALE=30.;CUT=-.24;DECK=.1425
# Transform the imported FBX geometry into world coordinates before cutting.
base=source.data.copy();base.transform(source.matrix_world)
def crop(name,low,high):
 mesh=base.copy();bm=bmesh.new();bm.from_mesh(mesh)
 for bound,lower in [(low,True),(high,False)]:
  if bound is None:continue
  bmesh.ops.bisect_plane(bm,geom=list(bm.verts)+list(bm.edges)+list(bm.faces),dist=1e-7,plane_co=(0,bound,0),plane_no=(0,1,0))
  doomed=[v for v in bm.verts if (v.co.y<bound-1e-7 if lower else v.co.y>bound+1e-7)]
  bmesh.ops.delete(bm,geom=doomed,context='VERTS')
 bm.to_mesh(mesh);bm.free();mesh.name=name
 obj=bpy.data.objects.new(name,mesh);bpy.context.scene.collection.objects.link(obj)
 return obj
def mirror_mesh(obj,name):
 mesh=obj.data.copy();bm=bmesh.new();bm.from_mesh(mesh)
 for v in bm.verts:v.co.y=-v.co.y
 bmesh.ops.reverse_faces(bm,faces=list(bm.faces));bm.normal_update();bm.to_mesh(mesh);bm.free()
 clone=bpy.data.objects.new(name,mesh);bpy.context.scene.collection.objects.link(clone);return clone
end=crop('Entrance_Start',None,CUT)
half=crop('Middle_Half',CUT,0)
other=mirror_mesh(half,'Middle_Half_Mirrored')
bpy.ops.object.select_all(action='DESELECT');half.select_set(True);other.select_set(True);bpy.context.view_layer.objects.active=half
bpy.ops.object.join();middle=half;middle.name='Middle_Bay'
bm=bmesh.new();bm.from_mesh(middle.data)
bmesh.ops.remove_doubles(bm,verts=[v for v in bm.verts if abs(v.co.y)<1e-6],dist=1e-6)
bm.normal_update();bm.to_mesh(middle.data);bm.free()
for obj in [end,middle]:
 for v in obj.data.vertices:v.co=Vector((v.co.x*SCALE,(v.co.y-CUT)*SCALE,(v.co.z-DECK)*SCALE))
end_far=mirror_mesh(end,'Entrance_End')
support=crop('Support_Pier',-.10,.24)
bm=bmesh.new();bm.from_mesh(support.data)
bmesh.ops.bisect_plane(bm,geom=list(bm.verts)+list(bm.edges)+list(bm.faces),dist=1e-7,plane_co=(0,0,.135),plane_no=(0,0,1))
bmesh.ops.delete(bm,geom=[v for v in bm.verts if v.co.z>.135+1e-7],context='VERTS')
bm.to_mesh(support.data);bm.free()
for v in support.data.vertices:v.co=Vector((v.co.x*SCALE,(v.co.y-.10)*SCALE,(v.co.z-DECK)*SCALE))
bpy.data.objects.remove(source,do_unlink=True)
length=-2*CUT*SCALE
modules=[end,middle,end_far,support]
support.location.y=length*.5
end_far.location.y=length
# Export the three modules in their short-bridge assembly positions.
bpy.ops.object.select_all(action='DESELECT')
for o in modules:o.select_set(True)
bpy.context.view_layer.objects.active=middle
bpy.ops.export_scene.gltf(filepath=str(out/'covered-bridge-modules.glb'),export_format='GLB',use_selection=True)
# Duplicate shared middle geometry to demonstrate longer assemblies.
scene=bpy.context.scene;cam=scene.camera
scene.render.resolution_x=1500;scene.render.resolution_y=1000
scene.cycles.samples=32
lights=[o for o in scene.objects if o.type=='LIGHT']
def setup_camera(objects):
 points=[o.matrix_world@Vector(p) for o in objects for p in o.bound_box]
 lo=Vector([min(p[a] for p in points) for a in range(3)]);hi=Vector([max(p[a] for p in points) for a in range(3)])
 center=(lo+hi)/2;size=max(hi-lo)
 cam.location=center+Vector((1.4,-1.5,1.0))*size
 cam.rotation_euler=(center-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.ortho_scale=size*1.2;cam.data.clip_end=1000
 for index,l in enumerate(lights):
  l.location=center+Vector((1,-1,2) if index==0 else (-1,1,1))*size
  l.rotation_euler=(center-l.location).to_track_quat('-Z','Y').to_euler();l.data.energy=(450 if index==0 else 250)*size*size;l.data.size=size*1.5
report={'scale_meters_per_source_unit':SCALE,'deck_source_z':DECK,'middle_length_m':length,'modules':{}}
for o in modules:
 report['modules'][o.name]={'triangles':sum(len(p.vertices)-2 for p in o.data.polygons),'vertices':len(o.data.vertices)}
for bays in [1,3]:
 extras=[]
 for i in range(1,bays):
  o=bpy.data.objects.new('Middle_Bay_%02d'%i,middle.data);scene.collection.objects.link(o);o.location.y=i*length;extras.append(o)
  pier=bpy.data.objects.new('Support_Pier_%02d'%i,support.data);scene.collection.objects.link(pier);pier.location.y=(i+.5)*length;extras.append(pier)
 end_far.location.y=bays*length
 bpy.context.view_layer.update();setup_camera(modules+extras)
 scene.render.filepath=str(out/('bridge-%d-bays.png'%bays));bpy.ops.render.render(write_still=True)
 if bays==3:
  bpy.ops.wm.save_as_mainfile(filepath=str(out/'covered-bridge-modular.blend'))
 for o in extras:bpy.data.objects.remove(o,do_unlink=True)
end_far.location.y=length
# Verify cut profiles coincide at each seam (geometry position comparison).
def boundary(obj,coordinate):
 return {(round(v.co.x,5),round(v.co.z,5)) for v in obj.data.vertices if abs(v.co.y-coordinate)<1e-4}
start=boundary(middle,0);finish=boundary(middle,length);entrance=boundary(end,0);exit=boundary(end_far,0)
report['seams']={'middle_start_count':len(start),'middle_end_count':len(finish),'repeat_profiles_match':start==finish,'entrance_profile_matches':start==entrance,'far_entrance_profile_matches':finish==exit}
(out/'module-report.json').write_text(json.dumps(report,indent=2))
print('MODULE_REPORT',json.dumps(report))
