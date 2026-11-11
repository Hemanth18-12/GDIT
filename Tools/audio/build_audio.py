#!/usr/bin/env python3
"""Synthesises the game's core sound effects, ambience loops and five adaptive music stems (own work, CC0).
   Output: Assets/_Project/Audio/Resources/Audio/*.wav  (22.05 kHz mono 16-bit).  python3 build_audio.py"""
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

# ---------------- footsteps by surface ----------------
def step(kind, v):
    d = 0.3
    if kind == "stone": x = lp(noise(d), 900 + 200 * v) * env(d, 0.002, 0.07) + 0.5 * sine(95 + 8 * v, d) * env(d, 0.002, 0.05) + 0.25 * hp(noise(d), 3000) * env(d, 0.02, 0.1)
    elif kind == "wet": x = lp(noise(d), 1400) * env(d, 0.002, 0.06) + 0.7 * bp(noise(d), 1500, 5000) * env(d, 0.01, 0.16) + 0.3 * sine(110, d) * env(d, 0.002, 0.04)
    elif kind == "wood": x = sine(150 + 20 * v, d) * env(d, 0.002, 0.09) + 0.5 * sine(310 + 30 * v, d) * env(d, 0.002, 0.05) + 0.3 * lp(noise(d), 1200) * env(d, 0.002, 0.04)
    elif kind == "gravel": x = bp(noise(d), 1200, 6000) * env(d, 0.004, 0.18) * (0.6 + 0.4 * np.abs(lp(noise(d), 60)) * 6)
    elif kind == "grass": x = bp(noise(d), 2500, 8000) * env(d, 0.03, 0.22) * 0.8 + 0.2 * lp(noise(d), 400) * env(d, 0.003, 0.06)
    else: x = lp(noise(d), 500) * env(d, 0.004, 0.12) + 0.6 * sweep(260, 90, d) * env(d, 0.02, 0.14)   # mud squelch
    return x
for kind, n in [("stone", 3), ("wet", 3), ("wood", 2), ("gravel", 2), ("grass", 2), ("mud", 2)]:
    for v in range(n): save(f"fs_{kind}_{v + 1}", step(kind, v), 0.6)

# ---------------- cloth, brass, bells ----------------
save("sfx_cloth", bp(noise(0.35), 800, 5000) * env(0.35, 0.05, 0.25, 2.0), 0.5)
def bell(f, d, bright=1.0):
    parts = [(1.0, 1.0), (2.76, 0.5 * bright), (5.4, 0.3 * bright), (8.93, 0.15 * bright), (0.5, 0.3)]
    return sum(a * sine(f * k, d) * env(d, 0.002, d / (1 + 0.4 * i)) for i, (k, a) in enumerate(parts))
save("sfx_bell_temple", bell(392, 4.0), 0.7)
save("sfx_bell_trip", mix(bell(1568, 0.9), at(bell(1760, 0.8), 0.09, 1.2), at(bell(1480, 0.7), 0.2, 1.2)), 0.7)
save("sfx_bell_muffled", lp(bell(1568, 0.25, 0.3), 1500) * env(0.25, 0.002, 0.06), 0.4)
save("sfx_brass_set", mix(bell(620, 0.7, 0.6), 0.5 * lp(noise(0.1), 800) * env(0.1, 0.002, 0.03)), 0.6)
save("sfx_token", mix(bell(2400, 0.35), at(bell(2700, 0.3), 0.12, 0.6), at(bell(2550, 0.25), 0.2, 0.6)), 0.5)

# ---------------- flames ----------------
def crackle(d, rate):
    x = np.zeros(int(SR * d))
    for _ in range(int(d * rate)):
        i = rng.integers(0, len(x) - 400); x[i:i + 300] += hp(noise(300 / SR), 1500)[:300] * np.exp(-np.arange(300) / 40) * rng.uniform(0.3, 1)
    return x
