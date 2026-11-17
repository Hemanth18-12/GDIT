# Indistinct village chatter (own work): short everyday phrases in the Mac's Indian English voices, mixed in pairs and
# low-passed so no words can be made out. Three loops for villagers who are talking.
import os, subprocess, sys, numpy as np, wave
ROOT = sys.argv[1]; OUT = os.path.join(ROOT, "Assets/_Project/Audio/Resources/Audio"); SR = 22050
PH = [("Aman", "Accha, haan, theek hai. Kal subah mandir chalenge. Chai peeyoge?"), ("Tara", "Haan ji, bilkul. Mela kab shuru hoga? Bazaar mein bheed hai."),
      ("Aman", "Arre bhai, suno. Woh gaadi kahan gayi? Abhi aata hoon."), ("Tara", "Dekho na, aaj bahut garmi hai. Paani lao, jaldi karo."),
      ("Rishi", "Chalo chalo, der ho rahi hai. Dukaan band karni hai."), ("Tara", "Mausi ji kaise hain? Sab theek hai, bhagwan ki daya hai.")]
def say(voice, text, rate):
    a = "/tmp/lot/ch.aiff"; w = "/tmp/lot/ch.wav"
    subprocess.run(["say", "-v", voice, "-r", str(rate), "-o", a, text], check=True)
    subprocess.run(["afconvert", "-f", "WAVE", "-d", "LEI16@22050", "-c", "1", a, w], check=True)
    with wave.open(w) as f: x = np.frombuffer(f.readframes(f.getnframes()), dtype=np.int16).astype(np.float32) / 32768
    return x
def lowpass(x, fc):
    k = np.exp(-2 * np.pi * fc / SR); y = np.zeros_like(x); acc = 0.0
    for i, v in enumerate(x): acc = (1 - k) * v + k * acc; y[i] = acc
    return y
os.makedirs("/tmp/lot", exist_ok=True)
for n in range(3):
    a = say(*PH[n * 2], 175); b = say(*PH[n * 2 + 1], 185)
    L = int(SR * 7.0); mix = np.zeros(L, np.float32)
    for x, start in ((a, 0), (b, int(SR * 1.6)), (a[::1], int(SR * 4.1))):
        seg = x[: max(0, L - start)]; mix[start:start + len(seg)] += seg * 0.8
    mix = lowpass(lowpass(mix, 900), 900)   # muffled: heard as talk, not as words
    fade = int(SR * 0.4); mix[:fade] *= np.linspace(0, 1, fade); mix[-fade:] *= np.linspace(1, 0, fade)
    mix = mix / (np.max(np.abs(mix)) + 1e-6) * 0.7
    with wave.open(os.path.join(OUT, f"talk_murmur_{n + 1}.wav"), "w") as f:
        f.setnchannels(1); f.setsampwidth(2); f.setframerate(SR); f.writeframes((mix * 32767).astype(np.int16).tobytes())
print("chatter loops made: 3")
