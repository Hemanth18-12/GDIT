using UnityEngine;
using LegacyOfTemples.Core;

namespace LegacyOfTemples.Gameplay.Puzzles
{
    /// <summary>Something the player picks up, carries in front of them and puts down or fits into a socket (a lamp piece, a gear, a lamp).</summary>
    public class Carryable : MonoBehaviour, IInteractable
    {
        public string itemKey = "piece";
        public string displayName = "piece";
        public Vector3 holdOffset = new Vector3(0f, 1.05f, 0.55f);
        public float radius = 1.8f;
        public static Carryable Held { get; private set; }
        public PlacementSocket InSocket { get; internal set; }
        Transform carrier;

        public string Id => "carry." + name;
        public string Prompt => Held == this ? (SocketInReach() != null ? "Place " : "Put down ") + displayName : Held != null ? "" : "Pick up " + displayName;
        public Transform Anchor => transform;
        public float Radius => Held != null && Held != this ? 0f : radius;

        public void Interact()
        {
            if (!isActiveAndEnabled) return;   // switched off = not usable yet (for example a lantern with no oil)
            if (Held == this) { var socket = SocketInReach(); if (socket != null) socket.Place(this); else Drop(); return; }
            if (Held != null) return;
            var pc = FindAnyObjectByType<LegacyOfTemples.Characters.PlayerController>();
            if (pc != null) PickUp(pc.transform);
        }

        public void PickUp(Transform by)
        {
            if (InSocket != null) InSocket.Remove();
            carrier = by; Held = this;
            foreach (var c in GetComponentsInChildren<Collider>()) if (!c.isTrigger) c.enabled = false;   // the trigger stays on so E can still reach the held thing
            var rb = GetComponent<Rigidbody>(); if (rb != null) rb.isKinematic = true;
        }
        public void Drop()
        {
            if (Held != this) return;
            Vector3 p = transform.position;
            if (Physics.Raycast(p + Vector3.up * 0.3f, Vector3.down, out RaycastHit h, 4f, ~0, QueryTriggerInteraction.Ignore)) p = h.point;
            Release(); transform.position = p;
        }
        internal void Release()
        {
            if (Held == this) Held = null; carrier = null;
            foreach (var c in GetComponentsInChildren<Collider>()) c.enabled = true;
        }
        /// <summary>An empty socket that takes this item and is within reach of whoever carries it.</summary>
        PlacementSocket SocketInReach()
        {
            if (carrier == null) return null;
            PlacementSocket best = null; float bd = float.MaxValue;
            foreach (var s in FindObjectsByType<PlacementSocket>())
            {
                if (s.locked || s.Item != null || !s.Fits(this)) continue;
                float d = Vector3.Distance(carrier.position, s.transform.position);
                if (d <= s.radius && d < bd) { bd = d; best = s; }
            }
            return best;
        }

        void LateUpdate()
        {
            if (Held != this || carrier == null) return;
            transform.SetPositionAndRotation(carrier.TransformPoint(holdOffset), carrier.rotation);
        }
        void OnDestroy() { if (Held == this) Held = null; }
    }
}
