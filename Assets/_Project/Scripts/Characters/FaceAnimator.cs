using System.Collections.Generic;
using UnityEngine;

namespace LegacyOfTemples.Characters
{
    /// <summary>
    /// Makes a face look alive from what the model really has (nothing is faked with generated clips):
    /// - lip sync from the playing voice clip: loudness opens the jaw, and the rough vowel picks a mouth shape.
    ///   Uses viseme / jawOpen blendshapes when the model has them (Meera), else a jaw bone (the Investigator).
    /// - blinking with eyelid blendshapes or eyelid bones.
    /// - a small head nod with the voice for models with no face rig at all (Pujari, villagers, robbers).
    /// </summary>
    public class FaceAnimator : MonoBehaviour
    {
        public AudioSource voice;
        [Range(0f, 40f)] public float jawOpenDegrees = 14f;
        public float gain = 9f;
        public bool blink = true;

        public enum Rig { None, Blendshapes, JawBone }
        public Rig LipRig { get; private set; }
        public bool CanBlink { get; private set; }
        public float Openness { get; private set; }
        public int Blinks { get; private set; }
        public bool Speaking => voice != null && voice.isPlaying && speakUntil > Time.time;

        SkinnedMeshRenderer face; int jawOpen = -1, blinkL = -1, blinkR = -1; readonly Dictionary<string, int> visemes = new Dictionary<string, int>();
        Transform jaw, head; Quaternion jawRest, headRest; readonly List<Transform> lids = new List<Transform>(); readonly List<Quaternion> lidRest = new List<Quaternion>(); readonly List<float> lidSign = new List<float>();
        readonly float[] samples = new float[256], spectrum = new float[256];
        float speakUntil, nextBlink, blinkT = -1f, smooth; string lastViseme = "";
        static readonly Dictionary<string, FaceAnimator> bySpeaker = new Dictionary<string, FaceAnimator>();

        void Awake() { Discover(); nextBlink = Time.time + Random.Range(1.5f, 4f); }

        public void Discover()
        {
            foreach (var smr in GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                var m = smr.sharedMesh; if (m == null || m.blendShapeCount == 0) continue;
                int jo = Find(m, "jawOpen"); if (jo < 0 && Find(m, "aa") < 0) continue;
                if (face == null || m.blendShapeCount > face.sharedMesh.blendShapeCount) face = smr;
            }
            if (face != null)
            {
                var m = face.sharedMesh; jawOpen = Find(m, "jawOpen"); blinkL = Find(m, "eyeBlinkLeft"); blinkR = Find(m, "eyeBlinkRight");
                foreach (var v in new[] { "aa", "E", "ih", "oh", "ou", "PP", "FF", "SS", "sil" }) { int i = Find(m, v); if (i >= 0) visemes[v] = i; }
                LipRig = Rig.Blendshapes; CanBlink = blinkL >= 0 && blinkR >= 0;
            }
            foreach (var t in GetComponentsInChildren<Transform>(true))
            {
                string n = t.name.ToLowerInvariant();
                if (jaw == null && (n == "c_jaw" || n == "jaw" || n.EndsWith(":jaw") || n.EndsWith("_jaw"))) jaw = t;
                if (head == null && (n == "head" || n == "bip_head" || n.EndsWith(":head"))) head = t;
                if (n.Contains("eye_lid") && !n.EndsWith("_end")) { lids.Add(t); lidRest.Add(t.localRotation); lidSign.Add(n.Contains("upper") ? 1f : -0.35f); }
            }
            if (jaw != null) { jawRest = jaw.localRotation; if (LipRig == Rig.None) LipRig = Rig.JawBone; }
            if (head != null) headRest = head.localRotation;
            if (!CanBlink) CanBlink = lids.Count > 0;
        }
        static int Find(Mesh m, string shape) { for (int i = 0; i < m.blendShapeCount; i++) { string n = m.GetBlendShapeName(i); if (n == shape || n.EndsWith("." + shape)) return i; } return -1; }

