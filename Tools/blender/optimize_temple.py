# Makes a game-ready copy of the user's temple.fbx (the original is never changed).
# Run: Blender --background --python Tools/blender/optimize_temple.py -- <in.fbx> <out_dir> [target_tris]
# Steps: reduce each object's triangles, drop specks too small to see, merge by material and 3x3 area chunks
# (so Unity can cull what is off screen), recalculate normals, add box UVs in metres, scale to a 62 m tower.
import bpy, bmesh, sys, os, math, time
from mathutils import Vector, Matrix

a = sys.argv[sys.argv.index("--") + 1:]
SRC, OUT = a[0], a[1]; TARGET = int(a[2]) if len(a) > 2 else 900000
os.makedirs(OUT, exist_ok=True); t0 = time.time()
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=SRC)
meshes = [o for o in bpy.data.objects if o.type == 'MESH']
total = 0
for o in meshes: o.data.calc_loop_triangles(); total += len(o.data.loop_triangles)
mn = Vector((1e9,) * 3); mx = Vector((-1e9,) * 3)
for o in meshes:
    for c in o.bound_box:
        w = o.matrix_world @ Vector(c); mn = Vector(map(min, mn, w)); mx = Vector(map(max, mx, w))
H = mx.z - mn.z; SCALE = 62.0 / H
print("SRC tris=%d objects=%d height=%.1f scale=%.5f" % (total, len(meshes), H, SCALE), flush=True)

# 1) drop specks smaller than 6 cm in game units; remember every kept instance (mesh, transform, area cell)
speck = 0.06 / SCALE; dropped = 0; inst = []; kept_tris = 0
cx = (mx.x - mn.x) / 3; cy = (mx.y - mn.y) / 3
for o in meshes:
    if Vector(o.dimensions).length < speck: dropped += 1; continue
    c = o.matrix_world.translation
    gx = min(2, max(0, int((c.x - mn.x) / max(cx, 1e-6)))); gy = min(2, max(0, int((c.y - mn.y) / max(cy, 1e-6))))
    o.data.use_fake_user = True; inst.append((o.data, o.matrix_world.copy(), (gx, gy))); kept_tris += len(o.data.loop_triangles)
# 2) empty the scene so each decimate evaluates instantly, then reduce every unique mesh once
bpy.data.batch_remove([o for o in bpy.data.objects])
ratio = min(1.0, TARGET / max(kept_tris, 1))
print("kept %d instances (%d tris), dropped %d specks, ratio %.4f  %.0fs" % (len(inst), kept_tris, dropped, ratio, time.time() - t0), flush=True)
unique = list({id(m): m for m, _, _ in inst}.values())
tmp = bpy.data.objects.new("tmp", unique[0]); bpy.context.scene.collection.objects.link(tmp)
dec = {}
for i, me in enumerate(unique):
    me.calc_loop_triangles(); n = len(me.loop_triangles)
    r = 1.0 if n < 120 else min(1.0, max(ratio, 60.0 / n))
    if r < 0.98:
        tmp.data = me; d = tmp.modifiers.new("dec", 'DECIMATE'); d.ratio = r; d.use_collapse_triangulate = True
        dg = bpy.context.evaluated_depsgraph_get(); nm = bpy.data.meshes.new_from_object(tmp.evaluated_get(dg)); tmp.modifiers.clear()
        for k, mt in enumerate(me.materials):
            if k < len(nm.materials): nm.materials[k] = mt
            else: nm.materials.append(mt)
        dec[id(me)] = nm
    else: dec[id(me)] = me
    if i % 1000 == 0: print("  reduced %d/%d unique meshes  %.0fs" % (i, len(unique), time.time() - t0), flush=True)
bpy.data.objects.remove(tmp, do_unlink=True)
# 3) merge by area cell, keeping per-face materials
centre = Vector(((mn.x + mx.x) / 2, (mn.y + mx.y) / 2, mn.z))
root = bpy.data.objects.new("Temple", None); bpy.context.scene.collection.objects.link(root)
out_tris = 0; cells = {}
for me, mw, cell in inst: cells.setdefault(cell, []).append((me, mw))
G = Matrix.Scale(SCALE, 4) @ Matrix.Translation(-centre)
for (gx, gy), items in cells.items():
    bm = bmesh.new(); slots = []
    for me, mw in items:
        src = dec[id(me)]; cp = src.copy(); cp.transform(G @ mw)
        remap = []
        for mt in src.materials:
            if mt not in slots: slots.append(mt)
            remap.append(slots.index(mt))
        if remap:
            for p in cp.polygons: p.material_index = remap[min(p.material_index, len(remap) - 1)]
        bm.from_mesh(cp); bpy.data.meshes.remove(cp)
    bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=0.002)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    uv = bm.loops.layers.uv.verify()
    for f in bm.faces:   # box UVs, 1 unit = 1 metre, so tiling textures keep their real size
        nn = f.normal; ax = max(range(3), key=lambda k: abs(nn[k]))
        for l in f.loops:
            co = l.vert.co; l[uv].uv = (co.y, co.z) if ax == 0 else (co.x, co.z) if ax == 1 else (co.x, co.y)
    name = "Temple_Area_%d%d" % (gx, gy); me = bpy.data.meshes.new(name); bm.to_mesh(me); bm.free()
    for mt in slots: me.materials.append(mt)
    for p in me.polygons: p.use_smooth = False
    ob = bpy.data.objects.new(name, me); bpy.context.scene.collection.objects.link(ob); ob.parent = root
    me.calc_loop_triangles(); out_tris += len(me.loop_triangles)
    print("  %s: %d tris, %d materials  %.0fs" % (name, len(me.loop_triangles), len(slots), time.time() - t0), flush=True)
groups = cells
for me in list(bpy.data.meshes):
    if me.users == 0: bpy.data.meshes.remove(me)
print("OUT tris=%d meshes=%d dropped_specks=%d time=%.0fs" % (out_tris, len(groups), dropped, time.time() - t0), flush=True)

for o in bpy.data.objects: o.select_set(o.type in ('MESH', 'EMPTY'))
bpy.ops.export_scene.fbx(filepath=os.path.join(OUT, "Temple_Game.fbx"), use_selection=True, apply_scale_options='FBX_SCALE_ALL',
                         bake_space_transform=True, object_types={'MESH', 'EMPTY'}, mesh_smooth_type='FACE', add_leaf_bones=False)
with open(os.path.join(OUT, "Temple_Game_report.txt"), "w") as f:
    f.write("source: Art_Source/Temple/temple.fbx (user's file, unchanged)\nsource tris: %d in %d objects, height %.1f units\n" % (total, len(meshes), H))
    f.write("game copy: %d tris in %d meshes (material x 3x3 area), specks dropped: %d, scale %.5f (tower = 62 m)\n" % (out_tris, len(groups), dropped, SCALE))
    f.write("bounds after scaling: %.1f x %.1f x %.1f m\n" % ((mx.x - mn.x) * SCALE, (mx.y - mn.y) * SCALE, H * SCALE))
print("TEMPLE DONE %.0fs" % (time.time() - t0), flush=True)
