using UnityEngine;
using UnityEngine.InputSystem;

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
        [SerializeField] private float xSpeed = 12.0f;
        [SerializeField] private float ySpeed = 8.0f;
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

        [Header("Cinematic & Special Modes")]
        private bool isDialogueMode = false;
        private Transform dialogueSpeaker;
        private Transform dialogueListener;
        private bool isInspectionMode = false;
        private Transform inspectionTarget;
        private bool isCutsceneOverride = false;
        private Vector3 cutscenePos;
        private Quaternion cutsceneRot;

        public void SetCutsceneOverride(Vector3 pos, Quaternion rot)
        {
            isCutsceneOverride = true;
            cutscenePos = pos;
            cutsceneRot = rot;
        }

        public void ClearCutsceneOverride()
        {
            isCutsceneOverride = false;
        }

        public void SetDialogueMode(Transform speaker, Transform listener)
        {
            isDialogueMode = true;
            dialogueSpeaker = speaker;
            dialogueListener = listener;
        }

        public void ClearDialogueMode()
        {
            isDialogueMode = false;
            dialogueSpeaker = null;
            dialogueListener = null;
        }

        public void SetInspectionMode(Transform targetClue)
        {
            isInspectionMode = true;
            inspectionTarget = targetClue;
        }

        public void ClearInspectionMode()
        {
            isInspectionMode = false;
            inspectionTarget = null;
        }

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
            if (isCutsceneOverride)
            {
                transform.position = Vector3.Lerp(transform.position, cutscenePos, Time.deltaTime * 6f);
                transform.rotation = Quaternion.Slerp(transform.rotation, cutsceneRot, Time.deltaTime * 6f);
                return;
            }

            if (isDialogueMode && dialogueSpeaker != null)
            {
                Vector3 midPoint = (dialogueSpeaker.position + (dialogueListener != null ? dialogueListener.position : dialogueSpeaker.position)) * 0.5f;
                Vector3 toSpeaker = (dialogueSpeaker.position - (dialogueListener != null ? dialogueListener.position : dialogueSpeaker.position)).normalized;
                Vector3 sideOffset = Vector3.Cross(toSpeaker, Vector3.up) * 1.5f;
                Vector3 diagCamPos = midPoint + sideOffset + Vector3.up * 1.5f + (-toSpeaker * 2.2f);
                Quaternion diagCamRot = Quaternion.LookRotation(dialogueSpeaker.position + Vector3.up * 1.4f - diagCamPos);

                transform.position = Vector3.Lerp(transform.position, diagCamPos, Time.deltaTime * 4f);
                transform.rotation = Quaternion.Slerp(transform.rotation, diagCamRot, Time.deltaTime * 4f);
                return;
            }

            if (isInspectionMode && inspectionTarget != null)
            {
                Vector3 inspectPos = inspectionTarget.position + inspectionTarget.forward * 1.2f + Vector3.up * 0.6f;
                Quaternion inspectRot = Quaternion.LookRotation((inspectionTarget.position + Vector3.up * 0.2f) - inspectPos);

                transform.position = Vector3.Lerp(transform.position, inspectPos, Time.deltaTime * 5f);
                transform.rotation = Quaternion.Slerp(transform.rotation, inspectRot, Time.deltaTime * 5f);
                return;
            }

            if (target == null)
            {
                FindPlayerTarget();
                if (target == null) return;
            }

            bool rightClick = Mouse.current != null && Mouse.current.rightButton.isPressed;
            bool canOrbit = !requireRightClickToOrbit || rightClick;

            Vector2 mouseDelta = Vector2.zero;
            if (Mouse.current != null)
            {
                mouseDelta = Mouse.current.delta.ReadValue();
            }

            Vector2 gamepadDelta = Vector2.zero;
            if (Gamepad.current != null)
            {
                gamepadDelta = Gamepad.current.rightStick.ReadValue() * 10f;
            }

            Vector2 lookDelta = mouseDelta + gamepadDelta;

            if (canOrbit && lookDelta.sqrMagnitude > 0.001f)
            {
                currentX += lookDelta.x * xSpeed * Time.deltaTime;
                currentY -= lookDelta.y * ySpeed * Time.deltaTime;
                currentY = ClampAngle(currentY, yMinLimit, yMaxLimit);
            }

            // Scroll zoom
            float scroll = 0f;
            if (Mouse.current != null)
            {
                scroll = Mouse.current.scroll.ReadValue().y * 0.01f;
            }
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
