import bpy, sys, os
ROOT = sys.argv[sys.argv.index("--") + 1]; D = os.path.join(ROOT, "Assets/_Project/Models/Temple")
bpy.ops.wm.read_factory_settings(use_empty=True); bpy.ops.import_scene.fbx(filepath=os.path.join(D, "Temple_Game_LOD2.fbx"))
objs = [o for o in bpy.data.objects if o.type == 'MESH']
for o in objs:
    o.data.calc_loop_triangles(); before = len(o.data.loop_triangles)
    bpy.context.view_layer.objects.active = o
    for o2 in objs: o2.select_set(o2 == o)
    bpy.ops.object.mode_set(mode='EDIT'); bpy.ops.mesh.select_all(action='SELECT'); bpy.ops.mesh.remove_doubles(threshold=0.03); bpy.ops.object.mode_set(mode='OBJECT')
    md = o.modifiers.new("dec", 'DECIMATE'); md.ratio = 0.22
    dg = bpy.context.evaluated_depsgraph_get(); me = bpy.data.meshes.new_from_object(o.evaluated_get(dg)); o.modifiers.clear(); o.data = me
    o.name = o.name.replace("LOD2", "LOD3")
tris = 0
for o in objs: o.data.calc_loop_triangles(); tris += len(o.data.loop_triangles)
for o in bpy.data.objects: o.select_set(o.type == 'MESH')
bpy.ops.export_scene.fbx(filepath=os.path.join(D, "Temple_Game_LOD3.fbx"), use_selection=True, apply_scale_options='FBX_SCALE_ALL', bake_space_transform=True, object_types={'MESH'}, mesh_smooth_type='FACE')
print("TEMPLE_LOD LOD3", tris)
