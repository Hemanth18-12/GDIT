using UnityEngine;
using LegacyOfTemples.Core;

namespace LegacyOfTemples.Gameplay.Puzzles
{
    /// <summary>Pulling a worn brace collapses a weak wall. Saved, resettable, and never destroys authored objects.</summary>
    public sealed class DestructibleWall : PuzzleElement, IInteractable
    {
        public GameObject intact, rubble;
        public ParticleSystem dust;
        public PuzzleElement release;
        public int releaseState = 1;
        public string Id => SaveId;
        public string Prompt => locked || State == 1 ? "" : "Pull loose brace";
        public Transform Anchor => transform;
        public float Radius => string.IsNullOrEmpty(Prompt) ? 0f : 2.2f;
        public void Interact() { if (!locked && (release == null || release.State == releaseState)) SetState(1); }
        protected override string DefaultSound(int state) => state == 1 ? "sfx_door_stone" : "sfx_stone_grind";
        protected override void Apply(int state, bool instant)
        {
            if (intact != null) intact.SetActive(state == 0);
            if (rubble != null) rubble.SetActive(state != 0);
            if (state == 1 && !instant && dust != null) dust.Play();
        }
    }
}
