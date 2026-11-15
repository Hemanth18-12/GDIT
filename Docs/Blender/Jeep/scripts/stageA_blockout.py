# Stage A - Investigator Jeep block-out. Run inside Blender (exec). Rebuilds everything named below.
# Axes: forward = -Y, up = +Z, vehicle LEFT = +X, vehicle RIGHT (driver, right-hand drive) = -X. 1 unit = 1 m.
import bpy, bmesh, math, os
from mathutils import Vector, Matrix

BASE = "/Users/hemanth/Desktop/GDIT/Docs/Blender/Jeep"
REFS = BASE + "/refs"

# ---------- measurements taken from the reference sheet (metres) ----------
P = dict(
    axle=1.36,            # front axle y=-1.36, rear axle y=+1.36  (wheelbase 2.72)
    tyre_r=0.39, tyre_w=0.27, track_x=0.825,
    body_hw=0.88,         # lower body half width
    sill_z=0.46, belt_z=1.22,
    nose_y=-1.90, tail_y=2.01,
    hood_front_z=1.08, cowl_y=-0.70, cowl_z=1.25,
    roof_z=1.79, roof_front_y=-0.37, roof_rear_y=1.93, roof_hw=0.68, belt_hw=0.82,
    arch_r=0.50, flare_r=0.58, flare_x=0.98,
    spare_x=-0.21, spare_z=1.02,
)
globals().update(P)

# ---------- clean slate (default cube/camera/light and any previous block-out) ----------
for o in list(bpy.data.objects):
    bpy.data.objects.remove(o, do_unlink=True)
for c in list(bpy.data.collections):
    bpy.data.collections.remove(c)
for blk in (bpy.data.meshes, bpy.data.cameras, bpy.data.lights, bpy.data.materials):
    for d in list(blk):
        if d.users == 0:
            blk.remove(d)

scene = bpy.context.scene
scene.unit_settings.system = 'METRIC'
scene.unit_settings.scale_length = 1.0

def coll(name, parent):
    c = bpy.data.collections.new(name)
    parent.children.link(c)
    return c

ROOT = coll("JEEP_Root", scene.collection)
C = {n: coll(n, ROOT) for n in ["BODY", "GLASS", "LIGHTS", "WHEELS", "DOORS", "INTERIOR",
                                "ACCESSORIES", "DAMAGE", "COLLISION", "RIG"]}
REFC = coll("JEEP_Reference", scene.collection)
CAMC = coll("JEEP_Cameras", scene.collection)

def mat(name, rgb, rough=0.5, metal=0.0):
    m = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    m.diffuse_color = (*rgb, 1.0)
    m.roughness = rough
    m.metallic = metal
    return m

M_PAINT = mat("BLK_Paint_Olive", (0.16, 0.19, 0.10), 0.45)
M_BLACK = mat("BLK_Plastic_Black", (0.03, 0.03, 0.03), 0.6)
M_TYRE = mat("BLK_Rubber_Tyre", (0.015, 0.015, 0.015), 0.9)
M_RIM = mat("BLK_Rim_Black", (0.10, 0.10, 0.11), 0.35, 0.8)
M_GLASS = mat("BLK_Glass_Tinted", (0.02, 0.03, 0.04), 0.1)
M_LIGHT = mat("BLK_Light_White", (0.95, 0.92, 0.80), 0.2)
M_RED = mat("BLK_Light_Red", (0.70, 0.03, 0.02), 0.3)
M_CHROME = mat("BLK_Metal_Chrome", (0.65, 0.66, 0.68), 0.2, 1.0)
M_PLATE = mat("BLK_Plate_White", (0.9, 0.9, 0.88), 0.5)
M_SEAT = mat("BLK_Seat_Grey", (0.12, 0.12, 0.13), 0.8)

