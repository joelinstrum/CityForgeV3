"""Export the supplied FBX as its two unwarped end sections and one stretchable center."""
import bpy,bmesh,json,shutil
from pathlib import Path
from mathutils import Vector
source_root=Path('/Users/joelinstrum/Downloads/buildings/bridges/stone-bridge-review-v01')
asset_root=Path('/Users/joelinstrum/dev/CityForge-workspaces/quarry-and-performance-updates/Assets/CityForgeV3/Resources/CityForgeV3/Bridges/StoneV02')
bpy.ops.wm.open_mainfile(filepath=str(source_root/'bridge-review.blend'))
source=next(o for o in bpy.context.scene.objects if o.type=='MESH')
base=source.data.copy();base.transform(source.matrix_world)
minimum=min(v.co.x for v in base.vertices);maximum=max(v.co.x for v in base.vertices)
left_cut=-.18;right_cut=.19;scale=35.0
inv=source.matrix_world.inverted()
def ray_height(x):
    origin=Vector((x,0,1));direction=Vector((0,0,-1))
    hit,p,normal,face=source.ray_cast(inv@origin,inv.to_3x3()@direction)
    if not hit: return None
    return (source.matrix_world@p).z*scale
heights=[ray_height(minimum+.005+(maximum-minimum-.01)*i/200) for i in range(201)]
valid=[i for i,h in enumerate(heights) if h is not None]
for i,h in enumerate(heights):
    if h is None:heights[i]=heights[min(valid,key=lambda j:abs(j-i))]
def crop(name,low,high):
    mesh=base.copy();bm=bmesh.new();bm.from_mesh(mesh)
    for bound,keep_positive in [(low,True),(high,False)]:
        if bound is None:continue
        bmesh.ops.bisect_plane(bm,geom=list(bm.verts)+list(bm.edges)+list(bm.faces),dist=1e-7,plane_co=(bound,0,0),plane_no=(1,0,0))
        bmesh.ops.delete(bm,geom=[v for v in bm.verts if v.co.x < bound-1e-7 if keep_positive] if keep_positive else [v for v in bm.verts if v.co.x > bound+1e-7],context='VERTS')
    bm.normal_update();bm.to_mesh(mesh);bm.free()
    obj=bpy.data.objects.new(name,mesh);bpy.context.scene.collection.objects.link(obj)
    for v in mesh.vertices:
        x,y,z=v.co;v.co=Vector((-y*scale,(x-left_cut)*scale,z*scale))
    return obj
parts=[crop('Entrance_Start',None,left_cut),crop('Middle_Bay',left_cut,right_cut),crop('Entrance_End',right_cut,None)]
package={'bayLength':(right_cut-left_cut)*scale,'capLength':(left_cut-minimum)*scale,
         'rightCapLength':(maximum-right_cut)*scale,'singleMiddle':True,
         'sourceMin':minimum,'sourceMax':maximum,'leftCut':left_cut,'rightCut':right_cut,
         'leftEndDeck':heights[0],'rightEndDeck':heights[-1],
         'deckHeights':heights,'modules':[]}
for obj in parts:
    mesh=obj.data;mesh.calc_loop_triangles();uv=mesh.uv_layers.active.data
    verts=[];norms=[];uvs=[];triangles=[];lookup={}
    for triangle in mesh.loop_triangles:
        indices=[]
        for li in triangle.loops:
            v=mesh.vertices[mesh.loops[li].vertex_index].co;n=mesh.corner_normals[li].vector;u=uv[li].uv
            key=tuple(round(c,6) for c in (*v,*n,*u))
            if key not in lookup:
                lookup[key]=len(verts)
                verts.append({'x':v.x,'y':v.z,'z':v.y})
                norms.append({'x':n.x,'y':n.z,'z':n.y})
                uvs.append({'x':u.x,'y':u.y})
            indices.append(lookup[key])
        triangles.extend(indices[::-1])
    package['modules'].append({'name':obj.name,'vertices':verts,'normals':norms,'uv':uvs,'triangles':triangles})
used={i for polygon in base.polygons for i in polygon.vertices}
for obj,is_left in [(parts[0],True),(parts[2],False)]:
    kept=[v.co for v in base.vertices if v.index in used and (v.co.x<left_cut-1e-6 if is_left else v.co.x>right_cut+1e-6)]
    from mathutils.kdtree import KDTree
    tree=KDTree(len(obj.data.vertices))
    for index,v in enumerate(obj.data.vertices):tree.insert(v.co,index)
    tree.balance()
    distances=[tree.find(Vector((-v.y*scale,(v.x-left_cut)*scale,v.z*scale)))[2] for v in kept]
    worst=max(distances,default=0)
    if worst>1e-4:raise RuntimeError(f'{obj.name}: source vertices moved (maximum {worst} m)')
    print(f'{obj.name}: {len(kept)} source vertices retained at their original positions (rigid coordinate conversion only)')
asset_root.mkdir(parents=True,exist_ok=True)
(asset_root/'modules.json').write_text(json.dumps(package,separators=(',',':')))
albedo=next((source_root/'Source').rglob('tripo_image_b7d9247b_0.jpg'))
shutil.copyfile(albedo,asset_root/'albedo.jpg')
shutil.copyfile(source_root/'overview.png',asset_root/'preview.png')
print(json.dumps({'source_bounds':[minimum,maximum],'cuts':[left_cut,right_cut],
  'cap_lengths':[package['capLength'],package['rightCapLength']],
  'middle_length':package['bayLength'],'end_deck_heights':[heights[0],heights[-1]],
  'triangles':[len(p['triangles'])//3 for p in package['modules']]},indent=2))
