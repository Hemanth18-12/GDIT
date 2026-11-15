import sys
from PIL import Image
d, out, names = sys.argv[1], sys.argv[2], sys.argv[3:]
ims = [Image.open(f"{d}/{n}.png").convert("RGB").resize((800, 450)) for n in names]
cols = 2; rows = (len(ims) + 1) // 2
sheet = Image.new("RGB", (cols * 800, rows * 450))
for i, im in enumerate(ims): sheet.paste(im, ((i % cols) * 800, (i // cols) * 450))
sheet.save(out)