def finish(bm, name, collection, mats, loc=(0, 0, 0), smooth=False):
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    me = bpy.data.meshes.new(name)
    bm.to_mesh(me)
    bm.free()
    for m in mats:
        me.materials.append(m)
    if smooth:
        for p in me.polygons:
            p.use_smooth = True
    ob = bpy.data.objects.new(name, me)
    ob.location = loc
    collection.objects.link(ob)
    return ob

def add_box(bm, x0, x1, y0, y1, z0, z1, mi=0):
    vs = [bm.verts.new((x, y, z)) for z in (z0, z1) for y in (y0, y1) for x in (x0, x1)]
    idx = [(0, 1, 3, 2), (4, 6, 7, 5), (0, 4, 5, 1), (2, 3, 7, 6), (0, 2, 6, 4), (1, 5, 7, 3)]
    for f in idx:
        bm.faces.new([vs[i] for i in f]).material_index = mi

def add_cyl(bm, center, r, depth, axis, mi=0, seg=32):
    d = Vector(axis).normalized()
    rot = Vector((0, 0, 1)).rotation_difference(d).to_matrix().to_4x4()
    mtx = Matrix.Translation(Vector(center)) @ rot
    g = bmesh.ops.create_cone(bm, cap_ends=True, segments=seg, radius1=r, radius2=r, depth=depth, matrix=mtx)
    for f in {f for v in g['verts'] for f in v.link_faces}:
        f.material_index = mi

def add_tube(bm, pts, r, mi=0, seg=12):
    for a, b in zip(pts[:-1], pts[1:]):
        a, b = Vector(a), Vector(b)
        add_cyl(bm, (a + b) / 2, r, (b - a).length, b - a, mi, seg)

def box(name, collection, m, *ext, bevel=0.0):
    bm = bmesh.new()
    add_box(bm, *ext)
    ob = finish(bm, name, collection, [m])
    if bevel:
        mod = ob.modifiers.new("Bevel", 'BEVEL')
        mod.width = bevel
        mod.segments = 2
    return ob

def cyl(name, collection, m, center, r, depth, axis, seg=32):
    bm = bmesh.new()
    add_cyl(bm, center, r, depth, axis, 0, seg)
    return finish(bm, name, collection, [m], smooth=False)

def tube(name, collection, m, runs, r):
    bm = bmesh.new()
    for pts in runs:
        add_tube(bm, pts, r)
    return finish(bm, name, collection, [m])

def quad(name, collection, m, verts):
    bm = bmesh.new()
    bm.faces.new([bm.verts.new(v) for v in verts])
    return finish(bm, name, collection, [m])

# ---------- BODY: lower tub from the side profile (wheel arches are part of the profile) ----------
def arch_pts(cy, start_deg, end_deg, n=14):
    out = []
    for i in range(n + 1):
        a = math.radians(start_deg + (end_deg - start_deg) * i / n)
        out.append((cy - arch_r * math.cos(a), tyre_r + arch_r * math.sin(a)))
    return out

a0 = math.degrees(math.asin((sill_z - tyre_r) / arch_r))          # where the arch meets the sill
profile = [(nose_y, 0.60), (nose_y, 0.98), (nose_y + 0.10, hood_front_z), (cowl_y, cowl_z),
           (cowl_y + 0.04, belt_z), (tail_y, belt_z), (tail_y + 0.02, 0.55), (tail_y - 0.06, sill_z)]
profile += [(2 * axle - y, z) for (y, z) in arch_pts(axle, a0, 180 - a0)]        # rear arch (mirrored order)
profile += [(-y, z) for (y, z) in arch_pts(axle, a0, 180 - a0)]                # front arch
bm = bmesh.new()
L = [bm.verts.new((body_hw, y, z)) for y, z in profile]
R = [bm.verts.new((-body_hw, y, z)) for y, z in profile]
bm.faces.new(L)
bm.faces.new(R[::-1])
n = len(profile)
for i in range(n):
    j = (i + 1) % n
    bm.faces.new([L[i], R[i], R[j], L[j]])
body = finish(bm, "Body_Lower_Blockout", C["BODY"], [M_PAINT])
mod = body.modifiers.new("Bevel", 'BEVEL'); mod.width = 0.025; mod.segments = 2; mod.limit_method = 'ANGLE'

