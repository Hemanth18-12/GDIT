using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using LegacyOfTemples.Core;
using LegacyOfTemples.Gameplay;

namespace LegacyOfTemples.UI
{
    public class TutorialManager : MonoBehaviour
    {
        private static TutorialManager instance;
        public static TutorialManager Instance
        {
            get
            {
                if (instance == null)
                {
                    GameObject go = new GameObject("TutorialManager");
                    instance = go.AddComponent<TutorialManager>();
                    DontDestroyOnLoad(go);
                }
                return instance;
            }
        }

        public enum TutorialStep
        {
            None = 0,
            Welcome = 1,
            LookAround = 2,
            Walk = 3,
            Run = 4,
            Jump = 5,
            Crouch = 6,
            Interact = 7,
            InspectClue = 8,
            TalkToPriest = 9,
            OpenJournal = 10,
            OpenMap = 11,
            OpenHelp = 12,
            Completed = 13
        }

        private TutorialStep currentStep = TutorialStep.None;
        private bool isTutorialActive = false;
        private Vector2 lastMousePos;
        private float mouseMovementAccumulator = 0f;
        private float stepHoldTimer = 0f;

        public bool IsActive => isTutorialActive;
        public TutorialStep CurrentStep => currentStep;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            // If New Game was requested or first play, trigger tutorial
            int tutorialDone = PlayerPrefs.GetInt("Tutorial_Completed", 0);
            int forceTutorial = PlayerPrefs.GetInt("Force_Tutorial_On_Start", 1);
            if (tutorialDone == 0 || forceTutorial == 1)
            {
                PlayerPrefs.SetInt("Force_Tutorial_On_Start", 0);
                PlayerPrefs.Save();
                StartCoroutine(DeferredStartTutorial());
            }
        }

        private IEnumerator DeferredStartTutorial()
        {
            yield return new WaitForSeconds(0.5f);
            StartTutorial();
        }

        public void StartTutorial()
        {
            isTutorialActive = true;
            currentStep = TutorialStep.Welcome;
            stepHoldTimer = 0f;
            mouseMovementAccumulator = 0f;
            ShowStep(currentStep);
        }

        public void SkipTutorial()
        {
            isTutorialActive = false;
            currentStep = TutorialStep.Completed;
            PlayerPrefs.SetInt("Tutorial_Completed", 1);
            PlayerPrefs.Save();
            UIManager.Instance.HideTutorial();
            UIManager.Instance.ShowHowToPlay(OnHowToPlayDismissed);
        }

        public void ReplayTutorial()
        {
            StartTutorial();
        }

        private void Update()
        {
            if (!isTutorialActive || currentStep == TutorialStep.None || currentStep == TutorialStep.Completed)
                return;

            stepHoldTimer += Time.deltaTime;
            CheckStepAction();
        }

        private void ShowStep(TutorialStep step)
        {
            int stepNum = (int)step;
            int total = 12;

            switch (step)
            {
                case TutorialStep.Welcome:
                    UIManager.Instance.ShowTutorialStep(
                        stepNum, total,
                        "THE SACRED INVESTIGATION",
                        "[ ENTER / SPACE ]",
                        "Three sacred fictional heritage relics have been stolen from the Puri Jagannath temple storehouse. As Detective Aryan, you must find them.",
                        "Press ENTER or Space to begin tutorial..."
                    );
                    break;

                case TutorialStep.LookAround:
                    UIManager.Instance.ShowTutorialStep(
                        stepNum, total,
                        "LOOK AROUND",
                        "[ MOUSE / RIGHT STICK ]",
                        "Move the mouse to rotate the camera and survey the grand temple courtyard and stone vimanas.",
                        "Move your mouse to continue..."
                    );
                    mouseMovementAccumulator = 0f;
                    break;

                case TutorialStep.Walk:
                    UIManager.Instance.ShowTutorialStep(
                        stepNum, total,
                        "WALK & EXPLORE",
                        "[ W ]  [ A ]  [ S ]  [ D ]",
                        "Use the W, A, S, and D keys to walk around the stone plaza with full humanoid locomotion.",
                        "Press W, A, S, or D to walk..."
                    );
                    break;

                case TutorialStep.Run:
                    UIManager.Instance.ShowTutorialStep(
                        stepNum, total,
                        "HIGH SPEED SPRINT",
                        "[ LEFT SHIFT ]",
                        "Hold Left Shift while moving forward to sprint across the courtyard plaza.",
                        "Hold Shift + W to sprint..."
                    );
                    break;

                case TutorialStep.Jump:
                    UIManager.Instance.ShowTutorialStep(
                        stepNum, total,
                        "TEMPLE STEPS & JUMP",
                        "[ SPACE ]",
                        "Press Space to jump over obstacles or ascend broken stone platforms.",
                        "Press SPACE to jump..."
                    );
                    break;

                case TutorialStep.Crouch:
                    UIManager.Instance.ShowTutorialStep(
                        stepNum, total,
                        "STEALTH CROUCH",
                        "[ C ] / [ CTRL ]",
                        "Press C or Left Control to crouch low to examine ground-level clues and stealthily track footprints.",
                        "Press C to crouch..."
                    );
                    break;

                case TutorialStep.Interact:
                    UIManager.Instance.ShowTutorialStep(
                        stepNum, total,
                        "EXAMINE & INTERACT",
                        "[ E ]",
                        "Walk up to glowing brass oil lamps or guardian statues and press E to interact.",
                        "Press E to interact..."
                    );
                    break;

                case TutorialStep.InspectClue:
                    UIManager.Instance.ShowTutorialStep(
                        stepNum, total,
                        "FORENSIC INSPECTION",
                        "[ F ] / [ E ]",
                        "When standing near a forensic marker, press F or E to examine the clue details.",
                        "Press F or E near a clue..."
                    );
                    break;

                case TutorialStep.TalkToPriest:
                    UIManager.Instance.ShowTutorialStep(
                        stepNum, total,
                        "QUESTION WITNESSES",
                        "[ E ]",
                        "Approach Head Priest Ananda at the courtyard steps and press E to hear his testimony.",
                        "Press E to speak with Priest Ananda..."
                    );
                    break;

                case TutorialStep.OpenJournal:
                    UIManager.Instance.ShowTutorialStep(
                        stepNum, total,
                        "INVESTIGATION JOURNAL",
                        "[ J ]",
                        "Press J at any time to review discovered clues, suspect dossiers, and the 3 stolen relics.",
                        "Press J to open Journal..."
                    );
                    break;

                case TutorialStep.OpenMap:
                    UIManager.Instance.ShowTutorialStep(
                        stepNum, total,
                        "TEMPLE ARCHITECTURAL MAP",
                        "[ M ]",
                        "Press M to inspect the temple layout, mandapas, shrines, and your current location.",
                        "Press M to open Map..."
                    );
                    break;

                case TutorialStep.OpenHelp:
                    UIManager.Instance.ShowTutorialStep(
                        stepNum, total,
                        "CONTROLS & HINTS",
                        "[ H ]",
                        "Press H if you ever get stuck to reveal progressive multi-tier hints.",
                        "Press H to open Hints..."
                    );
                    break;
            }
        }

