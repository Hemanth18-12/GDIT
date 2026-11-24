#!/usr/bin/env python3
"""
Procedural Devotional Temple Audio Generator (100% pure Python stdlib).
Generates calm, culturally respectful Jagannath-inspired acoustic temple soundscapes:
- Tanpura drone (D3 / A3 / D4 warm jawari harmonics)
- Bansuri bamboo flute (Raga Bhupali: D4, E4, F#4, A4, B4, D5)
- Soft distant temple brass ghanta bell chimes
- Gentle ocean coastal breeze ambiance (zero harsh buzz or square/sawtooth waves)
- Clean silent murmur replacements for background NPC chatter
"""
import array
import math
import os
import random
import wave

SR = 22050
SR_HQ = 44100

ROOT_AUDIO = os.path.join(os.path.dirname(__file__), "..", "..", "Assets", "_Project", "Audio")
RES_AUDIO = os.path.join(ROOT_AUDIO, "Resources", "Audio")

NOTES = {
    "D3": 146.83, "E3": 164.81, "F#3": 185.00, "A3": 220.00, "B3": 246.94,
    "D4": 293.66, "E4": 329.63, "F#4": 369.99, "A4": 440.00, "B4": 493.88,
    "D5": 587.33, "E5": 659.25, "F#5": 739.99, "A5": 880.00,
}


def write_wav(path: str, samples, sr: int = SR, peak_norm: float = 0.72):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    peak = max((abs(x) for x in samples), default=1.0)
    scale = (peak_norm / peak) if peak > 1e-6 else 1.0
    pcm = array.array("h", (max(-32767, min(32767, int(x * scale * 32767.0))) for x in samples))
    with wave.open(path, "wb") as wf:
        wf.setnchannels(1)
        wf.setsampwidth(2)
        wf.setframerate(sr)
        wf.writeframes(pcm.tobytes())


