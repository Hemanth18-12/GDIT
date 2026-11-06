using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LegacyOfTemples.Gameplay
{
    public class TutorialPromptManager : MonoBehaviour
    {
        public static TutorialPromptManager Instance { get; private set; }

        private enum TutorialStep
        {
            Movement,
            CameraLook,
            SprintCrouch,
            Interaction,
            JournalAndMap,
            Completed
        }

        private TutorialStep currentStep = TutorialStep.Movement;
        private float stepTimer = 0f;
        private Vector3 lastPlayerPos;
        private Transform playerTransform;

        private GUIStyle promptBoxStyle;
        private GUIStyle promptTextStyle;
        private GUIStyle promptKeyStyle;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            FindPlayer();
        }

        private void FindPlayer()
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
                lastPlayerPos = playerTransform.position;
            }
        }

        private void Update()
        {
            if (playerTransform == null)
            {
                FindPlayer();
                return;
            }

            stepTimer += Time.deltaTime;

            switch (currentStep)
            {
                case TutorialStep.Movement:
                    if (Vector3.Distance(playerTransform.position, lastPlayerPos) > 3.0f)
                    {
                        AdvanceStep(TutorialStep.CameraLook);
                    }
                    break;

                case TutorialStep.CameraLook:
                    if (Mouse.current != null && Mouse.current.delta.ReadValue().sqrMagnitude > 40f)
                    {
                        AdvanceStep(TutorialStep.SprintCrouch);
                    }
                    if (Gamepad.current != null && Gamepad.current.rightStick.ReadValue().sqrMagnitude > 0.3f)
                    {
                        AdvanceStep(TutorialStep.SprintCrouch);
                    }
                    if (stepTimer > 10f)
                    {
                        AdvanceStep(TutorialStep.SprintCrouch);
                    }
                    break;

                case TutorialStep.SprintCrouch:
                    var kb = Keyboard.current;
                    if (kb != null && (kb.leftShiftKey.isPressed || kb.cKey.isPressed))
                    {
                        AdvanceStep(TutorialStep.Interaction);
                    }
                    var gp = Gamepad.current;
                    if (gp != null && (gp.leftStickButton.isPressed || gp.buttonEast.isPressed))
                    {
                        AdvanceStep(TutorialStep.Interaction);
                    }
                    if (stepTimer > 12f)
                    {
                        AdvanceStep(TutorialStep.Interaction);
                    }
                    break;

                case TutorialStep.Interaction:
                    if (stepTimer > 15f)
                    {
                        AdvanceStep(TutorialStep.JournalAndMap);
                    }
                    break;

                case TutorialStep.JournalAndMap:
                    var kbJ = Keyboard.current;
                    if (kbJ != null && (kbJ.jKey.wasPressedThisFrame || kbJ.mKey.wasPressedThisFrame))
                    {
                        AdvanceStep(TutorialStep.Completed);
                    }
                    if (stepTimer > 20f)
                    {
                        AdvanceStep(TutorialStep.Completed);
                    }
                    break;
            }
        }

        private void AdvanceStep(TutorialStep nextStep)
        {
            currentStep = nextStep;
            stepTimer = 0f;
            if (playerTransform != null) lastPlayerPos = playerTransform.position;
        }

        private void OnGUI()
        {
            // Always show compact quick keys pill in top right corner
            InitStyles();
            DrawHUDQuickKeys();

            if (currentStep == TutorialStep.Completed) return;

            string message = "";
            string keyLabel = "";

            switch (currentStep)
            {
                case TutorialStep.Movement:
                    keyLabel = "[W] [A] [S] [D]";
                    message = "to walk Detective Aryan through the Courtyard";
                    break;

                case TutorialStep.CameraLook:
                    keyLabel = "[Mouse] or [Right Stick]";
                    message = "to orbit and survey the temple grounds";
                    break;

                case TutorialStep.SprintCrouch:
                    keyLabel = "[Shift] to Sprint  •  [C] to Crouch";
                    message = "for tactical movement across the complex";
                    break;

                case TutorialStep.Interaction:
                    keyLabel = "[E] to Interact";
                    message = "Examine broken locks, footprints, or speak with witnesses";
                    break;

                case TutorialStep.JournalAndMap:
                    keyLabel = "[J] Dossier  •  [M] Map  •  [H] Hints";
                    message = "Review case clues, architectural layout, and next leads";
                    break;
            }

            if (!string.IsNullOrEmpty(keyLabel))
            {
                float bannerWidth = 520f;
                float bannerHeight = 44f;
                float bannerX = (Screen.width - bannerWidth) * 0.5f;
                float bannerY = 24f;

                GUI.Box(new Rect(bannerX, bannerY, bannerWidth, bannerHeight), GUIContent.none, promptBoxStyle);
                string fullText = $"<color=#f4c430><b>{keyLabel}</b></color>  —  {message}";
                GUI.Label(new Rect(bannerX + 10f, bannerY + 10f, bannerWidth - 20f, 24f), fullText, promptTextStyle);
            }
        }

        private void DrawHUDQuickKeys()
        {
            float w = 290f;
            float h = 26f;
            float x = Screen.width - w - 16f;
            float y = 14f;

            GUI.Box(new Rect(x, y, w, h), GUIContent.none, promptBoxStyle);
            string quickText = "<color=#f4c430>[J]</color> Dossier   <color=#f4c430>[M]</color> Map   <color=#f4c430>[H]</color> Hints   <color=#f4c430>[1-5]</color> Dev";
            GUI.Label(new Rect(x, y + 3f, w, 20f), quickText, promptKeyStyle);
        }

        private void InitStyles()
        {
            if (promptBoxStyle == null)
            {
                promptBoxStyle = new GUIStyle(GUI.skin.box);
            }

            if (promptTextStyle == null)
            {
                promptTextStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 13,
                    alignment = TextAnchor.MiddleCenter,
                    richText = true,
                    normal = { textColor = Color.white }
                };
            }

            if (promptKeyStyle == null)
            {
                promptKeyStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 11,
                    alignment = TextAnchor.MiddleCenter,
                    richText = true,
                    normal = { textColor = new Color(0.9f, 0.9f, 0.9f) }
                };
            }
        }
    }
}

