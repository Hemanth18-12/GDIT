"""Read-only inspection and preview of the student's original temple.
Never calls save_as_mainfile or writes to the source directory.
"""
import bpy, json, math
from pathlib import Path
from mathutils import Vector

ROOT=Path('/Users/hemanth/Desktop/GDIT')
OUT=ROOT/'Docs/QA/2026-11-25-original-locations'
OUT.mkdir(exist_ok=True)
meshes=[o for o in bpy.context.scene.objects if o.type=='MESH']
report=[]
for obj in meshes:
    pts=[obj.matrix_world@Vector(c) for c in obj.bound_box]
    report.append({'name':obj.name,'min':[min(p[i] for p in pts) for i in range(3)],'max':[max(p[i] for p in pts) for i in range(3)],'polygons':len(obj.data.polygons),'materials':[m.name for m in obj.data.materials if m]})
(OUT/'original-temple-inventory.json').write_text(json.dumps({'source':bpy.data.filepath,'objects':report},indent=2))
points=[o.matrix_world@Vector(c) for o in meshes for c in o.bound_box]
lo=Vector([min(p[i] for p in points) for i in range(3)])
hi=Vector([max(p[i] for p in points) for i in range(3)])
center=(lo+hi)*0.5
radius=max(hi-lo)
scene=bpy.context.scene
scene.render.engine='CYCLES';scene.cycles.samples=16
scene.render.resolution_x=1200;scene.render.resolution_y=900;scene.render.resolution_percentage=100
for obj in scene.objects:
    if obj.type in {'LIGHT','CAMERA'}: obj.hide_render=True
camera=bpy.data.objects.new('Inspection camera',bpy.data.cameras.new('Inspection camera'))
scene.collection.objects.link(camera)
camera.location=center+Vector((0,-radius*1.4,radius*0.85))
camera.rotation_euler=(center-camera.location).to_track_quat('-Z','Y').to_euler()
camera.data.type='ORTHO';camera.data.ortho_scale=radius*1.35;scene.camera=camera
sun=bpy.data.objects.new('Inspection sun',bpy.data.lights.new('Inspection sun','SUN'))
scene.collection.objects.link(sun);sun.data.energy=3
sun.rotation_euler=(math.radians(25),math.radians(-25),math.radians(-25))
world=bpy.data.worlds.new('Inspection world');scene.world=world;world.use_nodes=True
world.node_tree.nodes['Background'].inputs['Color'].default_value=(0.3,0.35,0.42,1)
world.node_tree.nodes['Background'].inputs['Strength'].default_value=0.6
scene.render.filepath=str(OUT/'original-temple-reference.png')
bpy.ops.render.render(write_still=True)
print('READ_ONLY_SOURCE',bpy.data.filepath,'bounds',list(lo),list(hi),'meshes',len(meshes))
