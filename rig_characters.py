import bpy
import math
import sys
from mathutils import Vector, Euler

def rig_character(fbx_in, fbx_out, char_type, height_scale=1.0):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=fbx_in)

    mesh_obj = None
    for obj in bpy.data.objects:
        if obj.type == 'MESH':
            mesh_obj = obj
            break

    if not mesh_obj:
        print(f"ERROR: No mesh found in {fbx_in}")
        return False

    bpy.ops.object.select_all(action='DESELECT')
    mesh_obj.select_set(True)
    bpy.context.view_layer.objects.active = mesh_obj
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)

    # Create Armature
    arm_data = bpy.data.armatures.new(f"{char_type}_Armature")
    arm_obj = bpy.data.objects.new(f"{char_type}_Rig", arm_data)
    bpy.context.collection.objects.link(arm_obj)
    bpy.context.view_layer.objects.active = arm_obj
    bpy.ops.object.mode_set(mode='EDIT')

    eb = arm_data.edit_bones

    s = height_scale
    # Spine & Head
    hips = eb.new('Hips')
    hips.head = Vector((0, 0, 0.95 * s))
    hips.tail = Vector((0, 0, 1.10 * s))

    spine = eb.new('Spine')
    spine.head = Vector((0, 0, 1.10 * s))
    spine.tail = Vector((0, 0, 1.25 * s))
    spine.parent = hips

    chest = eb.new('Chest')
    chest.head = Vector((0, 0, 1.25 * s))
    chest.tail = Vector((0, 0, 1.48 * s))
    chest.parent = spine

    neck = eb.new('Neck')
    neck.head = Vector((0, 0, 1.48 * s))
    neck.tail = Vector((0, 0, 1.56 * s))
    neck.parent = chest

    head = eb.new('Head')
    head.head = Vector((0, 0, 1.56 * s))
    head.tail = Vector((0, 0, 1.75 * s))
    head.parent = neck

    # Left Arm
    l_sh = eb.new('LeftShoulder')
    l_sh.head = Vector((0.05 * s, 0, 1.45 * s))
    l_sh.tail = Vector((0.18 * s, 0, 1.42 * s))
    l_sh.parent = chest

    l_arm = eb.new('LeftArm')
    l_arm.head = Vector((0.18 * s, 0, 1.42 * s))
    l_arm.tail = Vector((0.42 * s, 0, 1.18 * s))
    l_arm.parent = l_sh

    l_forearm = eb.new('LeftForeArm')
    l_forearm.head = Vector((0.42 * s, 0, 1.18 * s))
    l_forearm.tail = Vector((0.60 * s, 0, 0.95 * s))
    l_forearm.parent = l_arm

    l_hand = eb.new('LeftHand')
    l_hand.head = Vector((0.60 * s, 0, 0.95 * s))
    l_hand.tail = Vector((0.68 * s, 0, 0.85 * s))
    l_hand.parent = l_forearm

    # Right Arm
    r_sh = eb.new('RightShoulder')
    r_sh.head = Vector((-0.05 * s, 0, 1.45 * s))
    r_sh.tail = Vector((-0.18 * s, 0, 1.42 * s))
    r_sh.parent = chest

    r_arm = eb.new('RightArm')
    r_arm.head = Vector((-0.18 * s, 0, 1.42 * s))
    r_arm.tail = Vector((-0.42 * s, 0, 1.18 * s))
    r_arm.parent = r_sh

    r_forearm = eb.new('RightForeArm')
    r_forearm.head = Vector((-0.42 * s, 0, 1.18 * s))
    r_forearm.tail = Vector((-0.60 * s, 0, 0.95 * s))
    r_forearm.parent = r_arm

    r_hand = eb.new('RightHand')
    r_hand.head = Vector((-0.60 * s, 0, 0.95 * s))
    r_hand.tail = Vector((-0.68 * s, 0, 0.85 * s))
    r_hand.parent = r_forearm

    # Left Leg
    l_upleg = eb.new('LeftUpLeg')
    l_upleg.head = Vector((0.12 * s, 0, 0.92 * s))
    l_upleg.tail = Vector((0.12 * s, 0, 0.50 * s))
    l_upleg.parent = hips

    l_leg = eb.new('LeftLeg')
    l_leg.head = Vector((0.12 * s, 0, 0.50 * s))
    l_leg.tail = Vector((0.12 * s, 0, 0.10 * s))
    l_leg.parent = l_upleg

    l_foot = eb.new('LeftFoot')
    l_foot.head = Vector((0.12 * s, 0, 0.10 * s))
    l_foot.tail = Vector((0.12 * s, 0.18 * s, 0.02 * s))
    l_foot.parent = l_leg

    # Right Leg
    r_upleg = eb.new('RightUpLeg')
    r_upleg.head = Vector((-0.12 * s, 0, 0.92 * s))
    r_upleg.tail = Vector((-0.12 * s, 0, 0.50 * s))
    r_upleg.parent = hips

    r_leg = eb.new('RightLeg')
    r_leg.head = Vector((-0.12 * s, 0, 0.50 * s))
    r_leg.tail = Vector((-0.12 * s, 0, 0.10 * s))
    r_leg.parent = r_upleg

    r_foot = eb.new('RightFoot')
    r_foot.head = Vector((-0.12 * s, 0, 0.10 * s))
    r_foot.tail = Vector((-0.12 * s, 0.18 * s, 0.02 * s))
    r_foot.parent = r_leg

    bpy.ops.object.mode_set(mode='OBJECT')

    # Parent Mesh to Armature with automatic weights
    bpy.ops.object.select_all(action='DESELECT')
    mesh_obj.select_set(True)
    arm_obj.select_set(True)
    bpy.context.view_layer.objects.active = arm_obj
    bpy.ops.object.parent_set(type='ARMATURE_AUTO')

    # Animate actions
    if char_type == 'Priest':
        create_priest_actions(arm_obj)
    elif char_type == 'Robber1':
        create_robber1_actions(arm_obj)
    elif char_type == 'Robber2':
        create_robber2_actions(arm_obj)

    # Export FBX
    bpy.ops.object.select_all(action='DESELECT')
    mesh_obj.select_set(True)
    arm_obj.select_set(True)
    bpy.context.view_layer.objects.active = arm_obj

    bpy.ops.export_scene.fbx(
        filepath=fbx_out,
        use_selection=True,
        apply_scale_options='FBX_SCALE_ALL',
        axis_forward='-Z',
        axis_up='Y',
        bake_anim=True,
        bake_anim_use_all_actions=True,
        bake_anim_use_nla_strips=False,
        bake_anim_step=1.0
    )
    print(f"SUCCESS: Exported rigged {char_type} to {fbx_out}")
    return True

