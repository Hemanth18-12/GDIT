# Investigator's jeep, version 2 (olive SUV matching Docs/References/jeep_3d_design_reference.png).
# Run headless:  Blender --background --python Tools/blender/build_jeep_v2.py -- <repo root>
# Axes: forward = -Y, up = +Z, vehicle LEFT = +X, driver (right-hand drive) = -X. 1 unit = 1 m.
# Object names (SM_Jeep_*) and material names (MAT_*) are the ones Jeep.cs / Part2Tools.BuildJeep look up.
import bpy, bmesh, math, os, sys, random, shutil
from mathutils import Vector, Matrix, noise

argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
ROOT = argv[0] if argv else os.getcwd()
OUT = os.path.join(ROOT, "Art_Source/Jeep")
UNITY = os.path.join(ROOT, "Assets/_Project/Models/Vehicles/Jeep")
WORK = os.path.join(ROOT, "Docs/Blender/Jeep")
QA = os.path.join(ROOT, "Docs/QA/Jeep")
TEX = os.path.join(OUT, "Car_Textures")
for d in (OUT, WORK, QA, os.path.join(OUT, "Car_Damaged"), os.path.join(OUT, "Car_Collision_Meshes"), os.path.join(OUT, "Car_Reference_Renders")):
    os.makedirs(d, exist_ok=True)

bpy.ops.wm.read_factory_settings(use_empty=True)
scene = bpy.context.scene
scene.unit_settings.system = 'METRIC'

# ---------------------------------------------------------------- dimensions (from the Stage A overlay)
W, SILL, BELT = 0.88, 0.46, 1.22
AX, TR, TW, TRACK = 1.36, 0.39, 0.27, 0.825
NOSE, TAIL = -1.90, 2.01
ROOF, ROOFH = 1.79, 1.755            # top of the black roof cap / top of the painted cabin
RHW, RF_Y0, RF_Y1, Y0B = 0.72, -0.37, 1.93, -0.68
ARCH_R, FLARE_R, FLARE_X = 0.50, 0.585, 0.98
SL = (W - RHW) / (ROOFH - BELT)      # tumblehome of the cabin sides
def side_x(z): return W - (z - BELT) * SL
def front_y(z): return Y0B + (z - BELT) * (RF_Y0 - Y0B) / (ROOFH - BELT)
def rear_y(z): return TAIL - (z - BELT) * (TAIL - RF_Y1) / (ROOFH - BELT)

# ---------------------------------------------------------------- collections
def coll(name, parent=None):
    c = bpy.data.collections.new(name); (parent or scene.collection).children.link(c); return c
CROOT = coll("JEEP_Root")
C = {n: coll(n, CROOT) for n in ["BODY", "GLASS", "LIGHTS", "WHEELS", "DOORS", "INTERIOR", "ACCESSORIES", "DAMAGE", "COLLISION", "RIG"]}
TMP = coll("TMP")

# ---------------------------------------------------------------- materials
def img(name):
    p = os.path.join(TEX, name)
    return bpy.data.images.load(p) if os.path.exists(p) else None

def mat(name, col, metal=0.0, rough=0.5, emit=None, alpha=1.0, tex=None, tex_alpha=False, coat=0.0):
    m = bpy.data.materials.new(name); m.use_nodes = True
    b = m.node_tree.nodes["Principled BSDF"]
    b.inputs["Base Color"].default_value = (*col, 1); b.inputs["Metallic"].default_value = metal; b.inputs["Roughness"].default_value = rough
    if coat and "Coat Weight" in b.inputs: b.inputs["Coat Weight"].default_value = coat; b.inputs["Coat Roughness"].default_value = 0.08
    if emit:
        b.inputs["Emission Color"].default_value = (*emit, 1); b.inputs["Emission Strength"].default_value = 1.0
    if alpha < 1.0:
        b.inputs["Alpha"].default_value = alpha; m.surface_render_method = 'BLENDED'
    if tex is not None:
        t = m.node_tree.nodes.new("ShaderNodeTexImage"); t.image = tex
        if tex_alpha:
            m.node_tree.links.new(t.outputs["Color"], b.inputs["Base Color"]); m.node_tree.links.new(t.outputs["Alpha"], b.inputs["Alpha"]); m.surface_render_method = 'BLENDED'
        else:
            mx = m.node_tree.nodes.new("ShaderNodeMix"); mx.data_type = 'RGBA'; mx.blend_type = 'MULTIPLY'; mx.inputs[0].default_value = 1.0
            mx.inputs[6].default_value = (*col, 1); m.node_tree.links.new(t.outputs["Color"], mx.inputs[7]); m.node_tree.links.new(mx.outputs[2], b.inputs["Base Color"])
    m.diffuse_color = (*col, alpha); m.roughness = rough; m.metallic = metal
    return m

OLIVE = (0.072, 0.098, 0.042)
M = {
    "paint": mat("MAT_Paint_Body", OLIVE, 0.1, 0.46, tex=img("T_Jeep_PaintDirt.png"), coat=0.2),
    "roof": mat("MAT_Hardtop_Black", (0.012, 0.012, 0.012), 0.0, 0.6),
    "trim": mat("MAT_BlackTrim", (0.012, 0.012, 0.013), 0.1, 0.5),
    "rubber": mat("MAT_Rubber", (0.012, 0.012, 0.012), 0.0, 0.88),
    "alloy": mat("MAT_Alloy", (0.02, 0.02, 0.022), 0.9, 0.35),
    "chrome": mat("MAT_Chrome", (0.72, 0.72, 0.74), 1.0, 0.12),
    "glass": mat("MAT_Glass", (0.02, 0.035, 0.04), 0.0, 0.04, alpha=0.42),
    "head": mat("MAT_Light_Head", (1.0, 0.94, 0.80), 0.0, 0.1, emit=(1.0, 0.9, 0.7)),
    "tail": mat("MAT_Light_Tail", (0.55, 0.01, 0.01), 0.0, 0.2, emit=(0.35, 0.0, 0.0)),
    "rev": mat("MAT_Light_Reverse", (0.85, 0.85, 0.85), 0.0, 0.2),
    "ind": mat("MAT_Light_Indicator", (1.0, 0.42, 0.02), 0.0, 0.2),
    "emblem": mat("MAT_Gold_Emblem", (0.83, 0.62, 0.22), 0.6, 0.35, tex=img("T_Jeep_Emblem.png"), tex_alpha=True),
    "interior": mat("MAT_Interior", (0.018, 0.018, 0.02), 0.0, 0.7),
    "seat": mat("MAT_Seat_Leather", (0.045, 0.045, 0.05), 0.0, 0.55),
    "plate": mat("MAT_Plate", (1, 1, 1), 0.0, 0.5, tex=img("T_Jeep_Plate.png")),
    "under": mat("MAT_Undercarriage", (0.02, 0.02, 0.022), 0.5, 0.6),
    "screen": mat("MAT_Screen", (0.01, 0.04, 0.08), 0.0, 0.15, emit=(0.01, 0.05, 0.10)),
    "steel": mat("MAT_Steel_Machined", (0.45, 0.45, 0.47), 1.0, 0.3),
}
SCUFFED = mat("MAT_Paint_Scuffed", (0.085, 0.10, 0.055), 0.1, 0.7)
BURNT = mat("MAT_Paint_Burnt", (0.012, 0.010, 0.008), 0.3, 0.9)

# ---------------------------------------------------------------- bmesh helpers
def faces_of(verts): return {f for v in verts for f in v.link_faces}

def add_box(bm, x0, x1, y0, y1, z0, z1, mi=0, mtx=None):
    vs = [bm.verts.new((x, y, z)) for z in (z0, z1) for y in (y0, y1) for x in (x0, x1)]
    for f in ((0, 1, 3, 2), (4, 6, 7, 5), (0, 4, 5, 1), (2, 3, 7, 6), (0, 2, 6, 4), (1, 5, 7, 3)):
        bm.faces.new([vs[i] for i in f]).material_index = mi
    if mtx is not None: bmesh.ops.transform(bm, matrix=mtx, verts=vs)
    return vs

