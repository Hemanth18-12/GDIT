using System;
using UnityEngine;
using UnityEngine.InputSystem;
using LegacyOfTemples.Core;

namespace LegacyOfTemples.Characters
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Locomotion")]
        [SerializeField] private float walkSpeed = 4.5f;
        [SerializeField] private float runSpeed = 7.5f;
        [SerializeField] private float crouchSpeed = 2.5f;
        [SerializeField] private float rotationSpeed = 12.0f;
        [SerializeField] private float jumpHeight = 1.2f;
        [SerializeField] private float gravity = 18.0f;

        [Header("Interaction")]
        [SerializeField] private float interactionRadius = 2.8f;

        [Header("Camera Reference")]
        [SerializeField] private Transform cameraTransform;

        [Header("Animation")]
        [SerializeField] private Animator animator;
        [Tooltip("Enable only after the confirmed character has real, validated motion clips. The old generated clips collapse its hips below the floor.")]
        [SerializeField] private bool useValidatedAnimation = false;
        public bool HasValidatedAnimation => useValidatedAnimation && animator != null && animator.enabled;

        private CharacterController characterController;
        private CharacterGrounding grounding;
        private Vector3 moveVelocity;
        private float verticalVelocity;
        private float footstepTimer = 0f;
        private IInteractable nearestInteractable;
        private GUIStyle promptBoxStyle;
        private GUIStyle promptTextStyle;

        public bool IsMoving { get; private set; }
        public bool IsRunning { get; private set; }
        public bool IsCrouching { get; private set; }
        public IInteractable CurrentInteractable => nearestInteractable;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            grounding = GetComponent<CharacterGrounding>();
            if (grounding == null) grounding = gameObject.AddComponent<CharacterGrounding>();
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }
            if (animator != null && !useValidatedAnimation) animator.enabled = false;
            if (cameraTransform == null && Camera.main != null)
            {
                cameraTransform = Camera.main.transform;
            }
        }

        private void Start()
        {
            grounding.SnapToGround(true);
            if (cameraTransform == null && Camera.main != null)
            {
                cameraTransform = Camera.main.transform;
            }
        }

        private void Update()
        {
            HandleMovement();
            HandleInteractionScan();

            bool interactPressed = false;
            var kb = Keyboard.current;
            if (kb != null && (kb.eKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame))
            {
                interactPressed = true;
            }
            var gp = Gamepad.current;
            if (gp != null && gp.buttonWest.wasPressedThisFrame)
            {
                interactPressed = true;
            }

            if (interactPressed)
            {
                TryInteract();
            }
        }

        private void HandleMovement()
        {
            if (characterController == null) return;

            float horizontal = 0f;
            float vertical = 0f;

            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.wKey.isPressed || kb.upArrowKey.isPressed) vertical += 1f;
                if (kb.sKey.isPressed || kb.downArrowKey.isPressed) vertical -= 1f;
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) horizontal += 1f;
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) horizontal -= 1f;
            }

            var gp = Gamepad.current;
            if (gp != null)
            {
                Vector2 stick = gp.leftStick.ReadValue();
                if (stick.sqrMagnitude > 0.01f)
                {
                    horizontal += stick.x;
                    vertical += stick.y;
                }
            }

            Vector3 inputDir = new Vector3(horizontal, 0f, vertical).normalized;

            // Sprint (Shift)
            IsRunning = false;
            if (kb != null && (kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed))
            {
                IsRunning = true;
            }
            if (gp != null && (gp.leftStickButton.isPressed || gp.rightTrigger.isPressed))
            {
                IsRunning = true;
            }

            // Crouch (C)
            IsCrouching = false;
            if (kb != null && (kb.cKey.isPressed || kb.leftCtrlKey.isPressed || kb.rightCtrlKey.isPressed))
            {
                IsCrouching = true;
            }
            if (gp != null && (gp.buttonEast.isPressed || gp.rightStickButton.isPressed))
            {
                IsCrouching = true;
            }

            float targetSpeed = walkSpeed;
            if (IsCrouching) targetSpeed = crouchSpeed;
            else if (IsRunning) targetSpeed = runSpeed;

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
                Quaternion targetRot = Quaternion.LookRotation(moveDir, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
            }

            // Gravity & Jumping (Space)
            bool jumpPressed = false;
            if (kb != null && kb.spaceKey.wasPressedThisFrame) jumpPressed = true;
            if (gp != null && gp.buttonSouth.wasPressedThisFrame) jumpPressed = true;

            if (characterController.isGrounded)
            {
                if (verticalVelocity < 0f)
                {
                    verticalVelocity = -2.0f; // Small downward force to stay grounded
                }

                if (jumpPressed && !IsCrouching)
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
            if (verticalVelocity <= 0f && grounding.SnapToGround()) verticalVelocity = -2f;

            // Footsteps
            if (characterController.isGrounded && IsMoving)
            {
                footstepTimer -= Time.deltaTime;
                if (footstepTimer <= 0f)
                {
                    AudioManager.Instance.PlayFootstep();
                    footstepTimer = IsCrouching ? 0.55f : (IsRunning ? 0.30f : 0.45f);
                }
            }
            else
            {
                footstepTimer = 0.1f;
            }

            if (animator != null)
            {
                float animSpeed = IsMoving ? (IsCrouching ? crouchSpeed : (IsRunning ? runSpeed : walkSpeed)) : 0f;
                animator.SetFloat("Speed", animSpeed);
                animator.SetBool("Grounded", characterController.isGrounded);
                animator.SetBool("Crouch", IsCrouching);
                animator.SetBool("Jump", jumpPressed);
                animator.SetFloat("MoveX", inputDir.x);
                animator.SetFloat("MoveZ", inputDir.z);
                animator.SetBool("IsMoving", IsMoving);
                animator.SetBool("IsRunning", IsRunning);
                animator.SetBool("IsCrouching", IsCrouching);
            }
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

            if (nearestInteractable != null)
            {
                EventBus.RaiseShowPrompt("E", nearestInteractable.Prompt);
            }
            else
            {
                EventBus.RaiseHidePrompt();
            }
        }

        public void TryInteract()
        {
            if (nearestInteractable != null)
            {
                Debug.Log($"[PlayerController] Interacting with: {nearestInteractable.Id} ({nearestInteractable.Prompt})");
                if (animator != null)
                {
                    animator.SetTrigger("Interact");
                }
                nearestInteractable.Interact();
            }
        }

        public void SetCameraTransform(Transform cam)
        {
            cameraTransform = cam;
        }

        public void Teleport(Vector3 position, Quaternion rotation)
        {
            characterController.enabled = false;
            transform.SetPositionAndRotation(position, rotation);
            verticalVelocity = 0f;
            characterController.enabled = true;
            Physics.SyncTransforms();
            grounding.SnapToGround(true);
        }

        private void OnGUI()
        {
            // Suppressed in favor of high-resolution TextMeshPro UIManager
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
