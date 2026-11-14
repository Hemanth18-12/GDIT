# Builds the investigator's Jeep for Legacy of the Temple (own work, no real brand).
# Run:  Blender --background --python Tools/blender/build_jeep.py -- <out_dir> <unity_dir>
# Makes: Car_Model.blend, Car_Model.fbx/.glb, Car_Damage_*.fbx, collision meshes and reference renders.
import bpy, bmesh, math, os, sys, random
from mathutils import Vector, Matrix, Euler

argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
OUT = argv[0] if len(argv) > 0 else os.path.abspath("Art_Source/Jeep")
UNITY = argv[1] if len(argv) > 1 else None
os.makedirs(OUT, exist_ok=True)

bpy.ops.wm.read_factory_settings(use_empty=True)
scene = bpy.context.scene

# ---------------------------------------------------------------- collections and materials
def coll(name):
    c = bpy.data.collections.new(name); scene.collection.children.link(c); return c
C = {n: coll(n) for n in ["Body", "Doors", "Wheels", "Lights", "Glass", "Interior", "Accessories", "Undercarriage", "Collision"]}

def mat(name, col, metallic=0.0, rough=0.5, emit=None, alpha=1.0):
    m = bpy.data.materials.new(name); m.use_nodes = True
    b = m.node_tree.nodes.get("Principled BSDF")
    b.inputs["Base Color"].default_value = (*col, 1); b.inputs["Metallic"].default_value = metallic; b.inputs["Roughness"].default_value = rough
    if emit:
        b.inputs["Emission Color"].default_value = (*emit[:3], 1); b.inputs["Emission Strength"].default_value = emit[3]
    if alpha < 1:
        b.inputs["Alpha"].default_value = alpha
        try: m.surface_render_method = 'BLENDED'
        except Exception: pass
    m.diffuse_color = (*col, alpha)
    return m

M = {
    "paint": mat("Jeep_Paint_Olive", (0.115, 0.15, 0.07), 0.55, 0.38),
    "trim": mat("Jeep_BlackTrim", (0.02, 0.02, 0.02), 0.1, 0.55),
    "rubber": mat("Jeep_Rubber", (0.015, 0.015, 0.015), 0.0, 0.9),
    "alloy": mat("Jeep_Alloy", (0.55, 0.56, 0.58), 1.0, 0.28),
    "chrome": mat("Jeep_Chrome", (0.8, 0.8, 0.8), 1.0, 0.12),
    "glass": mat("Jeep_Glass", (0.03, 0.05, 0.05), 0.0, 0.05, alpha=0.38),
    "head": mat("Jeep_Light_Head", (1.0, 0.96, 0.82), 0.0, 0.1, emit=(1.0, 0.93, 0.75, 2.5)),
    "tail": mat("Jeep_Light_Tail", (0.5, 0.02, 0.02), 0.0, 0.2, emit=(1.0, 0.05, 0.03, 0.6)),
    "reverse": mat("Jeep_Light_Reverse", (0.9, 0.9, 0.9), 0.0, 0.2),
    "indicator": mat("Jeep_Light_Indicator", (1.0, 0.5, 0.05), 0.0, 0.2),
    "gold": mat("Jeep_Gold_Emblem", (0.83, 0.62, 0.22), 1.0, 0.3),
    "interior": mat("Jeep_Interior", (0.06, 0.055, 0.05), 0.0, 0.75),
    "seat": mat("Jeep_Seat_Leather", (0.11, 0.075, 0.05), 0.0, 0.6),
    "plate": mat("Jeep_Plate", (0.92, 0.92, 0.88), 0.0, 0.5),
    "under": mat("Jeep_Undercarriage", (0.035, 0.035, 0.04), 0.4, 0.7),
}

# ---------------------------------------------------------------- mesh helpers
def finish(name, bm, c, m, smooth=False):
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    me = bpy.data.meshes.new(name); bm.to_mesh(me); bm.free()
    if smooth:
        for p in me.polygons: p.use_smooth = True
    me.materials.append(m)
    o = bpy.data.objects.new(name, me); C[c].objects.link(o); return o

def box(name, size, loc, c, m, bevel=0.0, rot=(0, 0, 0)):
    bm = bmesh.new(); bmesh.ops.create_cube(bm, size=1.0)
    for v in bm.verts: v.co = Vector((v.co.x * size[0], v.co.y * size[1], v.co.z * size[2]))
    o = finish(name, bm, c, m); o.location = loc; o.rotation_euler = rot
    if bevel > 0:
        b = o.modifiers.new("Bevel", 'BEVEL'); b.width = bevel; b.segments = 2; b.limit_method = 'ANGLE'
    return o

def cyl(name, r, depth, loc, c, m, axis='X', segs=28, r2=None, bevel=0.0):
    bm = bmesh.new(); bmesh.ops.create_cone(bm, cap_ends=True, segments=segs, radius1=r, radius2=r if r2 is None else r2, depth=depth)
    o = finish(name, bm, c, m, smooth=True); o.location = loc
    o.rotation_euler = (0, math.radians(90), 0) if axis == 'X' else (math.radians(90), 0, 0) if axis == 'Y' else (0, 0, 0)
    if bevel > 0:
        b = o.modifiers.new("Bevel", 'BEVEL'); b.width = bevel; b.segments = 2; b.limit_method = 'ANGLE'
    return o