def add_keyframe(pb, f, rot_deg):
    pb.rotation_mode = 'XYZ'
    pb.rotation_euler = Euler((math.radians(rot_deg[0]), math.radians(rot_deg[1]), math.radians(rot_deg[2])), 'XYZ')
    pb.keyframe_insert(data_path="rotation_euler", frame=f)

def create_priest_actions(arm_obj):
    # Action 1: Priest_Idle (Gentle prayer breathing, serene posture)
    act_idle = bpy.data.actions.new(name="Priest_Idle")
    arm_obj.animation_data_create()
    arm_obj.animation_data.action = act_idle

    pb_spine = arm_obj.pose.bones.get('Spine')
    pb_l_arm = arm_obj.pose.bones.get('LeftArm')
    pb_l_fore = arm_obj.pose.bones.get('LeftForeArm')
    pb_r_arm = arm_obj.pose.bones.get('RightArm')
    pb_r_fore = arm_obj.pose.bones.get('RightForeArm')
    pb_head = arm_obj.pose.bones.get('Head')

    for f in [1, 30, 60]:
        t = (f - 1) / 59.0
        breath = math.sin(t * math.pi * 2) * 2.0
        if pb_spine: add_keyframe(pb_spine, f, (breath, 0, 0))
        if pb_head: add_keyframe(pb_head, f, (-breath * 0.5, 0, 0))
        # Hands held gently in front
        if pb_l_arm: add_keyframe(pb_l_arm, f, (15, 10, 20))
        if pb_l_fore: add_keyframe(pb_l_fore, f, (45, 0, -30))
        if pb_r_arm: add_keyframe(pb_r_arm, f, (15, -10, -20))
        if pb_r_fore: add_keyframe(pb_r_fore, f, (45, 0, 30))

    # Action 2: Priest_Talk (Expressive hand gesturing and speaking)
    act_talk = bpy.data.actions.new(name="Priest_Talk")
    arm_obj.animation_data.action = act_talk

    for f in range(1, 61, 5):
        t = (f - 1) / 59.0
        nod = math.sin(t * math.pi * 4) * 5.0
        gesture = math.sin(t * math.pi * 3) * 12.0
        if pb_spine: add_keyframe(pb_spine, f, (nod * 0.4, 0, gesture * 0.2))
        if pb_head: add_keyframe(pb_head, f, (nod, 0, -gesture * 0.3))
        if pb_r_arm: add_keyframe(pb_r_arm, f, (25 + gesture, -10, -15))
        if pb_r_fore: add_keyframe(pb_r_fore, f, (55 + gesture * 0.8, 0, 25))
        if pb_l_arm: add_keyframe(pb_l_arm, f, (15 - gesture * 0.4, 10, 20))
        if pb_l_fore: add_keyframe(pb_l_fore, f, (45 - gesture * 0.3, 0, -30))

    # Action 3: Priest_Point (Pointing to storage room / sanctum)
    act_point = bpy.data.actions.new(name="Priest_Point")
    arm_obj.animation_data.action = act_point

    for f in [1, 15, 45, 60]:
        t = 1.0 if (f == 15 or f == 45) else 0.0
        if pb_r_arm: add_keyframe(pb_r_arm, f, (15 + t * 65, -t * 20, -t * 10))
        if pb_r_fore: add_keyframe(pb_r_fore, f, (45 - t * 35, 0, t * 15))
        if pb_head: add_keyframe(pb_head, f, (0, t * 15, 0))
        if pb_spine: add_keyframe(pb_spine, f, (t * 4, 0, t * 8))