def axis_mtx(center, axis):
    return Matrix.Translation(Vector(center)) @ Vector((0, 0, 1)).rotation_difference(Vector(axis).normalized()).to_matrix().to_4x4()

def add_cyl(bm, center, r, depth, axis, mi=0, seg=24, r2=None):
    g = bmesh.ops.create_cone(bm, cap_ends=True, segments=seg, radius1=r, radius2=r if r2 is None else r2, depth=depth, matrix=axis_mtx(center, axis))
    for f in faces_of(g['verts']): f.material_index = mi
    return g['verts']

def add_sphere(bm, center, r, mi=0, u=10, v=6):
    g = bmesh.ops.create_uvsphere(bm, u_segments=u, v_segments=v, radius=r, matrix=Matrix.Translation(Vector(center)))
    for f in faces_of(g['verts']): f.material_index = mi

def add_tube(bm, pts, r, mi=0, seg=8, joints=True):
    pts = [Vector(p) for p in pts]
    for a, b in zip(pts[:-1], pts[1:]):
        if (b - a).length > 1e-5: add_cyl(bm, (a + b) / 2, r, (b - a).length, b - a, mi, seg)
    if joints:
        for p in pts[1:-1]: add_sphere(bm, p, r * 1.0, mi, seg, max(4, seg // 2))

def add_prism(bm, pts, axis, a0, a1, mi=0):
    def P(a, p):
        return (a, p[0], p[1]) if axis == 'X' else (p[0], a, p[1]) if axis == 'Y' else (p[0], p[1], a)
    A = [bm.verts.new(P(a0, p)) for p in pts]; B = [bm.verts.new(P(a1, p)) for p in pts]
    bm.faces.new(A).material_index = mi; bm.faces.new(B[::-1]).material_index = mi
    for i in range(len(pts)):
        j = (i + 1) % len(pts); bm.faces.new([A[i], B[i], B[j], A[j]]).material_index = mi
    return A + B

def add_lathe(bm, profile, center, axis, mi=0, seg=40, cap=False):
    """profile = [(offset along axis, radius), ...] revolved around the axis through center."""
    mtx = axis_mtx(center, axis); rings = []
    for off, r in profile:
        rings.append([bm.verts.new(mtx @ Vector((r * math.cos(2 * math.pi * i / seg), r * math.sin(2 * math.pi * i / seg), off))) for i in range(seg)])
    for a, b in zip(rings[:-1], rings[1:]):
        for i in range(seg):
            j = (i + 1) % seg; bm.faces.new([a[i], a[j], b[j], b[i]]).material_index = mi
    if cap:
        bm.faces.new(rings[0][::-1]).material_index = mi; bm.faces.new(rings[-1]).material_index = mi

def add_torus(bm, center, R, r, axis, mi=0, seg=32, tseg=8):
    mtx = axis_mtx(center, axis); rings = []
    for i in range(seg):
        a = 2 * math.pi * i / seg
        rings.append([bm.verts.new(mtx @ Vector(((R + r * math.cos(2 * math.pi * k / tseg)) * math.cos(a), (R + r * math.cos(2 * math.pi * k / tseg)) * math.sin(a), r * math.sin(2 * math.pi * k / tseg)))) for k in range(tseg)])
    for i in range(seg):
        a, b = rings[i], rings[(i + 1) % seg]
        for k in range(tseg):
            k2 = (k + 1) % tseg; bm.faces.new([a[k], b[k], b[k2], a[k2]]).material_index = mi

def arc(cx, cy, r, a0, a1, n):
    return [(cx + r * math.cos(math.radians(a0 + (a1 - a0) * i / n)), cy + r * math.sin(math.radians(a0 + (a1 - a0) * i / n))) for i in range(n + 1)]

def inset(pts, d):
    c = Vector((sum(p[0] for p in pts) / len(pts), sum(p[1] for p in pts) / len(pts)))
    out = []
    for p in pts:
        v = Vector(p) - c; out.append(tuple(c + v * (1 - d / max(v.length, 1e-4))))
    return out

def make(name, bm, collection, mats, origin=None, rot=None):
    bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=1e-5)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    if origin is not None: bmesh.ops.translate(bm, verts=bm.verts, vec=-Vector(origin))
    me = bpy.data.meshes.new(name); bm.to_mesh(me); bm.free()
    for m in mats: me.materials.append(m)
    ob = bpy.data.objects.new(name, me); collection.objects.link(ob)
    if origin is not None: ob.location = origin
    if rot is not None: ob.rotation_euler = rot
    return ob

def apply_mods(o):
    dg = bpy.context.evaluated_depsgraph_get(); dg.update()
    me = bpy.data.meshes.new_from_object(o.evaluated_get(dg)); old = o.data
    o.modifiers.clear(); o.data = me; me.name = o.name; bpy.data.meshes.remove(old)

def cut(o, cutter_bm):
    c = make("cutter", cutter_bm, TMP, [])
    if o.location.length > 0: c.location = (0, 0, 0)
    md = o.modifiers.new("b", 'BOOLEAN'); md.object = c; md.operation = 'DIFFERENCE'; md.solver = 'EXACT'
    apply_mods(o); bpy.data.objects.remove(c, do_unlink=True)

def finish(o, bevel=0.012, segs=2, angle=38.0, uv='side'):
    """bevel hard edges, smooth by angle, weighted normals, simple UVs."""
    if bevel:
        md = o.modifiers.new("Bevel", 'BEVEL'); md.width = bevel; md.segments = segs; md.limit_method = 'ANGLE'; md.angle_limit = math.radians(32); md.harden_normals = False
        apply_mods(o)
    me = o.data
    for p in me.polygons: p.use_smooth = True
    me.set_sharp_from_angle(angle=math.radians(angle))
    md = o.modifiers.new("Tri", 'TRIANGULATE'); md.min_vertices = 5          # Unity's FBX importer drops concave n-gons
    md = o.modifiers.new("WN", 'WEIGHTED_NORMAL'); md.keep_sharp = True; apply_mods(o)
    me = o.data
    if not me.uv_layers: me.uv_layers.new(name="UV0")
    uvl = me.uv_layers[0].data; mw = o.matrix_world
    for l in me.loops:
        w = mw @ me.vertices[l.vertex_index].co
        uvl[l.index].uv = ((w.y + 2.2) / 4.6, w.z / 2.1) if uv == 'side' else ((w.x + 1.1) / 2.2, (w.y + 2.2) / 4.6)
    return o

parts = {}
def keep(o, **kw):
    parts[o.name] = o; return finish(o, **kw)

# ================================================================= BODY (hollow tub with wheel wells, cabin, engine bay, door openings)
front_door = [(-0.60, 0.52), (0.26, 0.52), (0.26, 1.68), (-0.33, 1.68), (-0.60, 1.24)]
front_hole = [(-0.50, 1.275), (0.215, 1.275), (0.215, 1.635), (-0.295, 1.635)]
rear_door = [(0.32, 0.52)] + [(AX - 0.60 * math.cos(math.radians(a)), TR + 0.60 * math.sin(math.radians(a))) for a in (12.5, 22, 32, 42, 52, 60)] + [(1.08, 0.97), (1.08, 1.68), (0.32, 1.68)]
rear_hole = [(0.365, 1.275), (1.035, 1.275), (1.035, 1.635), (0.365, 1.635)]
quarter = [(1.17, 1.275), (1.86, 1.275), (1.83, 1.635), (1.17, 1.635)]

profile = [(NOSE, 0.60), (NOSE, 0.96), (NOSE + 0.06, 1.022), (-0.74, 1.212), (-0.72, 1.25), (-0.60, 1.25), (-0.575, BELT), (TAIL, BELT),
           (TAIL + 0.02, 0.60), (TAIL - 0.05, SILL), (-1.80, SILL)]
bm = bmesh.new(); add_prism(bm, profile, 'X', -W, W)
body = make("SM_Jeep_Body", bm, C["BODY"], [M["paint"]])
bm = bmesh.new()                                                       # wheel wells
for sx in (1, -1):
    for sy in (-AX, AX): add_cyl(bm, (sx * 0.83, sy, TR), ARCH_R, 0.54, (1, 0, 0), seg=40)
cut(body, bm)
cab = bmesh.new(); add_box(cab, -(W - 0.05), W - 0.05, -0.56, 1.95, 0.51, 1.40)          # cabin space, leaving a liner around the rear wells
cabo = make("cabcut", cab, TMP, [])
bm = bmesh.new()
for sx in (1, -1): add_cyl(bm, (sx * 0.80, AX, TR), ARCH_R + 0.04, 0.56, (1, 0, 0), seg=40)
cut(cabo, bm)
md = body.modifiers.new("b", 'BOOLEAN'); md.object = cabo; md.operation = 'DIFFERENCE'; md.solver = 'EXACT'; apply_mods(body); bpy.data.objects.remove(cabo, do_unlink=True)
bm = bmesh.new(); add_box(bm, -0.54, 0.54, -1.82, -0.80, 0.66, 1.40); cut(body, bm)        # engine bay under the hood
for poly in (front_door, rear_door):                                    # door openings, both sides
    bm = bmesh.new()
    for sx in (1, -1): add_prism(bm, poly, 'X', sx * 0.70, sx * 1.3)
    cut(body, bm)
keep(body, bevel=0.014)

# cabin (painted) with window and door openings
bm = bmesh.new()
bz = BELT
b = [bm.verts.new(v) for v in [(W, Y0B, bz), (-W, Y0B, bz), (-W, TAIL, bz), (W, TAIL, bz)]]
t = [bm.verts.new(v) for v in [(RHW, RF_Y0, ROOFH), (-RHW, RF_Y0, ROOFH), (-RHW, RF_Y1, ROOFH), (RHW, RF_Y1, ROOFH)]]
bm.faces.new(b[::-1]); bm.faces.new(t)
for i in range(4): j = (i + 1) % 4; bm.faces.new([b[i], b[j], t[j], t[i]])
top = make("SM_Jeep_Hardtop", bm, C["BODY"], [M["paint"]])
bm = bmesh.new(); wl = 0.045; z0, z1 = BELT - 0.06, ROOFH - 0.045
b = [bm.verts.new(v) for v in [(side_x(z0) - wl, front_y(z0) + wl, z0), (-(side_x(z0) - wl), front_y(z0) + wl, z0), (-(side_x(z0) - wl), rear_y(z0) - wl, z0), (side_x(z0) - wl, rear_y(z0) - wl, z0)]]
t = [bm.verts.new(v) for v in [(side_x(z1) - wl, front_y(z1) + wl, z1), (-(side_x(z1) - wl), front_y(z1) + wl, z1), (-(side_x(z1) - wl), rear_y(z1) - wl, z1), (side_x(z1) - wl, rear_y(z1) - wl, z1)]]
bm.faces.new(b[::-1]); bm.faces.new(t)
for i in range(4): j = (i + 1) % 4; bm.faces.new([b[i], b[j], t[j], t[i]])
cut(top, bm)
for poly in (front_door, rear_door, quarter):
    bm = bmesh.new()
    for sx in (1, -1): add_prism(bm, poly, 'X', sx * 0.55, sx * 1.3)
    cut(top, bm)
WS = [(-0.75, 1.30), (0.75, 1.30), (0.635, 1.715), (-0.635, 1.715)]
RW = [(-0.60, 1.285), (0.60, 1.285), (0.56, 1.675), (-0.56, 1.675)]
bm = bmesh.new(); add_prism(bm, WS, 'Y', -1.0, -0.22); cut(top, bm)
bm = bmesh.new(); add_prism(bm, RW, 'Y', 1.78, 2.25); cut(top, bm)
keep(top, bevel=0.016)

bm = bmesh.new()                                                        # black roof cap with gutters
add_box(bm, -(RHW + 0.012), RHW + 0.012, RF_Y0 - 0.035, RF_Y1 + 0.02, ROOFH - 0.005, ROOF)
for sx in (1, -1): add_box(bm, sx * (RHW + 0.012), sx * (RHW + 0.035), RF_Y0 - 0.02, RF_Y1, ROOFH - 0.03, ROOFH + 0.005)
keep(make("SM_Jeep_RoofCap", bm, C["BODY"], [M["roof"]]), bevel=0.01)

# hood (origin at the rear hinge)
bm = bmesh.new()
hp = [(NOSE - 0.012, 0.975), (NOSE + 0.05, 1.068), (-0.725, 1.262), (-0.725, 1.222), (NOSE + 0.06, 1.03), (NOSE + 0.01, 0.975)]
add_prism(bm, hp, 'X', -(W - 0.03), W - 0.03)
bulge = [(NOSE + 0.10, 1.076), (NOSE + 0.22, 1.106), (-0.80, 1.268), (-0.80, 1.25)]
add_prism(bm, bulge, 'X', -0.36, 0.36)
keep(make("SM_Jeep_Hood", bm, C["BODY"], [M["paint"]], origin=(0, -0.725, 1.24)), bevel=0.012)

# fender flares
def flare(name, sx, cy):
    bm = bmesh.new(); N = 20; rings = []
    xi, xo = sx * (W - 0.03), sx * FLARE_X
    for i in range(N + 1):
        a = math.radians(-10 + 200 * i / N)
        rings.append([bm.verts.new((x, cy - r * math.cos(a), TR + r * math.sin(a))) for x, r in ((xi, ARCH_R - 0.006), (xo, ARCH_R - 0.006), (xo, FLARE_R - 0.012), (sx * (FLARE_X - 0.05), FLARE_R), (xi, FLARE_R))])
    for i in range(N):
        for k in range(5): k2 = (k + 1) % 5; bm.faces.new([rings[i][k], rings[i][k2], rings[i + 1][k2], rings[i + 1][k]])
    bm.faces.new(rings[0]); bm.faces.new(rings[-1][::-1])
    return keep(make(name, bm, C["BODY"], [M["trim"]]), bevel=0.008)
flare("SM_Jeep_Flare_FL", 1, -AX); flare("SM_Jeep_Flare_FR", -1, -AX); flare("SM_Jeep_Flare_RL", 1, AX); flare("SM_Jeep_Flare_RR", -1, AX)

# ================================================================= DOORS (origin = front hinge; glass and emblem are children)
def child(o, parent):
    mw = o.matrix_world.copy(); o.parent = parent; o.matrix_parent_inverse = parent.matrix_world.inverted(); o.matrix_world = mw

def door(tag, sx, poly, hole, emblem):
    bm = bmesh.new(); add_prism(bm, inset(poly, 0.0035), 'X', W - 0.05, W + 0.004)
    hy = (hole[0][0] + hole[1][0]) / 2
    yh = poly[1][0] - 0.16 if poly is front_door else 0.90
    add_box(bm, W + 0.004, W + 0.03, yh - 0.075, yh + 0.075, 1.115, 1.15, 1)                                  # handle
    add_box(bm, W + 0.003, W + 0.012, min(p[0] for p in poly) + 0.03, max(p[0] for p in poly) - 0.03 if poly is front_door else 0.74, 0.535, 0.60, 1)   # lower rub strip
    d = make("SM_Jeep_Door_" + tag, bm, C["DOORS"], [M["paint"], M["trim"]])
    bm = bmesh.new(); add_prism(bm, hole, 'X', 0.4, 1.4); cut(d, bm)
    me = d.data
    for v in me.vertices:
        if v.co.z > BELT: v.co.x -= (v.co.z - BELT) * SL
        v.co.x *= sx
    if sx < 0: me.flip_normals()
    hinge = (sx * (W - 0.02), min(p[0] for p in poly), 0.90)
    for v in me.vertices: v.co -= Vector(hinge)
    d.location = hinge; bpy.context.view_layer.update()
    keep(d, bevel=0.007)
    g = bmesh.new(); gp = inset(hole, -0.02)
    g.faces.new([g.verts.new((sx * (side_x(z) - 0.028), y, z)) for y, z in gp])
    go = make("SM_Jeep_Glass_Door_" + tag, g, C["GLASS"], [M["glass"]]); keep(go, bevel=0); child(go, d)
    if emblem:
        s, yc, zc = 0.50, -0.19, 0.84; x = sx * (W + 0.0065); e = bmesh.new()
        vs = [e.verts.new((x, yc - s / 2, zc - s / 2)), e.verts.new((x, yc + s / 2, zc - s / 2)), e.verts.new((x, yc + s / 2, zc + s / 2)), e.verts.new((x, yc - s / 2, zc + s / 2))]
        f = e.faces.new(vs if sx > 0 else vs[::-1]); uv = e.loops.layers.uv.new("UV0")
        for l in f.loops:
            u = (l.vert.co.y - (yc - s / 2)) / s; l[uv].uv = (u if sx > 0 else 1 - u, (l.vert.co.z - (zc - s / 2)) / s)
        e.normal_update()
        me2 = bpy.data.meshes.new("SM_Jeep_Emblem_" + tag); e.to_mesh(me2); e.free(); me2.materials.append(M["emblem"])
        eo = bpy.data.objects.new("SM_Jeep_Emblem_" + tag, me2); C["DOORS"].objects.link(eo); bpy.context.view_layer.update(); parts[eo.name] = eo; child(eo, d)
    return d

for sx, s in ((1, "L"), (-1, "R")):
    door("F" + s, sx, front_door, front_hole, True)
    door("R" + s, sx, rear_door, rear_hole, False)

# ================================================================= GLASS (fixed)
def pane(name, pts):
    bm = bmesh.new(); bm.faces.new([bm.verts.new(p) for p in pts]); return keep(make(name, bm, C["GLASS"], [M["glass"]]), bevel=0)
e = 0.02
pane("SM_Jeep_Glass_Windscreen", [(x * 1.02, front_y(z) + 0.022, z) for x, z in WS])
pane("SM_Jeep_Glass_Rear", [(x * 1.03, rear_y(z) - 0.022, z) for x, z in RW])
for sx, s in ((1, "L"), (-1, "R")):
    pane("SM_Jeep_Glass_Quarter_" + s, [(sx * (side_x(z) - 0.022), y, z) for y, z in inset(quarter, -0.02)])

# ================================================================= FRONT
bm = bmesh.new()                                                        # grille panel with horizontal slats
add_box(bm, -0.86, 0.86, NOSE - 0.022, NOSE + 0.01, 0.70, 0.975)
for i in range(5): add_box(bm, -0.44, 0.44, NOSE - 0.04, NOSE - 0.02, 0.745 + i * 0.046, 0.772 + i * 0.046)
add_box(bm, -0.47, -0.44, NOSE - 0.045, NOSE - 0.02, 0.73, 0.965); add_box(bm, 0.44, 0.47, NOSE - 0.045, NOSE - 0.02, 0.73, 0.965)
add_box(bm, -0.47, 0.47, NOSE - 0.045, NOSE - 0.02, 0.955, 0.972); add_box(bm, -0.47, 0.47, NOSE - 0.045, NOSE - 0.02, 0.722, 0.738)
keep(make("SM_Jeep_Grille", bm, C["BODY"], [M["trim"]]), bevel=0.004)

def lamp(name, c, r, depth, lens_mat, collection=None, ring=True):
    bm = bmesh.new(); x, y, z = c
    add_lathe(bm, [(0, r * 0.75), (depth * 0.5, r * 1.06), (depth, r * 1.06), (depth, r * 0.93)], (x, y + depth, z), (0, -1, 0), 2, 28)        # housing, open to the front
    bm.faces.new(bm.verts[:28][::-1]).material_index = 2
    add_cyl(bm, (x, y + 0.012, z), r * 0.93, 0.012, (0, 1, 0), 0, 28)                                                                         # lens
    if ring: add_torus(bm, (x, y, z), r * 1.0, r * 0.085, (0, 1, 0), 1, 28, 6)
    return keep(make(name, bm, collection or C["LIGHTS"], [lens_mat, M["chrome"], M["trim"]]), bevel=0, uv='top')

for sx, s in ((1, "L"), (-1, "R")):
    lamp("SM_Jeep_Headlight_" + s, (sx * 0.64, NOSE - 0.07, 0.86), 0.108, 0.06, M["head"])
    lamp("SM_Jeep_FogLight_" + s, (sx * 0.70, -1.985, 0.57), 0.052, 0.03, M["head"])
    bm = bmesh.new(); add_box(bm, sx * 0.775, sx * 0.855, NOSE - 0.035, NOSE - 0.015, 0.80, 0.915)
    keep(make("SM_Jeep_Indicator_F" + s, bm, C["LIGHTS"], [M["ind"]]), bevel=0.004)

def bumper(name, ys, z0, z1):
    bm = bmesh.new(); y0, y1, y2 = ys
    add_prism(bm, [(-0.95, y0), (-0.95, y1), (-0.80, y2), (0.80, y2), (0.95, y1), (0.95, y0)], 'Z', z0, z1)
    return keep(make(name, bm, C["BODY"], [M["trim"]]), bevel=0.014)
bumper("SM_Jeep_Bumper_Front", (-1.84, -1.90, -1.97), 0.44, 0.70)
bumper("SM_Jeep_Bumper_Rear", (2.00, 2.06, 2.13), 0.42, 0.66)

yb = -2.012; bm = bmesh.new()                                           # bull bar
hoop = [(0.42, yb, 0.40), (0.40, yb, 0.95)] + [(0.33 + 0.07 * math.cos(math.radians(a)), yb, 0.98 + 0.07 * math.sin(math.radians(a))) for a in (20, 45, 70, 90)]
hoop = hoop + [(-x, y, z) for x, y, z in hoop[::-1]]
add_tube(bm, hoop, 0.03, 0, 10); add_tube(bm, [(0.405, yb, 0.74), (-0.405, yb, 0.74)], 0.026, 0, 10)
for sx in (1, -1):
    add_tube(bm, [(sx * 0.41, yb, 0.52), (sx * 0.41, -1.93, 0.52)], 0.026, 0, 8); add_tube(bm, [(sx * 0.405, yb, 0.66), (sx * 0.405, -1.93, 0.66)], 0.022, 0, 8)
add_box(bm, -0.30, 0.30, yb - 0.028, yb - 0.02, 0.42, 0.56)
keep(make("SM_Jeep_BullBar", bm, C["ACCESSORIES"], [M["trim"]]), bevel=0, uv='top')

def plate(name, y, facing):
    bm = bmesh.new(); x0, x1, z0, z1 = -0.29, 0.29, 0.425, 0.555
    vs = [bm.verts.new((x0, y, z0)), bm.verts.new((x1, y, z0)), bm.verts.new((x1, y, z1)), bm.verts.new((x0, y, z1))]
    f = bm.faces.new(vs if facing < 0 else vs[::-1]); uv = bm.loops.layers.uv.new("UV0")
    for l in f.loops:
        u = (l.vert.co.x - x0) / (x1 - x0); l[uv].uv = (u if facing < 0 else 1 - u, (l.vert.co.z - z0) / (z1 - z0))
    bm.normal_update(); me = bpy.data.meshes.new(name); bm.to_mesh(me); bm.free(); me.materials.append(M["plate"])
    o = bpy.data.objects.new(name, me); C["ACCESSORIES"].objects.link(o); parts[name] = o; return o
plate("SM_Jeep_Plate_Front", yb - 0.0295, -1)
pr = plate("SM_Jeep_Plate_Rear", 2.1345, 1)
for v in pr.data.vertices: v.co.z -= 0.02

# ================================================================= REAR
for sx, s in ((1, "L"), (-1, "R")):
    for nm, z0, z1, m in (("SM_Jeep_Indicator_R", 0.745, 0.80, M["ind"]), ("SM_Jeep_TailLight_", 0.808, 1.0, M["tail"]), ("SM_Jeep_ReverseLight_", 1.008, 1.06, M["rev"])):
        bm = bmesh.new(); add_box(bm, sx * 0.73, sx * 0.865, TAIL + 0.012, TAIL + 0.04, z0, z1)
        keep(make(nm + s, bm, C["LIGHTS"], [m]), bevel=0.004)
bm = bmesh.new()
for sx in (1, -1):
    add_box(bm, sx * 0.715, sx * 0.875, TAIL - 0.005, TAIL + 0.022, 0.73, 1.075)                       # tail lamp bezels
    add_box(bm, sx * 0.70, sx * 0.95, 1.86, 1.875, 0.16, 0.50)                                        # mud flaps hang behind the rear wheels
add_box(bm, -0.33, -0.09, TAIL + 0.01, TAIL + 0.06, 0.92, 1.12)                                       # spare carrier
add_box(bm, 0.30, 0.46, TAIL + 0.005, TAIL + 0.03, 1.08, 1.115)                                       # tailgate handle
for z in (0.80, 1.12): add_box(bm, -0.80, -0.74, TAIL + 0.005, TAIL + 0.03, z - 0.04, z + 0.04)        # tailgate hinges (right side)
add_cyl(bm, (W + 0.004, 1.62, 1.02), 0.065, 0.012, (1, 0, 0), 0, 20)                                   # fuel cap
add_box(bm, -0.50, 0.50, -0.71, -0.60, 1.25, 1.262)                                                   # cowl vent
keep(make("SM_Jeep_Trim", bm, C["BODY"], [M["trim"]]), bevel=0.005)

# ================================================================= WHEELS (origin = centre, outboard = +X for the left side)
def wheel_mesh():
    bm = bmesh.new(); R = TR - 0.012; h = TW / 2
    add_lathe(bm, [(-h + 0.025, 0.262), (-h + 0.005, 0.285), (-h, 0.33), (-h + 0.012, R - 0.03), (-h + 0.04, R), (h - 0.04, R), (h - 0.012, R - 0.03), (h, 0.33), (h - 0.005, 0.285), (h - 0.025, 0.262)], (0, 0, 0), (1, 0, 0), 0, 48)
    for i in range(40):                                                 # centre tread blocks, two staggered rows
        a = 2 * math.pi * i / 40
        for row, x0 in ((0, -0.062), (1, 0.008)):
            ang = a + (math.pi / 40 if row else 0)
            add_box(bm, x0, x0 + 0.054, -0.021, 0.021, R - 0.004, TR, 0, Matrix.Rotation(ang, 4, 'X'))
    for i in range(24):                                                 # shoulder lugs
        a = 2 * math.pi * i / 24
        for sx in (1, -1):
            add_box(bm, sx * 0.072, sx * (h - 0.004), -0.034, 0.034, R - 0.035, TR, 0, Matrix.Rotation(a + (math.pi / 24 if sx > 0 else 0), 4, 'X'))
    add_lathe(bm, [(h - 0.02, 0.266), (h - 0.012, 0.258), (h - 0.03, 0.238), (h - 0.07, 0.228), (-h + 0.03, 0.228), (-h + 0.02, 0.262)], (0, 0, 0), (1, 0, 0), 1, 40)   # rim barrel and lip
    xs = h - 0.055
    for i in range(6):                                                  # six twin spokes
        for off in (-0.16, 0.16):
            a = 2 * math.pi * i / 6 + off
            add_box(bm, xs - 0.012, xs + 0.014, -0.016, 0.016, 0.06, 0.236, 1, Matrix.Rotation(a, 4, 'X'))
            add_box(bm, xs + 0.014, xs + 0.017, -0.011, 0.011, 0.075, 0.228, 3, Matrix.Rotation(a, 4, 'X'))     # machined face
    add_cyl(bm, (xs + 0.004, 0, 0), 0.078, 0.04, (1, 0, 0), 1, 24); add_cyl(bm, (xs + 0.028, 0, 0), 0.036, 0.012, (1, 0, 0), 2, 16)
    for i in range(5):
        a = 2 * math.pi * i / 5; add_cyl(bm, (xs + 0.026, 0.056 * math.cos(a), 0.056 * math.sin(a)), 0.01, 0.014, (1, 0, 0), 2, 8)
    add_cyl(bm, (0.02, 0, 0), 0.175, 0.022, (1, 0, 0), 4, 28)             # brake disc
    add_cyl(bm, (-h + 0.04, 0, 0), 0.228, 0.01, (1, 0, 0), 1, 28)         # back of the rim
    return bm

def wheel(name, loc, turn=0.0):
    bm = wheel_mesh()
    if turn: bmesh.ops.rotate(bm, cent=(0, 0, 0), matrix=Matrix.Rotation(turn, 3, 'Z'), verts=bm.verts)
    o = make(name, bm, C["WHEELS"], [M["rubber"], M["alloy"], M["chrome"], M["steel"], M["under"]]); o.location = loc; bpy.context.view_layer.update()
    return keep(o, bevel=0, angle=30, uv='top')
wheel("SM_Jeep_Wheel_FL", (TRACK, -AX, TR)); wheel("SM_Jeep_Wheel_FR", (-TRACK, -AX, TR), math.pi)
wheel("SM_Jeep_Wheel_RL", (TRACK, AX, TR)); wheel("SM_Jeep_Wheel_RR", (-TRACK, AX, TR), math.pi)
wheel("SM_Jeep_Spare", (-0.21, TAIL + 0.06 + TW / 2, 1.02), math.pi / 2)

# ================================================================= ROOF RACK, SPOT LIGHTS, STEPS, MIRRORS, WIPERS, ANTENNA
rk = dict(y0=-0.04, y1=1.64, hw=0.61, zb=1.85, zt=1.985)
def rrect(hw, y0, y1, z, r=0.07):
    pts = []
    for cx, cy, a0 in ((hw - r, y1 - r, 0), (-hw + r, y1 - r, 90), (-hw + r, y0 + r, 180), (hw - r, y0 + r, 270)):
        pts += [(cx + r * math.cos(math.radians(a0 + k * 30)), cy + r * math.sin(math.radians(a0 + k * 30)), z) for k in range(4)]
    return pts + [pts[0]]
bm = bmesh.new()
add_tube(bm, rrect(rk["hw"], rk["y0"], rk["y1"], rk["zt"]), 0.018, 0, 8)
add_tube(bm, rrect(rk["hw"], rk["y0"], rk["y1"], rk["zb"]), 0.015, 0, 8)
for i in range(1, 6):
    y = rk["y0"] + (rk["y1"] - rk["y0"]) * i / 6
    for sx in (1, -1): add_tube(bm, [(sx * rk["hw"], y, rk["zb"]), (sx * rk["hw"], y, rk["zt"])], 0.011, 0, 6, False)
for x in (-0.36, 0, 0.36):
    for y in (rk["y0"], rk["y1"]): add_tube(bm, [(x, y, rk["zb"]), (x, y, rk["zt"])], 0.011, 0, 6, False)
for i in range(1, 9):
    y = rk["y0"] + (rk["y1"] - rk["y0"]) * i / 9; add_tube(bm, [(rk["hw"], y, rk["zb"]), (-rk["hw"], y, rk["zb"])], 0.011, 0, 6, False)
for x in (-0.3, 0.3): add_tube(bm, [(x, rk["y0"], rk["zb"]), (x, rk["y1"], rk["zb"])], 0.011, 0, 6, False)
for y in (0.15, 0.80, 1.50):
    for sx in (1, -1):
        add_tube(bm, [(sx * rk["hw"], y, rk["zb"]), (sx * (RHW - 0.03), y, ROOF)], 0.014, 0, 6, False); add_box(bm, sx * (RHW - 0.07), sx * (RHW + 0.01), y - 0.05, y + 0.05, ROOF - 0.002, ROOF + 0.014)
add_tube(bm, [(rk["hw"] - 0.07, rk["y0"] - 0.05, 1.905), (-rk["hw"] + 0.07, rk["y0"] - 0.05, 1.905)], 0.014, 0, 6, False)      # light bar
for sx in (1, -1): add_tube(bm, [(sx * (rk["hw"] - 0.07), rk["y0"] - 0.05, 1.905), (sx * (rk["hw"] - 0.07), rk["y0"] + 0.02, rk["zb"])], 0.012, 0, 6, False)
keep(make("SM_Jeep_RoofRack", bm, C["ACCESSORIES"], [M["trim"]]), bevel=0, uv='top')
for i, x in enumerate((0.34, 0.115, -0.115, -0.34), 1):
    lamp(f"SM_Jeep_SpotLight_{i}", (x, rk["y0"] - 0.16, 1.93), 0.062, 0.09, M["head"])

for sx, s in ((1, "L"), (-1, "R")):
    bm = bmesh.new(); x = sx * 0.955
    add_tube(bm, [(sx * 0.86, -0.84, 0.36), (x, -0.78, 0.33), (x, 0.82, 0.33), (sx * 0.86, 0.88, 0.36)], 0.036, 0, 10)
    for y in (-0.55, 0.10, 0.62): add_tube(bm, [(x, y, 0.33), (sx * 0.78, y, 0.45)], 0.018, 1, 6, False)
    add_box(bm, sx * 0.925, sx * 0.985, -0.70, 0.74, 0.362, 0.37, 1)
    keep(make("SM_Jeep_SideStep_" + s, bm, C["ACCESSORIES"], [M["chrome"], M["trim"]]), bevel=0, uv='top')
    bm = bmesh.new()
    add_box(bm, sx * 0.86, sx * 0.93, -0.585, -0.555, 1.27, 1.31); add_box(bm, sx * 0.915, sx * 1.045, -0.62, -0.545, 1.235, 1.42)
    add_box(bm, sx * 0.925, sx * 1.035, -0.546, -0.542, 1.25, 1.405, 1)
    keep(make("SM_Jeep_Mirror_" + s, bm, C["ACCESSORIES"], [M["trim"], M["chrome"]]), bevel=0.012)
    bm = bmesh.new(); x0 = sx * 0.12 if sx > 0 else -0.52                 # both wipers park pointing to the vehicle's left
    add_tube(bm, [(x0, front_y(1.30) - 0.03, 1.285), (x0 + 0.02, front_y(1.33) - 0.03, 1.325), (x0 + 0.40, front_y(1.40) - 0.028, 1.40)], 0.007, 0, 6)
    add_box(bm, x0 + 0.10, x0 + 0.52, front_y(1.36) - 0.022, front_y(1.36) - 0.012, 1.345, 1.36, 0, Matrix.Rotation(0.0, 4, 'X'))
    keep(make("SM_Jeep_Wiper_" + s, bm, C["ACCESSORIES"], [M["trim"]]), bevel=0, uv='top')
bm = bmesh.new(); add_cyl(bm, (0.80, -0.80, 1.262), 0.016, 0.03, (0, 0, 1), 0, 10); add_tube(bm, [(0.80, -0.80, 1.27), (0.80, -0.74, 1.86)], 0.005, 0, 6, False)
keep(make("SM_Jeep_Antenna", bm, C["ACCESSORIES"], [M["trim"]]), bevel=0, uv='top')

# ================================================================= INTERIOR (right-hand drive)
bm = bmesh.new()
add_box(bm, -0.82, 0.82, -0.56, 1.94, 0.508, 0.522)                                                 # floor mat
add_box(bm, -0.82, 0.82, -0.575, -0.55, 0.52, 0.76); add_box(bm, -0.82, 0.82, -0.67, -0.42, 0.96, 1.215); add_box(bm, -0.80, 0.80, -0.63, -0.47, 0.74, 0.96)   # dashboard, lower dash
add_box(bm, -0.58, -0.22, -0.52, -0.39, 1.19, 1.275)                                                # instrument binnacle (driver's side)
add_box(bm, -0.115, 0.115, -0.44, 0.58, 0.52, 0.80); add_box(bm, -0.16, 0.16, -0.45, -0.40, 0.80, 1.19)   # console, centre stack
add_box(bm, -0.135, 0.135, -0.398, -0.392, 1.02, 1.17, 1)                                           # touch screen
add_box(bm, -0.54, -0.26, -0.392, -0.388, 1.20, 1.262, 1)                                           # instrument cluster
add_tube(bm, [(0.0, -0.10, 0.80), (0.0, -0.13, 0.95)], 0.012, 0, 6, False); add_sphere(bm, (0.0, -0.135, 0.965), 0.03, 0)   # gear lever
add_tube(bm, [(0.0, 0.25, 0.80), (0.0, 0.10, 0.88)], 0.012, 0, 6, False)                             # handbrake
add_box(bm, -0.12, 0.12, front_y(1.66) + 0.10, front_y(1.66) + 0.12, 1.60, 1.67)                      # rear-view mirror
add_tube(bm, [(-0.40, -0.52, 0.98), (-0.40, -0.335, 1.065)], 0.025, 0, 8, False)                     # steering column
for x in (-0.50, -0.38): add_box(bm, x - 0.03, x + 0.03, -0.52, -0.50, 0.58, 0.68)                    # pedals
keep(make("SM_Jeep_Interior", bm, C["INTERIOR"], [M["interior"], M["screen"]]), bevel=0.01)

def seat(name, x, y, w=0.50):
    bm = bmesh.new()
    add_box(bm, x - w / 2, x + w / 2, y, y + 0.50, 0.53, 0.68)
    add_box(bm, x - w / 2, x + w / 2, y + 0.42, y + 0.53, 0.66, 1.26, 0, Matrix.Translation((0, y + 0.47, 0.66)) @ Matrix.Rotation(math.radians(-9), 4, 'X') @ Matrix.Translation((0, -(y + 0.47), -0.66)))
    add_box(bm, x - 0.11, x + 0.11, y + 0.53, y + 0.61, 1.28, 1.45)
    return keep(make(name, bm, C["INTERIOR"], [M["seat"]]), bevel=0.03, segs=3)
seat("SM_Jeep_Seat_FR", -0.40, -0.14); seat("SM_Jeep_Seat_FL", 0.40, -0.14)
seat("SM_Jeep_Seat_RR", -0.40, 0.84); seat("SM_Jeep_Seat_RL", 0.40, 0.84)

bm = bmesh.new()                                                        # steering wheel, built around its own column axis (local Y)
add_torus(bm, (0, 0, 0), 0.185, 0.016, (0, 1, 0), 0, 28, 8); add_cyl(bm, (0, -0.02, 0), 0.05, 0.05, (0, 1, 0), 0, 16)
for a in (90, 210, 330):
    add_box(bm, -0.014, 0.014, -0.022, -0.006, 0.04, 0.18, 0, Matrix.Rotation(math.radians(a), 4, 'Y'))
sw = make("SM_Jeep_SteeringWheel", bm, C["INTERIOR"], [M["interior"]]); sw.location = (-0.40, -0.315, 1.075); sw.rotation_euler = (math.radians(24), 0, 0)
bpy.context.view_layer.update(); keep(sw, bevel=0, uv='top')

# ================================================================= UNDERCARRIAGE
bm = bmesh.new()
for sx in (1, -1): add_box(bm, sx * 0.38, sx * 0.46, -1.78, 1.96, 0.36, 0.46)
for y in (-AX, AX):
    add_cyl(bm, (0, y, TR), 0.045, 1.42, (1, 0, 0), 0, 12); add_cyl(bm, (0.10, y, TR), 0.13, 0.20, (1, 0, 0), 0, 16)
    for sx in (1, -1): add_tube(bm, [(sx * 0.56, y + 0.10, TR - 0.02), (sx * 0.50, y + 0.14, 0.62)], 0.028, 0, 8, False)
add_tube(bm, [(0.10, -AX, TR), (0.06, 0.0, 0.42), (0.10, AX, TR)], 0.03, 0, 8)
add_box(bm, -0.46, 0.46, -1.86, -1.22, 0.335, 0.35); add_box(bm, -0.36, 0.36, 0.20, 1.00, 0.33, 0.45)
add_tube(bm, [(-0.25, -0.70, 0.36), (-0.25, 1.30, 0.36), (-0.55, 1.85, 0.37), (-0.55, 2.10, 0.37)], 0.028, 0, 8); add_cyl(bm, (-0.25, 0.85, 0.36), 0.08, 0.5, (0, 1, 0), 0, 12)
add_box(bm, -0.34, 0.34, -1.66, -0.98, 0.66, 0.98); add_box(bm, -0.26, 0.26, -1.60, -1.04, 0.98, 1.03); add_box(bm, -0.50, 0.50, -1.80, -1.74, 0.66, 1.0)   # engine, cover, radiator
add_box(bm, 0.36, 0.52, -1.25, -0.90, 0.80, 1.0); add_cyl(bm, (-0.42, -1.30, 0.92), 0.07, 0.16, (0, 0, 1), 0, 12)
for sx in (1, -1):
    for cy in (-AX, AX):
        ro, ri = ARCH_R - 0.004, ARCH_R - 0.02
        shell = [(cy - ro * math.cos(math.radians(a)), TR + ro * math.sin(math.radians(a))) for a in range(-6, 187, 12)] + [(cy - ri * math.cos(math.radians(a)), TR + ri * math.sin(math.radians(a))) for a in range(186, -7, -12)]
        add_prism(bm, shell, 'X', sx * 0.565, sx * (W - 0.012))
        add_prism(bm, [(cy - ro * math.cos(math.radians(a)), TR + ro * math.sin(math.radians(a))) for a in range(-6, 187, 12)], 'X', sx * 0.565, sx * 0.575)
keep(make("SM_Jeep_Undercarriage", bm, C["BODY"], [M["under"]]), bevel=0.006, uv='top')

# ================================================================= collision, markers
def cbox(name, ext):
    bm = bmesh.new(); add_box(bm, *ext); o = make(name, bm, C["COLLISION"], []); o.display_type = 'WIRE'; o.hide_render = True; return o
col_objs = [cbox("UCX_Jeep_Body", (-0.93, 0.93, -2.0, 2.13, 0.46, 1.22)), cbox("UCX_Jeep_Cabin", (-0.86, 0.86, -0.68, 2.01, 1.22, 1.79)),
            cbox("UCX_Jeep_Rack", (-0.63, 0.63, -0.1, 1.66, 1.83, 2.0))]
for nm, loc in (("MK_Driver_DoorHandle", (-W - 0.05, 0.10, 1.13)), ("MK_Driver_Seat", (-0.40, 0.11, 0.83)), ("MK_Driver_Exit", (-1.75, 0.05, 0.0))):
    e = bpy.data.objects.new(nm, None); e.empty_display_size = 0.15; e.location = loc; C["RIG"].objects.link(e)
bpy.context.view_layer.update()

# ================================================================= stats
def tri_count(objs):
    n = 0
    for o in objs:
        if o.type == 'MESH': o.data.calc_loop_triangles(); n += len(o.data.loop_triangles)
    return n
model = [o for o in parts.values()]
per = sorted(((tri_count([o]), o.name) for o in model), reverse=True)
total = tri_count(model)

# ================================================================= export
def export(path_base, objs, fbx=True, glb=False, types={'MESH', 'EMPTY'}):
    for o in bpy.data.objects: o.select_set(False)
    for o in objs: o.hide_set(False); o.select_set(True)
    if fbx: bpy.ops.export_scene.fbx(filepath=path_base + ".fbx", use_selection=True, apply_scale_options='FBX_SCALE_ALL', bake_space_transform=True, object_types=types, mesh_smooth_type='FACE', add_leaf_bones=False)
    if glb:
        try: bpy.ops.export_scene.gltf(filepath=path_base + ".glb", use_selection=True, export_format='GLB')
        except Exception as ex: print("glb export failed:", ex)

export(os.path.join(OUT, "Car_Model"), model, glb=True)
export(os.path.join(OUT, "Car_Collision_Meshes", "Car_Collision"), col_objs)

# LODs: merged, decimated whole-car copies
def merged_copy(name, ratio):
    bm = bmesh.new(); mats = []; dg = bpy.context.evaluated_depsgraph_get()
    for o in model:
        me = o.data.copy(); me.transform(o.matrix_world); idx = []
        for m in o.data.materials:
            if m not in mats: mats.append(m)
            idx.append(mats.index(m))
        for p in me.polygons: p.material_index = idx[p.material_index] if idx else 0
        bm.from_mesh(me); bpy.data.meshes.remove(me)
    me = bpy.data.meshes.new(name); bm.to_mesh(me); bm.free()
    for m in mats: me.materials.append(m)
    o = bpy.data.objects.new(name, me); TMP.objects.link(o)
    md = o.modifiers.new("d", 'DECIMATE'); md.ratio = ratio; apply_mods(o)
    for p in o.data.polygons: p.use_smooth = True
    o.data.set_sharp_from_angle(angle=math.radians(40))
    return o
lods = {}
for n, r in (("LOD1", 0.42), ("LOD2", 0.12), ("LOD3", 0.035)):
    o = merged_copy("SM_Jeep_" + n, r); lods[n] = tri_count([o]); export(os.path.join(OUT, "Car_Model_" + n), [o], types={'MESH'})
    bpy.data.objects.remove(o, do_unlink=True)

# ================================================================= renders
def setup_render():
    eng = [e.identifier for e in bpy.types.RenderSettings.bl_rna.properties['engine'].enum_items]
    scene.render.engine = 'BLENDER_EEVEE_NEXT' if 'BLENDER_EEVEE_NEXT' in eng else 'BLENDER_EEVEE'
    w = bpy.data.worlds.new("W"); scene.world = w; w.use_nodes = True
    bg = w.node_tree.nodes["Background"]; bg.inputs[0].default_value = (0.55, 0.60, 0.68, 1); bg.inputs[1].default_value = 0.9
    sun = bpy.data.objects.new("Sun", bpy.data.lights.new("Sun", 'SUN')); sun.data.energy = 3.2; sun.data.angle = 0.2; sun.rotation_euler = (math.radians(52), 0, math.radians(-38)); scene.collection.objects.link(sun)
    fill = bpy.data.objects.new("Fill", bpy.data.lights.new("Fill", 'SUN')); fill.data.energy = 0.8; fill.rotation_euler = (math.radians(60), 0, math.radians(140)); scene.collection.objects.link(fill)
    bm = bmesh.new(); add_box(bm, -30, 30, -30, 30, -0.02, 0.0)
    g = make("RenderGround", bm, TMP, [mat("MAT_RenderGround", (0.16, 0.15, 0.14), 0, 0.9)])
    scene.render.resolution_x, scene.render.resolution_y = 1600, 900
    scene.view_settings.view_transform = 'Filmic' if 'Filmic' in [v.identifier for v in bpy.types.ColorManagedViewSettings.bl_rna.properties['view_transform'].enum_items] else 'AgX'
    cam = bpy.data.objects.new("Cam", bpy.data.cameras.new("Cam")); scene.collection.objects.link(cam); scene.camera = cam; cam.data.lens = 55
    return cam
cam = setup_render()
VIEWS = {"front": (0, -9.5, 1.15), "back": (0, 9.5, 1.15), "left": (10.5, 0.1, 1.1), "right": (-10.5, 0.1, 1.1),
         "three_quarter": (5.6, -6.8, 2.5), "three_quarter_rear": (-5.6, 7.0, 2.5), "top_front": (2.5, -5.5, 5.5), "interior": (0.25, 0.75, 1.42)}
def render(tag, folder, views=None):
    out = []
    for k, loc in VIEWS.items():
        if views and k not in views: continue
        cam.location = loc; tgt = Vector((-0.2, -0.6, 1.1)) if k == "interior" else Vector((0, 0.1, 0.95))
        cam.data.lens = 22 if k == "interior" else 55
        cam.rotation_euler = (tgt - Vector(loc)).to_track_quat('-Z', 'Y').to_euler()
        scene.render.filepath = os.path.join(folder, f"{tag}_{k}.png"); bpy.ops.render.render(write_still=True); out.append(scene.render.filepath)
    return out
render("normal", os.path.join(OUT, "Car_Reference_Renders"))

# ================================================================= save the intact model
with open(os.path.join(OUT, "jeep_validation.txt"), "w") as f:
    f.write("JEEP v2 VALIDATION\nforward axis: -Y in Blender (Unity +Z after FBX axis conversion); 1 unit = 1 m\n")
    lo = Vector((9, 9, 9)); hi = Vector((-9, -9, -9))
    for o in model:
        for v in o.data.vertices:
            w = o.matrix_world @ v.co; lo = Vector(map(min, lo, w)); hi = Vector(map(max, hi, w))
    f.write(f"size: {hi.y - lo.y:.2f} m long, {hi.x - lo.x:.2f} m wide (with mirrors), {hi.z - lo.z:.2f} m tall; wheelbase {2 * AX:.2f} m; tyre diameter {2 * TR:.2f} m\n")
    f.write(f"LOD0 triangles: {total}\nLOD1: {lods['LOD1']}  LOD2: {lods['LOD2']}  LOD3: {lods['LOD3']}\nobjects: {len(model)}  materials: {len(M)}\n")
    f.write("largest parts: " + ", ".join(f"{n} {t}" for t, n in per[:8]) + "\n")
    f.write("collision: " + ", ".join(o.name for o in col_objs) + "\n")
    f.write("pivots: wheels (centre), doors (front hinge), hood (rear hinge), steering wheel (column axis)\n")
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUT, "Car_Model.blend"))
n = 3
while os.path.exists(os.path.join(WORK, f"Jeep_v{n:03d}.blend")): n += 1
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(WORK, f"Jeep_v{n:03d}.blend"), copy=True)

