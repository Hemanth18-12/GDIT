using UnityEngine;
using LegacyOfTemples.Core;

namespace LegacyOfTemples.Gameplay.Puzzles
{
    /// <summary>A lever or pull handle. State 0 = up, 1 = pulled. Can spring back by itself after a few seconds.</summary>
    public class PuzzleLever : PuzzleElement, IInteractable
    {
        public Transform handle;
        public Vector3 pulledEuler = new Vector3(55f, 0f, 0f);
        public float springBackSeconds;
        public string verb = "Pull";
        public float radius = 2f;
        Quaternion rest; bool restSet; float pulledAt;

        public string Id => SaveId;
        public string Prompt => locked ? "" : verb;
        public Transform Anchor => transform;
        public float Radius => locked ? 0f : radius;
        public void Interact() { if (locked) return; SetState(State == 0 ? 1 : 0); pulledAt = Time.time; }

        protected override void Apply(int state, bool instant)
        {
            var h = handle != null ? handle : transform;
            if (!restSet) { rest = h.localRotation; restSet = true; }
            h.localRotation = state == 1 ? rest * Quaternion.Euler(pulledEuler) : rest;
        }
        void Update() { if (springBackSeconds > 0f && State == 1 && !locked && Time.time - pulledAt > springBackSeconds) SetState(0); }
    }
}