def tube(name, a, b, r, c, m, segs=12):
    a, b = Vector(a), Vector(b); d = b - a
    bm = bmesh.new(); bmesh.ops.create_cone(bm, cap_ends=True, segments=segs, radius1=r, radius2=r, depth=d.length)
    o = finish(name, bm, c, m, smooth=True); o.location = (a + b) / 2
    o.rotation_euler = Vector((0, 0, 1)).rotation_difference(d.normalized()).to_euler(); return o

def profile(name, pts, x0, x1, c, m, bevel=0.0):
    """Extrude a side profile (list of (y, z)) across the car from x0 to x1."""
    bm = bmesh.new(); vs = [bm.verts.new((x0, y, z)) for y, z in pts]; f = bm.faces.new(vs)
    r = bmesh.ops.extrude_face_region(bm, geom=[f])
    bmesh.ops.translate(bm, verts=[e for e in r["geom"] if isinstance(e, bmesh.types.BMVert)], vec=(x1 - x0, 0, 0))
    o = finish(name, bm, c, m)
    if bevel > 0:
        b = o.modifiers.new("Bevel", 'BEVEL'); b.width = bevel; b.segments = 3; b.limit_method = 'ANGLE'; b.angle_limit = math.radians(25)
    return o

def apply_mods(o):
    bpy.context.view_layer.update(); dg = bpy.context.evaluated_depsgraph_get()
    me = bpy.data.meshes.new_from_object(o.evaluated_get(dg)); old = o.data
    o.modifiers.clear(); o.data = me; me.name = o.name
    if old.users == 0: bpy.data.meshes.remove(old)

def cut(o, cutter):
    md = o.modifiers.new("Cut", 'BOOLEAN'); md.operation = 'DIFFERENCE'; md.object = cutter
    try: md.solver = 'EXACT'
    except Exception: pass
    apply_mods(o); bpy.data.objects.remove(cutter, do_unlink=True)

def join(name, parts, c, origin=None):
    """Bake every part's transform and modifiers into one mesh object whose origin is `origin` (world)."""
    bpy.context.view_layer.update(); bm = bmesh.new(); mats = []
    for p in parts:
        apply_mods(p); bpy.context.view_layer.update()
        for mt in p.data.materials:
            if mt not in mats: mats.append(mt)
        tmp = bmesh.new(); tmp.from_mesh(p.data); tmp.transform(p.matrix_world)
        me = bpy.data.meshes.new("tmp"); tmp.to_mesh(me); tmp.free()
        remap = [mats.index(mt) for mt in p.data.materials] or [0]
        for poly in me.polygons: poly.material_index = remap[min(poly.material_index, len(remap) - 1)]
        off = len(bm.verts); bm.from_mesh(me)
        bpy.data.meshes.remove(me)
    me = bpy.data.meshes.new(name); bm.to_mesh(me); bm.free()
    for mt in mats: me.materials.append(mt)
    # material indices were lost by from_mesh on separate temp meshes only if slots differ: rebuild per part
    for p in parts: bpy.data.objects.remove(p, do_unlink=True)
    o = bpy.data.objects.new(name, me); C[c].objects.link(o)
    if origin is not None:
        me.transform(Matrix.Translation(-Vector(origin))); o.location = origin
    return o

def single(o, origin=None):
    """Bake one object's transform so its mesh is in world space with the origin where asked."""
    nm = o.name; cn = [k for k, v in C.items() if o.name in v.objects][0]
    return join(nm, [o], cn, origin)

# ---------------------------------------------------------------- dimensions (metres). Front of the car is -Y.
HW = 0.90            # half width of the body
FA, RA = -1.38, 1.42  # front / rear axle (y)
TR = 0.80            # wheel centre x
WR, WW = 0.40, 0.27  # tyre radius and width
root = bpy.data.objects.new("Jeep", None); scene.collection.objects.link(root)
parts = {}
def keep(o, parent=None):
    o.parent = parent or root; parts[o.name] = o; return o

# ---------------------------------------------------------------- body shell
outer = [(-2.05, 0.46), (-2.10, 0.62), (-2.08, 0.98), (-1.96, 1.07), (-0.76, 1.13), (-0.72, 1.13), (-0.38, 1.75), (1.74, 1.77), (1.98, 1.72), (2.03, 1.00), (2.00, 0.46)]
body = profile("Body", outer, -HW, HW, "Body", M["paint"], bevel=0.035)
apply_mods(body)
# hollow cabin
cav = profile("cav", [(-0.60, 0.56), (-0.655, 1.11), (-0.325, 1.69), (1.92, 1.69), (1.95, 0.56)], -HW + 0.08, HW - 0.08, "Body", M["interior"]); cut(body, cav)
# wheel arches (each side separately so the floor stays closed)
for sx in (-1, 1):
    for ay in (FA, RA):
        cut(body, cyl("arch", 0.50, 0.36, (sx * (HW - 0.12), ay, WR), "Body", M["trim"], 'X', 40))
