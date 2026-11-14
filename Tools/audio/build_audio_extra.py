#!/usr/bin/env python3
"""Part 2 sounds: jeep doors, glass, metal hit, phone, village ambience, birds, intro score (own work, CC0)."""

import os, wave, numpy as np
SR = 22050
OUT = os.path.abspath(os.path.join(os.path.dirname(__file__), "../../Assets/_Project/Audio/Resources/Audio"))
rng = np.random.default_rng(17)
made = []

def T(d): return np.arange(int(SR * d)) / SR
def noise(d): return rng.uniform(-1, 1, int(SR * d))
def env(d, a=0.005, r=None, curve=3.0):
    t = T(d); r = d if r is None else r
    e = np.minimum(1, t / max(a, 1e-4)) * np.exp(-curve * t / r)
    return e
def lp(x, fc):
    a = np.exp(-2 * np.pi * fc / SR); y = np.empty_like(x); s = 0.0
    for i in range(len(x)): s = (1 - a) * x[i] + a * s; y[i] = s
    return y
def hp(x, fc): return x - lp(x, fc)
def bp(x, lo, hi): return hp(lp(x, hi), lo)
def sine(f, d, ph=0.0): return np.sin(2 * np.pi * f * T(d) + ph)
def sweep(f0, f1, d): t = T(d); return np.sin(2 * np.pi * (f0 * t + (f1 - f0) * t * t / (2 * d)))
def mix(*parts):
    n = max(len(p) for p in parts); y = np.zeros(n)
    for p in parts: y[:len(p)] += p
    return y
def at(x, t, total):
    y = np.zeros(int(SR * total)); i = int(SR * t); n = min(len(x), len(y) - i)
    if n > 0: y[i:i + n] += x[:n]
    return y
def loopable(x, fade=0.4):
    n = int(SR * fade); y = x[:-n].copy(); w = np.linspace(0, 1, n)
    y[:n] = y[:n] * w + x[-n:] * (1 - w); return y
def save(name, x, peak=0.8):
    x = np.asarray(x, dtype=np.float64); m = np.max(np.abs(x)) or 1.0
    x = np.clip(x / m * peak, -1, 1)
    with wave.open(os.path.join(OUT, name + ".wav"), "wb") as w:
        w.setnchannels(1); w.setsampwidth(2); w.setframerate(SR); w.writeframes((x * 32767).astype(np.int16).tobytes())
    made.append(name)

os.makedirs(OUT, exist_ok=True)

def bell(f, d, bright=1.0):
    parts = [(1.0, 1.0), (2.76, 0.5 * bright), (5.4, 0.3 * bright), (8.93, 0.15 * bright), (0.5, 0.3)]
    return sum(a * sine(f * k, d) * env(d, 0.002, d / (1 + 0.4 * i)) for i, (k, a) in enumerate(parts))
BAR = 3.0; LEN = 8 * BAR; BEAT = 0.75
D = {"D2": 73.42, "A2": 110.0, "D3": 146.83, "F3": 174.61, "G3": 196.0, "A3": 220.0, "Bb3": 233.08, "C4": 261.63, "D4": 293.66, "E4": 329.63, "F4": 349.23, "G4": 392.0, "A4": 440.0, "Bb4": 466.16, "C5": 523.25, "D5": 587.33}
def pad(f, d, bright=0.3): return (sine(f, d) + bright * sine(2 * f, d) + 0.15 * sine(3 * f, d, 0.7) + 0.3 * sine(f * 1.005, d)) * np.minimum(1, T(d) / 0.6) * np.minimum(1, (d - T(d)) / 0.6)
def pluck(f, d): return (sine(f, d) + 0.5 * sine(2 * f, d) + 0.25 * sine(3 * f, d)) * env(d, 0.004, d * 0.4, 3)
def flute(f, d):
    t = T(d); vib = 1 + 0.006 * np.sin(2 * np.pi * 5 * t)
    return (np.sin(2 * np.pi * f * t * vib) + 0.25 * np.sin(4 * np.pi * f * t * vib) + 0.06 * hp(noise(d), 2000)) * np.minimum(1, t / 0.12) * np.minimum(1, (d - t) / 0.25)
def drum(f, d, n): return sweep(f * 1.6, f, d) * env(d, 0.002, d * 0.4) + n * bp(noise(d), 300, 2500) * env(d, 0.001, d * 0.2)
def seq(events, total=LEN): return mix(np.zeros(int(SR * total)), *[at(x, t, total) for t, x in events])

