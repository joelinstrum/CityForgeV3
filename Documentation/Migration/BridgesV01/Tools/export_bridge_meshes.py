import bpy,bmesh,json,shutil,hashlib,math
from pathlib import Path
from mathutils import Vector
repo=Path('/Users/joelinstrum/dev/CityForge-workspaces/quarry-and-performance-updates')
root=Path('/Users/joelinstrum/Downloads/buildings/bridges')
assets=repo/'Assets/CityForgeV3/Resources/CityForgeV3/Bridges'
def export(objects,path,bay,cap):
 result={'bayLength':bay,'capLength':cap,'modules':[]}
 for name,o in objects:
  m=o.data;m.calc_loop_triangles();uv=m.uv_layers.active.data;verts=[];norms=[];uvs=[];tri=[];keys={}
  for t in m.loop_triangles:
   indices=[]
   for li in t.loops:
    p=m.vertices[m.loops[li].vertex_index].co;n=m.corner_normals[li].vector;u=uv[li].uv
    key=tuple(round(v,6) for v in (*p,*n,*u))
    if key not in keys:
     keys[key]=len(verts);verts.append({'x':p.x,'y':p.z,'z':p.y});norms.append({'x':n.x,'y':n.z,'z':n.y});uvs.append({'x':u.x,'y':u.y})
    indices.append(keys[key])
   tri.extend(indices[::-1])
  result['modules'].append({'name':name,'vertices':verts,'normals':norms,'uv':uvs,'triangles':tri})
 (path/'modules.json').write_text(json.dumps(result,separators=(',',':')))
 print(path.name,[(m['name'],len(m['vertices']),len(m['triangles'])//3) for m in result['modules']])
bpy.ops.wm.open_mainfile(filepath=str(root/'covered-wooden-bridge-modular-v01/covered-bridge-modular.blend'))
objects=[(n,bpy.data.objects[n]) for n in ['Entrance_Start','Middle_Bay','Entrance_End','Support_Pier']]
cap=-min(v.co.y for v in objects[0][1].data.vertices)
export(objects,assets/'CoveredWoodenV01',14.4,cap)
shutil.copyfile(next((root/'covered-wooden-bridge-review-v01/Source').rglob('tripo_image_439c97c5_0.jpg')),assets/'CoveredWoodenV01/albedo.jpg')
shutil.copyfile(root/'covered-wooden-bridge-modular-v01/bridge-1-bays.png',assets/'CoveredWoodenV01/preview.png')
bpy.ops.wm.open_mainfile(filepath=str(root/'stone-bridge-review-v01/bridge-review.blend'))
source=next(o for o in bpy.context.scene.objects if o.type=='MESH');base=source.data.copy();base.transform(source.matrix_world)
# Sample the source deck before adapting its irregular slope to a repeatable level span.
inv=source.matrix_world.inverted();samples=[]
for i in range(201):
 x=-.49+i*.98/200;r=source.ray_cast(inv@Vector((x,0,1)),inv.to_3x3()@Vector((0,0,-1)))
 if r[0]:samples.append((x,(source.matrix_world@r[1]).z))
def deck(x):
 if x<=samples[0][0]:return samples[0][1]
 for (a,h),(b,j) in zip(samples,samples[1:]):
  if x<=b:return h+(j-h)*(x-a)/(b-a)
 return samples[-1][1]
for v in base.vertices:
 x,y,z=v.co;v.co=Vector((-y,x,z-deck(x)))
def crop(name,low,high):
 m=base.copy();bm=bmesh.new();bm.from_mesh(m)
 for bound,lower in [(low,True),(high,False)]:
  if bound is None:continue
  bmesh.ops.bisect_plane(bm,geom=list(bm.verts)+list(bm.edges)+list(bm.faces),dist=1e-7,plane_co=(0,bound,0),plane_no=(0,1,0))
  bmesh.ops.delete(bm,geom=[v for v in bm.verts if (v.co.y<bound-1e-7 if lower else v.co.y>bound+1e-7)],context='VERTS')
 bm.normal_update();bm.to_mesh(m);bm.free();o=bpy.data.objects.new(name,m);bpy.context.scene.collection.objects.link(o);return o
def mirror(o,name):
 m=o.data.copy();bm=bmesh.new();bm.from_mesh(m)
 for v in bm.verts:v.co.y=-v.co.y
 bmesh.ops.reverse_faces(bm,faces=list(bm.faces));bm.normal_update();bm.to_mesh(m);bm.free()
 o=bpy.data.objects.new(name,m);bpy.context.scene.collection.objects.link(o);return o
start=crop('Entrance_Start',None,-.30);middle=crop('Middle_Bay',-.30,0);other=mirror(middle,'half')
bpy.ops.object.select_all(action='DESELECT');middle.select_set(True);other.select_set(True);bpy.context.view_layer.objects.active=middle;bpy.ops.object.join()
bm=bmesh.new();bm.from_mesh(middle.data);bmesh.ops.remove_doubles(bm,verts=[v for v in bm.verts if abs(v.co.y)<1e-6],dist=1e-6);bm.normal_update();bm.to_mesh(middle.data);bm.free()
for o in [start,middle]:
 for v in o.data.vertices:v.co=Vector((v.co.x*35,(v.co.y+.30)*35,v.co.z*35))
end=mirror(start,'Entrance_End');cap=-min(v.co.y for v in start.data.vertices)
export([('Entrance_Start',start),('Middle_Bay',middle),('Entrance_End',end)],assets/'StoneV01',21,cap)
bpy.data.objects.remove(source,do_unlink=True);end.location.y=21
bpy.ops.wm.save_as_mainfile(filepath=str(root/'stone-bridge-review-v01/stone-modular.blend'))
shutil.copyfile(next((root/'stone-bridge-review-v01/Source').rglob('tripo_image_b7d9247b_0.jpg')),assets/'StoneV01/albedo.jpg')
shutil.copyfile(root/'stone-bridge-review-v01/overview.png',assets/'StoneV01/preview.png')
