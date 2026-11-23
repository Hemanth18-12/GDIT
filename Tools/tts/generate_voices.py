# Builds every voice clip from Docs/VOICE_LINES.csv (the single source of truth).
#   Tools/tts/.venv/bin/python Tools/tts/generate_voices.py [--force] [--only id1,id2] [--no-asr]
# Order (best first): your own recording in Assets/_Project/Audio/Voice/Recorded/<id>.wav (or .mp3/.ogg/.aiff) is used as it is (only cleaned
# and levelled); otherwise Kokoro-82M (Apache-2.0) speaks the line with the speaker's voice from Tools/tts/voices.json.
# Every clip: trim silence, 10 ms fade in, 40 ms fade out, optional effect (cabin = inside the jeep, vault = stone hall), mono 44.1 kHz 16-bit,
# loudness -16 LUFS (two-pass EBU R128, true peak -1.5 dB). Then a check: duration, peak, loudness, pitch, and a Whisper transcription
# compared with the text. Report: Docs/QA/voice/clip_report.csv and summary.txt. Unchanged lines are skipped (Assets/_Project/Audio/Voice/manifest.json).
import argparse, csv, hashlib, json, os, re, subprocess, sys, tempfile
import numpy as np, soundfile as sf
ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
sys.path.insert(0, os.path.dirname(__file__))
from voice_qa import norm, median_f0
FF = "/opt/homebrew/bin/ffmpeg" if os.path.exists("/opt/homebrew/bin/ffmpeg") else "ffmpeg"
OUT = os.path.join(ROOT, "Assets", "_Project", "Audio", "Voice"); REC = os.path.join(OUT, "Recorded")
QA = os.path.join(ROOT, "Docs", "QA", "voice"); MANIFEST = os.path.join(OUT, "manifest.json")
NAMES = {"bhaskar", "suryapur", "jagannath", "kishore", "madhav", "meera", "pujari", "ananda", "aryan"}
NUM = {"1": "one", "2": "two", "3": "three", "4": "four", "5": "five", "6": "six", "7": "seven", "8": "eight", "9": "nine", "10": "ten"}
FX = {"cabin": "highpass=f=140,lowpass=f=5200,aecho=0.85:0.6:14|27:0.22|0.12",
      "vault": "lowpass=f=7500,aecho=0.85:0.7:55|110|170:0.32|0.2|0.1"}

def wer_names(ref, hyp):
    r = ["*" if w in NAMES else w for w in norm(ref)]; h = [NUM.get(w, w) for w in norm(hyp)]
    INF = 10 ** 6; d = np.full((len(r) + 1, len(h) + 1), INF); d[0, :] = range(len(h) + 1)
    for i in range(1, len(r) + 1):
        d[i, 0] = d[i - 1, 0] + (0 if r[i - 1] == "*" else 1)
        for j in range(1, len(h) + 1):
            if r[i - 1] == "*": d[i, j] = min(d[i - 1, j], d[i - 1, j - 1], d[i - 1, j - 2] if j >= 2 else INF)   # a name may be heard as 0, 1 or 2 words
            else: d[i, j] = min(d[i - 1, j] + 1, d[i, j - 1] + 1, d[i - 1, j - 1] + (r[i - 1] != h[j - 1]))
    return float(d[len(r), len(h)]) / max(1, sum(1 for w in r if w != "*"))

def loose(t): return [w.replace("'", "").rstrip("s") for w in norm(t) if w not in ("a", "an", "the", "is")]
def wer_loose(ref, hyp):
    """Like wer_names but forgiving about apostrophes, plural/possessive s and small words (a, an, the, is):
    homophones such as gate's/gates are not counted as mistakes."""
    return wer_names(" ".join(loose(ref)), " ".join(loose(hyp)))

def run(cmd): return subprocess.run(cmd, capture_output=True, text=True)
def loudness(path):
    p = run([FF, "-hide_banner", "-nostats", "-i", path, "-af", "loudnorm=I=-16:TP=-1.5:LRA=11:print_format=json", "-f", "null", "-"])
    return json.loads(p.stderr[p.stderr.rindex("{"):p.stderr.rindex("}") + 1])