# cabin / greenhouse: belt rectangle lofted to a narrower roof rectangle
bm = bmesh.new()
bz = belt_z - 0.02
b = [bm.verts.new(v) for v in [(belt_hw, cowl_y, bz), (-belt_hw, cowl_y, bz), (-belt_hw, tail_y, bz), (belt_hw, tail_y, bz)]]
t = [bm.verts.new(v) for v in [(roof_hw, roof_front_y, roof_z), (-roof_hw, roof_front_y, roof_z),
                               (-roof_hw, roof_rear_y, roof_z), (roof_hw, roof_rear_y, roof_z)]]
bm.faces.new(b[::-1]); bm.faces.new(t)
for i in range(4):
    j = (i + 1) % 4
    bm.faces.new([b[i], b[j], t[j], t[i]])
cab = finish(bm, "Body_Cabin_Blockout", C["BODY"], [M_PAINT])
mod = cab.modifiers.new("Bevel", 'BEVEL'); mod.width = 0.03; mod.segments = 2

# fender flares (black arch rings)
def flare(name, side, cy):
    bm = bmesh.new()
    xi, xo = side * (body_hw - 0.03), side * flare_x
    rings = []
    N = 18
    for i in range(N + 1):
        a = math.radians(-12 + 204 * i / N)
        ring = []
        for x, r in ((xi, arch_r), (xo, arch_r), (xo, flare_r), (xi, flare_r)):
            ring.append(bm.verts.new((x, cy - r * math.cos(a), tyre_r + r * math.sin(a))))
        rings.append(ring)
    for i in range(N):
        for k in range(4):
            k2 = (k + 1) % 4
            bm.faces.new([rings[i][k], rings[i][k2], rings[i + 1][k2], rings[i + 1][k]])
    bm.faces.new(rings[0]); bm.faces.new(rings[-1][::-1])
    return finish(bm, name, C["BODY"], [M_BLACK])

flare("Fender_Flare_FL", 1, -axle); flare("Fender_Flare_FR", -1, -axle)
flare("Fender_Flare_RL", 1, axle); flare("Fender_Flare_RR", -1, axle)

box("Front_Bumper", C["BODY"], M_BLACK, -0.93, 0.93, -1.97, -1.84, 0.46, 0.72, bevel=0.02)
box("Rear_Bumper", C["BODY"], M_BLACK, -0.93, 0.93, 2.00, 2.13, 0.42, 0.66, bevel=0.02)
box("Grille", C["BODY"], M_BLACK, -0.44, 0.44, nose_y - 0.02, nose_y + 0.02, 0.74, 1.01)

# ---------- DOORS (thin placeholder panels that mark the shut lines) ----------
def door(name, side, pts):
    bm = bmesh.new()
    x0, x1 = side * (body_hw - 0.005), side * (body_hw + 0.012)
    A = [bm.verts.new((x0, y, z)) for y, z in pts]
    B = [bm.verts.new((x1, y, z)) for y, z in pts]
    bm.faces.new(A); bm.faces.new(B[::-1])
    for i in range(len(pts)):
        j = (i + 1) % len(pts)
        bm.faces.new([A[i], B[i], B[j], A[j]])
    return finish(bm, name, C["DOORS"], [M_PAINT])

front_door = [(-0.62, 0.50), (0.29, 0.50), (0.29, 1.21), (-0.62, 1.21)]
rear_door = [(0.31, 0.50)]
for i in range(7):                                                    # rear door follows the rear flare
    a = math.radians(11 + (62 - 11) * i / 6)
    rear_door.append((axle - (flare_r + 0.02) * math.cos(a), tyre_r + (flare_r + 0.02) * math.sin(a)))
