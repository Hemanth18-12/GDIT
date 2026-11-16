# V3 voice lines with the Mac's built-in Indian English voices (macOS `say`, own work), one wav per line,
# into Resources/Voice/<speaker>/<line id>_tts.wav, where VoicePlayer looks for them.
import json, os, subprocess, sys
ROOT = sys.argv[1]
story = json.load(open(os.path.join(ROOT, "Assets/_Project/Resources/Story/story.json")))
VOICE = {"aryan": ("Rishi", 175), "pujari": ("Aman", 150), "meera": ("Tara", 180), "madhav": ("Aman", 165), "bhaskar": ("Rishi", 160), "courier": ("Rishi", 190), "kishore": ("Aman", 170)}
lines = list(story["barks"]) + [l for c in story["conversations"] if c["id"].startswith("conv_v3") for l in c["lines"]]
made = 0
for l in lines:
    if not l["id"].startswith("v3_"): continue
    v, rate = VOICE.get(l["s"], ("Rishi", 170))
    d = os.path.join(ROOT, "Assets/_Project/Resources/Voice", l["s"]); os.makedirs(d, exist_ok=True)
    aiff = f"/tmp/lot/{l['id']}.aiff"; wav = os.path.join(d, l["id"] + "_tts.wav")
    subprocess.run(["say", "-v", v, "-r", str(rate), "-o", aiff, l["t"]], check=True)
    subprocess.run(["afconvert", "-f", "WAVE", "-d", "LEI16@22050", "-c", "1", aiff, wav], check=True)
    made += 1
print("voices made", made)
