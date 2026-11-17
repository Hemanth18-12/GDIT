using UnityEngine;

namespace LegacyOfTemples.Gameplay.Puzzles
{
    /// <summary>A carved socket that wakes when a light beam rests on it. State 1 = lit. Optionally it must stay lit for a moment.</summary>
    public class LightSocket : PuzzleElement
    {
        public float holdSeconds;
        public Renderer glow;
        public Color litColor = new Color(1f, 0.82f, 0.4f) * 3f;
        float lastHit = -10f, litSince = -1f; MaterialPropertyBlock mpb;

        protected override string DefaultSound(int state) => state == 1 ? "sfx_token" : null;
        /// <summary>Called by a beam every frame it touches this socket.</summary>
        public void Touch() { lastHit = Time.time; if (litSince < 0f) litSince = Time.time; }
        void LateUpdate()
        {
            if (locked) return;
            bool hit = Time.time - lastHit < 0.12f;
            if (!hit) litSince = -1f;
            SetState(hit && Time.time - litSince >= holdSeconds ? 1 : 0, false);
        }
        protected override void Apply(int state, bool instant)
        {
            if (glow == null) return;
            if (mpb == null) mpb = new MaterialPropertyBlock();
            glow.GetPropertyBlock(mpb); mpb.SetColor("_EmissionColor", state == 1 ? litColor : Color.black); glow.SetPropertyBlock(mpb);
        }
    }
}