flame = lp(noise(6.0), 500) * (0.6 + 0.4 * lp(noise(6.0), 4) * 8) * 0.6 + 0.5 * crackle(6.0, 14)
save("amb_flame_loop", loopable(flame), 0.5)
save("sfx_lamp_ignite", mix(bp(noise(0.5), 300, 3000) * env(0.5, 0.06, 0.3, 2.5), 0.4 * sweep(200, 600, 0.3) * env(0.3, 0.02, 0.2)), 0.55)
save("sfx_lamp_blown", bp(noise(0.45), 400, 2500) * env(0.45, 0.01, 0.2), 0.5)
save("sfx_brazier_crash", mix(bell(180, 1.2, 1.4) * 0.9, lp(noise(1.2), 2500) * env(1.2, 0.002, 0.3), at(crackle(1.0, 60) * env(1.0, 0.01, 0.6, 2), 0.1, 1.4)), 0.85)

# ---------------- rain, drips, thunder, wind, jungle ----------------
def rain(d, density, bright):
    base = bp(noise(d), 1000, bright) * (0.8 + 0.2 * lp(noise(d), 3) * 6)
    return base * density + (1 - density) * 0.4 * crackle(d, 90) + 0.25 * lp(noise(d), 300)
save("amb_rain_light", loopable(rain(8.0, 0.45, 7000)), 0.4)
save("amb_rain_heavy", loopable(rain(8.0, 0.95, 9000) + 0.4 * lp(noise(8.0), 180)), 0.6)
save("amb_rain_interior", loopable(lp(rain(8.0, 0.8, 6000), 700) + 0.15 * crackle(8.0, 6)), 0.45)
save("sfx_drip", mix(sweep(1500, 700, 0.12) * env(0.12, 0.002, 0.05), at(0.4 * sweep(2200, 900, 0.1) * env(0.1, 0.002, 0.04), 0.07, 0.3)), 0.4)
def thunder(d, crack):
    rumble = lp(noise(d), 110) * env(d, 0.3, d * 0.6, 2.2) * (0.6 + 0.4 * np.abs(lp(noise(d), 2)) * 10)
    return mix(rumble * 1.4, crack * lp(noise(0.5), 3500) * env(0.5, 0.002, 0.12), at(0.5 * lp(noise(1.5), 400) * env(1.5, 0.05, 0.8), 0.25, d))
save("sfx_thunder_near", thunder(5.5, 1.0), 0.95)
save("sfx_thunder_far", thunder(6.0, 0.0), 0.6)
wind = bp(noise(10.0), 200, 1200) * (0.35 + 0.65 * (0.5 + 0.5 * np.sin(2 * np.pi * 0.13 * T(10.0) + lp(noise(10.0), 1) * 20)))
save("amb_wind", loopable(wind), 0.4)
jung = 0.25 * bp(noise(12.0), 200, 900)
for k in range(260):  # crickets
    t0 = rng.uniform(0, 11.7); f = rng.choice([4300, 4700, 5100]); c = sine(f, 0.045) * env(0.045, 0.004, 0.03, 1.5) * rng.uniform(0.05, 0.16)
    jung += at(np.tile(c, 3), t0, 12.0)
for k in range(7):    # frogs / night birds
    t0 = rng.uniform(0, 10.5); f = rng.uniform(380, 900)
    jung += at(sweep(f, f * rng.uniform(0.7, 1.3), 0.3) * env(0.3, 0.03, 0.2, 2) * 0.2, t0, 12.0)
save("amb_jungle_night", loopable(jung), 0.45)

# ---------------- stone and mechanism ----------------
save("sfx_stone_grind", lp(noise(1.6), 420) * (0.5 + 0.5 * np.abs(lp(noise(1.6), 14)) * 12) * env(1.6, 0.1, 1.6, 1.2), 0.75)
save("sfx_gear_click", mix(sine(880, 0.06) * env(0.06, 0.001, 0.015), 0.6 * lp(noise(0.06), 3000) * env(0.06, 0.001, 0.01), at(0.5 * sine(660, 0.05) * env(0.05, 0.001, 0.012), 0.05, 0.14)), 0.55)
save("sfx_dust_fall", bp(noise(1.8), 600, 4500) * env(1.8, 0.2, 1.2, 2.0) * (0.6 + 0.4 * lp(noise(1.8), 8) * 8), 0.45)
save("sfx_wall_open", mix(lp(noise(4.0), 300) * env(4.0, 0.4, 4.0, 1.0) * (0.5 + 0.5 * np.abs(lp(noise(4.0), 9)) * 12), at(bell(98, 2.0, 0.4) * 0.6, 3.2, 5.2)), 0.85)