def create_robber1_actions(arm_obj):
    # Kala: Confident, menacing, leader
    pb_spine = arm_obj.pose.bones.get('Spine')
    pb_l_arm = arm_obj.pose.bones.get('LeftArm')
    pb_l_fore = arm_obj.pose.bones.get('LeftForeArm')
    pb_r_arm = arm_obj.pose.bones.get('RightArm')
    pb_r_fore = arm_obj.pose.bones.get('RightForeArm')
    pb_head = arm_obj.pose.bones.get('Head')
    pb_l_leg = arm_obj.pose.bones.get('LeftUpLeg')
    pb_r_leg = arm_obj.pose.bones.get('RightUpLeg')

    # Robber1_Idle
    act_idle = bpy.data.actions.new(name="Robber1_Idle")
    arm_obj.animation_data_create()
    arm_obj.animation_data.action = act_idle
    for f in [1, 30, 60]:
        t = (f - 1) / 59.0
        b = math.sin(t * math.pi * 2) * 2.5
        if pb_spine: add_keyframe(pb_spine, f, (-4 + b, 0, 0))
        if pb_head: add_keyframe(pb_head, f, (4, 0, 0))
        if pb_l_arm: add_keyframe(pb_l_arm, f, (10, 5, 25))
        if pb_r_arm: add_keyframe(pb_r_arm, f, (10, -5, -25))
        if pb_l_fore: add_keyframe(pb_l_fore, f, (20, 0, 0))
        if pb_r_fore: add_keyframe(pb_r_fore, f, (20, 0, 0))

    # Robber1_Walk
    act_walk = bpy.data.actions.new(name="Robber1_Walk")
    arm_obj.animation_data.action = act_walk
    for f in range(1, 31, 3):
        w = ((f - 1) / 29.0) * math.pi * 2
        l_stride = math.sin(w) * 18.0
        r_stride = math.sin(w + math.pi) * 18.0
        if pb_l_leg: add_keyframe(pb_l_leg, f, (l_stride, 0, 0))
        if pb_r_leg: add_keyframe(pb_r_leg, f, (r_stride, 0, 0))
        if pb_l_arm: add_keyframe(pb_l_arm, f, (-l_stride * 0.7, 0, 15))
        if pb_r_arm: add_keyframe(pb_r_arm, f, (-r_stride * 0.7, 0, -15))
        if pb_spine: add_keyframe(pb_spine, f, (-2, math.sin(w) * 3, 0))

    # Robber1_Threaten
    act_threat = bpy.data.actions.new(name="Robber1_Threaten")
    arm_obj.animation_data.action = act_threat
    for f in [1, 15, 45, 60]:
        t = 1.0 if (f == 15 or f == 45) else 0.0
        if pb_r_arm: add_keyframe(pb_r_arm, f, (65 * t, 0, -10 * t))
        if pb_r_fore: add_keyframe(pb_r_fore, f, (15 * t, 0, 0))
        if pb_spine: add_keyframe(pb_spine, f, (8 * t, 0, 5 * t))
        if pb_head: add_keyframe(pb_head, f, (-5 * t, 0, 0))

