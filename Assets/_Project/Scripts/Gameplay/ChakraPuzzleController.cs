using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using LegacyOfTemples.Core;

namespace LegacyOfTemples.Gameplay
{
    public class ChakraPuzzleController : MonoBehaviour, IInteractable
    {
        [Header("Puzzle Wheels")]
        [SerializeField] private Transform outerWheel;
        [SerializeField] private Transform middleWheel;
        [SerializeField] private Transform innerWheel;

        [Header("Gateway Door")]
        [SerializeField] private Transform stoneGatewayDoor;
        [SerializeField] private Vector3 doorOpenOffset = new Vector3(0f, 5.0f, 0f);

        [Header("Target Angles (Euler Z)")]
        [SerializeField] private float outerTargetAngle = 0f;
        [SerializeField] private float middleTargetAngle = 90f;
        [SerializeField] private float innerTargetAngle = 180f;
        [SerializeField] private float toleranceAngle = 8f;

        [Header("Rotation Tuning")]
        [SerializeField] private float turnSpeed = 90f;
        [SerializeField] private float snapSpeed = 6f;

        [Header("Audio")]
        [SerializeField] private AudioClip stoneGrindSound;
        [SerializeField] private AudioClip alignmentChimeSound;
        [SerializeField] private AudioClip doorOpenSound;

        private AudioSource audioSource;
        private float curOuterAngle = 135f;
        private float curMiddleAngle = 225f;
        private float curInnerAngle = 45f;
        private int selectedRing = 0; // 0: Outer, 1: Middle, 2: Inner
        private bool isInteracting = false;
        private bool isSolved = false;
        private Vector3 initialDoorPos;
        private Vector3 targetDoorPos;

        private GUIStyle panelStyle;
        private GUIStyle titleStyle;
        private GUIStyle ringSelectStyle;
        private GUIStyle statusStyle;
        private GUIStyle hintStyle;

        public string Id => "puzzle_chakra_wall";
        public string Prompt => isSolved ? "Chakra Seal Aligned (Proceed through Gateway)" : "Solve Surya Chakra Puzzle";
        public Transform Anchor => transform;
        public float Radius => 3.5f;
        public bool IsSolved => isSolved;

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
            }