# door and window openings
DOORS = {"Door_FL": (-1, -0.62, 0.20), "Door_FR": (1, -0.62, 0.20), "Door_RL": (-1, 0.22, 0.92), "Door_RR": (1, 0.22, 0.92)}
for nm, (sx, y0, y1) in DOORS.items():
    cut(body, box("dc", (0.3, y1 - y0, 1.12), (sx * HW, (y0 + y1) / 2, 1.12), "Body", M["trim"]))
for sx in (-1, 1):
    cut(body, box("qw", (0.3, 0.80, 0.46), (sx * HW, 1.44, 1.39), "Body", M["trim"]))          # rear quarter windows
slope = math.atan2(0.34, 0.62)
cut(body, box("ws", (1.50, 0.5, 0.56), (0, -0.55, 1.44), "Body", M["trim"], rot=(-slope, 0, 0)))   # windshield
cut(body, box("rw", (1.24, 0.4, 0.40), (0, 2.0, 1.42), "Body", M["trim"]))                        # rear window
for i, p in enumerate(body.data.polygons): p.use_smooth = False
keep(single(body, (0, 0, 0)))
body = parts["Body"]

# interior lining so the inside is not paint-coloured
lin = [box("l1", (1.62, 2.5, 0.03), (0, 0.66, 0.575), "Interior", M["interior"]),
       box("l2", (0.26, 1.0, 0.44), (-0.70, RA, 0.76), "Interior", M["interior"], 0.03), box("l3", (0.26, 1.0, 0.44), (0.70, RA, 0.76), "Interior", M["interior"], 0.03),
       box("l4", (1.62, 0.03, 0.5), (0, -0.585, 0.82), "Interior", M["interior"])]
keep(join("Interior_Lining", lin, "Interior", (0, 0, 0)))

# hood (opens, hinge at the windshield end)
hood = box("Hood", (1.70, 1.22, 0.045), (0, -1.37, 1.118), "Body", M["paint"], 0.015, rot=(math.atan2(0.06, 1.2), 0, 0))
scoop = box("hs", (0.5, 0.6, 0.03), (0, -1.3, 1.15), "Body", M["paint"], 0.012, rot=(math.atan2(0.06, 1.2), 0, 0))
keep(join("Hood", [hood, scoop], "Body", (0, -0.77, 1.14)))

# ---------------------------------------------------------------- glass
keep(single(box("Glass_Windshield", (1.52, 0.012, 0.58), (0, -0.565, 1.44), "Glass", M["glass"], rot=(-slope, 0, 0)), (0, -0.565, 1.44)))
keep(single(box("Glass_Rear", (1.26, 0.012, 0.42), (0, 2.0, 1.42), "Glass", M["glass"]), (0, 2.0, 1.42)))
for sx, s in ((-1, "L"), (1, "R")):
    keep(single(box("Glass_Quarter_" + s, (0.012, 0.82, 0.48), (sx * (HW - 0.03), 1.44, 1.39), "Glass", M["glass"]), (sx * (HW - 0.03), 1.44, 1.39)))
wip = [box("w", (0.5, 0.02, 0.02), (sx * 0.36, -0.69, 1.19), "Accessories", M["trim"], rot=(-slope, 0, math.radians(8 * sx))) for sx in (-1, 1)]
keep(join("Wipers", wip, "Accessories", (0, -0.69, 1.19)))

# ---------------------------------------------------------------- doors (origin = hinge at the front edge)
def emblem(sx, yc):
    x = sx * (HW + 0.008); ps = []
    for i, (w, h, z) in enumerate([(0.26, 0.03, 0.80), (0.20, 0.035, 0.832), (0.14, 0.04, 0.87), (0.09, 0.05, 0.915)]):
        ps.append(box("e", (0.008, w, h), (x, yc, z), "Doors", M["gold"]))
    ps.append(cyl("e", 0.035, 0.008, (x, yc, 0.965), "Doors", M["gold"], 'X', 16))
    ps.append(box("e", (0.008, 0.008, 0.07), (x, yc, 1.03), "Doors", M["gold"]))
    ps.append(box("e", (0.008, 0.06, 0.025), (x, yc + 0.035, 1.05), "Doors", M["gold"]))
    return ps

for nm, (sx, y0, y1) in DOORS.items():
    yc = (y0 + y1) / 2; x = sx * (HW - 0.04)
    panel = box(nm, (0.075, y1 - y0 - 0.012, 1.108), (x, yc, 1.12), "Doors", M["paint"], 0.012); apply_mods(panel)
    cut(panel, box("wh", (0.3, y1 - y0 - 0.16, 0.46), (x, yc, 1.39), "Doors", M["trim"]))
    ps = [panel,
          box("h", (0.03, 0.14, 0.035), (sx * (HW + 0.012), y1 - 0.13, 1.05), "Doors", M["trim"], 0.008),
          box("s", (0.012, y1 - y0 - 0.03, 0.10), (sx * (HW + 0.004), yc, 0.64), "Doors", M["trim"]),
          box("in", (0.02, y1 - y0 - 0.06, 0.52), (sx * (HW - 0.085), yc, 0.86), "Doors", M["interior"], 0.01)]
    if nm.startswith("Door_F"):
        ps += emblem(sx, yc + 0.04)
        ps += [box("m", (0.16, 0.05, 0.03), (sx * (HW + 0.09), y0 + 0.07, 1.16), "Doors", M["trim"], 0.008),
               box("m", (0.07, 0.11, 0.20), (sx * (HW + 0.19), y0 + 0.075, 1.21), "Doors", M["trim"], 0.02),
               box("m", (0.05, 0.008, 0.16), (sx * (HW + 0.19), y0 + 0.134, 1.21), "Doors", M["chrome"])]
    door = keep(join(nm, ps, "Doors", (sx * (HW - 0.02), y0 + 0.006, 1.12)))
    g = single(box("Glass_" + nm, (0.012, y1 - y0 - 0.14, 0.48), (x, yc, 1.39), "Glass", M["glass"]), (sx * (HW - 0.02), y0 + 0.006, 1.12))
    g.parent = door; g.location = (0, 0, 0); parts[g.name] = g