rear_door += [(1.08, 1.21), (0.31, 1.21)]
for s, tag in ((1, "L"), (-1, "R")):
    door("Door_F" + tag, s, front_door)
    door("Door_R" + tag, s, rear_door)
    for nm, y in (("F", 0.20), ("R", 0.98)):
        box(f"Door_Handle_{nm}{tag}", C["DOORS"], M_BLACK, s * (body_hw + 0.012), s * (body_hw + 0.035),
            y - 0.07, y + 0.07, 1.11, 1.14)

# ---------- GLASS ----------
def side_x(z):                   # outer skin of the cabin side at height z
    return belt_hw - (z - bz) * (belt_hw - roof_hw) / (roof_z - bz)
def front_y(z):                  # windscreen plane
    return cowl_y + (z - bz) * (roof_front_y - cowl_y) / (roof_z - bz)
def rear_y(z):
    return tail_y - (z - bz) * (tail_y - roof_rear_y) / (roof_z - bz)

zb, zt, e = 1.26, 1.66, 0.008
for s, tag in ((1, "L"), (-1, "R")):
    wins = {"Window_F" + tag: [(front_y(zb) + 0.10, zb), (0.24, zb), (0.24, zt), (front_y(zt) + 0.10, zt)],
            "Window_R" + tag: [(0.36, zb), (1.03, zb), (1.03, zt), (0.36, zt)],
            "Window_Quarter_" + tag: [(1.12, zb), (1.84, zb), (1.80, zt), (1.12, zt)]}
    for nm, pts in wins.items():
        quad(nm, C["GLASS"], M_GLASS, [(s * (side_x(z) + e), y, z) for y, z in pts])
z0, z1 = 1.30, 1.74
quad("Windscreen", C["GLASS"], M_GLASS, [(side_x(z0) - 0.07, front_y(z0) - e, z0), (-(side_x(z0) - 0.07), front_y(z0) - e, z0),
                                        (-(side_x(z1) - 0.07), front_y(z1) - e, z1), (side_x(z1) - 0.07, front_y(z1) - e, z1)])
z0, z1 = 1.27, 1.68
quad("Window_Rear", C["GLASS"], M_GLASS, [(0.58, rear_y(z0) + e, z0), (-0.58, rear_y(z0) + e, z0),
                                         (-0.54, rear_y(z1) + e, z1), (0.54, rear_y(z1) + e, z1)])

# ---------- LIGHTS ----------
for s, tag in ((1, "L"), (-1, "R")):
    cyl("Headlight_" + tag, C["LIGHTS"], M_LIGHT, (s * 0.65, nose_y - 0.01, 0.88), 0.11, 0.06, (0, 1, 0))
    cyl("Headlight_Ring_" + tag, C["LIGHTS"], M_CHROME, (s * 0.65, nose_y, 0.88), 0.125, 0.05, (0, 1, 0))
    cyl("Fog_" + tag, C["LIGHTS"], M_LIGHT, (s * 0.71, -1.975, 0.56), 0.055, 0.03, (0, 1, 0))
    box("TailLight_" + tag, C["LIGHTS"], M_RED, s * 0.72, s * 0.87, tail_y - 0.01, tail_y + 0.035, 0.78, 1.04)
for i, x in enumerate((0.34, 0.115, -0.115, -0.34), 1):
    cyl(f"Spot_Light_{i}", C["LIGHTS"], M_BLACK, (x, -0.09, 1.93), 0.062, 0.10, (0, 1, 0))
    cyl(f"Spot_Light_{i}_Lens", C["LIGHTS"], M_LIGHT, (x, -0.142, 1.93), 0.052, 0.01, (0, 1, 0))

# ---------- WHEELS (origin at the wheel centre, spin axis = local X) ----------
def wheel(name, loc, axis=(1, 0, 0), cover=False):
    bm = bmesh.new()
    add_cyl(bm, (0, 0, 0), tyre_r, tyre_w, axis, 0, 40)
    add_cyl(bm, (0, 0, 0), 0.245, tyre_w + 0.012, axis, 1, 32)
    add_cyl(bm, (0, 0, 0), 0.07, tyre_w + 0.03, axis, 1, 16)
    return finish(bm, name, C["WHEELS"], [M_TYRE, M_RIM], loc=loc)

