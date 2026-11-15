# Makes game-ready, rigged copies of the two robber models in the owner's assets.blend (the file itself is never saved).
# Run: Blender --background "Heritage Mystery Project/assets.blend" --python Tools/blender/rig_robbers.py -- <out_dir>
# For each robber: copy the mesh, reduce it to ~40k triangles, stand it on the floor at 1.9 m, build a humanoid skeleton
# with Mixamo bone names fitted to its pose (arms down), skin it by distance to the bones, save its texture, export FBX.
import bpy, bmesh, sys, os, math
import numpy as np
from mathutils import Vector, Matrix

OUT = sys.argv[sys.argv.index("--") + 1]; os.makedirs(OUT, exist_ok=True)
ROBBERS = {"Robber1": "tripo_node_0f07ddad", "Robber2": "tripo_node_ca66fc79"}   # black-armoured king (Bhaskar), winged figure (Kishore)
H = 1.9; TARGET = 40000

def build(name, src_name):
    src = bpy.data.objects[src_name]
    me = src.data.copy(); me.transform(src.matrix_world)
    ob = bpy.data.objects.new(name, me); sc = bpy.data.scenes.new("Rig_" + name); bpy.context.window.scene = sc; sc.collection.objects.link(ob)
    # reduce
    me.calc_loop_triangles(); n = len(me.loop_triangles)
    if n > TARGET:
        d = ob.modifiers.new("dec", 'DECIMATE'); d.ratio = TARGET / n
        bpy.context.view_layer.update(); dg = bpy.context.evaluated_depsgraph_get()
        nm = bpy.data.meshes.new_from_object(ob.evaluated_get(dg)); ob.modifiers.clear(); ob.data = nm; me = nm
    v = np.array([vv.co[:] for vv in me.vertices])
    zmin, zmax = v[:, 2].min(), v[:, 2].max(); feet = v[v[:, 2] < zmin + 0.08 * (zmax - zmin)]
    cx, cy = feet[:, 0].mean(), feet[:, 1].mean(); s = H / (zmax - zmin)
    me.transform(Matrix.Scale(s, 4) @ Matrix.Translation((-cx, -cy, -zmin)))
    v = np.array([vv.co[:] for vv in me.vertices])
    def width(z0, z1, side):
        band = v[(v[:, 2] > z0 * H) & (v[:, 2] < z1 * H)]
        band = band[band[:, 0] * side > 0]
        return np.percentile(np.abs(band[:, 0]), 90) if len(band) else 0.2
    sh = min(max(width(0.76, 0.82, 1), width(0.76, 0.82, -1)) * 0.8, 0.26)
    # joints (Blender: -Y forward, +X = the character's left)
    J = {"Hips": (0, 0, .52), "Spine": (0, 0, .58), "Spine1": (0, 0, .66), "Spine2": (0, 0, .73), "Neck": (0, 0, .84), "Head": (0, 0, .88), "HeadTop_End": (0, 0, 1.0)}
    for side, sx in (("Left", 1), ("Right", -1)):
        J.update({side + "Shoulder": (sx * .03, 0, .80), side + "Arm": (sx * sh * 0.62, 0, .79), side + "ForeArm": (sx * sh * 0.82, 0.01, .62), side + "Hand": (sx * sh * 0.9, 0, .47), side + "HandEnd": (sx * sh * 0.92, -0.01, .40),
                  side + "UpLeg": (sx * .055, 0, .50), side + "Leg": (sx * .06, -0.01, .27), side + "Foot": (sx * .065, 0.01, .05), side + "ToeBase": (sx * .065, -0.07, .015), side + "Toe_End": (sx * .065, -0.11, .015)})
    P = {k: Vector((x if abs(x) > 0.2 * 1 else x * (H if k.endswith(("UpLeg", "Leg", "Foot", "ToeBase", "Toe_End", "Shoulder")) else 1), y * H, z * H)) for k, (x, y, z) in J.items()}
    for k in P:   # leg / shoulder x offsets above are fractions of H; arm x values are already metres
        if k.endswith(("UpLeg", "Leg", "Foot", "ToeBase", "Toe_End", "Shoulder")): P[k].x = J[k][0] * H
    chain = [("Hips", None, "Spine"), ("Spine", "Hips", "Spine1"), ("Spine1", "Spine", "Spine2"), ("Spine2", "Spine1", "Neck"), ("Neck", "Spine2", "Head"), ("Head", "Neck", "HeadTop_End")]
    for side in ("Left", "Right"):
        chain += [(side + "Shoulder", "Spine2", side + "Arm"), (side + "Arm", side + "Shoulder", side + "ForeArm"), (side + "ForeArm", side + "Arm", side + "Hand"), (side + "Hand", side + "ForeArm", side + "HandEnd"),
                  (side + "UpLeg", "Hips", side + "Leg"), (side + "Leg", side + "UpLeg", side + "Foot"), (side + "Foot", side + "Leg", side + "ToeBase"), (side + "ToeBase", side + "Foot", side + "Toe_End")]
    arm = bpy.data.armatures.new(name + "_Armature"); ao = bpy.data.objects.new(name + "_Rig", arm); sc.collection.objects.link(ao)
    bpy.context.view_layer.objects.active = ao; bpy.ops.object.mode_set(mode='EDIT')
    eb = {}
    for b, parent, tail in chain:
        e = arm.edit_bones.new("mixamorig:" + b); e.head = P[b]; e.tail = P[tail]
        if (e.tail - e.head).length < 0.01: e.tail = e.head + Vector((0, 0, 0.05))
        if parent: e.parent = eb[parent]; e.use_connect = False
        eb[b] = e
    bpy.ops.object.mode_set(mode='OBJECT')
    # skin: each vertex to its two nearest bone segments (1/d^4), legs only below the hips, arms only beside the torso
    segs = [(b, np.array(P[b][:]), np.array(P[t][:])) for b, _, t in chain]
    co = np.array([vv.co[:] for vv in me.vertices]); D = np.zeros((len(co), len(segs)))
    for i, (b, a, c) in enumerate(segs):
        ab = c - a; t = np.clip(((co - a) @ ab) / max(ab @ ab, 1e-9), 0, 1); d = np.linalg.norm(co - (a + t[:, None] * ab), axis=1)
        if "Leg" in b or "Foot" in b or "Toe" in b:
            d = d + np.where(co[:, 2] > 0.52 * H, 100.0, 0)
            d = d + np.where(np.abs(co[:, 1]) > 0.16, 100.0, 0)
            d = d + np.where(np.abs(co[:, 0]) > 0.28, 100.0, 0)
        if b.endswith(("Arm", "ForeArm", "Hand")):
            sx = 1 if b.startswith("Left") else -1; d = d + np.where(co[:, 0] * sx < sh * 0.45, 100.0, 0)
            if name == "Robber2":
                # Wings are behind the back: do NOT weight wings to arms! Keep them rigidly attached to spine
                d = d + np.where((co[:, 1] > 0.04) & (np.abs(co[:, 0]) > 0.18), 100.0, 0)
        if name == "Robber1" and (b.startswith("Hips") or "Spine" in b):
            # Do not weight extended weapon blades to spine/hips
            d = d + np.where(np.abs(co[:, 1]) > 0.22, 50.0, 0)
        D[:, i] = d
    idx = np.argsort(D, axis=1)[:, :2]; dd = np.take_along_axis(D, idx, axis=1); w = 1.0 / (dd ** 4 + 1e-8); w /= w.sum(axis=1, keepdims=True)
    groups = {b: ob.vertex_groups.new(name="mixamorig:" + b) for b, _, _ in segs}
    for vi in range(len(co)):
        # For Robber 1: weapon blade vertices (|Y| > 0.22) rigidly follow the hand holding them
        if name == "Robber1" and abs(co[vi, 1]) > 0.22:
            hand_bone = "mixamorig:RightHand" if co[vi, 0] < 0 else "mixamorig:LeftHand"
            groups[hand_bone.replace("mixamorig:", "")].add([vi], 1.0, 'REPLACE')
            continue
        # For Robber 2: wing vertices (behind back Y > 0.04 and wide |X| > 0.18) rigidly follow upper spine
        if name == "Robber2" and co[vi, 1] > 0.04 and abs(co[vi, 0]) > 0.18:
            groups["Spine2"].add([vi], 0.7, 'REPLACE')
            groups["Spine1"].add([vi], 0.3, 'REPLACE')
            continue
        for k in range(2): groups[segs[idx[vi, k]][0]].add([vi], float(w[vi, k]), 'REPLACE')
    ob.parent = ao; md = ob.modifiers.new("Armature", 'ARMATURE'); md.object = ao
    # texture: save the first image the material uses, so Unity gets a plain PNG
    tex = None
    for m in me.materials:
        if m and m.use_nodes:
            for nd in m.node_tree.nodes:
                if nd.type == 'TEX_IMAGE' and nd.image:
                    img = nd.image
                    if img.packed_file is not None:   # write the original packed bytes (png or jpg)
                        ext = ".jpg" if img.packed_file.data[:3] == b"\xff\xd8\xff" else ".png"; tex = os.path.join(OUT, name + "_Albedo" + ext)
                        open(tex, "wb").write(img.packed_file.data)
                    else:
                        tex = os.path.join(OUT, name + "_Albedo.png")
                        try: img.save_render(tex)
                        except Exception as e: print("texture not saved:", e); tex = None
                    break
        if tex: break
    me.calc_loop_triangles()
    for o in bpy.data.objects: o.select_set(False)
    ao.select_set(True); ob.select_set(True); bpy.context.view_layer.objects.active = ao
    bpy.ops.export_scene.fbx(filepath=os.path.join(OUT, name + ".fbx"), use_selection=True, object_types={'ARMATURE', 'MESH'}, add_leaf_bones=False,
                             apply_scale_options='FBX_SCALE_ALL', mesh_smooth_type='FACE', armature_nodetype='NULL', bake_anim=False)
    print("RIGGED %s from %s: %d triangles, %d bones, height %.2f m, texture %s" % (name, src_name, len(me.loop_triangles), len(arm.bones), H, tex))

for n, s in ROBBERS.items(): build(n, s)
print("ROBBERS DONE")
