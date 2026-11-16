# V3 hero props (own work): carved stone pillar, wall coping and plinth mouldings, brass lamp stand, round arch.
# Blender --background --python Tools/blender/build_v3_props.py -- <repo root>
import bpy, bmesh, math, sys, os
from mathutils import Vector, Matrix
ROOT = sys.argv[sys.argv.index("--") + 1]
OUT = os.path.join(ROOT, "Assets/_Project/Resources/Props/V3")
bpy.ops.wm.read_factory_settings(use_empty=True)
mats = {n: bpy.data.materials.new(n) for n in ("MAT_V3_Stone", "MAT_V3_Brass", "MAT_V3_Wood")}

def lathe(bm, prof, seg, mat_index=0, flutes=0, depth=0.0):
    rings = []
    for z, r in prof:
        ring = []
        for i in range(seg):
            a = 2 * math.pi * i / seg; rr = r - (depth if flutes and (i % (seg // flutes)) == 0 and 0.35 < z else 0.0)
            ring.append(bm.verts.new((rr * math.cos(a), rr * math.sin(a), z)))
        rings.append(ring)
    for ra, rb in zip(rings[:-1], rings[1:]):
        for i in range(seg):
            j = (i + 1) % seg; bm.faces.new([ra[i], ra[j], rb[j], rb[i]]).material_index = mat_index
    bm.faces.new(rings[0][::-1]).material_index = mat_index; bm.faces.new(rings[-1]).material_index = mat_index

def box(bm, x0, x1, y0, y1, z0, z1, mi=0):
    vs = [bm.verts.new((x, y, z)) for z in (z0, z1) for y in (y0, y1) for x in (x0, x1)]
    for f in ((0, 1, 3, 2), (4, 6, 7, 5), (0, 4, 5, 1), (2, 3, 7, 6), (0, 2, 6, 4), (1, 5, 7, 3)): bm.faces.new([vs[i] for i in f]).material_index = mi

def prism(bm, prof, x0, x1, mi=0):
    """profile in (y, z), extruded along x"""
    A = [bm.verts.new((x0, y, z)) for y, z in prof]; B = [bm.verts.new((x1, y, z)) for y, z in prof]
    bm.faces.new(A).material_index = mi; bm.faces.new(B[::-1]).material_index = mi
    for i in range(len(prof)):
        j = (i + 1) % len(prof); bm.faces.new([A[i], B[i], B[j], A[j]]).material_index = mi

def finish(name, bm, mlist, uv_scale=1.0):
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    me = bpy.data.meshes.new(name); bm.to_mesh(me); bm.free()
    for m in mlist: me.materials.append(mats[m])
    o = bpy.data.objects.new(name, me); bpy.context.scene.collection.objects.link(o)
    for p in me.polygons: p.use_smooth = True
    me.set_sharp_from_angle(angle=math.radians(35))
    uvl = me.uv_layers.new(name="UV0").data
    for poly in me.polygons:
        n = poly.normal
        for li in poly.loop_indices:
            v = me.vertices[me.loops[li].vertex_index].co
            u, w = (v.y, v.z) if abs(n.x) > 0.6 else (v.x, v.z) if abs(n.y) > 0.6 else (v.x, v.y)
            uvl[li].uv = (u * uv_scale, w * uv_scale)
    return o

def export(o):
    for x in bpy.data.objects: x.select_set(x == o)
    bpy.ops.export_scene.fbx(filepath=os.path.join(OUT, o.name + ".fbx"), use_selection=True, apply_scale_options='FBX_SCALE_ALL', bake_space_transform=True, object_types={'MESH'}, mesh_smooth_type='FACE')

made = []
# 1. carved pillar, 2.8 m: square plinth, octagonal fluted shaft with bands, pot-and-foliage capital, square abacus
bm = bmesh.new(); box(bm, -0.32, 0.32, -0.32, 0.32, 0.0, 0.18); box(bm, -0.27, 0.27, -0.27, 0.27, 0.18, 0.32)
lathe(bm, [(0.32, 0.2), (0.36, 0.22), (0.4, 0.19), (1.15, 0.17), (1.17, 0.2), (1.23, 0.2), (1.25, 0.17), (2.15, 0.16), (2.2, 0.21), (2.3, 0.24), (2.38, 0.2), (2.42, 0.15)], 16, 0, 8, 0.02)
lathe(bm, [(2.42, 0.15), (2.5, 0.25), (2.56, 0.27), (2.6, 0.22)], 16)
box(bm, -0.33, 0.33, -0.33, 0.33, 2.6, 2.76); box(bm, -0.38, 0.38, -0.38, 0.38, 2.76, 2.82)
made.append(finish("SM_V3_Pillar", bm, ["MAT_V3_Stone"], 1.2))
# 2. coping (wall top) and plinth (wall foot) mouldings, 1 m long along x, 1 m wide unit (scaled to each wall)
cope = [(-0.5, 0.0), (0.5, 0.0), (0.5, 0.06), (0.58, 0.1), (0.62, 0.16), (0.56, 0.2), (0.0, 0.26), (-0.56, 0.2), (-0.62, 0.16), (-0.58, 0.1), (-0.5, 0.06)]
bm = bmesh.new(); prism(bm, cope, -0.5, 0.5); made.append(finish("SM_V3_Coping", bm, ["MAT_V3_Stone"], 1.5))
plinth = [(-0.5, 0.0), (0.5, 0.0), (0.58, 0.0), (0.6, 0.12), (0.54, 0.18), (0.5, 0.3), (-0.5, 0.3), (-0.54, 0.18), (-0.6, 0.12), (-0.58, 0.0)]
bm = bmesh.new(); prism(bm, plinth, -0.5, 0.5); made.append(finish("SM_V3_Plinth", bm, ["MAT_V3_Stone"], 1.5))
# 3. brass lamp stand (deepa stambha), 1.5 m, five tiers of oil cups
prof = [(0, 0.24), (0.06, 0.24), (0.1, 0.16), (0.16, 0.06), (0.3, 0.045)]
z = 0.3
for k in range(5):
    r = 0.23 - k * 0.035
    prof += [(z + 0.18, 0.04), (z + 0.2, r), (z + 0.24, r + 0.015), (z + 0.26, 0.04)]; z += 0.24
prof += [(z + 0.1, 0.035), (z + 0.16, 0.07), (z + 0.22, 0.02)]
bm = bmesh.new(); lathe(bm, prof, 20); made.append(finish("SM_V3_LampStand", bm, ["MAT_V3_Brass"], 2.0))
# 4. round stone arch, 4 m span, 4.6 m tall, 0.9 m deep, with a keystone
bm = bmesh.new(); W, H, D, t = 2.0, 3.0, 0.45, 0.55
box(bm, -W - t, -W, -D, D, 0, H); box(bm, W, W + t, -D, D, 0, H)
N = 14; outer = [((W + t) * math.cos(math.pi * i / N), H + (W + t) * math.sin(math.pi * i / N)) for i in range(N + 1)]
inner = [(W * math.cos(math.pi * i / N), H + W * math.sin(math.pi * i / N)) for i in range(N + 1)]
for i in range(N):
    q = [(outer[i][0], outer[i][1]), (outer[i + 1][0], outer[i + 1][1]), (inner[i + 1][0], inner[i + 1][1]), (inner[i][0], inner[i][1])]
    A = [bm.verts.new((x, -D, y)) for x, y in q]; B = [bm.verts.new((x, D, y)) for x, y in q]
    bm.faces.new(A[::-1]); bm.faces.new(B)
    for a in range(4): b2 = (a + 1) % 4; bm.faces.new([A[a], A[b2], B[b2], B[a]])
box(bm, -0.25, 0.25, -D - 0.05, D + 0.05, H + W - 0.1, H + W + t + 0.12)
box(bm, -W - t - 0.1, -W + 0.1, -D - 0.06, D + 0.06, H - 0.18, H); box(bm, W - 0.1, W + t + 0.1, -D - 0.06, D + 0.06, H - 0.18, H)
made.append(finish("SM_V3_Arch", bm, ["MAT_V3_Stone"], 1.0))
for o in made: export(o)
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(ROOT, "Art_Source/V3/V3_Props.blend"))
print("PROPS_OK", [(o.name, len(o.data.polygons)) for o in made])