wheel("Wheel_FL", (track_x, -axle, tyre_r)); wheel("Wheel_FR", (-track_x, -axle, tyre_r))
wheel("Wheel_RL", (track_x, axle, tyre_r)); wheel("Wheel_RR", (-track_x, axle, tyre_r))
wheel("Spare_Wheel", (spare_x, tail_y + 0.03 + tyre_w / 2, spare_z), axis=(0, 1, 0))

# ---------- ACCESSORIES ----------
yb = -2.0 + 0.03                                                    # bull bar tube centre line
tube("Bull_Bar", C["ACCESSORIES"], M_BLACK, [
    [(0.40, yb, 0.40), (0.40, yb, 1.00), (0.35, yb, 1.05), (-0.35, yb, 1.05), (-0.40, yb, 1.00), (-0.40, yb, 0.40)],
    [(0.40, yb, 0.72), (-0.40, yb, 0.72)],
    [(0.40, yb, 0.50), (0.40, -1.90, 0.50)], [(-0.40, yb, 0.50), (-0.40, -1.90, 0.50)]], 0.03)

rk = dict(y0=-0.04, y1=1.64, hw=0.61, zb=1.85, zt=1.99)
runs = []
for z in (rk["zb"], rk["zt"]):
    runs.append([(rk["hw"], rk["y0"], z), (rk["hw"], rk["y1"], z), (-rk["hw"], rk["y1"], z), (-rk["hw"], rk["y0"], z), (rk["hw"], rk["y0"], z)])
for i in range(6):                                                  # posts along both sides
    y = rk["y0"] + (rk["y1"] - rk["y0"]) * i / 5
    for s in (1, -1):
        runs.append([(s * rk["hw"], y, rk["zb"]), (s * rk["hw"], y, rk["zt"])])
for i in range(1, 8):                                               # floor cross-bars
    y = rk["y0"] + (rk["y1"] - rk["y0"]) * i / 8
    runs.append([(rk["hw"], y, rk["zb"]), (-rk["hw"], y, rk["zb"])])
for y in (0.15, 0.80, 1.50):                                        # feet down to the roof
    for s in (1, -1):
        runs.append([(s * rk["hw"], y, rk["zb"]), (s * (roof_hw - 0.04), y, roof_z)])
tube("Roof_Rack", C["ACCESSORIES"], M_BLACK, runs, 0.016)

for s, tag in ((1, "L"), (-1, "R")):
    tube("Side_Step_" + tag, C["ACCESSORIES"], M_CHROME, [[(s * 0.95, -0.78, 0.33), (s * 0.95, 0.83, 0.33)],
         [(s * 0.95, -0.55, 0.33), (s * 0.80, -0.55, 0.44)], [(s * 0.95, 0.60, 0.33), (s * 0.80, 0.60, 0.44)]], 0.035)
    box("Mirror_" + tag, C["ACCESSORIES"], M_BLACK, s * 0.88, s * 1.03, -0.60, -0.52, 1.22, 1.40, bevel=0.015)
box("Plate_Front", C["ACCESSORIES"], M_PLATE, -0.29, 0.29, -2.045, -2.03, 0.43, 0.55)
box("Plate_Rear", C["ACCESSORIES"], M_PLATE, -0.29, 0.29, 2.13, 2.145, 0.40, 0.52)

# ---------- INTERIOR placeholders (right-hand drive: steering wheel on -X) ----------
for nm, x, y in (("Seat_FR_Driver", -0.38, -0.15), ("Seat_FL", 0.38, -0.15), ("Seat_RR", -0.38, 0.75), ("Seat_RL", 0.38, 0.75)):
    bm = bmesh.new()
    add_box(bm, x - 0.25, x + 0.25, y, y + 0.50, 0.62, 0.78)
    add_box(bm, x - 0.25, x + 0.25, y + 0.42, y + 0.54, 0.78, 1.42)
    finish(bm, nm, C["INTERIOR"], [M_SEAT])
