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

        private CharacterController characterController;
        private Vector3 moveVelocity;
        private float verticalVelocity;
        private IInteractable nearestInteractable;
        private GUIStyle promptBoxStyle;
        private GUIStyle promptTextStyle;

        // Procedural Locomotion Bones (Legs, Knees, Arms)
        private Transform bonePelvis;
        private Transform boneHipL;
        private Transform boneKneeL;
        private Transform boneFootL;
        private Transform boneHipR;
        private Transform boneKneeR;
        private Transform boneFootR;
        private Transform boneSpine;
        private Transform boneArmL;
        private Transform boneArmR;
        private float walkCyclePhase = 0f;
        private float currentLocomotionWeight = 0f;

        public bool IsMoving { get; private set; }
        public bool IsRunning { get; private set; }
        public bool IsCrouching { get; private set; }
        public IInteractable CurrentInteractable => nearestInteractable;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }
            if (cameraTransform == null && Camera.main != null)
            {
                cameraTransform = Camera.main.transform;
            }
            CacheBones();
        }

        private void CacheBones()
        {
            Transform[] allChildren = GetComponentsInChildren<Transform>(true);
            foreach (var t in allChildren)
            {
                string n = t.name.ToLowerInvariant();
                if (n == "bip_pelvis" || n.EndsWith("hips")) bonePelvis = t;
                else if (n == "bip_hip_l" || n.EndsWith("leftupleg")) boneHipL = t;
                else if (n == "bip_knee_l" || n.EndsWith("leftleg")) boneKneeL = t;
                else if (n == "bip_foot_l" || n.EndsWith("leftfoot")) boneFootL = t;
                else if (n == "bip_hip_r" || n.EndsWith("rightupleg")) boneHipR = t;
                else if (n == "bip_knee_r" || n.EndsWith("rightleg")) boneKneeR = t;
                else if (n == "bip_foot_r" || n.EndsWith("rightfoot")) boneFootR = t;
                else if (n == "bip_spine_0" || n.EndsWith("spine")) boneSpine = t;
                else if (n == "bip_upperarm_l" || n.EndsWith("leftarm")) boneArmL = t;
                else if (n == "bip_upperarm_r" || n.EndsWith("rightarm")) boneArmR = t;
            }
        }

        private void LateUpdate()
        {
            ApplyProceduralLocomotion();
        }

        private void ApplyProceduralLocomotion()
        {
            float speed = characterController != null ? characterController.velocity.magnitude : 0f;
            bool isActuallyMoving = IsMoving || speed > 0.15f;

            if (isActuallyMoving)
            {
                float freq = IsRunning ? 13.0f : (IsCrouching ? 5.5f : 8.5f);
                walkCyclePhase += Time.deltaTime * freq;
                if (walkCyclePhase > Mathf.PI * 2f) walkCyclePhase -= Mathf.PI * 2f;
            }

            float targetWeight = isActuallyMoving ? 1f : 0f;
            currentLocomotionWeight = Mathf.MoveTowards(currentLocomotionWeight, targetWeight, Time.deltaTime * 7f);

            if (currentLocomotionWeight > 0.001f)
            {
                float w = walkCyclePhase;
                float strideMax = IsRunning ? 44.0f : (IsCrouching ? 20.0f : 28.0f);
                float lSwing = Mathf.Sin(w) * strideMax * currentLocomotionWeight;
                float rSwing = Mathf.Sin(w + Mathf.PI) * strideMax * currentLocomotionWeight;

                float kneeBendMax = IsRunning ? 75.0f : (IsCrouching ? 45.0f : 55.0f);
                float lKneeBend = -Mathf.Max(0f, -Mathf.Sin(w)) * kneeBendMax * currentLocomotionWeight;
                float rKneeBend = -Mathf.Max(0f, -Mathf.Sin(w + Mathf.PI)) * kneeBendMax * currentLocomotionWeight;

                if (IsCrouching)
                {
                    lKneeBend -= 28f * currentLocomotionWeight;
                    rKneeBend -= 28f * currentLocomotionWeight;
                }

                if (boneHipL != null) boneHipL.localRotation *= Quaternion.Euler(lSwing, 0f, 0f);
                if (boneKneeL != null) boneKneeL.localRotation *= Quaternion.Euler(lKneeBend, 0f, 0f);
                if (boneHipR != null) boneHipR.localRotation *= Quaternion.Euler(rSwing, 0f, 0f);
                if (boneKneeR != null) boneKneeR.localRotation *= Quaternion.Euler(rKneeBend, 0f, 0f);

                // Arm swing opposite to legs
                float armSwingMax = IsRunning ? 38.0f : (IsCrouching ? 14.0f : 24.0f);
                float lArmSwing = -Mathf.Sin(w) * armSwingMax * currentLocomotionWeight;
                float rArmSwing = Mathf.Sin(w) * armSwingMax * currentLocomotionWeight;

                if (boneArmL != null) boneArmL.localRotation *= Quaternion.Euler(lArmSwing, 0f, 0f);
                if (boneArmR != null) boneArmR.localRotation *= Quaternion.Euler(rArmSwing, 0f, 0f);
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
            if (kb != null && kb.cKey.isPressed)
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

            if (animator != null)
            {
                animator.SetBool("IsMoving", IsMoving);
                animator.SetBool("IsRunning", IsRunning);
                animator.SetBool("IsCrouching", IsCrouching);
                animator.SetFloat("Speed", IsMoving ? (IsCrouching ? crouchSpeed : (IsRunning ? runSpeed : walkSpeed)) : 0f);
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
