using UnityEngine;

namespace LegacyOfTemples.Characters
{
    [DisallowMultipleComponent, RequireComponent(typeof(CharacterController))]
    public sealed class CharacterGrounding : MonoBehaviour
    {
        [SerializeField] private LayerMask groundLayers = ~0;
        [SerializeField, Min(0.01f)] private float snapDistance = 0.4f;
        [SerializeField] private float footClearance = 0.005f;
        private readonly RaycastHit[] hits = new RaycastHit[64];
        private CharacterController controller;
        private bool calibrated;
        public float FootOffset { get; private set; }
        public float FeetY => transform.TransformPoint(new Vector3(0, FootOffset, 0)).y;
        public bool HasSupport { get; private set; }

        private void Awake() => Calibrate();

        public void Calibrate()
        {
            controller = GetComponent<CharacterController>();
            // The capsule is the sole owner of locomotion. Imported root motion must not
            // independently move VisualModel away from the capsule.
            foreach (var animator in GetComponentsInChildren<Animator>(true)) animator.applyRootMotion = false;
            if (!CharacterMeshBounds.TryMeasure(transform, out var bounds))
            {
                Debug.LogError("Cannot calibrate player feet: no readable mesh vertices.", this);
                return;
            }
            FootOffset = bounds.min.y;
            controller.skinWidth = 0.02f;
            controller.minMoveDistance = 0f;
            controller.height = bounds.size.y;
            controller.radius = Mathf.Clamp(bounds.size.x * 0.35f, 0.2f, bounds.size.y * 0.2f);
            controller.center = new Vector3(0f, bounds.center.y + controller.skinWidth + footClearance, 0f);
            controller.stepOffset = Mathf.Min(0.3f, controller.height * 0.2f);
            calibrated = true;
        }

        public bool TryFindGround(float rise, float drop, out RaycastHit support)
        {
            support = default;
            float nearest = float.PositiveInfinity;
            int count = Physics.RaycastNonAlloc(new Vector3(transform.position.x, FeetY + rise, transform.position.z),
                Vector3.down, hits, rise + drop, groundLayers, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                var hit = hits[i];
                if (hit.collider.transform.IsChildOf(transform) ||
                    hit.collider.GetComponentInParent<NPCDialogueTrigger>() != null ||
                    hit.collider.GetComponentInParent<CharacterController>() != null ||
                    hit.normal.y < Mathf.Cos(controller.slopeLimit * Mathf.Deg2Rad)) continue;
                if (hit.distance < nearest) { nearest = hit.distance; support = hit; }
            }
            return nearest < float.PositiveInfinity;
        }

        public bool SnapToGround(bool spawning = false)
        {
            if (!calibrated || !controller.enabled) return false;
            HasSupport = TryFindGround(spawning ? 1.5f : 0.1f,
                spawning ? 3f : snapDistance, out var support);
            if (!HasSupport) return false;
            float delta = support.point.y + footClearance - FeetY;
            if (spawning)
            {
                controller.enabled = false;
                transform.position += Vector3.up * delta;
                controller.enabled = true;
                Physics.SyncTransforms();
            }
            else controller.Move(Vector3.up * delta);
            return true;
        }
    }
}