# ---------------- part 2: jeep, village, intro (own work, CC0) ----------------
d = 0.7; save("sfx_door_open", mix(0.6 * bp(noise(0.06), 1500, 6000) * env(0.06, 0.001, 0.02), at(0.25 * sweep(420, 300, 0.5) * env(0.5, 0.05, 0.4, 2) * (1 + 0.3 * np.sin(2 * np.pi * 23 * T(0.5))), 0.05, d)), 0.5)
save("sfx_door_close", mix(sweep(140, 60, 0.4) * env(0.4, 0.002, 0.12), 0.5 * lp(noise(0.4), 900) * env(0.4, 0.001, 0.05), at(0.4 * bp(noise(0.05), 2000, 7000) * env(0.05, 0.001, 0.015), 0.03, 0.4)), 0.6)
g = np.zeros(int(SR * 1.2))
for k in range(26):
    t0 = rng.uniform(0, 0.9) ** 2; f = rng.uniform(2500, 7000); dd = rng.uniform(0.05, 0.25)
    g += at((sine(f, dd) + 0.5 * sine(f * 1.53, dd)) * env(dd, 0.001, dd * 0.5) * rng.uniform(0.2, 0.6), t0, 1.2)
save("sfx_glass", g + 0.8 * bp(noise(1.2), 3000, 9000) * env(1.2, 0.001, 0.12), 0.55)
def metal(dd): return sum(a * sine(f, dd) * env(dd, 0.001, dd * r) for f, a, r in [(180, 1, 0.3), (437, 0.6, 0.25), (913, 0.4, 0.18), (1660, 0.25, 0.1)]) + 0.8 * lp(noise(dd), 1200) * env(dd, 0.001, 0.05)
save("sfx_metal_hit", metal(0.9), 0.7)
ring = np.concatenate([np.concatenate([(sine(440, 0.4) + sine(480, 0.4)) * 0.5 * (0.6 + 0.4 * np.sign(np.sin(2 * np.pi * 20 * T(0.4)))), np.zeros(int(SR * 0.2)), (sine(440, 0.4) + sine(480, 0.4)) * 0.5, np.zeros(int(SR * 1.6))]) for _ in range(2)])
save("sfx_phone_ring", ring, 0.45)
def chirp(f):
    dd = rng.uniform(0.06, 0.16); return sweep(f, f * rng.uniform(1.2, 1.7), dd) * env(dd, 0.004, dd * 0.5)
def birds(total, n):
    y = np.zeros(int(SR * total))
    for _ in range(n):
        t0 = rng.uniform(0, total - 1); f = rng.uniform(2200, 4200)
        for k in range(rng.integers(2, 6)): y += at(chirp(f) * rng.uniform(0.15, 0.4), t0 + k * rng.uniform(0.08, 0.16), total)
    return y
save("amb_birds", loopable(birds(14.0, 26) + 0.08 * lp(noise(14.0), 400)), 0.35)
L = 16.0
murmur = bp(noise(L), 250, 1400) * (0.35 + 0.65 * np.abs(lp(noise(L), 5)) * 9)   # far-off voices: band-limited noise at syllable rate
cow = np.zeros(int(SR * L))
for t0 in (3.1, 3.6, 10.4):
    cow += at(bell(820, 0.9, 0.6), t0, L) * 0.12
cart = at(0.15 * bp(noise(2.5), 300, 2000) * (0.5 + 0.5 * np.sign(np.sin(2 * np.pi * 3 * T(2.5)))) * np.minimum(1, T(2.5)) * np.minimum(1, 2.5 - T(2.5)), 7.0, L)
save("amb_village", loopable(0.35 * murmur / (np.max(np.abs(murmur)) or 1) + birds(L, 22) * 0.8 + cow + cart + 0.07 * lp(noise(L), 300)), 0.4)
# intro score, 64 s: dawn drone and flute (temple), low pulse (robbery), rise (arrival), calm (village)
I = 64.0
dr = 0.45 * pad(D["D2"], I, 0.4) + 0.3 * pad(D["A2"], I, 0.3)
mel1 = [("A4", 1, 2.5), ("D5", 3.5, 2.5), ("C5", 6, 1.5), ("A4", 7.5, 2.5)]
mel2 = [("F4", 40, 2), ("G4", 42, 2), ("A4", 44, 3), ("D5", 48, 3), ("C5", 51, 2), ("A4", 53, 4)]
puls = seq([(10 + i * 0.75, (0.5 if i % 4 == 0 else 0.25) * drum(60, 0.3, 0.15)) for i in range(26)], I)
rise = seq([(30 + i * 0.375, 0.2 * drum(120, 0.15, 0.4)) for i in range(26)], I)
score = dr + seq([(t, 0.45 * flute(D[n], dd)) for n, t, dd in mel1 + mel2], I) + puls + rise + seq([(t, 0.3 * bell(392, 4.0)) for t in (0.5, 58.0)], I)
save("mus_intro", score * np.minimum(1, T(I) / 2.0) * np.minimum(1, (I - T(I)) / 3.0), 0.55)
print(len(made), "extra files written")
