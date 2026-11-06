import bpy
import sys

bpy.ops.wm.read_factory_settings(use_empty=True)
path = sys.argv[-1]
bpy.ops.import_scene.fbx(filepath=path)

from mathutils import Vector
for obj in bpy.data.objects:
    if obj.type == 'MESH':
        bbox = [obj.matrix_world @ Vector(obj.bound_box[i]) for i in range(8)]
        min_z = min(v.z for v in bbox)
        max_z = max(v.z for v in bbox)
        min_y = min(v.y for v in bbox)
        max_y = max(v.y for v in bbox)
        min_x = min(v.x for v in bbox)
        max_x = max(v.x for v in bbox)
        print(f"File: {path}")
        print(f"Mesh: {obj.name}")
        print(f"Z range: {min_z:.2f} to {max_z:.2f} (Height: {max_z - min_z:.2f}m)")
        print(f"X range: {min_x:.2f} to {max_x:.2f} (Width: {max_x - min_x:.2f}m)")
        print(f"Y range: {min_y:.2f} to {max_y:.2f} (Depth: {max_y - min_y:.2f}m)")
