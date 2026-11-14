#!/usr/bin/env python3
"""Downloads a curated set of CC0 models and PBR textures from Poly Haven (polyhaven.com, licence CC0 1.0)
   into Assets/_Project/Art/PolyHaven/ and writes ATTRIBUTION_polyhaven.csv.   python3 fetch_polyhaven.py"""
import json, os, urllib.request, csv, sys, datetime
ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "../.."))
OUT = os.path.join(ROOT, "Assets/_Project/Art/PolyHaven")
UA = {"User-Agent": "legacy-of-the-temple-student-project"}
MODELS = ["brass_diya_lantern", "brass_pot_01", "brass_pot_02", "brass_vase_03", "brass_goblets", "vintage_oil_lamp", "wooden_lantern_01", "lion_head",
          "carved_wooden_elephant", "carved_wooden_plate", "fern_02", "nettle_plant", "periwinkle_plant", "rock_07", "rock_09", "rock_moss_set_01", "rock_moss_set_02",
          "tree_stump_01", "Barrel_01", "ceramic_vase_01", "antique_ceramic_vase_01", "stone_fire_pit", "large_castle_door", "WoodenTable_01", "folding_wooden_stool",
          "wooden_bowl_02", "calathea_orbifolia_01", "dry_branches_medium_01", "ceramic_pot", "planter_box_01"]
TEXTURES = {"red_sandstone_pavement": "2k", "red_sandstone_wall": "2k", "sandstone_blocks_08": "2k", "mossy_sandstone": "1k", "clay_plaster": "1k", "yellow_plaster": "1k",
            "white_plaster_rough_01": "1k", "thatch_roof_angled": "1k", "clay_roof_tiles_02": "1k", "red_laterite_soil_stones": "2k", "forest_leaves_02": "2k",
            "brown_mud_leaves_01": "1k", "weathered_planks": "1k", "stony_dirt_path": "1k", "bark_brown_02": "1k", "medieval_wood": "1k"}

def get(url): return urllib.request.urlopen(urllib.request.Request(url, headers=UA), timeout=120).read()
def save(url, path):
    if os.path.exists(path) and os.path.getsize(path) > 0: return 0
    os.makedirs(os.path.dirname(path), exist_ok=True)
    data = get(url); open(path, "wb").write(data); return len(data)

rows = []; total = 0
info_m = json.loads(get("https://api.polyhaven.com/assets?t=models")); info_t = json.loads(get("https://api.polyhaven.com/assets?t=textures"))
for name in MODELS:
    try:
        f = json.loads(get("https://api.polyhaven.com/files/" + name))
        d = os.path.join(OUT, "Models", name)
        total += save(f["fbx"]["1k"]["fbx"]["url"], os.path.join(d, name + ".fbx"))
        for rel, v in f["gltf"]["1k"]["gltf"]["include"].items():
            if rel.startswith("textures/") and rel.lower().endswith((".jpg", ".png")): total += save(v["url"], os.path.join(d, os.path.basename(rel)))
        a = info_m[name]; rows.append([f"Assets/_Project/Art/PolyHaven/Models/{name}/", a["name"], ", ".join(a["authors"].keys()), f"https://polyhaven.com/a/{name}", "CC0 1.0"])
        print("model", name, a.get("polycount")); sys.stdout.flush()
    except Exception as e: print("FAILED model", name, e)
for name, res in TEXTURES.items():
    try:
        f = json.loads(get("https://api.polyhaven.com/files/" + name))
        d = os.path.join(OUT, "Textures", name)
        for key, suffix in (("Diffuse", "diff"), ("nor_gl", "nor_gl"), ("arm", "arm")):
            total += save(f[key][res]["jpg"]["url"], os.path.join(d, f"{name}_{suffix}.jpg"))
        a = info_t[name]; rows.append([f"Assets/_Project/Art/PolyHaven/Textures/{name}/", a["name"], ", ".join(a["authors"].keys()), f"https://polyhaven.com/a/{name}", "CC0 1.0"])
        print("texture", name); sys.stdout.flush()
    except Exception as e: print("FAILED texture", name, e)
with open(os.path.join(ROOT, "Docs/ATTRIBUTION_polyhaven.csv"), "w", newline="") as fh:
    w = csv.writer(fh); w.writerow(["Files", "Title", "Author(s)", "URL", "Licence", "Checked"])
    for r in rows: w.writerow(r + [datetime.date.today().isoformat()])
print("downloaded MB:", round(total / 1e6, 1), "assets:", len(rows))