def clean(x, sr):
    x = np.asarray(x, dtype=np.float32)
    if x.ndim > 1: x = x.mean(axis=1)
    thr = 10 ** (-50 / 20) * max(1e-6, float(np.max(np.abs(x))))
    idx = np.where(np.abs(x) > thr)[0]
    if len(idx): x = x[max(0, idx[0] - int(0.03 * sr)): min(len(x), idx[-1] + int(0.05 * sr))]
    fi, fo = int(0.01 * sr), int(0.04 * sr); x[:fi] *= np.linspace(0, 1, fi); x[-fo:] *= np.linspace(1, 0, fo)
    return np.concatenate([x, np.zeros(int(0.06 * sr), dtype=np.float32)])

kokoro = None
def make(L, v, rec, say, speed, out, tmp, whisper):
    global kokoro
    raw = os.path.join(tmp, "raw.wav")
    if rec:
        x, sr = sf.read(rec, always_2d=False) if not rec.endswith((".mp3", ".m4a")) else (None, None)
        if x is None: run([FF, "-y", "-i", rec, "-ac", "1", raw]); x, sr = sf.read(raw)
        source = "recording"
    else:
        if kokoro is None:
            from kokoro_onnx import Kokoro
            kokoro = Kokoro(os.path.join(os.path.dirname(__file__), ".models", "kokoro-v1.0.onnx"), os.path.join(os.path.dirname(__file__), ".models", "voices-v1.0.bin"))
        style = sum(w * kokoro.get_voice_style(name) for name, w in v["blend"].items())
        x, sr = kokoro.create(say, voice=style, speed=speed, lang="en-us"); source = "kokoro " + "+".join(f"{n}:{w}" for n, w in v["blend"].items())
    sf.write(raw, clean(x, sr), sr)
    chain = []
    if not rec and abs(v["pitch"]) > 0.01:
        p = 2 ** (v["pitch"] / 12.0); chain += [f"asetrate={sr}*{p:.5f}", f"aresample={sr}", f"atempo={1 / p:.5f}"]
    if L["fx"] in FX: chain.append(FX[L["fx"]])
    chain.append("aresample=44100"); mid = os.path.join(tmp, "mid.wav")
    run([FF, "-y", "-hide_banner", "-i", raw, "-af", ",".join(chain), "-ac", "1", "-ar", "44100", mid])
    # levelling: gain to -16 LUFS, then a fast peak limiter at -1.5 dBTP (short TTS lines have sharp peaks, so plain linear
    # gain would stop short of the target); measured again and corrected once if the limiter took some loudness away
    gain = -16.0 - float(loudness(mid)["input_i"])
    for attempt in range(3):
        run([FF, "-y", "-hide_banner", "-i", mid, "-af", f"volume={gain:.2f}dB,alimiter=limit=0.82:attack=2:release=40:level=disabled,aresample=44100",
             "-ac", "1", "-ar", "44100", "-c:a", "pcm_s16le", out])
        f = loudness(out); miss = -16.0 - float(f["input_i"])
        if abs(miss) < 0.5: break
        gain += miss
    y, ysr = sf.read(out)
    qa = {"id": L["id"], "speaker": L["speaker"], "source": source, "seconds": round(len(y) / ysr, 2), "peak_db": round(20 * np.log10(max(1e-9, float(np.max(np.abs(y))))), 1),
          "lufs": float(f["input_i"]), "true_peak": float(f["input_tp"]), "f0": round(median_f0(y, ysr)), "text": L["text"], "heard": "", "wer": ""}
    interjection = len([w for w in norm(L["text"]) if w not in ("ugh", "ah", "oof", "hm", "whoa", "argh", "careful")]) < 1
    if whisper is not None and interjection: qa["heard"] = "(interjection, not checked)"
    elif whisper is not None:
        y16 = np.interp(np.arange(0, len(y), ysr / 16000), np.arange(len(y)), y).astype(np.float32)
        segs, _ = whisper.transcribe(y16, language="en", beam_size=5); qa["heard"] = " ".join(s.text for s in segs).strip(); qa["wer"] = round(wer_loose(L["text"], qa["heard"]), 2); qa["wer_strict"] = round(wer_names(L["text"], qa["heard"]), 2)
    return qa

