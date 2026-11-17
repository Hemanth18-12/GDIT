using System.Collections.Generic;
using UnityEngine;

namespace LegacyOfTemples.Gameplay.Puzzles
{
    /// <summary>A floor plate. State 1 while something heavy stands on it (the player, a guard, a pushed block). Needs a trigger collider.</summary>
    public class PressurePlate : PuzzleElement
    {
        public Transform plate;
        public float sink = 0.06f;
        readonly HashSet<Object> weights = new HashSet<Object>();
        Vector3 rest; bool restSet;

        protected override string DefaultSound(int state) => state == 1 ? "sfx_gear_click" : null;
        public void Press(Object who) { if (who == null) return; weights.Add(who); Refresh(); }
        public void Release(Object who) { weights.Remove(who); Refresh(); }
        void Refresh() { weights.RemoveWhere(o => o == null); if (!locked) SetState(weights.Count > 0 ? 1 : 0, false); }

        static bool Heavy(Collider c) => c.GetComponentInParent<CharacterController>() != null || c.attachedRigidbody != null || c.GetComponentInParent<PushableBlock>() != null;
        static Object Who(Collider c) => c.attachedRigidbody != null ? c.attachedRigidbody.gameObject : c.gameObject;   // each block or person counts once
        void OnTriggerEnter(Collider c) { if (Heavy(c)) Press(Who(c)); }
        void OnTriggerExit(Collider c) { Release(Who(c)); }

        public override void ResetState() { weights.Clear(); base.ResetState(); }
        protected override void Apply(int state, bool instant)
        {
            var p = plate != null ? plate : transform;
            if (!restSet) { rest = p.localPosition; restSet = true; }
            p.localPosition = rest + Vector3.down * (state == 1 ? sink : 0f);
        }
    }
}
