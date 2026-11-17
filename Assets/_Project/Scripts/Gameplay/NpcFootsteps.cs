using UnityEngine;
using LegacyOfTemples.Core;

namespace LegacyOfTemples.Gameplay
{
    /// <summary>Footsteps for a non-player character: a step sound on the surface below whenever it has moved about one stride.</summary>
    public sealed class NpcFootsteps : MonoBehaviour
    {
        public float stride = 0.75f, volume = 0.38f;
        Vector3 last; float travelled;
        void Start() { last = transform.position; }
        void Update()
        {
            Vector3 d = transform.position - last; d.y = 0f; last = transform.position;
            float m = d.magnitude; if (m > 2f) { travelled = 0f; return; }   // a teleport is not a step
            travelled += m;
            if (travelled < stride) return;
            travelled = 0f;
            var cam = Camera.main; if (cam != null && (cam.transform.position - transform.position).sqrMagnitude > 30f * 30f) return;   // too far to hear
            GameSfx.Footstep(transform.position, volume * Mathf.Clamp(m / Mathf.Max(Time.deltaTime, 1e-4f) / 3f, 0.6f, 1.3f));
        }
    }
}
