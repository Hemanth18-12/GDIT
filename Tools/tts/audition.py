import os, sys, numpy as np
sys.path.insert(0, "Tools/tts"); os.environ["HF_HOME"] = os.path.abspath("Tools/tts/.models/hf")
from voice_qa import wer, median_f0
from kokoro_onnx import Kokoro
from faster_whisper import WhisperModel
k = Kokoro("Tools/tts/.models/kokoro-v1.0.onnx", "Tools/tts/.models/voices-v1.0.bin"); m = WhisperModel("base.en", device="cpu", compute_type="int8")
lines = ["Five treasures. Gone before sunrise.", "The river road. Trees everywhere.", "Excuse me, could you tell me the way to Jagannath Temple?",
         "Bhaskar will not wait for me.", "We are praying for the festival in Suryapur."]
voices = ["hm_omega", "hm_psi", "hf_alpha", "hf_beta", "am_michael", "am_fenrir", "am_onyx", "am_puck", "bm_george", "bm_fable", "bm_lewis", "af_heart", "bf_emma"]
print(f"{'voice':12} {'WER':>5} {'F0':>6}  worst transcript")
for v in voices:
    ws, fs, worst = [], [], (0, "")
    for t in lines:
        s, sr = k.create(t, voice=v, speed=1.0, lang="en-us")
        segs, _ = m.transcribe(s.astype(np.float32) if sr == 16000 else np.interp(np.arange(0, len(s), sr / 16000), np.arange(len(s)), s).astype(np.float32), language="en", beam_size=5)
        hyp = " ".join(x.text for x in segs); e = wer(t, hyp); ws.append(e); fs.append(median_f0(s, sr))
        if e >= worst[0]: worst = (e, hyp.strip())
    print(f"{v:12} {np.mean(ws):5.2f} {np.median(fs):6.0f}  {worst[1][:70]}")
