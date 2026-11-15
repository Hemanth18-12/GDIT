# Builds the QA images from the transparent renders: plain views, 50% overlays on the reference, side-by-side sheet.
# usage: python3 compose_qa.py <render_prefix> <out_prefix>
import sys
from PIL import Image, ImageDraw
B = "/Users/hemanth/Desktop/GDIT/Docs/Blender/Jeep/refs/"
src, out = sys.argv[1], sys.argv[2]
BG = (58, 60, 64)

def flat(im):
    bg = Image.new("RGBA", im.size, BG + (255,)); bg.alpha_composite(im); return bg.convert("RGB")

tiles = []
for k in ("left", "right", "front", "back"):
    r = Image.open(f"{src}{k}.png").convert("RGBA")
    ref = Image.open(f"{B}ref_{k}.png").convert("RGBA").resize(r.size)
    flat(r).save(f"{out}{k}.png")
    half = r.copy(); half.putalpha(r.getchannel("A").point(lambda a: a * 0.55))
    ov = ref.copy(); ov.alpha_composite(half)
    # outline of the model silhouette in yellow so edges are easy to compare
    a = r.getchannel("A").point(lambda v: 255 if v > 128 else 0)
    from PIL import ImageFilter
    edge = a.filter(ImageFilter.FIND_EDGES)
    yel = Image.new("RGBA", r.size, (255, 220, 0, 255)); ov.paste(yel, (0, 0), edge)
    ov.convert("RGB").save(f"{out}overlay_{k}.png")
    tiles.append((k, ref.convert("RGB"), flat(r), ov.convert("RGB")))
for k in ("34_front", "34_rear"):
    flat(Image.open(f"{src}{k}.png").convert("RGBA")).save(f"{out}{k}.png")

# comparison sheet: reference | model | overlay, scaled to 280 px high
rows = []
for k, a, b, c in tiles:
    s = 280 / a.height
    ims = [i.resize((int(i.width * s), 280)) for i in (a, b, c)]
    row = Image.new("RGB", (sum(i.width for i in ims) + 20, 300), (20, 20, 22)); x = 0
    for i in ims:
        row.paste(i, (x, 20)); x += i.width + 10
    ImageDraw.Draw(row).text((4, 4), f"{k.upper()}: reference | block-out | overlay (yellow = model outline)", fill=(255, 255, 255))
    rows.append(row)
W = max(r.width for r in rows)
sheet = Image.new("RGB", (W, sum(r.height for r in rows)), (20, 20, 22)); y = 0
for r in rows:
    sheet.paste(r, (0, y)); y += r.height
sheet.save(f"{out}compare_sheet.png")
print("ok", sheet.size)
