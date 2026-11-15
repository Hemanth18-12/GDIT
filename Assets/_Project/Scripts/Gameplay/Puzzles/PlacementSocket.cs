using UnityEngine;
using LegacyOfTemples.Core;

namespace LegacyOfTemples.Gameplay.Puzzles
{
    /// <summary>A place something is fitted into (lamp stand, gear peg, empty stand). State 1 = holds the right item, 2 = holds a wrong one.</summary>
    public class PlacementSocket : PuzzleElement, IInteractable
    {
        public string acceptsKey = "piece";
        [Tooltip("If set, any item can be placed and a wrong one gives state 2 (used for gears on pegs).")]
        public bool takesAnyItem;
        public Transform seat;
        public float radius = 2f;
        public Carryable Item { get; private set; }

        public string Id => SaveId;
        public string Prompt => locked ? "" : Item == null && Carryable.Held != null && Fits(Carryable.Held) ? "Place " + Carryable.Held.displayName : Item != null && Carryable.Held == null ? "Take " + Item.displayName : "";
        public Transform Anchor => transform;
        public float Radius => string.IsNullOrEmpty(Prompt) ? 0f : radius;
        public bool Fits(Carryable c) => c != null && (takesAnyItem || c.itemKey == acceptsKey);

        public void Interact()
        {
            if (locked) return;
            if (Item == null && Carryable.Held != null && Fits(Carryable.Held)) Place(Carryable.Held);
            else if (Item != null && Carryable.Held == null) { var it = Item; var pc = FindAnyObjectByType<LegacyOfTemples.Characters.PlayerController>(); if (pc != null) it.PickUp(pc.transform); }
        }

        public void Place(Carryable c)
        {
            if (c == null || Item != null) return;
            c.Release(); c.InSocket = this; Item = c;
            var s = seat != null ? seat : transform; c.transform.SetPositionAndRotation(s.position, s.rotation);
            SetState(c.itemKey == acceptsKey ? 1 : 2, false);
        }
        internal void Remove() { if (Item != null) Item.InSocket = null; Item = null; SetState(0, false); }
        public override void ResetState() { if (Item != null) { Item.InSocket = null; Item = null; } base.ResetState(); }
        protected override void Apply(int state, bool instant) { }
    }
}
