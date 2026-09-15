"""Run in background Blender. Preserve the source quarry; remove its fused upper crane for the runtime jib."""
import bpy, bmesh, json, sys
from pathlib import Path
args=sys.argv[sys.argv.index('--')+1:]
source=Path(args[0]); output=Path(args[1]);output.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(source))
objects=[o for o in bpy.context.scene.objects if o.type=='MESH' and len(o.data.polygons)>100]
assert len(objects)==1
obj=objects[0]
bpy.ops.object.select_all(action='DESELECT');obj.select_set(True);bpy.context.view_layer.objects.active=obj
bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
bm=bmesh.new();bm.from_mesh(obj.data)
# Registered Blender meters (Unity local = -x,z,-y). Retain the stone platform and lower trestle.
faces=[f for f in bm.faces if -3.9<f.calc_center_median().x<.4 and -2.8<f.calc_center_median().y<3.5 and f.calc_center_median().z>1.4]
removed=len(faces);assert 100<removed<1800,removed
bmesh.ops.delete(bm,geom=faces,context='FACES');bm.to_mesh(obj.data);bm.free();obj.data.update();obj.name='QuarryBaseCraneV02'
bpy.ops.export_scene.fbx(filepath=str(output/'QuarryBase.fbx'),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',bake_anim=False,add_leaf_bones=False)
(output/'lineage.json').write_text(json.dumps({'source':str(source),'operation':'Remove fused upper crane faces for separate runtime swivel jib; preserve original UV atlas, stone platform, lower trestle, and quarry grounds','removed_faces':removed,'remaining_faces':len(obj.data.polygons),'script':'Tools/Quarry/derive_crane_base.py'},indent=2)+'\n')
print('CRANE_BASE_DERIVED',removed)