# rear door with spare wheel carrier (visual, opens sideways)
rd = [box("rd", (1.5, 0.04, 0.62), (0, 2.035, 0.86), "Doors", M["paint"], 0.012), box("rd", (0.14, 0.03, 0.035), (0.55, 2.062, 1.02), "Doors", M["trim"], 0.008)]
rear_door = keep(join("Door_Rear", rd, "Doors", (-0.75, 2.03, 0.86)))

# ---------------------------------------------------------------- front: grille, lights, bumper, bull bar, plate
gr = [box("g", (1.10, 0.05, 0.38), (0, -2.10, 0.86), "Body", M["trim"], 0.01)]
for i in range(7): gr.append(box("g", (0.07, 0.03, 0.30), (-0.39 + i * 0.13, -2.135, 0.86), "Body", M["chrome"], 0.008))
keep(join("Grille", gr, "Body", (0, -2.10, 0.86)))
for sx, s in ((-1, "L"), (1, "R")):
    hl = [cyl("h", 0.125, 0.06, (sx * 0.70, -2.105, 0.88), "Lights", M["chrome"], 'Y', 28), cyl("h", 0.105, 0.065, (sx * 0.70, -2.112, 0.88), "Lights", M["head"], 'Y', 28)]
    keep(join("Headlight_" + s, hl, "Lights", (sx * 0.70, -2.11, 0.88)))
    keep(single(box("Indicator_F" + s, (0.16, 0.04, 0.05), (sx * 0.70, -2.105, 0.70), "Lights", M["indicator"], 0.01), (sx * 0.70, -2.105, 0.70)))
    fg = [cyl("f", 0.065, 0.05, (sx * 0.62, -2.20, 0.56), "Lights", M["trim"], 'Y', 20), cyl("f", 0.05, 0.055, (sx * 0.62, -2.205, 0.56), "Lights", M["head"], 'Y', 20)]
    keep(join("FogLight_" + s, fg, "Lights", (sx * 0.62, -2.20, 0.56)))
    keep(single(box("TailLight_" + s, (0.14, 0.05, 0.30), (sx * 0.80, 2.03, 0.98), "Lights", M["tail"], 0.012), (sx * 0.80, 2.03, 0.98)))
    keep(single(box("ReverseLight_" + s, (0.12, 0.05, 0.07), (sx * 0.80, 2.03, 0.76), "Lights", M["reverse"], 0.008), (sx * 0.80, 2.03, 0.76)))
    keep(single(box("Indicator_R" + s, (0.12, 0.05, 0.07), (sx * 0.80, 2.03, 1.18), "Lights", M["indicator"], 0.008), (sx * 0.80, 2.03, 1.18)))
keep(single(box("Bumper_Front", (1.90, 0.16, 0.20), (0, -2.17, 0.54), "Body", M["trim"], 0.03), (0, -2.17, 0.54)))
keep(single(box("Bumper_Rear", (1.90, 0.14, 0.20), (0, 2.09, 0.54), "Body", M["trim"], 0.03), (0, 2.09, 0.54)))
bb = []
for sx in (-1, 1):
    bb += [tube("b", (sx * 0.42, -2.27, 0.46), (sx * 0.42, -2.27, 1.10), 0.028, "Accessories", M["trim"]),
           tube("b", (sx * 0.42, -2.27, 1.10), (sx * 0.42, -2.12, 1.10), 0.028, "Accessories", M["trim"]),
           tube("b", (sx * 0.42, -2.27, 0.60), (sx * 0.86, -2.20, 0.62), 0.024, "Accessories", M["trim"])]
bb += [tube("b", (-0.42, -2.27, 1.08), (0.42, -2.27, 1.08), 0.028, "Accessories", M["trim"]), tube("b", (-0.42, -2.27, 0.78), (0.42, -2.27, 0.78), 0.024, "Accessories", M["trim"]),
       box("b", (0.9, 0.012, 0.16), (0, -2.26, 0.50), "Accessories", M["trim"])]
keep(join("BullBar", bb, "Accessories", (0, -2.27, 0.78)))

