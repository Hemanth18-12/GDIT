import bpy
import sys
import os

print("\n=======================================================")
print("FILE:", bpy.data.filepath)
print("=======================================================")

print("--- ARMATURES ---")
for arm in bpy.data.armatures:
    print(f"Armature: '{arm.name}', Bones: {len(arm.bones)}")
    for b in arm.bones[:15]:
        print(f"   bone: {b.name}")

print("\n--- ACTIONS (ANIMATIONS) ---")
for act in bpy.data.actions:
    print(f"Action: '{act.name}', Frame Range: {act.frame_range}")

print("\n--- ROOT OBJECTS ---")
for obj in bpy.data.objects:
    if not obj.parent:
        child_count = len(obj.children)
        print(f"Root Object: '{obj.name}' ({obj.type}), Children: {child_count}")

print("\n--- MESH TOTALS ---")
total_polys = sum(len(o.data.polygons) for o in bpy.data.objects if o.type == 'MESH')
print(f"Total Meshes: {len([o for o in bpy.data.objects if o.type == 'MESH'])}, Total Polys: {total_polys}")

