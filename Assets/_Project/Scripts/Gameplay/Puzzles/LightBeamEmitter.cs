using System.Collections.Generic;
using UnityEngine;

namespace LegacyOfTemples.Gameplay.Puzzles
{
    /// <summary>
    /// A beam of light (sun through a gap, a lamp). It travels straight, bounces off <see cref="LightMirror"/>s,
    /// stops at anything solid and wakes any <see cref="LightSocket"/> it lands on. Drawn with a LineRenderer if one is present.
    /// </summary>
    public class LightBeamEmitter : MonoBehaviour
    {
        public bool on = true;
        public float maxDistance = 80f;
        public int maxBounces = 8;
        public LayerMask mask = ~0;
        public LineRenderer line;
        public readonly List<Vector3> Points = new List<Vector3>();
        public LightSocket LastSocket { get; private set; }

        void Update() { Trace(); }

        /// <summary>Follow the beam now. Returns the socket it ends on, or null.</summary>
        public LightSocket Trace()
        {
            Points.Clear(); LastSocket = null;
            if (on)
            {
                Vector3 p = transform.position, d = transform.forward; Points.Add(p);
                for (int i = 0; i <= maxBounces; i++)
                {
                    if (!Physics.Raycast(p, d, out RaycastHit h, maxDistance, mask, QueryTriggerInteraction.Ignore)) { Points.Add(p + d * maxDistance); break; }
                    Points.Add(h.point);
                    var socket = h.collider.GetComponentInParent<LightSocket>();
                    if (socket != null) { socket.Touch(); LastSocket = socket; break; }
                    var mirror = h.collider.GetComponentInParent<LightMirror>();
                    if (mirror == null) break;
                    Vector3 n = mirror.Normal; if (Vector3.Dot(n, d) > -0.08f) break;   // the back or the thin edge of a mirror does not reflect
                    d = PuzzleLogic.Reflect(d, n); p = h.point + d * 0.02f;
                }
            }
            if (line != null) { line.enabled = on && Points.Count > 1; line.positionCount = Points.Count; for (int i = 0; i < Points.Count; i++) line.SetPosition(i, Points[i]); }
            return LastSocket;
        }
    }
}