            if (stoneGatewayDoor != null)
            {
                initialDoorPos = stoneGatewayDoor.position;
                targetDoorPos = initialDoorPos;
            }
        }

        private void Start()
        {
            // Scramble initial angles if not already solved
            if (PlayerPrefs.GetInt("ChakraPuzzle_Solved", 0) == 1)
            {
                curOuterAngle = outerTargetAngle;
                curMiddleAngle = middleTargetAngle;
                curInnerAngle = innerTargetAngle;
                isSolved = true;
                if (stoneGatewayDoor != null) targetDoorPos = initialDoorPos + doorOpenOffset;
            }
            ApplyWheelRotationsImmediate();
        }

        private void Update()
        {
            if (isInteracting && !isSolved)
            {
                HandlePuzzleInput();
            }

            // Animate door opening
            if (stoneGatewayDoor != null && isSolved)
            {
                stoneGatewayDoor.position = Vector3.Lerp(stoneGatewayDoor.position, targetDoorPos, Time.deltaTime * 2f);
            }
        }

        public void Interact()
        {
            EventBus.RaisePlayerInteract(this);
            if (!isSolved)
            {
                isInteracting = !isInteracting;
            }
            else
            {
                // If solved, transition to JunglePath
                SceneManager.LoadScene("JunglePath");
            }
        }

        private void HandlePuzzleInput()
        {
            var kb = Keyboard.current;
            if (kb == null) return;

            // Ring selection: [1], [2], [3] or Up/Down
            if (kb.digit1Key.wasPressedThisFrame) selectedRing = 0;
            if (kb.digit2Key.wasPressedThisFrame) selectedRing = 1;
            if (kb.digit3Key.wasPressedThisFrame) selectedRing = 2;
            if (kb.upArrowKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame)
            {
                selectedRing = (selectedRing + 2) % 3;
            }
            if (kb.downArrowKey.wasPressedThisFrame || kb.sKey.wasPressedThisFrame)
            {
                selectedRing = (selectedRing + 1) % 3;
            }

            // Rotation input: [A]/[D] or [Left]/[Right] or [Q]/[E]
            float rotInput = 0f;
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed || kb.qKey.isPressed) rotInput -= 1f;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed || kb.eKey.isPressed) rotInput += 1f;

            if (Mathf.Abs(rotInput) > 0.01f)
            {
                float delta = rotInput * turnSpeed * Time.deltaTime;
                switch (selectedRing)
                {
                    case 0:
                        curOuterAngle = Mathf.Repeat(curOuterAngle + delta, 360f);
                        break;
                    case 1:
                        curMiddleAngle = Mathf.Repeat(curMiddleAngle + delta, 360f);
                        break;
                    case 2:
                        curInnerAngle = Mathf.Repeat(curInnerAngle + delta, 360f);
                        break;
                }
                ApplyWheelRotations();
                CheckAlignment();
            }

            // Exit puzzle mode with Escape
            if (kb.escapeKey.wasPressedThisFrame)
            {
                isInteracting = false;
            }
        }

        private void ApplyWheelRotationsImmediate()
        {
            if (outerWheel != null) outerWheel.localRotation = Quaternion.Euler(0, 0, curOuterAngle);
            if (middleWheel != null) middleWheel.localRotation = Quaternion.Euler(0, 0, curMiddleAngle);
            if (innerWheel != null) innerWheel.localRotation = Quaternion.Euler(0, 0, curInnerAngle);
        }

        private void ApplyWheelRotations()
        {
            if (outerWheel != null)
                outerWheel.localRotation = Quaternion.Slerp(outerWheel.localRotation, Quaternion.Euler(0, 0, curOuterAngle), Time.deltaTime * snapSpeed);
            if (middleWheel != null)
                middleWheel.localRotation = Quaternion.Slerp(middleWheel.localRotation, Quaternion.Euler(0, 0, curMiddleAngle), Time.deltaTime * snapSpeed);
            if (innerWheel != null)
                innerWheel.localRotation = Quaternion.Slerp(innerWheel.localRotation, Quaternion.Euler(0, 0, curInnerAngle), Time.deltaTime * snapSpeed);
        }

        private void CheckAlignment()
        {
            bool outerAligned = Mathf.Abs(Mathf.DeltaAngle(curOuterAngle, outerTargetAngle)) <= toleranceAngle;
            bool middleAligned = Mathf.Abs(Mathf.DeltaAngle(curMiddleAngle, middleTargetAngle)) <= toleranceAngle;
            bool innerAligned = Mathf.Abs(Mathf.DeltaAngle(curInnerAngle, innerTargetAngle)) <= toleranceAngle;

            if (outerAligned && middleAligned && innerAligned && !isSolved)
            {
                SolvePuzzle();
            }
        }

        public void SolvePuzzle()
        {
            isSolved = true;
            isInteracting = false;
            curOuterAngle = outerTargetAngle;
            curMiddleAngle = middleTargetAngle;
            curInnerAngle = innerTargetAngle;
            ApplyWheelRotationsImmediate();

            PlayerPrefs.SetInt("ChakraPuzzle_Solved", 1);
            PlayerPrefs.SetInt("LevelUnlocked_3", 1);
            PlayerPrefs.Save();

            if (stoneGatewayDoor != null)
            {
                targetDoorPos = initialDoorPos + doorOpenOffset;
            }

            if (doorOpenSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(doorOpenSound);
            }

            EventBus.RaisePuzzleSolved("chakra_wall");
            Debug.Log("[ChakraPuzzleController] SURYA CHAKRA ALIGNED! Ancient stone gateway unsealed!");
        }

        private void OnGUI()
        {
            if (!isInteracting && !isSolved) return;

            InitStyles();

            if (isInteracting && !isSolved)
            {
                float w = 580f;
                float h = 260f;
                float x = (Screen.width - w) * 0.5f;
                float y = Screen.height - h - 30f;

                GUI.Box(new Rect(x, y, w, h), GUIContent.none, panelStyle);

                GUI.Label(new Rect(x + 20f, y + 14f, w - 40f, 24f), "ANCIENT SURYA CHAKRA PUZZLE MECHANISM", titleStyle);

                bool outerAligned = Mathf.Abs(Mathf.DeltaAngle(curOuterAngle, outerTargetAngle)) <= toleranceAngle;
                bool middleAligned = Mathf.Abs(Mathf.DeltaAngle(curMiddleAngle, middleTargetAngle)) <= toleranceAngle;
                bool innerAligned = Mathf.Abs(Mathf.DeltaAngle(curInnerAngle, innerTargetAngle)) <= toleranceAngle;

                string oText = $"[1] Outer Solar Disc (12 Spokes): {(outerAligned ? "<color=#4cd137>ALIGNED ✓</color>" : "<color=#e1b12c>Misaligned</color>")}";
                string mText = $"[2] Middle Lunar Arc (8 Phases): {(middleAligned ? "<color=#4cd137>ALIGNED ✓</color>" : "<color=#e1b12c>Misaligned</color>")}";
                string iText = $"[3] Inner Lotus Core (Sun Center): {(innerAligned ? "<color=#4cd137>ALIGNED ✓</color>" : "<color=#e1b12c>Misaligned</color>")}";

                float textY = y + 48f;
                GUI.Label(new Rect(x + 24f, textY, w - 48f, 22f), (selectedRing == 0 ? $"<color=#00d2d3>▶ </color><b>{oText}</b>" : oText), ringSelectStyle);
                GUI.Label(new Rect(x + 24f, textY + 28f, w - 48f, 22f), (selectedRing == 1 ? $"<color=#00d2d3>▶ </color><b>{mText}</b>" : mText), ringSelectStyle);
                GUI.Label(new Rect(x + 24f, textY + 56f, w - 48f, 22f), (selectedRing == 2 ? $"<color=#00d2d3>▶ </color><b>{iText}</b>" : iText), ringSelectStyle);

                // Guidance
                GUI.Label(new Rect(x + 24f, y + 140f, w - 48f, 38f), "Use [1], [2], [3] to select a stone ring.\nHold [A]/[D] or [Left]/[Right] to rotate until all three notches lock into solar symmetry.", statusStyle);

                // Quick solve dev shortcut for testing
                if (GUI.Button(new Rect(x + w - 160f, y + 142f, 140f, 26f), "Auto-Align [Dev]"))
                {
                    SolvePuzzle();
                }

                GUI.Label(new Rect(x + 20f, y + h - 26f, w - 40f, 20f), "Press [Esc] to Step Away from Mechanism", hintStyle);
            }
            else if (isSolved)
            {
                float bw = 540f;
                float bh = 70f;
                float bx = (Screen.width - bw) * 0.5f;
                float by = 30f;

                GUI.Box(new Rect(bx, by, bw, bh), GUIContent.none, panelStyle);
                GUI.Label(new Rect(bx + 10f, by + 10f, bw - 20f, 24f), "<color=#4cd137>★ SURYA CHAKRA ALIGNED — GATEWAY UNSEALED ★</color>", titleStyle);
                GUI.Label(new Rect(bx + 10f, by + 36f, bw - 20f, 24f), "The stone door has opened. Walk through to track Kala and Rudra into the Jungle!", hintStyle);
            }
        }

        private void InitStyles()
        {
            if (panelStyle == null) panelStyle = new GUIStyle(GUI.skin.box);

            if (titleStyle == null)
            {
                titleStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 15,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                    richText = true,
                    normal = { textColor = new Color(0.96f, 0.82f, 0.28f) }
                };
            }

            if (ringSelectStyle == null)
            {
                ringSelectStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 13,
                    richText = true,
                    normal = { textColor = Color.white }
                };
            }

            if (statusStyle == null)
            {
                statusStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 12,
                    wordWrap = true,
                    normal = { textColor = new Color(0.85f, 0.85f, 0.85f) }
                };
            }

            if (hintStyle == null)
            {
                hintStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 11,
                    alignment = TextAnchor.MiddleCenter,
                    richText = true,
                    normal = { textColor = new Color(0.7f, 0.7f, 0.7f) }
                };
            }
        }
    }
}

