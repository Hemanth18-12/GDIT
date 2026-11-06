import bpy
import sys

# clean scene
bpy.ops.wm.read_factory_settings(use_empty=True)

path = sys.argv[-1]
print("====================================================")
print("INSPECTING FILE:", path)

if path.endswith(".glb") or path.endswith(".gltf"):
    bpy.ops.import_scene.gltf(filepath=path)
elif path.endswith(".fbx"):
    bpy.ops.import_scene.fbx(filepath=path)

print("--- ARMATURES ---")
for arm in bpy.data.armatures:
    print(f"Armature: '{arm.name}', Bone Count: {len(arm.bones)}")
    sample = [b.name for b in arm.bones[:15]]
    print(f"   Sample bones: {sample}")

print("--- ACTIONS (ANIMATIONS) ---")
for act in bpy.data.actions:
    print(f"Action: '{act.name}', Frame Range: {act.frame_range}")

print("--- MESHES ---")
for obj in bpy.data.objects:
    if obj.type == 'MESH':
        poly_count = len(obj.data.polygons)
        vert_count = len(obj.data.vertices)
        mats = [m.name for m in obj.data.materials if m]
        print(f"Mesh: '{obj.name}' | Polys: {poly_count} | Verts: {vert_count} | Parent: {obj.parent.name if obj.parent else 'None'} | Mats: {mats}")

print("====================================================")
