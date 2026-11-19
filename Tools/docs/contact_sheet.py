# Contact sheet of every walkthrough shot (thumbnails with file names): Docs/Walkthrough/contact_sheet.png
import os, sys
from PIL import Image, ImageDraw, ImageFont
d = sys.argv[1] if len(sys.argv) > 1 else os.path.join(os.path.dirname(__file__), "..", "..", "Docs", "Walkthrough", "shots")
out = sys.argv[2] if len(sys.argv) > 2 else os.path.join(os.path.dirname(__file__), "..", "..", "Docs", "Walkthrough", "contact_sheet.png")
files = sorted(f for f in os.listdir(d) if f.endswith(".png"))
cols, tw, th, lab = 6, 400, 225, 30
rows = (len(files) + cols - 1) // cols
sheet = Image.new("RGB", (cols * tw, rows * (th + lab)), (30, 20, 12)); dr = ImageDraw.Draw(sheet)
try: f = ImageFont.truetype("/System/Library/Fonts/Supplemental/Arial.ttf", 17)
except Exception: f = ImageFont.load_default()
for i, name in enumerate(files):
    im = Image.open(os.path.join(d, name)).convert("RGB"); im.thumbnail((tw - 6, th - 6))
    x, y = (i % cols) * tw, (i // cols) * (th + lab); sheet.paste(im, (x + 3, y + 3)); dr.text((x + 4, y + th + 4), name[:44], fill=(232, 199, 102), font=f)
sheet.save(out); print(out, len(files), "shots")