def plate(name, y, face):
    ps = [box("p", (0.46, 0.012, 0.12), (0, y, 0.60), "Accessories", M["plate"], 0.004)]
    try:
        cu = bpy.data.curves.new("txt", 'FONT'); cu.body = "LOT 7721"; cu.size = 0.085; cu.extrude = 0.003; cu.align_x = 'CENTER'; cu.align_y = 'CENTER'
        t = bpy.data.objects.new("t", cu); C["Accessories"].objects.link(t)
        t.rotation_euler = (math.radians(90), 0, 0 if face < 0 else math.radians(180)); t.location = (0, y + face * 0.008, 0.60)
        bpy.context.view_layer.update(); me = bpy.data.meshes.new_from_object(t.evaluated_get(bpy.context.evaluated_depsgraph_get()))
        mo = bpy.data.objects.new("pt", me); C["Accessories"].objects.link(mo); mo.matrix_world = t.matrix_world.copy(); me.materials.append(M["trim"])
        bpy.data.objects.remove(t, do_unlink=True); ps.append(mo)
    except Exception as e: print("plate text skipped:", e)
    return keep(join(name, ps, "Accessories", (0, y, 0.60)))
plate("Plate_Front", -2.262, -1); plate("Plate_Rear", 2.165, 1)

# ---------------------------------------------------------------- roof rack with four spot lights
rk = []
for sx in (-1, 1):
    rk.append(tube("r", (sx * 0.74, -0.25, 1.90), (sx * 0.74, 1.80, 1.90), 0.022, "Accessories", M["trim"]))
    for y in (-0.2, 0.75, 1.72): rk.append(tube("r", (sx * 0.74, y, 1.76), (sx * 0.74, y, 1.90), 0.02, "Accessories", M["trim"]))
for i in range(7): rk.append(tube("r", (-0.74, -0.25 + i * 0.34, 1.90), (0.74, -0.25 + i * 0.34, 1.90), 0.016, "Accessories", M["trim"]))
rk.append(tube("r", (-0.74, -0.25, 1.98), (0.74, -0.25, 1.98), 0.02, "Accessories", M["trim"]))
for sx in (-1, 1): rk.append(tube("r", (sx * 0.74, -0.25, 1.90), (sx * 0.74, -0.25, 1.98), 0.02, "Accessories", M["trim"]))
keep(join("RoofRack", rk, "Accessories", (0, 0.78, 1.90)))
for i in range(4):
    x = -0.48 + i * 0.32
    sp = [cyl("s", 0.085, 0.09, (x, -0.29, 2.07), "Lights", M["trim"], 'Y', 22), cyl("s", 0.07, 0.095, (x, -0.297, 2.07), "Lights", M["head"], 'Y', 22),
          tube("s", (x, -0.27, 1.98), (x, -0.27, 2.02), 0.015, "Lights", M["trim"], 8)]
    keep(join("SpotLight_%d" % (i + 1), sp, "Lights", (x, -0.29, 2.07)))

# side steps, arch flares, mud flaps, antenna
for sx, s in ((-1, "L"), (1, "R")):
    st = [box("s", (0.20, 1.62, 0.05), (sx * (HW + 0.06), 0.03, 0.42), "Accessories", M["trim"], 0.015), box("s", (0.10, 1.5, 0.008), (sx * (HW + 0.07), 0.03, 0.449), "Accessories", M["chrome"])]
    for y in (-0.6, 0.66): st.append(tube("s", (sx * (HW - 0.05), y, 0.47), (sx * (HW + 0.06), y, 0.42), 0.02, "Accessories", M["trim"], 8))
    keep(join("SideStep_" + s, st, "Accessories", (sx * (HW + 0.06), 0.03, 0.42)))
    fl = []
    for ay in (FA, RA):
        bpy.ops.mesh.primitive_torus_add(major_radius=0.525, minor_radius=0.06, major_segments=40, minor_segments=8, location=(sx * (HW - 0.01), ay, WR), rotation=(0, math.radians(90), 0))
        t = bpy.context.active_object
        for cc in t.users_collection: cc.objects.unlink(t)
        C["Accessories"].objects.link(t); t.data.materials.append(M["trim"])
        for p in t.data.polygons: p.use_smooth = True
        bm = bmesh.new(); bm.from_mesh(t.data)
        zcut = (0.47 - WR)  # keep only the part above the sill, in the torus' local frame (local X = world Z after the rotation)
        geom = bm.verts[:] + bm.edges[:] + bm.faces[:]
        bmesh.ops.bisect_plane(bm, geom=geom, plane_co=(-zcut, 0, 0), plane_no=(1, 0, 0), clear_inner=False, clear_outer=True)
        bm.to_mesh(t.data); bm.free(); fl.append(t)
    keep(join("ArchFlares_" + s, fl, "Accessories", (sx * HW, 0, WR)))
keep(single(tube("Antenna", (-0.80, -0.70, 1.14), (-0.80, -0.62, 1.95), 0.006, "Accessories", M["trim"], 6), (-0.80, -0.70, 1.14)))

