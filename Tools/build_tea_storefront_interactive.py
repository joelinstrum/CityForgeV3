import bpy
import bmesh
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "Assets/CityForgeV3/Resources/CityForgeV3/BuildingProps/Storefronts/TeaShopV01/Models/TeaShopStorefrontV01.fbx"
OUTPUT = ROOT / "Assets/CityForgeV3/Resources/CityForgeV3/BuildingProps/Storefronts/TeaShopV01/Models/TeaShopStorefrontInteractiveV01.fbx"

# Bounds taken from the separately supplied component model. Keeping the cut
# inside the jamb and threshold preserves the polished model's frame and UVs.
DOOR_MIN_X = -0.132
DOOR_MAX_X = 0.062
DOOR_MIN_Z = 0.032
DOOR_MAX_Z = 0.438


def face_is_door(face):
    center = face.calc_center_median()
    return (DOOR_MIN_X <= center.x <= DOOR_MAX_X and
            DOOR_MIN_Z <= center.z <= DOOR_MAX_Z)


def retain_faces(obj, retain_door):
    mesh = obj.data
    bm = bmesh.new()
    bm.from_mesh(mesh)
    remove = [face for face in bm.faces
              if face_is_door(face) != retain_door]
    bmesh.ops.delete(bm, geom=remove, context="FACES")
    orphaned = [vertex for vertex in bm.verts if not vertex.link_faces]
    if orphaned:
        bmesh.ops.delete(bm, geom=orphaned, context="VERTS")
    bm.to_mesh(mesh)
    bm.free()
    mesh.update()


bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=str(SOURCE))
storefront = next(obj for obj in bpy.context.scene.objects
                  if obj.type == "MESH")
storefront.name = "CF_STOREFRONT_BODY"

door = storefront.copy()
door.data = storefront.data.copy()
bpy.context.collection.objects.link(door)
door.name = "CF_STOREFRONT_DOOR"
retain_faces(storefront, False)
retain_faces(door, True)

bpy.ops.object.empty_add(type="PLAIN_AXES",
                         location=(DOOR_MIN_X, 0.0, DOOR_MIN_Z))
hinge = bpy.context.object
hinge.name = "CF_STOREFRONT_DOOR_HINGE"
door.parent = hinge
door.matrix_parent_inverse = hinge.matrix_world.inverted()

# A shallow dark backing prevents the host building texture from showing
# through the doorway while the door is open. Runtime assigns this renderer a
# warm interior material instead of the storefront atlas.
interior_mesh = bpy.data.meshes.new("CF_STOREFRONT_INTERIOR_MESH")
interior_mesh.from_pydata(
    [
        (DOOR_MIN_X, 0.092, DOOR_MIN_Z),
        (DOOR_MAX_X, 0.092, DOOR_MIN_Z),
        (DOOR_MAX_X, 0.092, DOOR_MAX_Z),
        (DOOR_MIN_X, 0.092, DOOR_MAX_Z),
    ],
    [],
    [(0, 1, 2, 3)],
)
interior = bpy.data.objects.new("CF_STOREFRONT_INTERIOR", interior_mesh)
bpy.context.collection.objects.link(interior)

bpy.ops.object.select_all(action="SELECT")
bpy.ops.export_scene.fbx(
    filepath=str(OUTPUT),
    use_selection=True,
    add_leaf_bones=False,
    bake_anim=False,
    path_mode="COPY",
)
print(f"Exported interactive storefront to {OUTPUT}")
