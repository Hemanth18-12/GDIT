using UnityEngine;
using LegacyOfTemples.Core;
using LegacyOfTemples.Characters;

namespace LegacyOfTemples.Gameplay.Puzzles
{
    /// <summary>A repeatable physical crossing. The rider follows a pendulum arc; an obstructed landing returns safely.</summary>
    public sealed class RopeSwing : PuzzleElement, IInteractable
    {
        public Transform landing, rope;
        public PlayerController actor;
        public float seconds = 1.4f, arcHeight = 2f;
        PlayerController rider;
        CharacterController capsule;
        Vector3 origin;
        float elapsed;
        public string Id => SaveId;
        public string Prompt => locked || rider != null ? "" : "Grab rope";
        public Transform Anchor => transform;
        public float Radius => string.IsNullOrEmpty(Prompt) ? 0f : 2.4f;
        public bool Riding => rider != null;
        public void Interact()
        {
            if (locked || rider != null || landing == null) return;
            rider = actor != null ? actor : FindAnyObjectByType<PlayerController>(); if (rider == null) return;
            origin = rider.transform.position; capsule = rider.GetComponent<CharacterController>();
            capsule.enabled = false; rider.enabled = false; elapsed = 0f;
            GameSfx.At("sfx_rope", transform.position, 0.7f);
        }
        void Update()
        {
            if (rider == null) return;
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / Mathf.Max(0.1f, seconds));
            rider.transform.position = Vector3.Lerp(origin, landing.position, t) + Vector3.up * (Mathf.Sin(t * Mathf.PI) * arcHeight);
            if (rope != null) rope.LookAt(rider.transform.position + Vector3.up);
            if (t < 1f) return;
            Vector3 p = landing.position;
            float radius = capsule.radius * 0.9f;
            bool blocked = Physics.CheckCapsule(p + Vector3.up * (radius + 0.15f), p + Vector3.up * (capsule.height - radius), radius, ~0, QueryTriggerInteraction.Ignore);
            rider.Teleport(blocked ? origin : p, rider.transform.rotation);
            rider.enabled = true; capsule.enabled = true; rider = null;
            if (!blocked) SetState(1);
        }
        void OnDisable()
        {
            if (rider == null) return;
            rider.Teleport(origin, rider.transform.rotation); rider.enabled = true; capsule.enabled = true; rider = null;
        }
        protected override void Apply(int state, bool instant) { }
    }
}