# ================================================================= damage states: deformed body and hood shells
def damaged(src, strength, crush, seed, material):
    random.seed(seed)
    me = src.data.copy(); bm = bmesh.new(); bm.from_mesh(me); bpy.data.meshes.remove(me)
    bmesh.ops.triangulate(bm, faces=bm.faces)
    for _ in range(3):
        long = [e for e in bm.edges if e.calc_length() > 0.16]
        if not long: break
        bmesh.ops.subdivide_edges(bm, edges=long, cuts=1); bmesh.ops.triangulate(bm, faces=bm.faces)
    mw = src.matrix_world; inv = mw.inverted()
    dents = [(Vector((random.choice((-1, 1)) * W, random.uniform(-1.8, 1.9), random.uniform(0.6, 1.15))), random.uniform(0.25, 0.5)) for _ in range(int(3 + strength * 40))]
    for v in bm.verts:
        w = mw @ v.co
        n3 = noise.noise_vector(w * 2.3 + Vector((seed, seed * 0.37, 0))); w += Vector((n3.x, n3.y * 0.6, n3.z * 0.5)) * strength
        n4 = noise.noise_vector(w * 7.0 + Vector((0, seed, 0))); w += n4 * strength * 0.25
        for c, r in dents:
            d = (w - c).length
            if d < r: w.x -= math.copysign(1, c.x) * strength * 1.6 * (1 - d / r) ** 2
        if w.y < -0.85 and crush > 0:                                       # front-end crush: pushed back, pinched and dropped
            k = min(1.0, (-0.85 - w.y) / 1.0) ** 1.2
            w.y += crush * k * (0.75 + 0.5 * noise.noise(w * 3.0 + Vector((seed, 0, 0))))
            w.z += crush * 0.35 * k * math.sin(w.x * 5.0 + seed) - crush * 0.12 * k
            w.x *= 1 - 0.12 * crush * k
        v.co = inv @ w
    me = bpy.data.meshes.new(src.name); bm.to_mesh(me); bm.free(); me.materials.append(material)
    for p in me.polygons: p.use_smooth = True
    me.set_sharp_from_angle(angle=math.radians(50))
    o = bpy.data.objects.new(src.name, me); o.matrix_world = src.matrix_world.copy(); C["DAMAGE"].objects.link(o)
    return o

