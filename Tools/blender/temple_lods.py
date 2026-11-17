# Two lighter versions of the game temple for distance viewing (about 20 % and 5 % of the triangles).
# Reads Assets/_Project/Models/Temple/Temple_Game.fbx (the optimised game copy; the user's temple.fbx is never opened).
import bpy, sys, os
ROOT = sys.argv[sys.argv.index("--") + 1]; D = os.path.join(ROOT, "Assets/_Project/Models/Temple")
for tag, ratio in (("LOD2", 0.2),):   # LOD3 is made from LOD2 by temple_lod3.py (collapse stalls below 20 % on this mesh)
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=os.path.join(D, "Temple_Game.fbx"))
    tris = 0; objs = [o for o in bpy.data.objects if o.type == 'MESH']
    for o in objs:
        md = o.modifiers.new("dec", 'DECIMATE'); md.ratio = ratio; md.use_collapse_triangulate = True
    dg = bpy.context.evaluated_depsgraph_get()
    for o in objs:
        me = bpy.data.meshes.new_from_object(o.evaluated_get(dg)); o.modifiers.clear(); old = o.data; o.data = me; bpy.data.meshes.remove(old)
        me.calc_loop_triangles(); tris += len(me.loop_triangles); o.name = o.name.replace("Temple_Game_", "Temple_Game_" + tag + "_")
    for o in bpy.data.objects: o.select_set(o.type == 'MESH')
    bpy.ops.export_scene.fbx(filepath=os.path.join(D, f"Temple_Game_{tag}.fbx"), use_selection=True, apply_scale_options='FBX_SCALE_ALL', bake_space_transform=True, object_types={'MESH'}, mesh_smooth_type='FACE')
    print("TEMPLE_LOD", tag, len(objs), "objects", tris, "triangles")
