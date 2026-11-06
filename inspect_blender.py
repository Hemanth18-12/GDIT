import bpy
import sys

print("=== BLENDER ASSET INSPECTION SCRIPT ===")
print("File:", bpy.data.filepath)

print("\n--- COLLECTIONS ---")
for col in bpy.data.collections:
    print(f"Collection: {col.name} (objects: {len(col.objects)})")

print("\n--- ARMATURES & BONES ---")
for arm in bpy.data.armatures:
    print(f"Armature: {arm.name}, Bone Count: {len(arm.bones)}")
    bone_names = [b.name for b in arm.bones[:10]]
    print(f"   Sample bones: {bone_names}")

print("\n--- ACTIONS (ANIMATIONS) ---")
for act in bpy.data.actions:
    curves_count = 0
    try:
        curves_count = len(act.fcurves)
    except:
        pass
    print(f"Action: {act.name}, Frame Range: {act.frame_range}, Curves: {curves_count}")

print("\n--- MESH OBJECTS & POLY COUNTS ---")
for obj in bpy.data.objects:
    if obj.type == 'MESH':
        poly_count = len(obj.data.polygons)
        vert_count = len(obj.data.vertices)
        mats = [m.name for m in obj.data.materials if m]
        parent = obj.parent.name if obj.parent else "None"
        print(f"Mesh: {obj.name} | Polys: {poly_count} | Verts: {vert_count} | Parent: {parent} | Mats: {mats}")
    elif obj.type == 'ARMATURE':
        print(f"Armature Object: {obj.name} | Parent: {obj.parent.name if obj.parent else 'None'}")

print("\n--- MATERIALS ---")
for mat in bpy.data.materials:
    textures = []
    if mat.use_nodes and mat.node_tree:
        for node in mat.node_tree.nodes:
            if node.type == 'TEX_IMAGE' and node.image:
                textures.append(node.image.name)
    print(f"Material: {mat.name} | Textures: {textures}")

print("\n--- IMAGES ---")
for img in bpy.data.images:
    print(f"Image: {img.name} | Size: {img.size[0]}x{img.size[1]} | Filepath: {img.filepath}")

print("\n=== END OF INSPECTION ===")