def hide(names, on=True):
    for o in model:
        if any(o.name.startswith(n) for n in names): o.hide_render = on; o.hide_set(on)

dmg_tris = {"Normal": total}
STATES = (("Damaged", 0.022, 0.05, 3, SCUFFED, ["SM_Jeep_Glass_Windscreen", "SM_Jeep_Headlight_L", "SM_Jeep_Mirror_L"]),
          ("Wrecked", 0.05, 0.22, 7, SCUFFED, ["SM_Jeep_Glass", "SM_Jeep_Headlight", "SM_Jeep_FogLight", "SM_Jeep_Bumper_Front", "SM_Jeep_Mirror", "SM_Jeep_Plate_Front", "SM_Jeep_Indicator_F", "SM_Jeep_Wiper", "SM_Jeep_SpotLight_1", "SM_Jeep_SpotLight_2"]),
          ("Destroyed", 0.085, 0.40, 11, BURNT, ["SM_Jeep_Glass", "SM_Jeep_Headlight", "SM_Jeep_FogLight", "SM_Jeep_Bumper", "SM_Jeep_BullBar", "SM_Jeep_Mirror", "SM_Jeep_Plate", "SM_Jeep_Indicator", "SM_Jeep_Wiper",
                                                  "SM_Jeep_SpotLight", "SM_Jeep_Door_FL", "SM_Jeep_Door_RR", "SM_Jeep_Emblem_FL", "SM_Jeep_Hood", "SM_Jeep_Wheel_FL", "SM_Jeep_TailLight", "SM_Jeep_ReverseLight", "SM_Jeep_SideStep_L", "SM_Jeep_Grille", "SM_Jeep_Antenna"]))
