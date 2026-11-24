using UnityEngine;

namespace LegacyOfTemples.Gameplay
{
    /// <summary>Marks the actual built road, shared by Jeep recovery and the minimap.</summary>
    public sealed class DriveableRoad : MonoBehaviour
    {
        public float Width => transform.lossyScale.x;
        public Vector3 Start => transform.TransformPoint(new Vector3(0f, 0.5f, -0.5f));
        public Vector3 End => transform.TransformPoint(new Vector3(0f, 0.5f, 0.5f));

        public Vector3 ClosestPoint(Vector3 position)
        {
            Vector3 a = Start, delta = End - a;
            Vector3 flat = new Vector3(delta.x, 0f, delta.z);
            Vector3 offset = position - a; offset.y = 0f;
            float t = flat.sqrMagnitude > 0.001f ? Mathf.Clamp01(Vector3.Dot(offset, flat) / flat.sqrMagnitude) : 0f;
            // Leave room at segment ends for the vehicle's wheelbase.
            float inset = Mathf.Min(0.25f, 3f / Mathf.Max(0.01f, flat.magnitude));
            return a + delta * Mathf.Clamp(t, inset, 1f - inset);
        }
    }
}