# ---------------------------------------------------------------- wheels (tyre with tread lugs, alloy rim, 6 spokes)
def wheel(name, loc, spare=False):
    ps = [cyl("t", WR - 0.012, WW, (0, 0, 0), "Wheels", M["rubber"], 'X', 40, bevel=0.04)]
    for i in range(30):   # tread lugs, staggered
        a = i / 30 * math.tau; side = -1 if i % 2 else 1
        lug = box("l", (WW * 0.46, 0.055, 0.03), (side * WW * 0.24, math.cos(a) * (WR - 0.006), math.sin(a) * (WR - 0.006)), "Wheels", M["rubber"], rot=(a - math.pi / 2, 0, 0))
        ps.append(lug)
    ps.append(cyl("r", 0.265, WW + 0.004, (0, 0, 0), "Wheels", M["alloy"], 'X', 32, bevel=0.012))
    ps.append(cyl("r", 0.215, WW + 0.012, (0, 0, 0), "Wheels", M["trim"], 'X', 32))
    for sxx in (-1, 1):
        for i in range(6):
            a = i / 6 * math.tau
            ps.append(box("s", (0.03, 0.062, 0.215), (sxx * (WW / 2 - 0.004), math.cos(a) * 0.118, math.sin(a) * 0.118), "Wheels", M["alloy"], 0.008, rot=(a - math.pi / 2, 0, 0)))
        ps.append(cyl("h", 0.07, 0.04, (sxx * (WW / 2 + 0.002), 0, 0), "Wheels", M["alloy"], 'X', 18, bevel=0.01))
        for i in range(5):
            a = i / 5 * math.tau; ps.append(cyl("n", 0.011, 0.02, (sxx * (WW / 2 + 0.02), math.cos(a) * 0.045, math.sin(a) * 0.045), "Wheels", M["chrome"], 'X', 6))
    w = join(name, ps, "Wheels", (0, 0, 0))
    if spare: w.rotation_euler = (0, 0, math.radians(90))
    w.location = loc; return keep(w)
wheel("Wheel_FL", (-TR, FA, WR)); wheel("Wheel_FR", (TR, FA, WR)); wheel("Wheel_RL", (-TR, RA, WR)); wheel("Wheel_RR", (TR, RA, WR))
sp = wheel("Wheel_Spare", (0.12, 2.23, 1.02), spare=True); sp.parent = rear_door; sp.location = Vector((0.12, 2.23, 1.02)) - Vector((-0.75, 2.03, 0.86))
mount = single(tube("SpareMount", (0.12, 2.05, 1.02), (0.12, 2.12, 1.02), 0.06, "Accessories", M["trim"], 12), (-0.75, 2.03, 0.86)); mount.parent = rear_door; mount.location = (0, 0, 0); parts[mount.name] = mount

# ---------------------------------------------------------------- interior
def seat(name, x, y, w):
    ps = [box("s", (w, 0.50, 0.14), (x, y, 0.80), "Interior", M["seat"], 0.04), box("s", (w, 0.13, 0.62), (x, y + 0.27, 1.12), "Interior", M["seat"], 0.04, rot=(math.radians(-10), 0, 0)),
          box("s", (w * 0.5, 0.09, 0.17), (x, y + 0.34, 1.51), "Interior", M["seat"], 0.03), box("s", (w * 0.9, 0.44, 0.16), (x, y, 0.66), "Interior", M["interior"])]
    return keep(join(name, ps, "Interior", (x, y, 0.80)))
seat("Seat_FL", -0.40, -0.02, 0.50); seat("Seat_FR", 0.40, -0.02, 0.50); seat("Seat_Rear", 0, 0.84, 1.40)
dash = [box("d", (1.60, 0.30, 0.30), (0, -0.50, 1.00), "Interior", M["interior"], 0.05), box("d", (0.36, 0.03, 0.20), (0, -0.345, 1.03), "Interior", M["trim"], 0.01),
        box("d", (0.26, 0.012, 0.14), (0, -0.328, 1.035), "Interior", mat("Jeep_Screen", (0.05, 0.18, 0.3), 0, 0.2, emit=(0.1, 0.4, 0.7, 0.8))),
        box("d", (0.28, 0.9, 0.22), (0, 0.10, 0.70), "Interior", M["interior"], 0.03), cyl("d", 0.022, 0.2, (0, 0.02, 0.88), "Interior", M["trim"], 'Z', 10), cyl("d", 0.035, 0.05, (0, 0.02, 0.99), "Interior", M["chrome"], 'Z', 12)]
keep(join("Dashboard", dash, "Interior", (0, -0.50, 1.00)))
# steering wheel (right-hand drive as in the reference). The empty is tilted; the wheel spins about its own local Z.
col = bpy.data.objects.new("SteeringColumn", None); C["Interior"].objects.link(col); col.location = (0.40, -0.30, 1.07); col.rotation_euler = (math.radians(-62), 0, 0); keep(col)
bpy.ops.mesh.primitive_torus_add(major_radius=0.17, minor_radius=0.018, major_segments=32, minor_segments=8, location=(0, 0, 0))
tor = bpy.context.active_object
for cc in tor.users_collection: cc.objects.unlink(tor)
C["Interior"].objects.link(tor); tor.data.materials.append(M["trim"])
for p in tor.data.polygons: p.use_smooth = True
sw = [tor, cyl("h", 0.05, 0.03, (0, 0, 0), "Interior", M["trim"], 'Z', 16)] + [box("sp", (0.022, 0.16, 0.014), (math.sin(a) * 0.08, math.cos(a) * 0.08, 0), "Interior", M["trim"], rot=(0, 0, -a)) for a in (0, math.radians(120), math.radians(240))]
stw = join("SteeringWheel", sw, "Interior", (0, 0, 0)); stw.parent = col; parts[stw.name] = stw
keep(single(tube("SteeringShaft", (0.40, -0.30, 1.07), (0.40, -0.52, 0.95), 0.02, "Interior", M["trim"], 8), (0.40, -0.30, 1.07)))

