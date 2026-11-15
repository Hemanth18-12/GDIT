# Makes the jeep's three textures (own work): number plate, original temple emblem, paint dirt mask.
import sys, numpy as np
from PIL import Image, ImageDraw, ImageFont, ImageFilter
out = sys.argv[1]
# ---- plate (fictional number)
W, H = 1024, 256
im = Image.new("RGB", (W, H), (236, 236, 228)); d = ImageDraw.Draw(im)
d.rounded_rectangle((6, 6, W - 7, H - 7), 22, outline=(20, 20, 20), width=10)
f = ImageFont.truetype("/System/Library/Fonts/Supplemental/Arial Bold.ttf", 122)
t = "IND 01 BX 7721"; bb = d.textbbox((0, 0), t, font=f)
d.text(((W - (bb[2] - bb[0])) / 2 - bb[0], (H - (bb[3] - bb[1])) / 2 - bb[1]), t, font=f, fill=(15, 15, 15))
im.save(out + "/T_Jeep_Plate.png")
# ---- emblem: an original temple-dome silhouette in a ring (not any real logo)
S = 1024; e = Image.new("RGBA", (S, S), (0, 0, 0, 0)); d = ImageDraw.Draw(e); G = (214, 170, 88, 255); C = (0, 0, 0, 0); c = S // 2
d.ellipse((40, 40, S - 40, S - 40), outline=G, width=22)
for i, (hw, y0, y1) in enumerate(((330, 760, 800), (290, 720, 752), (250, 684, 712))): d.rectangle((c - hw, y0, c + hw, y1), fill=G)   # steps
d.rectangle((c - 150, 470, c + 150, 684), fill=G)                                   # main hall
d.pieslice((c - 150, 300, c + 150, 600), 180, 360, fill=G)                          # main dome
d.polygon([(c - 40, 330), (c, 200), (c + 40, 330)], fill=G); d.ellipse((c - 16, 176, c + 16, 208), fill=G)   # spire and finial
d.rectangle((c - 6, 150, c + 6, 190), fill=G)
for s in (-1, 1):                                                                   # side towers
    x = c + s * 215
    d.rectangle((x - 55, 540, x + 55, 684), fill=G); d.pieslice((x - 55, 470, x + 55, 610), 180, 360, fill=G)
    d.polygon([(x - 14, 490), (x, 430), (x + 14, 490)], fill=G)
    d.rounded_rectangle((x - 20, 590, x + 20, 684), 20, fill=C)
d.rounded_rectangle((c - 48, 540, c + 48, 690), 48, fill=C)                         # arched door
for s in (-1, 1): d.rounded_rectangle((c + s * 100 - 18, 520, c + s * 100 + 18, 600), 18, fill=C)   # windows
e = e.resize((512, 512), Image.LANCZOS); e.save(out + "/T_Jeep_Emblem.png")
# ---- paint dirt: near-white (the paint colour multiplies it), mud and dust low on the body, a few fine scratches
rng = np.random.default_rng(7); W, H = 1024, 512
def blur_noise(sigma):
    a = Image.fromarray((rng.random((H, W)) * 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(sigma))
    a = np.asarray(a, dtype=np.float32); return (a - a.min()) / (a.max() - a.min() + 1e-6)
v = np.linspace(1, 0, H)[:, None] * np.ones((1, W))            # 1 = roof, 0 = ground
low = np.clip((0.50 - v) / 0.27, 0, 1) ** 1.1
mud = np.clip(low * (0.45 + 0.9 * blur_noise(14)) + low * 0.5 * (blur_noise(3) > 0.6), 0, 1)
spk = ((rng.random((H, W)) > 0.9975) * (v < 0.50) * (rng.random((H, W)) < np.clip((0.52 - v) / 0.3, 0, 1))).astype(np.float32)
spk = np.asarray(Image.fromarray((spk * 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(1.3)), dtype=np.float32) / 255 * 6
mud = np.clip(mud + np.clip(spk, 0, 1) * 0.8, 0, 1) * 0.85
dust = 0.93 + 0.07 * blur_noise(40)
base = np.stack([dust, dust, dust], -1); mudc = np.array([0.50, 0.40, 0.29], dtype=np.float32)
img = base * (1 - mud[..., None]) + mudc * mud[..., None]
pil = Image.fromarray((np.clip(img, 0, 1) * 255).astype(np.uint8)); d = ImageDraw.Draw(pil)
for _ in range(26):
    x, y = rng.integers(0, W), rng.integers(int(H * 0.35), int(H * 0.8)); L = rng.integers(20, 90)
    d.line((x, y, x + L, y + rng.integers(-5, 6)), fill=(255, 255, 255) if rng.random() > 0.5 else (196, 196, 190), width=1)
pil.save(out + "/T_Jeep_PaintDirt.png"); print("textures ok")