def create_robber2_actions(arm_obj):
    # Rudra: Anxious, skittish, crouching
    pb_spine = arm_obj.pose.bones.get('Spine')
    pb_l_arm = arm_obj.pose.bones.get('LeftArm')
    pb_l_fore = arm_obj.pose.bones.get('LeftForeArm')
    pb_r_arm = arm_obj.pose.bones.get('RightArm')
    pb_r_fore = arm_obj.pose.bones.get('RightForeArm')
    pb_head = arm_obj.pose.bones.get('Head')
    pb_hips = arm_obj.pose.bones.get('Hips')

    # Robber2_Idle (Nervous, looking around)
    act_idle = bpy.data.actions.new(name="Robber2_Idle")
    arm_obj.animation_data_create()
    arm_obj.animation_data.action = act_idle
    for f in [1, 20, 40, 60]:
        look = 18.0 if f == 20 else (-18.0 if f == 40 else 0.0)
        if pb_head: add_keyframe(pb_head, f, (5, look, 0))
        if pb_spine: add_keyframe(pb_spine, f, (12, look * 0.3, 0))
        if pb_l_arm: add_keyframe(pb_l_arm, f, (25, 0, 15))
        if pb_r_arm: add_keyframe(pb_r_arm, f, (25, 0, -15))
        if pb_l_fore: add_keyframe(pb_l_fore, f, (45, 0, 0))
        if pb_r_fore: add_keyframe(pb_r_fore, f, (45, 0, 0))

    # Robber2_Stealth (Crouched creeping walk)
    act_stealth = bpy.data.actions.new(name="Robber2_Stealth")
    arm_obj.animation_data.action = act_stealth
    pb_l_leg = arm_obj.pose.bones.get('LeftUpLeg')
    pb_r_leg = arm_obj.pose.bones.get('RightUpLeg')
    for f in range(1, 31, 3):
        w = ((f - 1) / 29.0) * math.pi * 2
        l_stride = math.sin(w) * 12.0
        r_stride = math.sin(w + math.pi) * 12.0
        if pb_l_leg: add_keyframe(pb_l_leg, f, (20 + l_stride, 0, 0))
        if pb_r_leg: add_keyframe(pb_r_leg, f, (20 + r_stride, 0, 0))
        if pb_spine: add_keyframe(pb_spine, f, (16, math.sin(w) * 4, 0))
        if pb_l_arm: add_keyframe(pb_l_arm, f, (25 - l_stride * 0.5, 0, 20))
        if pb_r_arm: add_keyframe(pb_r_arm, f, (25 - r_stride * 0.5, 0, -20))

    # Robber2_Cornered (Defensive surrender)
    act_corner = bpy.data.actions.new(name="Robber2_Cornered")
    arm_obj.animation_data.action = act_corner
    for f in [1, 20, 45, 60]:
        t = 1.0 if (f == 20 or f == 45) else 0.0
        if pb_l_arm: add_keyframe(pb_l_arm, f, (55 * t, 20 * t, 35 * t))
        if pb_r_arm: add_keyframe(pb_r_arm, f, (55 * t, -20 * t, -35 * t))
        if pb_l_fore: add_keyframe(pb_l_fore, f, (70 * t, 0, 0))
        if pb_r_fore: add_keyframe(pb_r_fore, f, (70 * t, 0, 0))
        if pb_spine: add_keyframe(pb_spine, f, (-10 * t, 0, 0))
        if pb_head: add_keyframe(pb_head, f, (15 * t, 0, 0))

if __name__ == "__main__":
    char = sys.argv[-2]
    out_fbx = sys.argv[-1]
    in_fbx = out_fbx # Modify in-place or from current
    scale = 1.0 if char == 'Priest' else 1.05
    rig_character(in_fbx, out_fbx, char, scale)