# ---------------- hits, blade, bodies ----------------
def thud(f, d, n=0.6): return sweep(f, f * 0.45, d) * env(d, 0.002, d * 0.35) + n * lp(noise(d), 1400) * env(d, 0.001, d * 0.15)
save("sfx_hit_light", thud(180, 0.18), 0.7)
save("sfx_hit_heavy", mix(thud(120, 0.35, 0.9), 0.4 * hp(noise(0.08), 2500) * env(0.08, 0.001, 0.02)), 0.9)
save("sfx_hit_taken", mix(thud(140, 0.3, 1.0), 0.3 * sweep(900, 300, 0.2) * env(0.2, 0.002, 0.08)), 0.85)
save("sfx_block", thud(220, 0.16, 1.1), 0.7)
save("sfx_blade_block", mix(bell(2100, 0.5, 1.6), bell(3150, 0.35, 1.2) * 0.6, 0.6 * hp(noise(0.05), 4000) * env(0.05, 0.001, 0.012)), 0.8)
save("sfx_blade_drop", mix(bell(1700, 0.6, 1.3), at(bell(1900, 0.4, 1.2) * 0.7, 0.16, 0.9), at(bell(1800, 0.3) * 0.5, 0.3, 0.9)), 0.7)
save("sfx_body_fall", mix(thud(85, 0.5, 1.0), at(thud(110, 0.3, 0.8) * 0.6, 0.14, 0.7), 0.3 * bp(noise(0.5), 800, 4000) * env(0.5, 0.05, 0.3, 2)), 0.85)
save("sfx_pole_crash", mix(*[at(thud(rng.uniform(140, 260), 0.3, 0.7) * rng.uniform(0.5, 1), rng.uniform(0, 0.5), 1.3) for _ in range(8)]), 0.9)
save("sfx_stagger", mix(sweep(500, 140, 0.4) * env(0.4, 0.01, 0.3, 2), 0.5 * lp(noise(0.4), 900) * env(0.4, 0.01, 0.2)), 0.6)
def swish(d, f0, f1):
    t = T(d); fc = f0 + (f1 - f0) * t / d; x = noise(d); y = np.empty_like(x); s = 0.0
    for i in range(len(x)): a = np.exp(-2 * np.pi * fc[i] / SR); s = (1 - a) * x[i] + a * s; y[i] = s
    return hp(y, 500) * np.sin(np.pi * t / d) ** 2
save("sfx_swish_quick", swish(0.22, 1500, 6000), 0.6)
save("sfx_swish_wide", swish(0.5, 700, 4500), 0.7)
save("sfx_dodge", mix(swish(0.3, 500, 2500), 0.5 * bp(noise(0.3), 800, 4000) * env(0.3, 0.05, 0.2, 2)), 0.55)

# ---------------- grunts (formant-shaped pulses; placeholders for recorded efforts) ----------------
def grunt(f0, d, drop, formants):
    t = T(d); f = f0 * (1 - drop * t / d); ph = 2 * np.pi * np.cumsum(f) / SR
    src = np.sign(np.sin(ph)) * 0.5 + 0.5 * np.sin(ph) + 0.25 * noise(d)
    y = sum(bp(src, fm * 0.8, fm * 1.25) * a for fm, a in formants)
    return y * env(d, 0.015, d * 0.55, 2.2)
save("sfx_grunt_aryan", grunt(125, 0.32, 0.25, [(650, 1), (1100, 0.6), (2500, 0.2)]), 0.6)
save("sfx_grunt_pain", grunt(150, 0.38, 0.35, [(750, 1), (1250, 0.5), (2600, 0.2)]), 0.6)
save("sfx_grunt_bhaskar", grunt(100, 0.4, 0.3, [(550, 1), (950, 0.6), (2300, 0.2)]), 0.6)

