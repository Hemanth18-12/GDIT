using UnityEngine;

namespace LegacyOfTemples.Characters
{
    /// <summary>Small collision-checked steps, intentional ledge hops, and bounded stuck recovery.
    /// Uses the existing jump animation. Never disables the capsule to pass through geometry.</summary>
    [RequireComponent(typeof(CharacterController), typeof(CharacterGrounding))]
    public sealed class CharacterTraversal : MonoBehaviour
    {
        CharacterController capsule;
        CharacterGrounding grounding;
        PlayerController player;
        Animator animator;
        Vector3 climbStart, climbEnd, lastSafe;
        float climbTime, blockedTime, stalledTime;
        public bool IsClimbing { get; private set; }
        public int Recoveries { get; private set; }

        void Awake()
        {
            capsule = GetComponent<CharacterController>(); grounding = GetComponent<CharacterGrounding>();
            player = GetComponent<PlayerController>(); animator = GetComponentInChildren<Animator>();
            lastSafe = transform.position;
        }

        bool IsVehicle(Collider c) => c.attachedRigidbody != null && c.attachedRigidbody.gameObject.name == "Jeep";

        public bool ClearAt(Vector3 root)
        {
            Vector3 scale = transform.lossyScale;
            float r = capsule.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z));
            float h = Mathf.Max(r, capsule.height * Mathf.Abs(scale.y) * 0.5f);
            Vector3 centre = root + transform.rotation * Vector3.Scale(capsule.center, scale);
            foreach (var c in Physics.OverlapCapsule(centre - Vector3.up * (h-r), centre + Vector3.up * (h-r), r, ~0, QueryTriggerInteraction.Ignore))
                if (!c.transform.IsChildOf(transform)) return false;
            return true;
        }

        bool GroundAt(Vector3 horizontal, float rise, float drop, out Vector3 root)
        {
            root = default;
            var hits = Physics.RaycastAll(new Vector3(horizontal.x, grounding.FeetY + rise, horizontal.z), Vector3.down, rise + drop, ~0, QueryTriggerInteraction.Ignore);
            System.Array.Sort(hits, (a,b) => a.distance.CompareTo(b.distance));
            foreach (var hit in hits)
            {
                if (hit.collider.transform.IsChildOf(transform) || hit.collider.GetComponentInParent<CharacterController>() != null || IsVehicle(hit.collider)) continue;
                if (hit.normal.y < Mathf.Cos(capsule.slopeLimit * Mathf.Deg2Rad)) return false;
                root = hit.point + Vector3.up * (0.005f - grounding.FootOffset * transform.lossyScale.y);
                return ClearAt(root);
            }
            return false;
        }

        public bool TryClimb(Vector3 direction)
        {
            if (IsClimbing || direction.sqrMagnitude < 0.1f) return false;
            direction = Vector3.ProjectOnPlane(direction, Vector3.up).normalized;
            Vector3 low = new Vector3(transform.position.x, grounding.FeetY + 0.25f, transform.position.z);
            if (!Physics.Raycast(low, direction, out var wall, 0.9f, ~0, QueryTriggerInteraction.Ignore) || IsVehicle(wall.collider)) return false;
            Vector3 landing = wall.point + direction * (capsule.radius + 0.18f);
            if (!GroundAt(landing, 1.25f, 1.2f, out var target)) return false;
            float rise = target.y - transform.position.y;
            if (rise < 0.4f || rise > 1.2f) return false;
            // Check the full vertical route before committing to a jump beside the ledge.
            if (!ClearAt(new Vector3(transform.position.x, target.y + 0.08f, transform.position.z))) return false;
            climbStart = transform.position; climbEnd = target; climbTime = stalledTime = 0f; IsClimbing = true;
            grounding.ClearSupport();
            if (animator != null && player.HasValidatedAnimation) { animator.SetBool("Jump", true); animator.SetBool("Grounded", false); }
            Debug.Log($"[Traversal] ledge hop {rise:0.00} m from {climbStart:F3} to {climbEnd:F3}");
            return true;
        }

        public bool TickClimb()
        {
            if (!IsClimbing) return false;
            climbTime += Time.deltaTime;
            float t = Mathf.Clamp01(climbTime / 0.7f);
            Vector3 lifted = new Vector3(climbStart.x, climbEnd.y + 0.08f, climbStart.z);
            Vector3 desired = t < 0.55f ? Vector3.Lerp(climbStart, lifted, Mathf.SmoothStep(0f, 1f, t/0.55f))
                : Vector3.Lerp(lifted, climbEnd + Vector3.up * 0.02f, Mathf.SmoothStep(0f, 1f, (t-0.55f)/0.45f));
            Vector3 before = transform.position;
            capsule.Move(desired - before);
            if (Vector3.Distance(desired, transform.position) > 0.16f) stalledTime += Time.deltaTime; else stalledTime = 0f;
            if (stalledTime > 0.18f || (t >= 1f && Vector3.Distance(transform.position, climbEnd) > 0.16f))
            {
                IsClimbing = false; player.Teleport(climbStart, transform.rotation);
                Debug.Log("[Traversal] ledge route obstructed; returned to safe start");
            }
            else if (t >= 1f) { IsClimbing = false; grounding.SnapToGround(true); lastSafe = transform.position; }
            if (!IsClimbing && animator != null) { animator.SetBool("Jump", false); animator.SetBool("Grounded", true); }
            return true;
        }

        public void Observe(Vector3 before, Vector3 intended, bool supported)
        {
            if (IsClimbing || !supported) { blockedTime = 0f; return; }
            bool onVehicle = false;
            foreach (var hit in Physics.RaycastAll(new Vector3(transform.position.x, grounding.FeetY + 0.15f, transform.position.z), Vector3.down, 0.35f, ~0, QueryTriggerInteraction.Ignore))
                if (!hit.collider.transform.IsChildOf(transform) && IsVehicle(hit.collider)) onVehicle = true;
            float moved = Vector3.ProjectOnPlane(transform.position-before, Vector3.up).magnitude;
            if (!onVehicle && intended.sqrMagnitude > 0.1f && moved < 0.15f * Time.deltaTime) blockedTime += Time.deltaTime; else blockedTime = 0f;
            if (!onVehicle && blockedTime < 0.15f && moved > 0.01f && ClearAt(transform.position)) lastSafe = transform.position;
            if (!onVehicle && blockedTime > 0.15f)
            {
                Vector3 ahead = transform.position + intended.normalized * 0.4f;
                if (GroundAt(ahead, 0.42f, 0.4f, out var step) && step.y > transform.position.y + 0.04f && step.y < transform.position.y + 0.42f)
                {
                    Vector3 up = transform.position + Vector3.up * (step.y - transform.position.y + 0.025f);
                    if (ClearAt(up)) { capsule.Move(up-transform.position); capsule.Move(step-transform.position); blockedTime = 0f; return; }
                }
            }
            if (!onVehicle && blockedTime < 3f) return;
            // Search bounded nearby floor, checking the full capsule. Never rescue across a wall.
            float radius = onVehicle ? 2.8f : 0.75f;
            for (int i = 0; i < 8; i++)
            {
                float a = i * Mathf.PI / 4f;
                Vector3 candidate = transform.position + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * radius;
                if (!GroundAt(candidate, 0.45f, onVehicle ? 5f : 0.6f, out var safe)) continue;
                if (!onVehicle && Physics.Linecast(transform.position + Vector3.up, safe + Vector3.up, ~0, QueryTriggerInteraction.Ignore)) continue;
                player.Teleport(safe, transform.rotation); Recoveries++; blockedTime = 0f; lastSafe = safe;
                Debug.Log($"[Traversal] {(onVehicle ? "vehicle roof" : "stuck")} recovery to {safe:F3}"); return;
            }
            if (!onVehicle && Vector3.Distance(lastSafe, transform.position) < 2f && ClearAt(lastSafe))
            { player.Teleport(lastSafe, transform.rotation); Recoveries++; }
            blockedTime = 0f;
        }
    }
}
