using UnityEngine;

namespace LegacyOfTemples.Characters
{
    public class OrbitCamera : MonoBehaviour
    {
        [Header("Target Tracking")]
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 targetOffset = new Vector3(0f, 1.4f, 0f);

        [Header("Distance & Zoom")]
        [SerializeField] private float defaultDistance = 4.5f;
        [SerializeField] private float minDistance = 1.5f;
        [SerializeField] private float maxDistance = 8.5f;
        [SerializeField] private float zoomSpeed = 2.5f;

        [Header("Orbit Controls")]
        [SerializeField] private float xSpeed = 120.0f;
        [SerializeField] private float ySpeed = 80.0f;
        [SerializeField] private float yMinLimit = -15.0f;
        [SerializeField] private float yMaxLimit = 70.0f;
        [SerializeField] private bool requireRightClickToOrbit = false;

        [Header("Collision & Smoothing")]
        [SerializeField] private bool collisionAvoidance = true;
        [SerializeField] private float collisionRadius = 0.25f;
        [SerializeField] private LayerMask collisionLayers = ~0; // Default all layers
        [SerializeField] private float smoothTime = 0.05f;

        private float currentX = 0.0f;
        private float currentY = 15.0f;
        private float currentDistance;
        private float desiredDistance;
        private Vector3 currentVelocity;

        private void Start()
        {
            currentDistance = defaultDistance;
            desiredDistance = defaultDistance;

            Vector3 angles = transform.eulerAngles;
            currentX = angles.y;
            currentY = angles.x;

            // Exclude Player and Ignore Raycast layers from camera collision
            collisionLayers &= ~(1 << LayerMask.NameToLayer("Ignore Raycast"));
            int playerLayer = LayerMask.NameToLayer("Player");
            if (playerLayer != -1)
            {
                collisionLayers &= ~(1 << playerLayer);
            }

            if (target == null)
            {
                FindPlayerTarget();
            }
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
            if (target != null)
            {
                currentX = target.eulerAngles.y;
            }
        }

        private void FindPlayerTarget()
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                SetTarget(player.transform);
            }
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                FindPlayerTarget();
                if (target == null) return;
            }

            bool canOrbit = !requireRightClickToOrbit || Input.GetMouseButton(1);
            if (canOrbit)
            {
                currentX += Input.GetAxis("Mouse X") * xSpeed * Time.deltaTime;
                currentY -= Input.GetAxis("Mouse Y") * ySpeed * Time.deltaTime;
                currentY = ClampAngle(currentY, yMinLimit, yMaxLimit);
            }

            // Scroll zoom
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.001f)
            {
                desiredDistance = Mathf.Clamp(desiredDistance - scroll * zoomSpeed, minDistance, maxDistance);
            }

            Quaternion rotation = Quaternion.Euler(currentY, currentX, 0);
            Vector3 targetPivot = target.position + targetOffset;

            // Camera collision avoidance
            float targetDist = desiredDistance;
            if (collisionAvoidance)
            {
                Vector3 rayDir = rotation * -Vector3.forward;
                if (Physics.SphereCast(targetPivot, collisionRadius, rayDir, out RaycastHit hit, desiredDistance, collisionLayers, QueryTriggerInteraction.Ignore))
                {
                    targetDist = Mathf.Clamp(hit.distance - 0.1f, minDistance, desiredDistance);
                }
            }

            currentDistance = Mathf.SmoothDamp(currentDistance, targetDist, ref currentVelocity.z, smoothTime);
            Vector3 position = targetPivot + rotation * new Vector3(0.0f, 0.0f, -currentDistance);

            transform.rotation = rotation;
            transform.position = position;
        }

        private static float ClampAngle(float angle, float min, float max)
        {
            if (angle < -360F) angle += 360F;
            if (angle > 360F) angle -= 360F;
            return Mathf.Clamp(angle, min, max);
        }
    }
}