# ---------------- tools, stealth, tension ----------------
save("sfx_flashlight_click", mix(sine(2400, 0.03) * env(0.03, 0.001, 0.008), at(sine(1800, 0.03) * env(0.03, 0.001, 0.008) * 0.6, 0.04, 0.09)), 0.4)
save("sfx_flash_burst", mix(hp(noise(0.35), 2500) * env(0.35, 0.002, 0.1), sweep(3000, 900, 0.3) * env(0.3, 0.002, 0.1) * 0.6), 0.7)
save("sfx_battery", mix(sine(880, 0.08) * env(0.08, 0.002, 0.04), at(sine(1320, 0.14) * env(0.14, 0.002, 0.08), 0.09, 0.28)), 0.45)
save("sfx_pebble", mix(sine(1900, 0.05) * env(0.05, 0.001, 0.012), at(sine(1500, 0.05) * env(0.05, 0.001, 0.012) * 0.6, 0.11, 0.4), at(sine(1700, 0.04) * env(0.04, 0.001, 0.01) * 0.35, 0.2, 0.4), 0.3 * lp(noise(0.05), 2500) * env(0.05, 0.001, 0.01)), 0.5)
save("sfx_rope", mix(sweep(140, 320, 0.5) * env(0.5, 0.05, 0.4, 2) * (0.5 + 0.5 * np.sign(sine(38, 0.5))), 0.5 * bp(noise(0.5), 500, 2500) * env(0.5, 0.05, 0.3, 2)), 0.6)
beat = mix(thud(60, 0.2, 0.2), at(thud(52, 0.22, 0.2) * 0.75, 0.3, 1.0))
save("amb_heartbeat", np.tile(np.pad(beat, (0, int(SR * 1.0) - len(beat))), 4), 0.7)
save("sfx_spotted", mix(sweep(300, 1200, 0.35) * env(0.35, 0.01, 0.35, 1.0), at(bell(1175, 0.8, 1.5), 0.3, 1.2)), 0.7)
save("sfx_penalty", mix(sine(220, 0.35) * env(0.35, 0.005, 0.2), sine(207, 0.35) * env(0.35, 0.005, 0.2)), 0.5)
save("sfx_checkpoint", mix(sine(587, 0.2) * env(0.2, 0.005, 0.1), at(sine(880, 0.4) * env(0.4, 0.005, 0.25), 0.15, 0.6)), 0.4)

# ---------------- UI ----------------
save("sfx_ui_back", sweep(700, 420, 0.1) * env(0.1, 0.003, 0.05), 0.35)
save("sfx_codex", mix(bell(1318, 0.7), at(bell(1976, 0.7), 0.12, 0.9)), 0.4)
save("sfx_objective", mix(bell(784, 0.6), at(bell(1175, 0.8), 0.14, 1.0)), 0.45)
save("sfx_choice", mix(sine(660, 0.09) * env(0.09, 0.003, 0.04), at(sine(990, 0.12) * env(0.12, 0.003, 0.06), 0.07, 0.2)), 0.35)

# ---------------- adaptive music: five loop-synchronised stems, D minor, 80 BPM, 8 bars (24 s) ----------------
BAR = 3.0; LEN = 8 * BAR; BEAT = 0.75
D = {"D2": 73.42, "A2": 110.0, "D3": 146.83, "F3": 174.61, "G3": 196.0, "A3": 220.0, "Bb3": 233.08, "C4": 261.63, "D4": 293.66, "E4": 329.63, "F4": 349.23, "G4": 392.0, "A4": 440.0, "Bb4": 466.16, "C5": 523.25, "D5": 587.33}
def pad(f, d, bright=0.3): return (sine(f, d) + bright * sine(2 * f, d) + 0.15 * sine(3 * f, d, 0.7) + 0.3 * sine(f * 1.005, d)) * np.minimum(1, T(d) / 0.6) * np.minimum(1, (d - T(d)) / 0.6)
def pluck(f, d): return (sine(f, d) + 0.5 * sine(2 * f, d) + 0.25 * sine(3 * f, d)) * env(d, 0.004, d * 0.4, 3)
def flute(f, d):
    t = T(d); vib = 1 + 0.006 * np.sin(2 * np.pi * 5 * t)
    return (np.sin(2 * np.pi * f * t * vib) + 0.25 * np.sin(4 * np.pi * f * t * vib) + 0.06 * hp(noise(d), 2000)) * np.minimum(1, t / 0.12) * np.minimum(1, (d - t) / 0.25)