        /// <summary>Start following a voice clip that is about to play on this source.</summary>
        public void Speak(AudioSource source, float seconds = 30f) { voice = source; speakUntil = Time.time + seconds; }
        public void StopSpeaking() { speakUntil = 0f; }

        /// <summary>Register which character a speaker id belongs to, so the voice system can find the face.</summary>
        public static FaceAnimator For(string speakerId, GameObject character)
        {
            if (character == null) return null;
            if (bySpeaker.TryGetValue(speakerId, out var f) && f != null && f.gameObject == character) return f;
            f = character.GetComponent<FaceAnimator>(); if (f == null) f = character.AddComponent<FaceAnimator>();
            bySpeaker[speakerId] = f; return f;
        }

        /// <summary>Loudness (0..1) and a rough vowel from the playing clip. Public so tests can feed a level directly.</summary>
        public void Feed(float level, string viseme) { smooth = Mathf.Lerp(smooth, Mathf.Clamp01(level), 0.55f); Openness = smooth; lastViseme = viseme; }

        void LateUpdate()
        {
            if (Speaking)
            {
                voice.GetOutputData(samples, 0); float sum = 0f; for (int i = 0; i < samples.Length; i++) sum += samples[i] * samples[i];
                float rms = Mathf.Sqrt(sum / samples.Length);
                voice.GetSpectrumData(spectrum, 0, FFTWindow.Hanning);
                float lo = 0f, mid = 0f, hi = 0f; for (int i = 1; i < 12; i++) lo += spectrum[i]; for (int i = 12; i < 40; i++) mid += spectrum[i]; for (int i = 40; i < 120; i++) hi += spectrum[i];
                string v = rms < 0.01f ? "sil" : hi > mid && hi > lo ? "SS" : mid > lo * 1.2f ? "E" : lo > mid * 2.5f ? "oh" : "aa";
                Feed(rms * gain, v);
            }
            else Feed(0f, "sil");
            ApplyMouth(); ApplyBlink();
            if (LipRig == Rig.None && head != null) head.localRotation = headRest * Quaternion.Euler(Openness * 4f, 0f, 0f);   // no face rig: a small nod with the voice
        }

        void ApplyMouth()
        {
            if (LipRig == Rig.Blendshapes)
            {
                if (jawOpen >= 0) face.SetBlendShapeWeight(jawOpen, Openness * 70f);
                foreach (var kv in visemes) face.SetBlendShapeWeight(kv.Value, Mathf.MoveTowards(face.GetBlendShapeWeight(kv.Value), kv.Key == lastViseme && kv.Key != "sil" ? Openness * 100f : 0f, 900f * Time.deltaTime));
            }
            else if (LipRig == Rig.JawBone) jaw.localRotation = jawRest * Quaternion.Euler(Openness * jawOpenDegrees, 0f, 0f);
        }

        void ApplyBlink()
        {
            if (!blink || !CanBlink) return;
            if (blinkT < 0f && Time.time >= nextBlink) { blinkT = 0f; Blinks++; }
            float w = 0f;
            if (blinkT >= 0f) { blinkT += Time.deltaTime / 0.14f; w = Mathf.Sin(Mathf.Clamp01(blinkT) * Mathf.PI); if (blinkT >= 1f) { blinkT = -1f; nextBlink = Time.time + Random.Range(2.2f, 5.5f); } }
            if (blinkL >= 0) { face.SetBlendShapeWeight(blinkL, w * 100f); face.SetBlendShapeWeight(blinkR, w * 100f); }
            else for (int i = 0; i < lids.Count; i++) lids[i].localRotation = lidRest[i] * Quaternion.Euler(w * 22f * lidSign[i], 0f, 0f);
        }
        /// <summary>Blink now (tests, and reactions).</summary>
        public void BlinkNow() { nextBlink = 0f; }
    }
}
