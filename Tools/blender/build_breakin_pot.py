# Zone 2 break-in puzzle: a terracotta oil pot, whole and as three shards (own work).
# Blender --background --python Tools/blender/build_breakin_pot.py -- <repo root>
import bpy, bmesh, math, sys, os, random
ROOT = sys.argv[sys.argv.index("--") + 1]
bpy.ops.wm.read_factory_settings(use_empty=True)
random.seed(4)
PROFILE = [(0.00, 0.10), (0.02, 0.13), (0.08, 0.19), (0.16, 0.225), (0.24, 0.22), (0.31, 0.17), (0.35, 0.11), (0.37, 0.095), (0.40, 0.115), (0.415, 0.12)]   # (height, radius) in metres
SEG = 36; mat = bpy.data.materials.new("MAT_Terracotta")

def pot(name, a0, a1, jag):
    """part of the pot between two angles (segments); jag = how ragged the broken edges are"""
    bm = bmesh.new(); uv = bm.loops.layers.uv.new("UV0"); rings = []
    for z, r in PROFILE:
        ring = []
        for i in range(a0, a1 + 1):
            a = 2 * math.pi * i / SEG
            if jag and i in (a0, a1): a += random.uniform(-jag, jag)
            ring.append((bm.verts.new((r * math.cos(a), r * math.sin(a), z)), i / SEG, z / 0.415))
        rings.append(ring)
    for ra, rb in zip(rings[:-1], rings[1:]):
        for k in range(len(ra) - 1):
            f = bm.faces.new([ra[k][0], ra[k + 1][0], rb[k + 1][0], rb[k][0]])
            for l, src in zip(f.loops, (ra[k], ra[k + 1], rb[k + 1], rb[k])): l[uv].uv = (src[1] * 3.0, src[2])
    if not jag:                                   # the whole pot has a bottom
        f = bm.faces.new([v[0] for v in rings[0][:-1]][::-1])
        for l in f.loops: l[uv].uv = (0.5 + l.vert.co.x, 0.5 + l.vert.co.y)
    me = bpy.data.meshes.new(name); bm.to_mesh(me); bm.free(); me.materials.append(mat)
    o = bpy.data.objects.new(name, me); bpy.context.scene.collection.objects.link(o)
    md = o.modifiers.new("s", 'SOLIDIFY'); md.thickness = 0.012; md.offset = -1
    dg = bpy.context.evaluated_depsgraph_get(); o.data = bpy.data.meshes.new_from_object(o.evaluated_get(dg)); o.modifiers.clear()
    for p in o.data.polygons: p.use_smooth = True
    return o

objs = [pot("SM_Pot_Whole", 0, SEG, 0.0)]
for n, (a0, a1) in enumerate(((0, 13), (13, 25), (25, 36)), 1):
    o = pot(f"SM_Pot_Shard_{n}", a0, a1, 0.05)
    c = sum((v.co for v in o.data.vertices), o.data.vertices[0].co * 0) / len(o.data.vertices)   # pivot at the shard's middle, lying on its side
    for v in o.data.vertices: v.co -= c
    o.location = c; objs.append(o)
bpy.context.view_layer.update()
for o in bpy.data.objects: o.select_set(o in objs)
out = os.path.join(ROOT, "Assets/_Project/Resources/Props/V3/BreakInPot.fbx")
bpy.ops.export_scene.fbx(filepath=out, use_selection=True, apply_scale_options='FBX_SCALE_ALL', bake_space_transform=True, object_types={'MESH'}, mesh_smooth_type='FACE')
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(ROOT, "Art_Source/V3/BreakInPot.blend"))
print("POT_OK", [(o.name, len(o.data.polygons)) for o in objs])
