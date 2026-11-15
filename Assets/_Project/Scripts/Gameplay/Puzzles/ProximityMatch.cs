using UnityEngine;

namespace LegacyOfTemples.Gameplay.Puzzles
{
    /// <summary>
    /// State 1 while a moved object stands on the right spot (and faces the right way, if asked).
    /// Used for shadow matching: the lamp must stand where the robber's lamp stood so the shadows line up.
    /// </summary>
    public class ProximityMatch : PuzzleElement
    {
        public Transform subject;
        public Transform target;
        public float tolerance = 0.6f;
        [Tooltip("0 = facing does not matter.")]
        public float yawTolerance;
        float next;

        public bool Matches()
        {
            if (subject == null || target == null) return false;
            Vector3 d = subject.position - target.position; d.y = 0f;
            if (d.magnitude > tolerance) return false;
            return yawTolerance <= 0f || Mathf.Abs(Mathf.DeltaAngle(subject.eulerAngles.y, target.eulerAngles.y)) <= yawTolerance;
        }
        void Update() { if (locked || Time.time < next) return; next = Time.time + 0.15f; Check(); }
        public void Check() { if (Carryable.Held != null && subject != null && Carryable.Held.transform == subject) { SetState(0, false); return; } SetState(Matches() ? 1 : 0, false); }
        protected override void Apply(int state, bool instant) { }
    }
}