box("Dashboard", C["INTERIOR"], M_BLACK, -0.78, 0.78, -0.72, -0.48, 0.92, 1.20)
cyl("Steering_Wheel", C["INTERIOR"], M_BLACK, (-0.38, -0.40, 1.10), 0.185, 0.03, (0, 1, -0.45))

# ---------- reference planes (image empties, visible in orthographic views only) ----------
REF = {   # name: (image, width m, location, rotation deg, ortho camera centre)
    "REF_Left":  ("ref_left.png", 430 / 93.08, (-2.5, 0.147, 1.012), (90, 0, 90)),
    "REF_Right": ("ref_right.png", 440 / 96.9, (2.5, 0.173, 0.972), (90, 0, -90)),
    "REF_Front": ("ref_front.png", 215 / 91.8, (0.0, 3.5, 1.044), (90, 0, 0)),
    "REF_Back":  ("ref_back.png", 210 / 92.3, (0.0, -3.5, 1.038), (90, 0, 180)),
}
for nm, (img, w, loc, rot) in REF.items():
    e = bpy.data.objects.new(nm, None)
    e.empty_display_type = 'IMAGE'
    e.data = bpy.data.images.load(os.path.join(REFS, img), check_existing=True)
    e.empty_display_size = w
    e.location = loc
    e.rotation_euler = [math.radians(a) for a in rot]
    e.show_empty_image_perspective = False
    e.use_empty_image_alpha = True
    e.color[3] = 0.6
    e.hide_select = True
    e.empty_image_side = 'FRONT'      # only visible from its own side, so the opposite view is not covered
    e.empty_image_depth = 'BACK'      # always drawn behind the model
    REFC.objects.link(e)

# ---------- cameras ----------
def cam(name, loc, rot=None, ortho=None, look=None, lens=50):
    cd = bpy.data.cameras.new(name)
    ob = bpy.data.objects.new(name, cd)
    ob.location = loc
    if ortho:
        cd.type = 'ORTHO'; cd.ortho_scale = ortho
        ob.rotation_euler = [math.radians(a) for a in rot]
    else:
        cd.lens = lens
        ob.rotation_euler = (Vector(look) - Vector(loc)).to_track_quat('-Z', 'Y').to_euler()
    cd.clip_end = 200
    CAMC.objects.link(ob)
    return ob

cam("CAM_Left", (10, 0.147, 1.012), (90, 0, 90), REF["REF_Left"][1])
cam("CAM_Right", (-10, 0.173, 0.972), (90, 0, -90), REF["REF_Right"][1])
cam("CAM_Front", (0, -10, 1.044), (90, 0, 0), REF["REF_Front"][1])
cam("CAM_Back", (0, 10, 1.038), (90, 0, 180), REF["REF_Back"][1])
cam("CAM_34_Front", (5.4, -6.6, 2.6), look=(0, 0.1, 0.9), lens=50)
cam("CAM_34_Rear", (-5.4, 6.9, 2.6), look=(0, 0.1, 0.9), lens=50)

# ---------- report ----------
dg = bpy.context.evaluated_depsgraph_get()
tris = 0; lo = Vector((1e9,) * 3); hi = Vector((-1e9,) * 3)
for o in ROOT.all_objects:
    if o.type != 'MESH':
        continue
    ev = o.evaluated_get(dg); me = ev.to_mesh()
    me.calc_loop_triangles(); tris += len(me.loop_triangles)
    for v in me.vertices:
        w = o.matrix_world @ v.co
        lo = Vector(map(min, lo, w)); hi = Vector(map(max, hi, w))
    ev.to_mesh_clear()
result = {"objects": len(ROOT.all_objects), "tris": tris,
          "size_xyz_m": [round(hi[i] - lo[i], 3) for i in range(3)],
          "min": [round(v, 3) for v in lo], "max": [round(v, 3) for v in hi],
          "per_collection": {k: len(c.objects) for k, c in C.items()}}
