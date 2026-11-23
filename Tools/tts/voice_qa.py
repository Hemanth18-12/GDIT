# Shared QA helpers for the voice pipeline: word error rate (via Whisper transcription) and median pitch.
import re, numpy as np
def norm(t): return re.sub(r"[^a-z0-9' ]+", " ", t.lower().replace("-", " ")).split()
def wer(ref, hyp):
    r, h = norm(ref), norm(hyp)
    d = np.zeros((len(r) + 1, len(h) + 1), dtype=int); d[:, 0] = range(len(r) + 1); d[0, :] = range(len(h) + 1)
    for i in range(1, len(r) + 1):
        for j in range(1, len(h) + 1): d[i, j] = min(d[i-1, j] + 1, d[i, j-1] + 1, d[i-1, j-1] + (r[i-1] != h[j-1]))
    return d[len(r), len(h)] / max(1, len(r))
def median_f0(x, sr):
    x = np.asarray(x, dtype=np.float64); n = int(0.04 * sr); hop = n // 2; f0s = []
    if len(x) < n: return 0.0
    thr = 0.1 * np.max(np.abs(x))
    for s in range(0, len(x) - n, hop):
        f = x[s:s + n] * np.hanning(n)
        if np.sqrt(np.mean(f ** 2)) < thr * 0.3: continue
        ac = np.correlate(f, f, "full")[n - 1:]; lo, hi = int(sr / 400), int(sr / 60)
        if hi >= len(ac): continue
        k = lo + int(np.argmax(ac[lo:hi]))
        if ac[k] > 0.35 * ac[0]: f0s.append(sr / k)
    return float(np.median(f0s)) if f0s else 0.0
