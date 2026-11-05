using System;
using UnityEngine;
using LegacyOfTemples.Core;

namespace LegacyOfTemples.Characters
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Locomotion")]
        [SerializeField] private float walkSpeed = 4.5f;
        [SerializeField] private float runSpeed = 7.5f;
        [SerializeField] private float rotationSpeed = 12.0f;
        [SerializeField] private float jumpHeight = 1.2f;
        [SerializeField] private float gravity = 18.0f;

        [Header("Interaction")]
        [SerializeField] private float interactionRadius = 2.8f;
        [SerializeField] private KeyCode interactKey = KeyCode.E;

        [Header("Camera Reference")]
        [SerializeField] private Transform cameraTransform;

        private CharacterController characterController;
        private Vector3 moveVelocity;
        private float verticalVelocity;
        private IInteractable nearestInteractable;
        private GUIStyle promptBoxStyle;
        private GUIStyle promptTextStyle;

        public bool IsMoving { get; private set; }
        public bool IsRunning { get; private set; }
        public IInteractable CurrentInteractable => nearestInteractable;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            if (cameraTransform == null && Camera.main != null)
            {
                cameraTransform = Camera.main.transform;
            }
        }

        private void Start()
        {
            if (cameraTransform == null && Camera.main != null)
            {
                cameraTransform = Camera.main.transform;
            }
        }

        private void Update()
        {
            HandleMovement();
            HandleInteractionScan();

            if (Input.GetKeyDown(interactKey) || Input.GetKeyDown(KeyCode.Return))
            {
                TryInteract();
            }
        }

        private void HandleMovement()
        {
            if (characterController == null) return;

            float horizontal = Input.GetAxisRaw("Horizontal");
            float vertical = Input.GetAxisRaw("Vertical");
            Vector3 inputDir = new Vector3(horizontal, 0f, vertical).normalized;

            IsRunning = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            float targetSpeed = IsRunning ? runSpeed : walkSpeed;

            // Camera-relative direction
            Vector3 moveDir = Vector3.zero;
            if (cameraTransform != null)
            {
                Vector3 camFwd = cameraTransform.forward;
                Vector3 camRight = cameraTransform.right;
                camFwd.y = 0f;
                camRight.y = 0f;
                camFwd.Normalize();
                camRight.Normalize();

                moveDir = (camFwd * inputDir.z + camRight * inputDir.x).normalized;
            }
            else
            {
                moveDir = inputDir;
            }

            IsMoving = moveDir.sqrMagnitude > 0.01f;

            if (IsMoving)
            {
                // Smooth rotation towards movement direction
                Quaternion targetRot = Quaternion.LookRotation(moveDir, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
            }

            // Gravity & Jumping
            if (characterController.isGrounded)
            {
                if (verticalVelocity < 0f)
                {
                    verticalVelocity = -2.0f; // Small downward force to stay grounded
                }

                if (Input.GetKeyDown(KeyCode.Space))
                {
                    verticalVelocity = Mathf.Sqrt(2f * jumpHeight * gravity);
                }
            }
            else
            {
                verticalVelocity -= gravity * Time.deltaTime;
            }

            Vector3 finalMove = (moveDir * targetSpeed) + Vector3.up * verticalVelocity;
            characterController.Move(finalMove * Time.deltaTime);
        }

        private void HandleInteractionScan()
        {
            nearestInteractable = null;
            float closestDist = float.MaxValue;

            Collider[] hits = Physics.OverlapSphere(transform.position + Vector3.up * 0.8f, interactionRadius);
            foreach (Collider hit in hits)
            {
                IInteractable interactable = hit.GetComponent<IInteractable>() ?? hit.GetComponentInParent<IInteractable>();
                if (interactable != null)
                {
                    float dist = Vector3.Distance(transform.position, interactable.Anchor.position);
                    if (dist < closestDist && dist <= interactable.Radius)
                    {
                        closestDist = dist;
                        nearestInteractable = interactable;
                    }
                }
            }
        }

        public void TryInteract()
        {
            if (nearestInteractable != null)
            {
                Debug.Log($"[PlayerController] Interacting with: {nearestInteractable.Id} ({nearestInteractable.Prompt})");
                nearestInteractable.Interact();
            }
        }

        public void SetCameraTransform(Transform cam)
        {
            cameraTransform = cam;
        }

        private void OnGUI()
        {
            if (nearestInteractable != null)
            {
                InitGUIStyles();

                float boxWidth = 320f;
                float boxHeight = 44f;
                float boxX = (Screen.width - boxWidth) * 0.5f;
                float boxY = Screen.height - 90f;

                GUI.Box(new Rect(boxX, boxY, boxWidth, boxHeight), GUIContent.none, promptBoxStyle);
                string text = $"<color=#f4c430>[E]</color>  {nearestInteractable.Prompt}";
                GUI.Label(new Rect(boxX, boxY + 8f, boxWidth, 28f), text, promptTextStyle);
            }
        }

        private void InitGUIStyles()
        {
            if (promptBoxStyle == null)
            {
                promptBoxStyle = new GUIStyle(GUI.skin.box);
            }

            if (promptTextStyle == null)
            {
                promptTextStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 14,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                    richText = true
                };
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.2f, 0.9f, 0.4f, 0.35f);
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.8f, interactionRadius);
        }
    }
}

