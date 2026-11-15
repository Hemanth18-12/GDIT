# Renders the block-out from the reference cameras (Workbench, transparent). Run inside Blender.
# Set OUT_PREFIX before exec, e.g. OUT_PREFIX = ".../refs/_r1_"
import bpy
sc = bpy.context.scene
sc.render.engine = 'BLENDER_WORKBENCH'
sh = sc.display.shading
sh.light = 'STUDIO'; sh.color_type = 'MATERIAL'; sh.show_cavity = True; sh.show_shadows = False
sh.show_object_outline = True
sc.render.film_transparent = True
sc.render.image_settings.file_format = 'PNG'
sc.render.image_settings.color_mode = 'RGBA'
sc.view_settings.view_transform = 'Standard'
VIEWS = {"left": ("CAM_Left", 1720, 840), "right": ("CAM_Right", 1760, 840), "front": ("CAM_Front", 860, 840),
         "back": ("CAM_Back", 840, 840), "34_front": ("CAM_34_Front", 1600, 900), "34_rear": ("CAM_34_Rear", 1600, 900)}
done = []
for k, (cn, w, h) in VIEWS.items():
    sc.camera = bpy.data.objects[cn]
    sc.render.resolution_x = w; sc.render.resolution_y = h; sc.render.resolution_percentage = 100
    sc.render.filepath = OUT_PREFIX + k + ".png"
    bpy.ops.render.render(write_still=True)
    done.append(sc.render.filepath)
result = {"rendered": done}
