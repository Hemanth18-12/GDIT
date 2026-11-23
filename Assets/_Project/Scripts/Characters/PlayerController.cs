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
        [SerializeField] private float walkSpeed = 4.0f;    // a brisk jog; was 1.6 (felt far too slow)
        [SerializeField] private float runSpeed = 7.5f;     // Shift; was 4.2
        [SerializeField] private float crouchSpeed = 1.8f;  // was 1.1
        [SerializeField] private float acceleration = 28f;  // m/s per second: full run in about a quarter second
        /// <summary>Caps Lock toggles auto-run (keeps moving forward without holding W).</summary>
        public bool AutoRun { get; private set; }
        public float CurrentSpeed { get; private set; }
        public float WalkSpeed => walkSpeed; public float RunSpeed => runSpeed;
        [SerializeField] private float rotationSpeed = 12.0f;
        [SerializeField] private float jumpHeight = 0.7f; // ~0.7 s hop with gravity 11.4
        [SerializeField] private float gravity = 11.4f;

        [Header("Interaction")]
        [SerializeField] private float interactionRadius = 2.8f;

        [Header("Camera Reference")]
        [SerializeField] private Transform cameraTransform;

        [Header("Animation")]
        [SerializeField] private Animator animator;
        [Tooltip("Enable only after the confirmed character has real, validated motion clips. The old generated clips collapse its hips below the floor.")]
        [SerializeField] private bool useValidatedAnimation = true;
        public bool HasValidatedAnimation => useValidatedAnimation && animator != null && animator.enabled;

        private CharacterController characterController;
        private CharacterGrounding grounding;
        private CharacterTraversal traversal;
        private Vector3 moveVelocity;
        private float verticalVelocity;
        private float footstepTimer = 0f;
        private bool wasAirborne;
        private IInteractable nearestInteractable;
        private GUIStyle promptBoxStyle;
        private GUIStyle promptTextStyle;

        /// <summary>Current objective target (set by the story director); interaction prefers it.</summary>
        public static Transform ObjectiveFocus;
        public bool IsMoving { get; private set; }
        public bool IsRunning { get; private set; }
        public bool IsCrouching { get; private set; }
        public IInteractable CurrentInteractable => nearestInteractable;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            grounding = GetComponent<CharacterGrounding>();
            if (grounding == null) grounding = gameObject.AddComponent<CharacterGrounding>();
            traversal = GetComponent<CharacterTraversal>();
            if (traversal == null) traversal = gameObject.AddComponent<CharacterTraversal>();
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
            bool wallOpen = Time.frameCount <= InputGate.EnterReservedUntil; // Enter turns the Sun Wall centre wheel there
            if (kb != null && (kb.eKey.wasPressedThisFrame || (kb.enterKey.wasPressedThisFrame && !wallOpen)))
            {
                interactPressed = true;
            }
            var gp = Gamepad.current;
            if (gp != null && gp.buttonWest.wasPressedThisFrame)
            {
                interactPressed = true;
            }

            if (LegacyOfTemples.Core.VirtualInput.Down("interact")) interactPressed = true;
            if (LegacyOfTemples.Core.InputGate.GameplayBlocked) interactPressed = false; // cards, kits and dialogue own E and Enter
            if (interactPressed)
            {
                TryInteract();
            }
        }

        private const float CoyoteTime = 0.1f;
        private float lastGroundedTime = -1f;
        public int JumpCount { get; private set; }
        public float VerticalVelocity => verticalVelocity;

        // ---- hooks used by combat, chases and the on-screen controls ----
        /// <summary>Movement speed multiplier (blocking, attacking, grappling).</summary>
        public float SpeedScale { get; set; } = 1f;
        public Vector3 MoveDirection { get; private set; }
        private Vector3 pushVelocity; private float pushUntil, stumbleUntil;
        /// <summary>Short forced movement (dodge dash, attack step, knock-back).</summary>
        public void Push(Vector3 velocity, float seconds) { pushVelocity = velocity; pushUntil = Time.time + seconds; }
        public void Stumble(float seconds) { stumbleUntil = Time.time + seconds; }
        public void FaceTowards(Vector3 worldPoint)
        {
            Vector3 d = worldPoint - transform.position; d.y = 0f;
            if (d.sqrMagnitude > 0.001f) transform.rotation = Quaternion.LookRotation(d.normalized);
        }

        private void HandleMovement()
        {
            if (characterController == null) return;
            if (traversal.TickClimb()) { CurrentSpeed = 0f; IsMoving = false; return; }

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

            horizontal += LegacyOfTemples.Core.VirtualInput.Move.x; vertical += LegacyOfTemples.Core.VirtualInput.Move.y; // on-screen joystick
            if (kb != null && kb.capsLockKey.wasPressedThisFrame) AutoRun = !AutoRun;
            if (AutoRun && vertical < -0.1f) AutoRun = false;               // pulling back cancels auto-run
            if (AutoRun) vertical = Mathf.Max(vertical, 1f);
            if (LegacyOfTemples.Core.InputGate.GameplayBlocked) { horizontal = 0f; vertical = 0f; AutoRun = false; } // UI owns the keys
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

            if (LegacyOfTemples.Core.VirtualInput.Sprint) IsRunning = true;

            // Crouch (C)
            IsCrouching = LegacyOfTemples.Core.VirtualInput.CrouchHeld;
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
            targetSpeed *= SpeedScale * (Time.time < stumbleUntil ? 0.3f : 1f);

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

            IsMoving = moveDir.sqrMagnitude > 0.01f && targetSpeed > 0.05f;
            MoveDirection = moveDir;

            if (IsMoving)
            {
                Quaternion targetRot = Quaternion.LookRotation(moveDir, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
            }

            // Gravity & Jumping (Space)
            bool jumpPressed = false;
            bool uiOwnsInput = LegacyOfTemples.Core.InputGate.GameplayBlocked; // Space advances cards/dialogue there, never jumps
            if (!uiOwnsInput && kb != null && kb.spaceKey.wasPressedThisFrame) jumpPressed = true;
            if (!uiOwnsInput && gp != null && gp.buttonSouth.wasPressedThisFrame) jumpPressed = true;
            if (!uiOwnsInput && LegacyOfTemples.Core.VirtualInput.Down("jump")) jumpPressed = true;

            // Support only counts while not moving upward: a stale 'HasSupport' after a jump used to cancel gravity (hero floated up).
            bool isGrounded = characterController.isGrounded || (grounding != null && grounding.HasSupport && verticalVelocity <= 0f);

            if (isGrounded) lastGroundedTime = Time.time;
            bool canJump = isGrounded || (verticalVelocity <= 0f && Time.time - lastGroundedTime < CoyoteTime); // short coyote time vs. flicker
            if (jumpPressed && canJump && !IsCrouching)
            {
                if (isGrounded && traversal.TryClimb(moveDir)) { verticalVelocity = 0f; return; }
                verticalVelocity = Mathf.Sqrt(2f * jumpHeight * gravity); // once per press, never in the air
                grounding?.ClearSupport();
                lastGroundedTime = -1f;
                isGrounded = false;
                JumpCount++;
                AudioManager.Instance?.PlaySFX("sfx_cloth", 0.35f);
            }
            if (isGrounded)
            {
                if (verticalVelocity < 0f)
                {
                    verticalVelocity = -2.0f; // Small downward force to stay grounded
                }

            }
            else
            {
                verticalVelocity -= gravity * Time.deltaTime;
            }

            // quick acceleration instead of an instant jump to full speed; stopping is immediate
            CurrentSpeed = IsMoving ? Mathf.MoveTowards(CurrentSpeed, targetSpeed, acceleration * Time.deltaTime) : 0f;
            Vector3 finalMove = (moveDir * CurrentSpeed) + Vector3.up * verticalVelocity;
            if (Time.time < pushUntil) finalMove += new Vector3(pushVelocity.x, 0f, pushVelocity.z);
            // The capsule's slope and step limits decide what is walkable. A height
            // limit measured from spawn also blocked legitimate stairs and platforms.
            Vector3 beforeMove = transform.position;
            CollisionFlags collisions = characterController.Move(finalMove * Time.deltaTime);
            if ((collisions & CollisionFlags.Above) != 0 && verticalVelocity > 0f)
                verticalVelocity = 0f;
            if (verticalVelocity <= 0f && grounding.SnapToGround())
            {
                verticalVelocity = -2f;
                isGrounded = true;
            }
            traversal.Observe(beforeMove, moveDir, isGrounded);
            if (isGrounded && wasAirborne) GameSfx.Footstep(transform.position, 0.65f);
            wasAirborne = !isGrounded;

            // Footsteps
            if (isGrounded && IsMoving)
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
                // The Locomotion blend tree expects 0..1 (Idle 0, Walking 0.33, Running 0.67). It used to receive metres per
                // second (1.6 / 4.2), which always picked the Running clip and played it in slow motion: the "moonwalk".
                // Map the real speed onto the tree: 1.65 m/s = the Walking clip's own speed, 4.24 m/s = the Running clip's.
                float s = IsMoving ? CurrentSpeed : 0f;
                float blend = s <= 1.65f ? 0.33f * s / 1.65f : Mathf.Min(0.6667f, 0.33f + (s - 1.65f) / (4.24f - 1.65f) * 0.3367f);
                animator.SetFloat("Speed", blend, 0.08f, Time.deltaTime);
                // Match clip playback to real ground speed using the clip's own average root speed (less foot sliding).
                float playRate = 1f;
                if (IsMoving && isGrounded)
                {
                    // play the clips as fast as the feet really travel: weighted root speed of the blended clips
                    float clipSpeed = 0f, wsum = 0f;
                    foreach (var ci in animator.GetCurrentAnimatorClipInfo(0)) { clipSpeed += ci.weight * new Vector2(ci.clip.averageSpeed.x, ci.clip.averageSpeed.z).magnitude; wsum += ci.weight; }
                    if (wsum > 0.01f) clipSpeed /= wsum;
                    if (clipSpeed > 0.3f) playRate = Mathf.Clamp(CurrentSpeed / clipSpeed, 0.6f, 1.9f);
                }
                animator.speed = Mathf.MoveTowards(animator.speed, playRate, Time.deltaTime * 4f);
                animator.SetBool("Grounded", isGrounded);
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

            Collider[] hits = Physics.OverlapSphere(transform.position + Vector3.up * 0.8f, Mathf.Max(interactionRadius, 4f));
            foreach (Collider hit in hits)
            {
                IInteractable interactable = hit.GetComponent<IInteractable>() ?? hit.GetComponentInParent<IInteractable>();
                if (interactable is Behaviour beh && !beh.isActiveAndEnabled) continue;   // switched off (e.g. the robbers during the fight)
                if (interactable != null)
                {
                    Vector3 to = interactable.Anchor.position - transform.position;
                    float dist = to.magnitude;
                    if (dist > interactable.Radius) continue;
                    // Score = distance weighted by facing (what you look at wins) and by "already examined".
                    Vector3 face = transform.forward;
                    face.y = 0f; to.y = 0f;
                    float dot = (face.sqrMagnitude > 0.001f && to.sqrMagnitude > 0.001f) ? Vector3.Dot(face.normalized, to.normalized) : 1f;
                    float score = dist * (2f - dot);
                    if (interactable is MonoBehaviour mb && mb != null)
                    {
                        var f = mb.GetType().GetProperty("IsExamined");
                        if (f != null && f.PropertyType == typeof(bool) && (bool)f.GetValue(mb)) score *= 1.6f;
                    }
                    Transform anchor = interactable.Anchor;
                    if (ObjectiveFocus != null && anchor != null && (anchor == ObjectiveFocus || anchor.IsChildOf(ObjectiveFocus) || ObjectiveFocus.IsChildOf(anchor))) score *= 0.35f; // current goal first
                    else if (interactable is NPCDialogueTrigger) score *= 0.75f;                 // then people
                    else if (interactable.GetType().Name == "InteractableObject") score *= 1.5f; // decorations last
                    if (score < closestDist)
                    {
                        closestDist = score;
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
            verticalVelocity = 0f; wasAirborne = false;
            characterController.enabled = true;
            Physics.SyncTransforms();
            grounding.SnapToGround(true);
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