def make_loop(buf, sr: int, fade_sec: float = 0.8):
    """Crossfades the end into the start seamlessly for zero-click looping."""
    n = len(buf)
    f = min(int(fade_sec * sr), n // 6)
    if f <= 0:
        return buf
    for i in range(f):
        t = i / float(f)
        # Equal-power crossfade between start and tail wrap
        buf[i] = buf[i] * math.sin(t * math.pi * 0.5) + buf[n - f + i] * math.cos(t * math.pi * 0.5)
    return buf[: n - f]


def add_tanpura(buf, sr: int, dur: float, gain: float = 0.28):
    """Warm plucked tanpura cycle: Pa(A3) - Sa'(D4) - Sa'(D4) - Sa(D3) every 3.0s."""
    cycle = 3.0
    plucks = [(0.0, NOTES["A3"], 0.75), (0.75, NOTES["D4"], 0.85), (1.5, NOTES["D4"], 0.80), (2.25, NOTES["D3"], 1.0)]
    two_pi = 2.0 * math.pi
    t = 0.0
    while t < dur:
        for offset, freq, w in plucks:
            start = int((t + offset) * sr)
            plen = int(min(3.2 * sr, len(buf) - start))
            if plen <= 0:
                continue
            for i in range(plen):
                dt = i / float(sr)
                attack = 1.0 - math.exp(-dt * 45.0)
                decay = math.exp(-dt * 0.85)
                jawari = 0.14 * math.sin(two_pi * 2.2 * dt)
                s = (
                    1.00 * math.sin(two_pi * freq * dt)
                    + (0.42 + jawari) * math.sin(two_pi * freq * 2.0 * dt)
                    + 0.18 * math.sin(two_pi * freq * 3.0 * dt)
                    + 0.07 * math.sin(two_pi * freq * 4.0 * dt)
                )
                buf[start + i] += s * attack * decay * w * gain * 0.45
        t += cycle


def add_bansuri_phrase(buf, sr: int, phrase, gain: float = 0.42):
    """Adds expressive Indian bamboo flute melody [(start_s, dur_s, freq, vibrato_hz)]."""
    two_pi = 2.0 * math.pi
    rng = random.Random(108)
    for start_s, dur_s, freq, vib in phrase:
        start = int(start_s * sr)
        n = int(dur_s * sr)
        if start >= len(buf) or n <= 0:
            continue
        n = min(n, len(buf) - start)
        phase = 0.0
        breath_lp = 0.0
        for i in range(n):
            dt = i / float(sr)
            u = i / float(n)
            env = math.sin(min(1.0, u / 0.18) * math.pi * 0.5) * math.sin(min(1.0, (1.0 - u) / 0.22) * math.pi * 0.5)
            f_inst = freq * (1.0 + 0.0045 * math.sin(two_pi * vib * dt) * min(1.0, dt * 2.5))
            phase += two_pi * f_inst / sr
            tone = math.sin(phase) + 0.16 * math.sin(2.0 * phase) + 0.04 * math.sin(3.0 * phase)
            noise = rng.uniform(-1.0, 1.0)
            breath_lp = 0.92 * breath_lp + 0.08 * noise
            buf[start + i] += (tone + breath_lp * 0.05) * env * gain


def add_temple_bell(buf, sr: int, at_sec: float, freq: float = 587.33, gain: float = 0.22):
    """Gentle distant Puri brass ghanta bell shimmer."""
    start = int(at_sec * sr)
    n = min(int(3.6 * sr), len(buf) - start)
    if n <= 0:
        return
    two_pi = 2.0 * math.pi
    partials = [(1.0, 1.0, 0.9), (2.0, 0.45, 1.6), (2.76, 0.22, 2.5), (4.07, 0.08, 3.8)]
    for i in range(n):
        dt = i / float(sr)
        atk = 1.0 - math.exp(-dt * 90.0)
        s = 0.0
        for mul, amp, dcy in partials:
            s += amp * math.exp(-dt * dcy) * math.sin(two_pi * freq * mul * dt)
        buf[start + i] += s * atk * gain


def add_soft_drum_pulse(buf, sr: int, dur: float, bpm: float = 84.0, gain: float = 0.25):
    """Warm acoustic Pakhawaj/Tabla low beat (pure sine pitch envelope, zero buzz)."""
    step = 60.0 / bpm
    two_pi = 2.0 * math.pi
    beat = 0
    t = 0.0
    while t < dur - 0.5:
        start = int(t * sr)
        n = min(int(0.28 * sr), len(buf) - start)
        base = 73.42 if (beat % 4 == 0) else 110.0
        w = 1.0 if (beat % 4 == 0) else 0.55 if (beat % 2 == 0) else 0.35
        phase = 0.0
        for i in range(n):
            dt = i / float(sr)
            f = base * (1.0 + 0.45 * math.exp(-dt * 28.0))
            phase += two_pi * f / sr
            buf[start + i] += math.sin(phase) * math.exp(-dt * 12.0) * w * gain
        beat += 1
        t += step


def build_bhupali_calm(sr: int, total_dur: float, gentle_pulse: bool = False):
    n = int((total_dur + 0.8) * sr)
    buf = [0.0] * n
    add_tanpura(buf, sr, total_dur + 0.7, gain=0.26)
    melody = [
        (0.8, 2.6, NOTES["D4"], 4.8), (3.6, 1.9, NOTES["E4"], 5.0), (5.7, 2.8, NOTES["F#4"], 5.2),
        (8.8, 2.2, NOTES["A4"], 5.0), (11.2, 2.9, NOTES["B4"], 4.6), (14.4, 3.2, NOTES["D5"], 5.1),
        (17.9, 2.1, NOTES["B4"], 4.8), (20.2, 2.4, NOTES["A4"], 4.6), (22.8, 2.2, NOTES["F#4"], 4.8),
    ]
    scaled = [(a * (total_dur / 24.0), b * (total_dur / 24.0), f, v) for (a, b, f, v) in melody]
    add_bansuri_phrase(buf, sr, scaled, gain=0.40)
    add_temple_bell(buf, sr, 0.5, NOTES["D5"], gain=0.18)
    add_temple_bell(buf, sr, total_dur * 0.52, NOTES["A5"], gain=0.14)
    if gentle_pulse:
        add_soft_drum_pulse(buf, sr, total_dur + 0.6, bpm=78.0, gain=0.22)
    return make_loop(buf, sr, 0.8)


def build_tension_soft(sr: int, total_dur: float):
    """Mysterious yet serene low tanpura + slow deep D-A bansuri (zero harsh drone)."""
    n = int((total_dur + 0.8) * sr)
    buf = [0.0] * n
    add_tanpura(buf, sr, total_dur + 0.7, gain=0.28)
    melody = [
        (1.0, 3.2, NOTES["D4"], 4.0), (5.0, 3.4, NOTES["A3"], 3.8),
        (9.5, 3.0, NOTES["E4"], 4.2), (13.4, 3.8, NOTES["D4"], 4.0),
    ]
    scaled = [(a * (total_dur / 16.0), b * (total_dur / 16.0), f, v) for (a, b, f, v) in melody]
    add_bansuri_phrase(buf, sr, scaled, gain=0.30)
    return make_loop(buf, sr, 0.8)


def build_breeze(sr: int, total_dur: float, with_chimes: bool):
    n = int((total_dur + 0.6) * sr)
    buf = [0.0] * n
    rng = random.Random(42)
    lp1 = 0.0
    lp2 = 0.0
    for i in range(n):
        dt = i / float(sr)
        swell = 0.65 + 0.35 * math.sin(2.0 * math.pi * (dt / max(4.0, total_dur * 0.5)))
        w = rng.uniform(-1.0, 1.0)
        lp1 = 0.96 * lp1 + 0.04 * w
        lp2 = 0.96 * lp2 + 0.04 * lp1
        buf[i] = lp2 * swell * 0.14
    if with_chimes:
        add_temple_bell(buf, sr, 2.0, NOTES["D5"], gain=0.14)
        add_temple_bell(buf, sr, total_dur * 0.6, NOTES["A5"], gain=0.10)
    return make_loop(buf, sr, 0.6)


def main():
    print("[DevotionalAudio] Generating serene Puri temple BGM and clean ambience...")
    # High-quality 44.1 kHz menu & exploration themes (both Audio/ and Audio/Resources/Audio/)
    bgm_menu = build_bhupali_calm(SR_HQ, 20.0)
    bgm_exp = build_bhupali_calm(SR_HQ, 24.0)
    bgm_susp = build_tension_soft(SR_HQ, 16.0)
    amb_temple = build_breeze(SR_HQ, 16.0, with_chimes=True)

    for folder in (ROOT_AUDIO, RES_AUDIO):
        write_wav(os.path.join(folder, "bgm_menu.wav"), bgm_menu, SR_HQ, 0.52)
        write_wav(os.path.join(folder, "bgm_exploration.wav"), bgm_exp, SR_HQ, 0.48)
        write_wav(os.path.join(folder, "bgm_suspense.wav"), bgm_susp, SR_HQ, 0.40)
        write_wav(os.path.join(folder, "ambience_temple.wav"), amb_temple, SR_HQ, 0.25)

    # Gameplay stems in Resources/Audio (22.05 kHz matching Unity import metadata)
    write_wav(os.path.join(RES_AUDIO, "mus_calm.wav"), build_bhupali_calm(SR, 24.0), SR, 0.48)
    write_wav(os.path.join(RES_AUDIO, "mus_resolve.wav"), build_bhupali_calm(SR, 24.0), SR, 0.52)
    write_wav(os.path.join(RES_AUDIO, "mus_intro.wav"), build_bhupali_calm(SR, 24.0), SR, 0.46)
    write_wav(os.path.join(RES_AUDIO, "mus_tension.wav"), build_tension_soft(SR, 24.0), SR, 0.36)
    write_wav(os.path.join(RES_AUDIO, "mus_stealth.wav"), build_tension_soft(SR, 24.0), SR, 0.32)
    write_wav(os.path.join(RES_AUDIO, "mus_combat.wav"), build_bhupali_calm(SR, 24.0, gentle_pulse=True), SR, 0.54)

    # Calm village breeze without harsh synth chatter
    amb_vill = build_breeze(SR, 15.6, with_chimes=True)
    write_wav(os.path.join(RES_AUDIO, "amb_village.wav"), amb_vill, SR, 0.22)

    # Replace distracting random gibberish murmur clips with whisper-quiet room air
    silent = [0.0] * int(2.0 * SR)
    for k in (1, 2, 3):
        write_wav(os.path.join(RES_AUDIO, f"talk_murmur_{k}.wav"), silent, SR, 0.0)

    print("[DevotionalAudio] Done.")


if __name__ == "__main__":
    main()