        private void CheckStepAction()
        {
            var kb = Keyboard.current;
            var mouse = Mouse.current;
            var gp = Gamepad.current;

            bool stepCompleted = false;

            switch (currentStep)
            {
                case TutorialStep.Welcome:
                    if (stepHoldTimer > 0.4f && kb != null && (kb.enterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame))
                        stepCompleted = true;
                    if (gp != null && gp.buttonSouth.wasPressedThisFrame)
                        stepCompleted = true;
                    break;

                case TutorialStep.LookAround:
                    if (mouse != null)
                    {
                        Vector2 delta = mouse.delta.ReadValue();
                        mouseMovementAccumulator += delta.magnitude;
                        if (mouseMovementAccumulator > 80f)
                            stepCompleted = true;
                    }
                    if (gp != null && gp.rightStick.ReadValue().sqrMagnitude > 0.2f)
                        stepCompleted = true;
                    break;

                case TutorialStep.Walk:
                    if (kb != null && (kb.wKey.isPressed || kb.aKey.isPressed || kb.sKey.isPressed || kb.dKey.isPressed))
                        stepCompleted = true;
                    if (gp != null && gp.leftStick.ReadValue().sqrMagnitude > 0.2f)
                        stepCompleted = true;
                    break;

                case TutorialStep.Run:
                    if (kb != null && (kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed))
                        stepCompleted = true;
                    if (gp != null && gp.leftStickButton.isPressed)
                        stepCompleted = true;
                    break;

                case TutorialStep.Jump:
                    if (kb != null && kb.spaceKey.wasPressedThisFrame)
                        stepCompleted = true;
                    if (gp != null && gp.buttonSouth.wasPressedThisFrame)
                        stepCompleted = true;
                    break;

                case TutorialStep.Crouch:
                    if (kb != null && (kb.cKey.wasPressedThisFrame || kb.leftCtrlKey.wasPressedThisFrame))
                        stepCompleted = true;
                    if (gp != null && gp.buttonEast.wasPressedThisFrame)
                        stepCompleted = true;
                    break;

                case TutorialStep.Interact:
                    if (kb != null && kb.eKey.wasPressedThisFrame)
                        stepCompleted = true;
                    if (gp != null && gp.buttonWest.wasPressedThisFrame)
                        stepCompleted = true;
                    break;

                case TutorialStep.InspectClue:
                    if (kb != null && (kb.fKey.wasPressedThisFrame || kb.eKey.wasPressedThisFrame))
                        stepCompleted = true;
                    break;

                case TutorialStep.TalkToPriest:
                    if (kb != null && (kb.eKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame))
                        stepCompleted = true;
                    break;

                case TutorialStep.OpenJournal:
                    if (kb != null && kb.jKey.wasPressedThisFrame)
                        stepCompleted = true;
                    break;

                case TutorialStep.OpenMap:
                    if (kb != null && kb.mKey.wasPressedThisFrame)
                        stepCompleted = true;
                    break;

                case TutorialStep.OpenHelp:
                    if (kb != null && kb.hKey.wasPressedThisFrame)
                        stepCompleted = true;
                    break;
            }

            if (stepCompleted)
            {
                AdvanceStep();
            }
        }

        public void AdvanceStep()
        {
            AudioManager.Instance.PlaySFX("sfx_ui_click");
            stepHoldTimer = 0f;
            int next = (int)currentStep + 1;
            if (next <= 12)
            {
                currentStep = (TutorialStep)next;
                ShowStep(currentStep);
            }
            else
            {
                // Completed all 12 steps!
                isTutorialActive = false;
                currentStep = TutorialStep.Completed;
                PlayerPrefs.SetInt("Tutorial_Completed", 1);
                PlayerPrefs.Save();
                UIManager.Instance.HideTutorial();
                UIManager.Instance.ShowHowToPlay(OnHowToPlayDismissed);
            }
        }

        private void OnHowToPlayDismissed()
        {
            UIManager.Instance.SetObjective(
                "Speak with Head Priest Ananda",
                "Head Priest Ananda stands at the courtyard temple steps. Question him regarding the midnight break-in.",
                "Stolen Relics: 0/3  |  Clues: 0/5"
            );
        }
    }
}
