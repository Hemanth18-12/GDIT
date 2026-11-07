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





    }
}