def drum(f, d, n): return sweep(f * 1.6, f, d) * env(d, 0.002, d * 0.4) + n * bp(noise(d), 300, 2500) * env(d, 0.001, d * 0.2)
def seq(events, total=LEN): return mix(np.zeros(int(SR * total)), *[at(x, t, total) for t, x in events])
drone = 0.5 * pad(D["D2"], LEN, 0.5) + 0.35 * pad(D["A2"], LEN, 0.4)
mel = [("A4", 0, 3), ("F4", 3, 1.5), ("G4", 4.5, 1.5), ("A4", 6, 2), ("D5", 8.25, 2.5), ("C5", 12, 1.5), ("A4", 13.5, 1.5), ("G4", 15, 1.5), ("F4", 16.5, 1.5), ("E4", 18, 1.5), ("D4", 19.5, 3.5)]
calm = drone * 0.7 + seq([(t, 0.5 * flute(D[n], d)) for n, t, d in mel]) + seq([(b * BAR + 1.5, 0.25 * pluck(D[n], 1.4)) for b, n in enumerate(["D4", "F4", "A4", "F4", "D4", "G4", "A4", "D4"])])
tension = 0.6 * pad(D["D2"], LEN, 0.8) + 0.4 * pad(D["Bb3"] / 2, LEN, 0.6) * (0.5 + 0.5 * np.sin(2 * np.pi * T(LEN) / 6)) + seq([(i * BEAT * 2, 0.5 * drum(55, 0.35, 0.1)) for i in range(16)]) + seq([(b * BAR + 2.25, 0.2 * pluck(D[n] , 0.7)) for b, n in enumerate(["D3", "F3", "D3", "Bb3", "D3", "F3", "A3", "G3"])])
stealth = seq([(i * BEAT / 2, (0.4 if i % 4 == 0 else 0.18) * drum(180 if i % 2 else 120, 0.12, 0.5)) for i in range(64) if i % 8 not in (3, 6)]) + seq([(b * BAR + o, 0.3 * pluck(D[n], 0.5)) for b in range(8) for o, n in [(0, "D3"), (1.125, "F3"), (1.875, "D3")]]) + 0.2 * pad(D["D2"], LEN, 0.3)
combat = seq([(i * BEAT / 2, (0.7 if i % 4 == 0 else 0.35 if i % 2 == 0 else 0.22) * drum(70 if i % 4 == 0 else 150, 0.2, 0.7)) for i in range(64)]) + seq([(i * BEAT / 2, 0.3 * pluck(D["D3"] if (i // 8) % 4 < 2 else D["Bb3"] / 2 * 1.0 if (i // 8) % 4 == 2 else D["A2"] * 1.0, 0.3)) for i in range(64)]) + seq([(b * BAR, 0.35 * pad(D[n], 1.2, 1.2)) for b, n in enumerate(["D4", "D4", "F4", "E4", "D4", "D4", "G4", "A4"])])
resolve = 0.5 * pad(D["D3"], LEN, 0.3) + 0.4 * pad(D["F3"] * 1.0595, LEN, 0.3) + 0.35 * pad(D["A3"], LEN, 0.3) + seq([(t, 0.45 * flute(D[n] * (1.0595 if n.startswith("F") else 1), d)) for n, t, d in [("D4", 0, 3), ("F4", 3, 3), ("A4", 6, 4.5), ("G4", 10.5, 1.5), ("F4", 12, 3), ("A4", 15, 3), ("D5", 18, 5.5)]]) + seq([(b * BAR * 2, 0.25 * bell(587, 2.5, 0.5)) for b in range(4)])
for name, x, pk in [("mus_calm", calm, 0.5), ("mus_tension", tension, 0.55), ("mus_stealth", stealth, 0.5), ("mus_combat", combat, 0.7), ("mus_resolve", resolve, 0.5)]:
    save(name, x, pk)
print(len(made), "files written to", OUT)