for tag, st, crush, seed, mt, gone in STATES:
    b = damaged(parts["SM_Jeep_Body"], st, crush, seed, mt); h = damaged(parts["SM_Jeep_Hood"], st * 1.6, crush, seed + 1, mt)
    export(os.path.join(OUT, "Car_Damaged", "Car_Damage_" + tag), [b, h])
    # evidence render of the whole car in this state
    parts["SM_Jeep_Body"].hide_render = True; parts["SM_Jeep_Hood"].hide_render = True
    hide(gone, True)
    swapped = []
    if tag != "Damaged":
        for o in model:
            if o.data.materials and o.data.materials[0] == M["paint"] and o.name not in ("SM_Jeep_Body", "SM_Jeep_Hood"):
                swapped.append(o); o.data.materials[0] = mt
    if tag == "Destroyed": h.hide_render = True
    vis = [o for o in model if not o.hide_render] + [b] + ([] if h.hide_render else [h])
    dmg_tris[tag] = tri_count(vis)
    render(tag.lower(), os.path.join(OUT, "Car_Reference_Renders"), views=("three_quarter", "left", "front"))
    for o in swapped: o.data.materials[0] = M["paint"]
    hide(gone, False); parts["SM_Jeep_Body"].hide_render = False; parts["SM_Jeep_Hood"].hide_render = False
    b.hide_render = True; h.hide_render = True
with open(os.path.join(OUT, "jeep_validation.txt"), "a") as f:
    f.write("damage state triangles (visible car): " + ", ".join(f"{k} {v}" for k, v in dmg_tris.items()) + "\n")
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUT, "Car_Damaged.blend"))

# ================================================================= copy into the Unity project
if os.path.isdir(UNITY):
    for f in ["Car_Model.fbx", "Car_Model_LOD1.fbx", "Car_Model_LOD2.fbx", "jeep_validation.txt", "Car_Collision_Meshes/Car_Collision.fbx",
              "Car_Damaged/Car_Damage_Damaged.fbx", "Car_Damaged/Car_Damage_Wrecked.fbx", "Car_Damaged/Car_Damage_Destroyed.fbx"]:
        shutil.copy(os.path.join(OUT, f), os.path.join(UNITY, os.path.basename(f)))
print("JEEP_BUILD_OK", total, lods, dmg_tris)
print("TOP", per[:10])