# ---------------------------------------------------------------- undercarriage
uc = [box("u", (0.10, 3.6, 0.10), (-0.45, 0, 0.44), "Undercarriage", M["under"]), box("u", (0.10, 3.6, 0.10), (0.45, 0, 0.44), "Undercarriage", M["under"]),
      tube("u", (-TR + 0.1, FA, WR), (TR - 0.1, FA, WR), 0.045, "Undercarriage", M["under"]), tube("u", (-TR + 0.1, RA, WR), (TR - 0.1, RA, WR), 0.055, "Undercarriage", M["under"]),
      cyl("u", 0.12, 0.2, (0, RA, WR), "Undercarriage", M["under"], 'Y', 14), tube("u", (0, -0.6, 0.42), (0, RA, WR), 0.03, "Undercarriage", M["under"]),
      tube("u", (0.30, -1.0, 0.40), (0.30, 1.9, 0.40), 0.03, "Undercarriage", M["under"]), tube("u", (0.30, 1.9, 0.40), (0.62, 2.12, 0.42), 0.035, "Undercarriage", M["chrome"]),
      box("u", (0.5, 0.7, 0.16), (-0.25, 1.5, 0.42), "Undercarriage", M["under"], 0.03), box("u", (1.5, 2.6, 0.02), (0, 0.5, 0.455), "Undercarriage", M["under"])]
for sx in (-1, 1):
    for ay in (FA, RA): uc.append(cyl("u", 0.06, 0.3, (sx * 0.62, ay, 0.62), "Undercarriage", M["chrome"], 'Z', 10))
keep(join("Undercarriage", uc, "Undercarriage", (0, 0, 0.44)))

# ---------------------------------------------------------------- collision meshes (simple boxes)
col_objs = [single(box("COL_Body", (1.84, 4.3, 0.62), (0, 0, 0.77), "Collision", M["trim"]), (0, 0, 0)), single(box("COL_Cabin", (1.80, 2.5, 0.70), (0, 0.75, 1.42), "Collision", M["trim"]), (0, 0, 0))]
for o in col_objs: o.hide_render = True; o.display_type = 'WIRE'

# ---------------------------------------------------------------- UVs and stats
tris = 0
for o in parts.values():
    if o.type != 'MESH': continue
    bm = bmesh.new(); bm.from_mesh(o.data)
    uv = bm.loops.layers.uv.verify()
    for f in bm.faces:     # box projection, 1 unit = 1 m
        n = f.normal; ax = max(range(3), key=lambda i: abs(n[i]))
        for l in f.loops:
            co = l.vert.co; l[uv].uv = (co.y, co.z) if ax == 0 else (co.x, co.z) if ax == 1 else (co.x, co.y)
    bm.to_mesh(o.data); bm.free(); o.data.calc_loop_triangles(); tris += len(o.data.loop_triangles)
print("JEEP parts=%d tris=%d" % (len(parts), tris))

def export(path_base, objs, fbx=True, glb=False):
    for o in bpy.data.objects: o.select_set(False)
    for o in objs:
        try: o.hide_set(False); o.select_set(True)
        except Exception: pass
    if fbx:
        bpy.ops.export_scene.fbx(filepath=path_base + ".fbx", use_selection=True, apply_scale_options='FBX_SCALE_ALL', bake_space_transform=True, object_types={'MESH', 'EMPTY'}, mesh_smooth_type='FACE', add_leaf_bones=False)
    if glb:
        try: bpy.ops.export_scene.gltf(filepath=path_base + ".glb", use_selection=True, export_format='GLB')
        except Exception as e: print("glb export failed:", e)

# ---------------------------------------------------------------- renders (evidence)
def render_views(tag):
    try:
        scene.render.engine = 'BLENDER_EEVEE_NEXT' if 'BLENDER_EEVEE_NEXT' in [e.identifier for e in bpy.types.RenderSettings.bl_rna.properties['engine'].enum_items] else 'BLENDER_EEVEE'
    except Exception: scene.render.engine = 'BLENDER_WORKBENCH'
    scene.render.resolution_x, scene.render.resolution_y = 1280, 720
    w = bpy.data.worlds.new("W"); scene.world = w; w.use_nodes = True; w.node_tree.nodes["Background"].inputs[0].default_value = (0.55, 0.6, 0.68, 1); w.node_tree.nodes["Background"].inputs[1].default_value = 0.9
    sun = bpy.data.objects.new("Sun", bpy.data.lights.new("Sun", 'SUN')); sun.data.energy = 4.0; sun.rotation_euler = (math.radians(50), 0, math.radians(-40)); scene.collection.objects.link(sun)
    gm = mat("Ground", (0.32, 0.27, 0.2), 0, 0.9); bm = bmesh.new(); bmesh.ops.create_grid(bm, x_segments=1, y_segments=1, size=30); gme = bpy.data.meshes.new("G"); bm.to_mesh(gme); bm.free(); gme.materials.append(gm)
    g = bpy.data.objects.new("Ground", gme); scene.collection.objects.link(g)
    cam = bpy.data.objects.new("Cam", bpy.data.cameras.new("Cam")); scene.collection.objects.link(cam); scene.camera = cam; cam.data.lens = 50
    os.makedirs(os.path.join(OUT, "Car_Reference_Renders"), exist_ok=True)
    for nm, pos in {"front": (0, -9, 1.3), "back": (0, 9, 1.3), "left": (-9, 0, 1.3), "right": (9, 0, 1.3), "three_quarter": (-5.2, -6.2, 2.6), "top_rear": (4.5, 6.5, 4.5)}.items():
        cam.location = pos; cam.rotation_euler = (Vector((0, 0, 0.95)) - Vector(pos)).to_track_quat('-Z', 'Y').to_euler()
        scene.render.filepath = os.path.join(OUT, "Car_Reference_Renders", "%s_%s.png" % (tag, nm))
        try: bpy.ops.render.render(write_still=True)
        except Exception as e: print("render failed", nm, e); break
    for o in (sun, g, cam): bpy.data.objects.remove(o, do_unlink=True)

