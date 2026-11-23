using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace LegacyOfTemples.Gameplay
{
    /// <summary>Bounded swing trails, reversible material-property flashes and a brief hurt vignette.</summary>
    public sealed class CombatFeedback : MonoBehaviour
    {
        static CombatFeedback instance;
        sealed class FlashState { public Renderer renderer; public MaterialPropertyBlock saved = new MaterialPropertyBlock(); public float until; }
        readonly List<FlashState> flashes = new List<FlashState>();
        readonly List<LineRenderer> trails = new List<LineRenderer>();
        readonly List<float> ends = new List<float>();
        Volume hurtVolume;
        float hurtUntil;
        public static int Impacts { get; private set; }
        public static int Swings { get; private set; }
        static CombatFeedback Get()
        {
            if (instance == null) { var go = new GameObject("CombatFeedback"); DontDestroyOnLoad(go); instance = go.AddComponent<CombatFeedback>(); }
            return instance;
        }
        public static void Flash(Transform actor, Color colour)
        {
            if (actor == null) return;
            var self = Get(); Impacts++;
            foreach (var renderer in actor.GetComponentsInChildren<Renderer>())
            {
                if (renderer is ParticleSystemRenderer || renderer is LineRenderer) continue;
                var state = self.flashes.Find(f => f.renderer == renderer);
                if (state == null)
                {
                    state = new FlashState { renderer = renderer }; renderer.GetPropertyBlock(state.saved); self.flashes.Add(state);
                }
                var block = new MaterialPropertyBlock(); renderer.GetPropertyBlock(block);
                block.SetColor("_BaseColor", colour); renderer.SetPropertyBlock(block); state.until = Time.unscaledTime + 0.09f;
            }
        }
        public static void Swing(Transform actor, bool heavy)
        {
            if (actor == null) return;
            var self = Get(); Swings++;
            int i = self.ends.FindIndex(t => t < Time.unscaledTime);
            if (i < 0 && self.trails.Count < 8)
            {
                var go = new GameObject("SwingTrail"); go.transform.SetParent(self.transform);
                var line = go.AddComponent<LineRenderer>(); line.useWorldSpace = true; line.positionCount = 7;
                line.sharedMaterial = Resources.Load<Material>("FX/Glow"); line.shadowCastingMode = ShadowCastingMode.Off;
                line.receiveShadows = false; line.numCapVertices = 2; self.trails.Add(line); self.ends.Add(0f); i = self.trails.Count-1;
            }
            if (i < 0) i = 0;
            var trail = self.trails[i]; trail.enabled = true;
            trail.startWidth = heavy ? 0.05f : 0.025f; trail.endWidth = 0.005f;
            trail.startColor = new Color(1f,0.86f,0.6f,0.45f); trail.endColor = new Color(1f,0.86f,0.6f,0f);
            for(int n=0;n<7;n++) { float a = Mathf.Lerp(-65f,65f,n/6f)*Mathf.Deg2Rad;
                trail.SetPosition(n,actor.position+Vector3.up*1.2f+(actor.forward*Mathf.Cos(a)+actor.right*Mathf.Sin(a))*(heavy?1.1f:0.85f)); }
            self.ends[i] = Time.unscaledTime + 0.12f;
        }
        public static void Hurt()
        {
            var self = Get(); self.hurtUntil = Time.unscaledTime + 0.25f;
            if (self.hurtVolume == null)
            {
                self.hurtVolume = self.gameObject.AddComponent<Volume>(); self.hurtVolume.isGlobal = true; self.hurtVolume.priority = 120f;
                var profile = ScriptableObject.CreateInstance<VolumeProfile>();
                var vignette = profile.Add<Vignette>(true); vignette.intensity.Override(0.28f); vignette.color.Override(new Color(0.24f,0.015f,0.01f)); vignette.smoothness.Override(0.5f);
                self.hurtVolume.sharedProfile = profile;
            }
            self.hurtVolume.weight = 1f;
        }
        void Update()
        {
            for(int i=0;i<flashes.Count;i++)
            {
                var f=flashes[i]; if(f.renderer == null) {flashes.RemoveAt(i--);continue;}
                if(f.until>0f && Time.unscaledTime>=f.until) {f.renderer.SetPropertyBlock(f.saved);f.until=0f;}
            }
            for(int i=0;i<trails.Count;i++) if(trails[i]!=null && Time.unscaledTime>ends[i]) trails[i].enabled=false;
            if(hurtVolume!=null) hurtVolume.weight=Mathf.Clamp01((hurtUntil-Time.unscaledTime)/0.25f);
        }
    }
}
