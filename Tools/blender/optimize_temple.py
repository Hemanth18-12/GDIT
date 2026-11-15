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
# the file contains a photo-studio backdrop (a curved floor and back wall far bigger than the temple): leave it out
big = max(mx.x - mn.x, mx.y - mn.y, mx.z - mn.z); backdrop = [o for o in meshes if max(o.dimensions) > 0.3 * big]
print("backdrop objects removed:", [(o.name, tuple(round(d) for d in o.dimensions)) for o in backdrop], flush=True)
for o in backdrop: meshes.remove(o); bpy.data.objects.remove(o, do_unlink=True)
total = 0; mn = Vector((1e9,) * 3); mx = Vector((-1e9,) * 3)
for o in meshes:
    total += len(o.data.loop_triangles)
    for c in o.bound_box:
        w = o.matrix_world @ Vector(c); mn = Vector(map(min, mn, w)); mx = Vector(map(max, mx, w))
H = mx.z - mn.z; SCALE = 62.0 / H
print("SRC tris=%d objects=%d height=%.1f scale=%.5f" % (total, len(meshes), H, SCALE), flush=True)

# 1) drop specks smaller than 6 cm in game units; remember every kept instance (mesh, transform, area cell)
speck = 0.08 / SCALE; dropped = 0; inst = []; kept_tris = 0
cx = (mx.x - mn.x) / 3; cy = (mx.y - mn.y) / 3
for o in meshes:
    if Vector(o.dimensions).length < speck: dropped += 1; continue
    c = sum((o.matrix_world @ Vector(b) for b in o.bound_box), Vector()) / 8.0   # real centre (origins all sit at the middle)
    gx = min(2, max(0, int((c.x - mn.x) / max(cx, 1e-6)))); gy = min(2, max(0, int((c.y - mn.y) / max(cy, 1e-6))))
    o.data.use_fake_user = True; inst.append((o.data, o.matrix_world.copy(), (gx, gy))); kept_tris += len(o.data.loop_triangles)
# 2) empty the scene so each decimate evaluates instantly, then reduce every unique mesh once
bpy.data.batch_remove([o for o in bpy.data.objects])
centre = Vector(((mn.x + mx.x) / 2, (mn.y + mx.y) / 2, mn.z)); G = Matrix.Scale(SCALE, 4) @ Matrix.Translation(-centre)
unique = list({id(m): m for m, _, _ in inst}.values())
cells = {}
for me, mw, cell in inst: cells.setdefault(cell, []).append((me, mw))
report = []

def build(target, base):
    ratio = min(1.0, target / max(kept_tris, 1))
    print("%s: target %d, ratio %.4f  %.0fs" % (base, target, ratio, time.time() - t0), flush=True)
    tmp = bpy.data.objects.new("tmp", unique[0]); bpy.context.scene.collection.objects.link(tmp)
    dec = {}
    for me in unique:
        me.calc_loop_triangles(); n = len(me.loop_triangles)
        r = 1.0 if n < 80 else min(1.0, max(ratio, 24.0 / n))
        if r < 0.98:
            tmp.data = me
            pl = tmp.modifiers.new("planar", 'DECIMATE'); pl.decimate_type = 'DISSOLVE'; pl.angle_limit = math.radians(1.5)   # flat walls first: keeps the silhouette
            d = tmp.modifiers.new("dec", 'DECIMATE'); d.ratio = min(1.0, r * 1.6); d.use_collapse_triangulate = True
            dg = bpy.context.evaluated_depsgraph_get(); nm = bpy.data.meshes.new_from_object(tmp.evaluated_get(dg)); tmp.modifiers.clear()
            for k, mt in enumerate(me.materials):
                if k < len(nm.materials): nm.materials[k] = mt
                else: nm.materials.append(mt)
            dec[id(me)] = nm
        else: dec[id(me)] = me
    bpy.data.objects.remove(tmp, do_unlink=True)
    root = bpy.data.objects.new(base, None); bpy.context.scene.collection.objects.link(root); made = [root]; out = 0
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
        for f in bm.faces:   # box UVs, 1 unit = 1 metre
            nn = f.normal; ax = max(range(3), key=lambda k: abs(nn[k]))
            for l in f.loops:
                co = l.vert.co; l[uv].uv = (co.y, co.z) if ax == 0 else (co.x, co.z) if ax == 1 else (co.x, co.y)
        name = "%s_Area_%d%d" % (base, gx, gy); me = bpy.data.meshes.new(name); bm.to_mesh(me); bm.free()
        for mt in slots: me.materials.append(mt)
        for p in me.polygons: p.use_smooth = True
        ob = bpy.data.objects.new(name, me); bpy.context.scene.collection.objects.link(ob); ob.parent = root; made.append(ob)
        me.calc_loop_triangles(); out += len(me.loop_triangles)
    for x in bpy.data.objects: x.select_set(x in made)
    bpy.ops.export_scene.fbx(filepath=os.path.join(OUT, base + ".fbx"), use_selection=True, apply_scale_options='FBX_SCALE_ALL', bake_space_transform=True, object_types={'MESH', 'EMPTY'}, mesh_smooth_type='EDGE', add_leaf_bones=False)
    for x in made: bpy.data.objects.remove(x, do_unlink=True)
    for nm in set(id(v) for v in dec.values()):
        pass
    for v in list(dec.values()):
        if not v.use_fake_user and v.users == 0: bpy.data.meshes.remove(v)
    print("%s: %d tris in %d area meshes  %.0fs" % (base, out, len(cells), time.time() - t0), flush=True)
    report.append("%s: %d triangles in %d area meshes" % (base, out, len(cells)))

build(TARGET, "Temple_Game")
build(int(TARGET * 0.2), "Temple_Game_LOD1")
with open(os.path.join(OUT, "Temple_Game_report.txt"), "w") as f:
    f.write("source: Art_Source/Temple/temple.fbx (user's file, unchanged): a kit of about 25 temple buildings in rows, plus a studio backdrop 'Floor' (removed)\n")
    f.write("source tris: %d in %d objects (436 unique meshes, instanced), temple height %.1f m\n" % (total, len(meshes), H * SCALE))
    f.write("\n".join(report) + "\nLOD1 is also the collision mesh. Bounds: %.1f x %.1f x %.1f m\n" % ((mx.x - mn.x) * SCALE, (mx.y - mn.y) * SCALE, H * SCALE))
print("TEMPLE DONE %.0fs" % (time.time() - t0), flush=True)
