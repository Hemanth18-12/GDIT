using UnityEngine;
using LegacyOfTemples.Core;

namespace LegacyOfTemples.Gameplay.Puzzles
{
    /// <summary>A stone disc, wheel or mirror the player turns one stop at a time. State = current stop.</summary>
    public class RotatingDisc : PuzzleElement, IInteractable
    {
        public int steps = 8;
        public Vector3 axis = Vector3.up;
        public float turnSeconds = 0.45f;
        public string verb = "Turn";
        public Transform visual;
        public float radius = 2.2f;
        Quaternion rest, from, to; float t = 1f; bool restSet;

        public string Id => SaveId;
        public string Prompt => locked ? "" : verb;
        public Transform Anchor => transform;
        public float Radius => locked ? 0f : radius;
        public void Interact() { if (!locked && t >= 1f) SetState(PuzzleLogic.NextStep(State, steps)); }

        Transform V => visual != null ? visual : transform;
        protected override void Apply(int state, bool instant)
        {
            if (!restSet) { rest = V.localRotation; restSet = true; }
            to = rest * Quaternion.AngleAxis(360f / Mathf.Max(1, steps) * state, axis.normalized);
            if (instant) { V.localRotation = to; t = 1f; } else { from = V.localRotation; t = 0f; }
        }
        void Update()
        {
            if (t >= 1f) return;
            t = Mathf.Min(1f, t + Time.deltaTime / Mathf.Max(0.01f, turnSeconds));
            V.localRotation = Quaternion.Slerp(from, to, Mathf.SmoothStep(0f, 1f, t));
        }
    }
}