def main():
    ap = argparse.ArgumentParser(); ap.add_argument("--force", action="store_true"); ap.add_argument("--only", default=""); ap.add_argument("--no-asr", action="store_true"); a = ap.parse_args()
    lines = list(csv.DictReader(open(os.path.join(ROOT, "Docs", "VOICE_LINES.csv"))))
    voices = json.load(open(os.path.join(os.path.dirname(__file__), "voices.json")))
    only = set(x for x in a.only.split(",") if x)
    manifest = json.load(open(MANIFEST)) if os.path.exists(MANIFEST) else {}
    os.makedirs(QA, exist_ok=True)
    whisper = None
    if not a.no_asr:
        os.environ.setdefault("HF_HOME", os.path.join(os.path.dirname(__file__), ".models", "hf"))
        from faster_whisper import WhisperModel; whisper = WhisperModel("base.en", device="cpu", compute_type="int8")
    report = []
    for L in lines:
        if only and L["id"] not in only: continue
        spk = L["speaker"]; v = voices[spk]; out = os.path.join(ROOT, L["clip"]); os.makedirs(os.path.dirname(out), exist_ok=True)
        rec = next((os.path.join(REC, L["id"] + e) for e in (".wav", ".mp3", ".ogg", ".aiff", ".m4a") if os.path.exists(os.path.join(REC, L["id"] + e))), None)
        key = hashlib.sha1(json.dumps([L["text"], v, L["fx"], rec and os.path.getmtime(rec)], sort_keys=True).encode()).hexdigest()[:16]
        if not a.force and manifest.get(L["id"], {}).get("key") == key and os.path.exists(out):
            q = manifest[L["id"]]["qa"]
            if q.get("heard") and not q["heard"].startswith("("): q["wer"] = round(wer_loose(L["text"], q["heard"]), 2); q["wer_strict"] = round(wer_names(L["text"], q["heard"]), 2)
            report.append(q); continue
        tmp = tempfile.mkdtemp(prefix="vo_")
        takes = [(L["text"], v["speed"], "take 1")]
        if not rec and not a.no_asr:
            takes += [(L["text"], v["speed"] * 0.9, "take 2 (slower)"), (L["text"], v["speed"] * 1.1, "take 3 (faster)")]
            if ", " in L["text"]: takes.append((L["text"].replace(", ", "... "), v["speed"], "take 4 (pause at comma)"))
        best = None
        for n, (say, speed, label) in enumerate(takes):
            if n > 0 and best is not None and (best["wer"] == "" or float(best["wer"]) <= 0.25): break   # only retake lines that were heard wrongly
            out_n = out if n == 0 else os.path.join(tmp, f"take{n}.wav")
            qa = make(L, v, rec, say, speed, out_n, tmp, whisper); qa["take"] = label
            if best is None or (qa["wer"] != "" and best["wer"] != "" and float(qa["wer"]) < float(best["wer"])):
                if n > 0: os.replace(out_n, out)
                best = qa
        qa = best
        print(f"{L['id']:22} {spk:10} {qa['seconds']:5.2f}s {qa['lufs']:6.1f} LUFS tp {qa['true_peak']:5.1f} f0 {qa['f0']:4} wer {qa['wer']} {qa['take']}  | {qa['heard'][:60]}", flush=True)
        report.append(qa); manifest[L["id"]] = {"key": key, "qa": qa}
    json.dump(manifest, open(MANIFEST, "w"), indent=1, sort_keys=True)
    with open(os.path.join(QA, "clip_report.csv"), "w", newline="") as fh:
        cols = ["id", "speaker", "source", "take", "seconds", "peak_db", "lufs", "true_peak", "f0", "text", "heard", "wer", "wer_strict"]
        w = csv.DictWriter(fh, fieldnames=cols, restval="", extrasaction="ignore"); w.writeheader(); w.writerows(report)
    print(f"done: {len(report)} clips")

if __name__ == "__main__": main()