render_objs = [root] + list(parts.values())
export(os.path.join(OUT, "Car_Model"), render_objs, glb=True)
os.makedirs(os.path.join(OUT, "Car_Collision_Meshes"), exist_ok=True)
export(os.path.join(OUT, "Car_Collision_Meshes", "Car_Collision"), col_objs)
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUT, "Car_Model.blend"))
render_views("normal")

# ---------------------------------------------------------------- damage variants: deformed Body and Hood shells
burnt = mat("Jeep_Paint_Burnt", (0.035, 0.032, 0.028), 0.3, 0.8); scuffed = mat("Jeep_Paint_Scuffed", (0.10, 0.115, 0.07), 0.35, 0.6)
def damaged_copy(src, name, strength, seed, material, crush_front=0.0):
    me = src.data.copy(); o = bpy.data.objects.new(name, me); C["Body"].objects.link(o); o.matrix_world = src.matrix_world.copy()
    bm = bmesh.new(); bm.from_mesh(me)
    bmesh.ops.subdivide_edges(bm, edges=[e for e in bm.edges if e.calc_length() > 0.35], cuts=2, use_grid_fill=True)
    bmesh.ops.triangulate(bm, faces=bm.faces[:])
    rnd = random.Random(seed); dents = [(Vector((rnd.uniform(-0.9, 0.9), rnd.uniform(-2.1, 2.0), rnd.uniform(0.5, 1.7))), rnd.uniform(0.35, 0.8)) for _ in range(int(4 + strength * 30))]
    for v in bm.verts:
        w = o.matrix_world @ v.co; push = Vector((0, 0, 0))
        for c, r in dents:
            d = (w - c).length
            if d < r: push += (Vector((0, 0, 1.0)) * -0.15 + (Vector((0, w.y, 1.1)) - w).normalized()) * strength * (1 - d / r) ** 2
        if crush_front > 0 and w.y < -0.9: push += Vector((rnd.uniform(-0.03, 0.03), crush_front * (-0.9 - w.y) / 1.2, -crush_front * 0.25 * (-0.9 - w.y) / 1.2))
        v.co += o.matrix_world.inverted().to_3x3() @ push
    bm.to_mesh(me); bm.free(); me.materials.clear(); me.materials.append(material); me.materials.append(M["interior"]); return o

os.makedirs(os.path.join(OUT, "Car_Damaged"), exist_ok=True)
for tag, st, seed, mt, crush in (("Damaged", 0.05, 3, scuffed, 0.05), ("Wrecked", 0.12, 7, scuffed, 0.22), ("Destroyed", 0.2, 11, burnt, 0.4)):
    b = damaged_copy(parts["Body"], "Body", st, seed, mt, crush); h = damaged_copy(parts["Hood"], "Hood", st * 1.5, seed + 1, mt, crush)
    b.name = "Body"; h.name = "Hood"
    export(os.path.join(OUT, "Car_Damaged", "Car_Damage_" + tag), [b, h])
    if tag == "Destroyed":
        for o in bpy.data.objects: o.hide_render = True
        b.hide_render = False; h.hide_render = False
        for n in ("Undercarriage", "Wheel_FL", "Wheel_RR", "Seat_FL", "Seat_FR", "Dashboard", "RoofRack"): parts[n].hide_render = False
        render_views("destroyed")
        for o in bpy.data.objects: o.hide_render = False
    bpy.data.objects.remove(b, do_unlink=True); bpy.data.objects.remove(h, do_unlink=True)
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUT, "Car_Damaged.blend"))

if UNITY:
    import shutil
    os.makedirs(UNITY, exist_ok=True)
    for f in ["Car_Model.fbx", "Car_Collision_Meshes/Car_Collision.fbx", "Car_Damaged/Car_Damage_Damaged.fbx", "Car_Damaged/Car_Damage_Wrecked.fbx", "Car_Damaged/Car_Damage_Destroyed.fbx"]:
        shutil.copy(os.path.join(OUT, f), os.path.join(UNITY, os.path.basename(f)))
print("JEEP DONE", OUT)
