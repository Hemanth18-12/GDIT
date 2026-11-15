using UnityEngine;
using LegacyOfTemples.Core;

namespace LegacyOfTemples.Gameplay.Puzzles
{
    /// <summary>A stone block pushed one step at a time, away from the player, if the next cell is free. State packs its grid offset.</summary>
    public class PushableBlock : PuzzleElement, IInteractable
    {
        public float step = 1f;
        public float moveSeconds = 0.35f;
        public Vector3 halfExtents = new Vector3(0.45f, 0.45f, 0.45f);
        public LayerMask blockers = ~0;
        public float radius = 1.8f;
        Vector3 origin, from, to; bool originSet; float t = 1f;

        public string Id => SaveId;
        public string Prompt => locked ? "" : "Push";
        public Transform Anchor => transform;
        public float Radius => locked ? 0f : radius;

        public static int Pack(int dx, int dz) => (dx + 500) * 1000 + (dz + 500);
        public static void Unpack(int s, out int dx, out int dz) { dx = s / 1000 - 500; dz = s % 1000 - 500; }

        void Reset() { startState = Pack(0, 0); }
        protected override void Awake() { if (startState == 0) startState = Pack(0, 0); origin = transform.position; originSet = true; base.Awake(); }

        public void Interact()
        {
            var pc = FindAnyObjectByType<LegacyOfTemples.Characters.PlayerController>();
            if (pc != null) Push(PuzzleLogic.PushDirection(pc.transform.position, transform.position));
        }

        /// <summary>Try to move one step in a direction. Returns false if something is in the way.</summary>
        public bool Push(Vector3 dir)
        {
            if (locked || t < 1f) return false;
            Unpack(State, out int dx, out int dz);
            int nx = dx + Mathf.RoundToInt(dir.x), nz = dz + Mathf.RoundToInt(dir.z);
            Vector3 target = origin + new Vector3(nx, 0f, nz) * step;
            foreach (var hit in Physics.OverlapBox(target + Vector3.up * 0.05f, halfExtents * 0.9f, transform.rotation, blockers, QueryTriggerInteraction.Ignore))
                if (!hit.transform.IsChildOf(transform) && hit.GetComponentInParent<LegacyOfTemples.Characters.PlayerController>() == null && hit.bounds.max.y > target.y - halfExtents.y + 0.1f) return false;
            SetState(Pack(nx, nz));
            return true;
        }

        protected override void Apply(int state, bool instant)
        {
            if (!originSet) { origin = transform.position; originSet = true; }
            Unpack(state, out int dx, out int dz);
            to = origin + new Vector3(dx, 0f, dz) * step;
            if (instant) { transform.position = to; t = 1f; } else { from = transform.position; t = 0f; }
        }
        void Update()
        {
            if (t >= 1f) return;
            t = Mathf.Min(1f, t + Time.deltaTime / Mathf.Max(0.01f, moveSeconds));
            transform.position = Vector3.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t));
            if (t >= 1f) Physics.SyncTransforms();
        }
    }
}
