# Top-down plan of the V3 world drawn from REAL coordinates the game logs when it builds each place
# (Docs/Walkthrough/raw/player.log: "[BreakIn] zone 2 puzzle built at ...", etc.). North (-z in Unity) is up. A diagram, not a screenshot.
from PIL import Image, ImageDraw, ImageFont
import os
P = {  # name: (x, z) in metres, from the log of the walkthrough run on 2026-11-19
    "Arrival / tea stall": (2, 59), "1 Bazaar yard": (21, 145), "2 Storeroom": (-17, -74), "3 Vaults": (0, -202.5),
    "4 Stepwell": (137.1, -134), "5 Sun Wall": (-137.6, -135), "6 River road start": (30.5, -221), "6 Ruin court (final fight)": (39.5, -457),
    "Temple tower (tallest point)": (-111.3, -149.1)}
W, H, S = 1400, 1500, 2.1; ox, top, zmin = 700, 130, -470   # north (-z) at the top: y grows with z
def px(x, z): return (ox + x * S, top + (z - zmin) * S)
img = Image.new("RGB", (W, H), (36, 24, 14)); d = ImageDraw.Draw(img)
for gx in range(0, W, int(50 * S)): d.line([(gx, 80), (gx, H - 70)], fill=(52, 38, 24), width=1)
for gy in range(80, H - 70, int(50 * S)): d.line([(0, gy), (W, gy)], fill=(52, 38, 24), width=1)

try: f = ImageFont.truetype("/System/Library/Fonts/Supplemental/Arial.ttf", 30); fb = ImageFont.truetype("/System/Library/Fonts/Supplemental/Arial Bold.ttf", 34)
except Exception: f = fb = ImageFont.load_default()
# (the temple footprint is not drawn: only the tower top position is logged)
for u, v in [("1 Bazaar yard", "Arrival / tea stall"), ("Arrival / tea stall", "2 Storeroom"), ("2 Storeroom", "3 Vaults"), ("2 Storeroom", "4 Stepwell"), ("2 Storeroom", "5 Sun Wall"), ("3 Vaults", "6 River road start"), ("6 River road start", "6 Ruin court (final fight)")]:
    d.line([px(*P[u]), px(*P[v])], fill=(120, 96, 60), width=6)
cols = {"1": (196, 120, 50), "2": (214, 172, 80), "3": (140, 120, 170), "4": (90, 130, 200), "5": (230, 215, 160), "6": (90, 160, 90)}
for name, (x, z) in P.items():
    c = cols.get(name[0], (240, 220, 170)); p = px(x, z); r = 16
    if name.startswith("Temple"): d.polygon([(p[0], p[1] - 26), (p[0] - 18, p[1] + 14), (p[0] + 18, p[1] + 14)], fill=(232, 199, 102))
    else: d.ellipse([p[0] - r, p[1] - r, p[0] + r, p[1] + r], fill=c, outline=(20, 12, 6), width=3)
    tw = d.textlength(name, font=f); lx = p[0] + 24 if p[0] + 24 + tw < W - 10 else p[0] - 24 - tw
    ly = p[1] - 66 if name.startswith("Temple") else p[1] - 17
    d.text((lx, ly), name, fill=(243, 226, 181), font=f)
d.text((30, 24), "World plan from real game coordinates (north up, 1 square = 50 m)", fill=(232, 199, 102), font=fb)
d.text((30, H - 50), "Drawn from positions the game logs. Lines are links, not road shapes.", fill=(200, 180, 140), font=f)
d.text((W - 80, 90), "N", fill=(243, 226, 181), font=fb); d.line([(W - 68, 135), (W - 68, 200)], fill=(243, 226, 181), width=4)
out = os.path.join(os.path.dirname(__file__), "..", "..", "Docs", "Walkthrough", "shots", "map_plan_from_coordinates.png"); img.save(out); print(out)
