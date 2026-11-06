using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using LegacyOfTemples.Core;
using LegacyOfTemples.Gameplay;

namespace LegacyOfTemples.UI
{
    public class UIManager : MonoBehaviour
    {
        private static UIManager instance;
        public static UIManager Instance
        {
            get
            {
                if (instance == null)
                {
                    GameObject go = new GameObject("UIManager");
                    instance = go.AddComponent<UIManager>();
                    DontDestroyOnLoad(go);
                }
                return instance;
            }
        }

        [Header("Font Assets")]
        private TMP_FontAsset defaultFont;

        // Canvas & Scaler
        private Canvas mainCanvas;
        private CanvasScaler canvasScaler;

        // UI Panels
        private GameObject hudPanel;
        private TextMeshProUGUI objectiveTitleText;
        private TextMeshProUGUI objectiveBodyText;
        private TextMeshProUGUI objectiveCounterText;

        private GameObject promptPanel;
        private TextMeshProUGUI promptKeyText;
        private TextMeshProUGUI promptActionText;

        private GameObject tutorialPanel;
        private TextMeshProUGUI tutorialStepText;
        private TextMeshProUGUI tutorialTitleText;
        private TextMeshProUGUI tutorialKeycapText;
        private TextMeshProUGUI tutorialBodyText;
        private TextMeshProUGUI tutorialActionPromptText;

        private GameObject howToPlayPanel;
        private Action onHowToPlayDismiss;

        private GameObject dialoguePanel;
        private TextMeshProUGUI dialogueSpeakerText;
        private TextMeshProUGUI dialoguePortraitInitial;
        private TextMeshProUGUI dialogueBodyText;
        private TextMeshProUGUI dialoguePromptText;
        private GameObject dialogueChoicesContainer;
        private Button[] choiceButtons;
        private TextMeshProUGUI[] choiceButtonTexts;

        private GameObject pausePanel;
        private Slider masterSlider;
        private Slider musicSlider;
        private Slider sfxSlider;
        private TextMeshProUGUI textSizeLabel;

        // Dialogue State
        private bool isDialogueActive = false;
        private string[] activeDialogueLines;
        private int currentDialogueLine = 0;
        private bool isTypewriting = false;
        private Coroutine typewriterCoroutine;
        private string activeFullLine = "";
        private Action onDialogueCompletedCallback;
        private Action<int> onChoiceCallback;

        // Text Scale
        private float currentFontMultiplier = 1.0f;
        private int textScaleIndex = 1; // 0: Small (0.85), 1: Med (1.0), 2: Large (1.15), 3: XL (1.30)
        private readonly float[] fontMultipliers = new float[] { 0.85f, 1.0f, 1.15f, 1.30f };
        private readonly string[] fontScaleNames = new string[] { "Small (85%)", "Medium (100%)", "Large (115%)", "Extra Large (130%)" };

        public bool IsDialogueActive => isDialogueActive;
        public bool IsTutorialActive => tutorialPanel != null && tutorialPanel.activeSelf;
        public bool IsPaused => pausePanel != null && pausePanel.activeSelf;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);

            LoadFont();
            textScaleIndex = PlayerPrefs.GetInt("Settings_TextScaleIndex", 1);
            currentFontMultiplier = fontMultipliers[Mathf.Clamp(textScaleIndex, 0, fontMultipliers.Length - 1)];

            BuildCanvasHierarchy();
            SubscribeEvents();
        }

        private void OnDestroy()
        {
            UnsubscribeEvents();
        }

        private void LoadFont()
        {
            defaultFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            if (defaultFont == null)
            {
                // Fallback to standard TMP font
                defaultFont = TMP_Settings.defaultFontAsset;
            }
        }

        private void SubscribeEvents()
        {
            EventBus.OnClueFound += HandleClueFound;
            EventBus.OnPuzzleSolved += HandlePuzzleSolved;
            EventBus.OnDialogueStart += HandleDialogueEvent;
            EventBus.OnRequestDialogue += StartDialogue;
            EventBus.OnShowPrompt += ShowInteractionPrompt;
            EventBus.OnHidePrompt += HideInteractionPrompt;
            EventBus.OnUpdateObjective += SetObjective;
        }

        private void UnsubscribeEvents()
        {
            EventBus.OnClueFound -= HandleClueFound;
            EventBus.OnPuzzleSolved -= HandlePuzzleSolved;
            EventBus.OnDialogueStart -= HandleDialogueEvent;
            EventBus.OnRequestDialogue -= StartDialogue;
            EventBus.OnShowPrompt -= ShowInteractionPrompt;
            EventBus.OnHidePrompt -= HideInteractionPrompt;
            EventBus.OnUpdateObjective -= SetObjective;
        }

        private void Update()
        {
            // Pause Toggle (Esc)
            var kb = Keyboard.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame)
            {
                if (howToPlayPanel != null && howToPlayPanel.activeSelf)
                {
                    DismissHowToPlay();
                }
                else if (isDialogueActive)
                {
                    // Do nothing or skip
                }
                else
                {
                    TogglePause();
                }
            }

            // Dialogue Advance (Space, Enter, E)
            if (isDialogueActive && (dialogueChoicesContainer == null || !dialogueChoicesContainer.activeSelf))
            {
                bool advance = false;
                if (kb != null && (kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame || kb.eKey.wasPressedThisFrame))
                {
                    advance = true;
                }
                var gp = Gamepad.current;
                if (gp != null && gp.buttonSouth.wasPressedThisFrame)
                {
                    advance = true;
                }

                if (advance)
                {
                    if (isTypewriting)
                    {
                        // Instantly finish line
                        if (typewriterCoroutine != null) StopCoroutine(typewriterCoroutine);
                        dialogueBodyText.text = activeFullLine;
                        isTypewriting = false;
                    }
                    else
                    {
                        AdvanceDialogue();
                    }
                }
            }

            // How To Play dismiss (Enter / Space)
            if (howToPlayPanel != null && howToPlayPanel.activeSelf)
            {
                if (kb != null && (kb.enterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame))
                {
                    DismissHowToPlay();
                }
            }
        }

        #region Hierarchy Builder

        private void BuildCanvasHierarchy()
        {
            // 1. Root Canvas
            GameObject canvasGO = new GameObject("LOT_MainCanvas");
            canvasGO.transform.parent = transform;
            mainCanvas = canvasGO.AddComponent<Canvas>();
            mainCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            mainCanvas.sortingOrder = 100;

            canvasScaler = canvasGO.AddComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(1920, 1080);
            canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            canvasScaler.matchWidthOrHeight = 0.5f;

            canvasGO.AddComponent<GraphicRaycaster>();

            // 2. HUD / Objective Panel (Top-Left)
            BuildHUDPanel(canvasGO.transform);

            // 3. Interaction Prompt Panel (Bottom-Center)
            BuildPromptPanel(canvasGO.transform);

            // 4. Guided Tutorial Panel (Center Modal)
            BuildTutorialPanel(canvasGO.transform);

            // 5. How To Play Mission Card (Center Modal)
            BuildHowToPlayPanel(canvasGO.transform);

            // 6. Dialogue Panel (Bottom-Center)
            BuildDialoguePanel(canvasGO.transform);

            // 7. Pause & Settings Menu (Center Modal)
            BuildPausePanel(canvasGO.transform);
        }

        private Sprite CreateColorSprite(Color color)
        {
            Texture2D tex = new Texture2D(4, 4);
            Color[] pix = new Color[16];
            for (int i = 0; i < 16; i++) pix[i] = color;
            tex.SetPixels(pix);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f));
        }

        private void BuildHUDPanel(Transform parent)
        {
            hudPanel = new GameObject("HUD_ObjectivePanel");
            hudPanel.transform.SetParent(parent, false);

            RectTransform rt = hudPanel.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(32f, -32f);
            rt.sizeDelta = new Vector2(520f, 160f);

            Image bg = hudPanel.AddComponent<Image>();
            bg.sprite = CreateColorSprite(new Color(0.06f, 0.07f, 0.09f, 0.88f));

            // Gold accent strip on left
            GameObject strip = new GameObject("AccentStrip");
            strip.transform.SetParent(hudPanel.transform, false);
            RectTransform srt = strip.AddComponent<RectTransform>();
            srt.anchorMin = new Vector2(0f, 0f);
            srt.anchorMax = new Vector2(0f, 1f);
            srt.pivot = new Vector2(0f, 0.5f);
            srt.sizeDelta = new Vector2(6f, 0f);
            srt.anchoredPosition = Vector2.zero;
            Image stripImg = strip.AddComponent<Image>();
            stripImg.color = new Color(0.96f, 0.77f, 0.2f);

            // Header TMP (>= 32 px)
            GameObject titleObj = new GameObject("ObjectiveTitle");
            titleObj.transform.SetParent(hudPanel.transform, false);
            objectiveTitleText = titleObj.AddComponent<TextMeshProUGUI>();
            objectiveTitleText.font = defaultFont;
            objectiveTitleText.fontSize = 32f * currentFontMultiplier;
            objectiveTitleText.fontStyle = FontStyles.Bold;
            objectiveTitleText.color = new Color(0.98f, 0.82f, 0.28f);
            objectiveTitleText.text = "CURRENT OBJECTIVE";
            RectTransform trt = titleObj.GetComponent<RectTransform>();
            trt.anchorMin = new Vector2(0f, 1f);
            trt.anchorMax = new Vector2(1f, 1f);
            trt.pivot = new Vector2(0f, 1f);
            trt.anchoredPosition = new Vector2(20f, -14f);
            trt.sizeDelta = new Vector2(-40f, 36f);

            // Body TMP (>= 28 px)
            GameObject bodyObj = new GameObject("ObjectiveBody");
            bodyObj.transform.SetParent(hudPanel.transform, false);
            objectiveBodyText = bodyObj.AddComponent<TextMeshProUGUI>();
            objectiveBodyText.font = defaultFont;
            objectiveBodyText.fontSize = 28f * currentFontMultiplier;
            objectiveBodyText.color = new Color(0.94f, 0.92f, 0.88f);
            objectiveBodyText.text = "Speak with Head Priest Ananda at the courtyard steps.";
            objectiveBodyText.textWrappingMode = TextWrappingModes.Normal;
            RectTransform brt = bodyObj.GetComponent<RectTransform>();
            brt.anchorMin = new Vector2(0f, 0f);
            brt.anchorMax = new Vector2(1f, 1f);
            brt.pivot = new Vector2(0f, 1f);
            brt.anchoredPosition = new Vector2(20f, -18f);
            brt.sizeDelta = new Vector2(-40f, -65f);

            // Counter TMP
            GameObject counterObj = new GameObject("ObjectiveCounter");
            counterObj.transform.SetParent(hudPanel.transform, false);
            objectiveCounterText = counterObj.AddComponent<TextMeshProUGUI>();
            objectiveCounterText.font = defaultFont;
            objectiveCounterText.fontSize = 22f * currentFontMultiplier;
            objectiveCounterText.fontStyle = FontStyles.Italic;
            objectiveCounterText.color = new Color(0.75f, 0.70f, 0.60f);
            objectiveCounterText.text = "Stolen Relics Recovered: 0/3  |  Clues Found: 0/5";
            RectTransform crt = counterObj.GetComponent<RectTransform>();
            crt.anchorMin = new Vector2(0f, 0f);
            crt.anchorMax = new Vector2(1f, 0f);
            crt.pivot = new Vector2(0f, 0f);
            crt.anchoredPosition = new Vector2(20f, 10f);
            crt.sizeDelta = new Vector2(-40f, 26f);
        }

        private void BuildPromptPanel(Transform parent)
        {
            promptPanel = new GameObject("Prompt_InteractionPanel");
            promptPanel.transform.SetParent(parent, false);

            RectTransform rt = promptPanel.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0f, 75f);
            rt.sizeDelta = new Vector2(520f, 68f);

            Image bg = promptPanel.AddComponent<Image>();
            bg.sprite = CreateColorSprite(new Color(0.08f, 0.09f, 0.12f, 0.92f));

            // Keycap Box
            GameObject keyObj = new GameObject("PromptKeycap");
            keyObj.transform.SetParent(promptPanel.transform, false);
            RectTransform krt = keyObj.AddComponent<RectTransform>();
            krt.anchorMin = new Vector2(0f, 0.5f);
            krt.anchorMax = new Vector2(0f, 0.5f);
            krt.pivot = new Vector2(0f, 0.5f);
            krt.anchoredPosition = new Vector2(16f, 0f);
            krt.sizeDelta = new Vector2(60f, 48f);
            Image keyBg = keyObj.AddComponent<Image>();
            keyBg.color = new Color(0.24f, 0.20f, 0.12f);

            GameObject keyTextObj = new GameObject("KeyText");
            keyTextObj.transform.SetParent(keyObj.transform, false);
            promptKeyText = keyTextObj.AddComponent<TextMeshProUGUI>();
            promptKeyText.font = defaultFont;
            promptKeyText.fontSize = 32f * currentFontMultiplier;
            promptKeyText.fontStyle = FontStyles.Bold;
            promptKeyText.alignment = TextAlignmentOptions.Center;
            promptKeyText.color = new Color(1.0f, 0.84f, 0.3f);
            promptKeyText.text = "E";
            RectTransform ktrt = keyTextObj.GetComponent<RectTransform>();
            ktrt.anchorMin = Vector2.zero;
            ktrt.anchorMax = Vector2.one;
            ktrt.sizeDelta = Vector2.zero;

            // Prompt Action Text (>= 32 px)
            GameObject actionObj = new GameObject("PromptAction");
            actionObj.transform.SetParent(promptPanel.transform, false);
            promptActionText = actionObj.AddComponent<TextMeshProUGUI>();
            promptActionText.font = defaultFont;
            promptActionText.fontSize = 32f * currentFontMultiplier;
            promptActionText.fontStyle = FontStyles.Bold;
            promptActionText.alignment = TextAlignmentOptions.MidlineLeft;
            promptActionText.color = Color.white;
            promptActionText.text = "Talk to Priest Ananda";
            RectTransform art = actionObj.GetComponent<RectTransform>();
            art.anchorMin = new Vector2(0f, 0f);
            art.anchorMax = new Vector2(1f, 1f);
            art.pivot = new Vector2(0f, 0.5f);
            art.anchoredPosition = new Vector2(90f, 0f);
            art.sizeDelta = new Vector2(-105f, 0f);

            promptPanel.SetActive(false);
        }

        private void BuildTutorialPanel(Transform parent)
        {
            tutorialPanel = new GameObject("Tutorial_ModalPanel");
            tutorialPanel.transform.SetParent(parent, false);

            RectTransform rt = tutorialPanel.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0f, 20f);
            rt.sizeDelta = new Vector2(1040f, 500f);

            Image bg = tutorialPanel.AddComponent<Image>();
            bg.sprite = CreateColorSprite(new Color(0.06f, 0.07f, 0.09f, 0.96f));

            // Header Banner
            GameObject header = new GameObject("TutorialHeader");
            header.transform.SetParent(tutorialPanel.transform, false);
            RectTransform hrt = header.AddComponent<RectTransform>();
            hrt.anchorMin = new Vector2(0f, 1f);
            hrt.anchorMax = new Vector2(1f, 1f);
            hrt.pivot = new Vector2(0.5f, 1f);
            hrt.anchoredPosition = Vector2.zero;
            hrt.sizeDelta = new Vector2(0f, 75f);
            Image hbg = header.AddComponent<Image>();
            hbg.color = new Color(0.18f, 0.14f, 0.08f);

            // Step Progress TMP
            GameObject stepObj = new GameObject("TutorialStepText");
            stepObj.transform.SetParent(header.transform, false);
            tutorialStepText = stepObj.AddComponent<TextMeshProUGUI>();
            tutorialStepText.font = defaultFont;
            tutorialStepText.fontSize = 28f * currentFontMultiplier;
            tutorialStepText.fontStyle = FontStyles.Bold;
            tutorialStepText.color = new Color(0.96f, 0.77f, 0.2f);
            tutorialStepText.text = "TUTORIAL · STEP 1 OF 12";
            tutorialStepText.alignment = TextAlignmentOptions.MidlineLeft;
            RectTransform srt = stepObj.GetComponent<RectTransform>();
            srt.anchorMin = new Vector2(0f, 0f);
            srt.anchorMax = new Vector2(1f, 1f);
            srt.anchoredPosition = new Vector2(30f, 0f);
            srt.sizeDelta = new Vector2(-60f, 0f);

            // Big Title TMP (>= 56 px)
            GameObject titleObj = new GameObject("TutorialTitle");
            titleObj.transform.SetParent(tutorialPanel.transform, false);
            tutorialTitleText = titleObj.AddComponent<TextMeshProUGUI>();
            tutorialTitleText.font = defaultFont;
            tutorialTitleText.fontSize = 56f * currentFontMultiplier;
            tutorialTitleText.fontStyle = FontStyles.Bold;
            tutorialTitleText.color = new Color(1.0f, 0.88f, 0.45f);
            tutorialTitleText.alignment = TextAlignmentOptions.Center;
            tutorialTitleText.text = "WELCOME TO PURI";
            RectTransform trt = titleObj.GetComponent<RectTransform>();
            trt.anchorMin = new Vector2(0f, 1f);
            trt.anchorMax = new Vector2(1f, 1f);
            trt.pivot = new Vector2(0.5f, 1f);
            trt.anchoredPosition = new Vector2(0f, -95f);
            trt.sizeDelta = new Vector2(-60f, 70f);

            // Large Keycap Display TMP
            GameObject keycapObj = new GameObject("TutorialKeycap");
            keycapObj.transform.SetParent(tutorialPanel.transform, false);
            tutorialKeycapText = keycapObj.AddComponent<TextMeshProUGUI>();
            tutorialKeycapText.font = defaultFont;
            tutorialKeycapText.fontSize = 44f * currentFontMultiplier;
            tutorialKeycapText.fontStyle = FontStyles.Bold;
            tutorialKeycapText.alignment = TextAlignmentOptions.Center;
            tutorialKeycapText.color = new Color(0.98f, 0.82f, 0.28f);
            tutorialKeycapText.text = "[ W ]  [ A ]  [ S ]  [ D ]";
            RectTransform krt = keycapObj.GetComponent<RectTransform>();
            krt.anchorMin = new Vector2(0f, 0.5f);
            krt.anchorMax = new Vector2(1f, 0.5f);
            krt.pivot = new Vector2(0.5f, 0.5f);
            krt.anchoredPosition = new Vector2(0f, 25f);
            krt.sizeDelta = new Vector2(-60f, 60f);

            // Body TMP (>= 32 px)
            GameObject bodyObj = new GameObject("TutorialBody");
            bodyObj.transform.SetParent(tutorialPanel.transform, false);
            tutorialBodyText = bodyObj.AddComponent<TextMeshProUGUI>();
            tutorialBodyText.font = defaultFont;
            tutorialBodyText.fontSize = 32f * currentFontMultiplier;
            tutorialBodyText.alignment = TextAlignmentOptions.Center;
            tutorialBodyText.color = new Color(0.94f, 0.92f, 0.88f);
            tutorialBodyText.textWrappingMode = TextWrappingModes.Normal;
            tutorialBodyText.text = "Three sacred fictional heritage relics have vanished overnight from the temple storage chamber.";
            RectTransform brt = bodyObj.GetComponent<RectTransform>();
            brt.anchorMin = new Vector2(0f, 0f);
            brt.anchorMax = new Vector2(1f, 0.5f);
            brt.pivot = new Vector2(0.5f, 1f);
            brt.anchoredPosition = new Vector2(0f, -20f);
            brt.sizeDelta = new Vector2(-80f, 120f);

            // Action / Pulsing Prompt TMP
            GameObject promptObj = new GameObject("TutorialActionPrompt");
            promptObj.transform.SetParent(tutorialPanel.transform, false);
            tutorialActionPromptText = promptObj.AddComponent<TextMeshProUGUI>();
            tutorialActionPromptText.font = defaultFont;
            tutorialActionPromptText.fontSize = 28f * currentFontMultiplier;
            tutorialActionPromptText.fontStyle = FontStyles.Bold;
            tutorialActionPromptText.alignment = TextAlignmentOptions.Center;
            tutorialActionPromptText.color = new Color(0.4f, 0.88f, 0.4f);
            tutorialActionPromptText.text = "Press ENTER to continue...";
            RectTransform prt = promptObj.GetComponent<RectTransform>();
            prt.anchorMin = new Vector2(0f, 0f);
            prt.anchorMax = new Vector2(1f, 0f);
            prt.pivot = new Vector2(0.5f, 0f);
            prt.anchoredPosition = new Vector2(0f, 25f);
            prt.sizeDelta = new Vector2(-60f, 40f);

            tutorialPanel.SetActive(false);
        }

        private void BuildHowToPlayPanel(Transform parent)
        {
            howToPlayPanel = new GameObject("HowToPlay_MissionCard");
            howToPlayPanel.transform.SetParent(parent, false);

            RectTransform rt = howToPlayPanel.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(1120f, 640f);

            Image bg = howToPlayPanel.AddComponent<Image>();
            bg.sprite = CreateColorSprite(new Color(0.06f, 0.07f, 0.09f, 0.98f));

            // Title (>= 56 px)
            GameObject titleObj = new GameObject("CardTitle");
            titleObj.transform.SetParent(howToPlayPanel.transform, false);
            TextMeshProUGUI title = titleObj.AddComponent<TextMeshProUGUI>();
            title.font = defaultFont;
            title.fontSize = 56f * currentFontMultiplier;
            title.fontStyle = FontStyles.Bold;
            title.alignment = TextAlignmentOptions.Center;
            title.color = new Color(1.0f, 0.84f, 0.3f);
            title.text = "MISSION: THE SACRED RECOVERY";
            RectTransform trt = titleObj.GetComponent<RectTransform>();
            trt.anchorMin = new Vector2(0f, 1f);
            trt.anchorMax = new Vector2(1f, 1f);
            trt.pivot = new Vector2(0.5f, 1f);
            trt.anchoredPosition = new Vector2(0f, -30f);
            trt.sizeDelta = new Vector2(-60f, 70f);

            // Mission Statement & Story Goals (>= 30 px)
            GameObject bodyObj = new GameObject("CardBody");
            bodyObj.transform.SetParent(howToPlayPanel.transform, false);
            TextMeshProUGUI body = bodyObj.AddComponent<TextMeshProUGUI>();
            body.font = defaultFont;
            body.fontSize = 30f * currentFontMultiplier;
            body.alignment = TextAlignmentOptions.TopLeft;
            body.color = new Color(0.92f, 0.90f, 0.86f);
            body.textWrappingMode = TextWrappingModes.Normal;
            body.text =
                "<b>INVESTIGATION BRIEF:</b>\n" +
                "Three sacred fictional heritage relics have been stolen overnight from the Jagannath Temple storehouse:\n" +
                "  <color=#f4c430>1.</color> <b>Sacred Pradip Brass Vessel</b> (16th-century ceremonial Aarti lamp)\n" +
                "  <color=#f4c430>2.</color> <b>Carved Sandstone Rosette</b> (Ancient 12th-century Surya medallion)\n" +
                "  <color=#f4c430>3.</color> <b>Ancient Tala Patra Palm-Leaf Manuscript</b> (Sacred architecture text)\n\n" +
                "<b>HOW TO SUCCEED:</b>\n" +
                "• <b>Inspect Clues:</b> Approach glowing forensic markers and press <color=#f4c430>[E]</color> or <color=#f4c430>[F]</color>.\n" +
                "• <b>Question Witnesses:</b> Talk to Head Priest Ananda and eyewitness Meera in the courtyard.\n" +
                "• <b>Solve Temple Puzzles:</b> Align the Surya Chakra stones to reveal the thieves' trail.\n" +
                "• <b>Recover All 3 Relics:</b> Confront antiquities smugglers Vikram and Dev at the jungle ruins.";

            RectTransform brt = bodyObj.GetComponent<RectTransform>();
            brt.anchorMin = new Vector2(0f, 0f);
            brt.anchorMax = new Vector2(1f, 1f);
            brt.pivot = new Vector2(0.5f, 0.5f);
            brt.anchoredPosition = new Vector2(50f, -20f);
            brt.sizeDelta = new Vector2(-100f, -200f);

            // Dismiss Button / Prompt
            GameObject contObj = new GameObject("ContinuePrompt");
            contObj.transform.SetParent(howToPlayPanel.transform, false);
            TextMeshProUGUI contText = contObj.AddComponent<TextMeshProUGUI>();
            contText.font = defaultFont;
            contText.fontSize = 32f * currentFontMultiplier;
            contText.fontStyle = FontStyles.Bold;
            contText.alignment = TextAlignmentOptions.Center;
            contText.color = new Color(0.4f, 0.88f, 0.4f);
            contText.text = "Press ENTER or Space to Begin Your Investigation";
            RectTransform crt = contObj.GetComponent<RectTransform>();
            crt.anchorMin = new Vector2(0f, 0f);
            crt.anchorMax = new Vector2(1f, 0f);
            crt.pivot = new Vector2(0.5f, 0f);
            crt.anchoredPosition = new Vector2(0f, 35f);
            crt.sizeDelta = new Vector2(-60f, 45f);

            howToPlayPanel.SetActive(false);
        }

        private void BuildDialoguePanel(Transform parent)
        {
            dialoguePanel = new GameObject("Dialogue_Panel");
            dialoguePanel.transform.SetParent(parent, false);

            RectTransform rt = dialoguePanel.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0f, 40f);
            rt.sizeDelta = new Vector2(1280f, 290f);

            Image bg = dialoguePanel.AddComponent<Image>();
            bg.sprite = CreateColorSprite(new Color(0.06f, 0.07f, 0.09f, 0.95f));

            // Speaker Portrait Box
            GameObject portObj = new GameObject("PortraitBox");
            portObj.transform.SetParent(dialoguePanel.transform, false);
            RectTransform prt = portObj.AddComponent<RectTransform>();
            prt.anchorMin = new Vector2(0f, 0.5f);
            prt.anchorMax = new Vector2(0f, 0.5f);
            prt.pivot = new Vector2(0f, 0.5f);
            prt.anchoredPosition = new Vector2(25f, 0f);
            prt.sizeDelta = new Vector2(140f, 180f);
            Image portBg = portObj.AddComponent<Image>();
            portBg.color = new Color(0.22f, 0.18f, 0.12f);

            GameObject portInitialObj = new GameObject("PortraitInitial");
            portInitialObj.transform.SetParent(portObj.transform, false);
            TextMeshProUGUI portInitial = portInitialObj.AddComponent<TextMeshProUGUI>();
            dialoguePortraitInitial = portInitial;
            portInitial.font = defaultFont;
            portInitial.fontSize = 60f * currentFontMultiplier;
            portInitial.fontStyle = FontStyles.Bold;
            portInitial.alignment = TextAlignmentOptions.Center;
            portInitial.color = new Color(0.96f, 0.77f, 0.2f);
            portInitial.text = "★";
            RectTransform pirt = portInitialObj.GetComponent<RectTransform>();
            pirt.anchorMin = Vector2.zero;
            pirt.anchorMax = Vector2.one;
            pirt.sizeDelta = Vector2.zero;

            // Speaker Name TMP (>= 36 px)
            GameObject nameObj = new GameObject("SpeakerName");
            nameObj.transform.SetParent(dialoguePanel.transform, false);
            dialogueSpeakerText = nameObj.AddComponent<TextMeshProUGUI>();
            dialogueSpeakerText.font = defaultFont;
            dialogueSpeakerText.fontSize = 36f * currentFontMultiplier;
            dialogueSpeakerText.fontStyle = FontStyles.Bold;
            dialogueSpeakerText.color = new Color(1.0f, 0.84f, 0.3f);
            dialogueSpeakerText.text = "Priest Ananda";
            RectTransform nrt = nameObj.GetComponent<RectTransform>();
            nrt.anchorMin = new Vector2(0f, 1f);
            nrt.anchorMax = new Vector2(1f, 1f);
            nrt.pivot = new Vector2(0f, 1f);
            nrt.anchoredPosition = new Vector2(190f, -20f);
            nrt.sizeDelta = new Vector2(-220f, 44f);

            // Dialogue Body TMP (>= 34 px)
            GameObject bodyObj = new GameObject("DialogueBody");
            bodyObj.transform.SetParent(dialoguePanel.transform, false);
            dialogueBodyText = bodyObj.AddComponent<TextMeshProUGUI>();
            dialogueBodyText.font = defaultFont;
            dialogueBodyText.fontSize = 34f * currentFontMultiplier;
            dialogueBodyText.color = new Color(0.95f, 0.93f, 0.89f);
            dialogueBodyText.textWrappingMode = TextWrappingModes.Normal;
            dialogueBodyText.text = "Namaskaram, Detective Aryan...";
            RectTransform brt = bodyObj.GetComponent<RectTransform>();
            brt.anchorMin = new Vector2(0f, 0f);
            brt.anchorMax = new Vector2(1f, 1f);
            brt.pivot = new Vector2(0f, 1f);
            brt.anchoredPosition = new Vector2(190f, -25f);
            brt.sizeDelta = new Vector2(-220f, -115f);

            // Advance Prompt TMP
            GameObject promptObj = new GameObject("DialoguePrompt");
            promptObj.transform.SetParent(dialoguePanel.transform, false);
            dialoguePromptText = promptObj.AddComponent<TextMeshProUGUI>();
            dialoguePromptText.font = defaultFont;
            dialoguePromptText.fontSize = 26f * currentFontMultiplier;
            dialoguePromptText.fontStyle = FontStyles.Italic;
            dialoguePromptText.color = new Color(0.7f, 0.68f, 0.62f);
            dialoguePromptText.alignment = TextAlignmentOptions.MidlineRight;
            dialoguePromptText.text = "[Space / Enter / E to advance]";
            RectTransform drt = promptObj.GetComponent<RectTransform>();
            drt.anchorMin = new Vector2(0f, 0f);
            drt.anchorMax = new Vector2(1f, 0f);
            drt.pivot = new Vector2(1f, 0f);
            drt.anchoredPosition = new Vector2(-30f, 15f);
            drt.sizeDelta = new Vector2(-220f, 32f);

            // Choice Buttons Container (for interactive dialogue choices)
            dialogueChoicesContainer = new GameObject("DialogueChoicesContainer");
            dialogueChoicesContainer.transform.SetParent(dialoguePanel.transform, false);
            RectTransform crt = dialogueChoicesContainer.AddComponent<RectTransform>();
            crt.anchorMin = new Vector2(0f, 0f);
            crt.anchorMax = new Vector2(1f, 1f);
            crt.anchoredPosition = new Vector2(190f, -20f);
            crt.sizeDelta = new Vector2(-220f, -40f);

            choiceButtons = new Button[3];
            choiceButtonTexts = new TextMeshProUGUI[3];
            for (int i = 0; i < 3; i++)
            {
                int choiceIndex = i;
                GameObject btnObj = new GameObject($"ChoiceBtn_{i + 1}");
                btnObj.transform.SetParent(dialogueChoicesContainer.transform, false);
                RectTransform btrt = btnObj.AddComponent<RectTransform>();
                btrt.anchorMin = new Vector2(0f, 1f);
                btrt.anchorMax = new Vector2(1f, 1f);
                btrt.pivot = new Vector2(0f, 1f);
                btrt.anchoredPosition = new Vector2(0f, -50f - i * 65f);
                btrt.sizeDelta = new Vector2(-30f, 55f);

                Image btnImg = btnObj.AddComponent<Image>();
                btnImg.color = new Color(0.14f, 0.16f, 0.22f, 0.95f);
                Button btn = btnObj.AddComponent<Button>();
                ColorBlock cb = btn.colors;
                cb.highlightedColor = new Color(0.35f, 0.28f, 0.12f);
                cb.pressedColor = new Color(0.5f, 0.4f, 0.15f);
                btn.colors = cb;
                btn.onClick.AddListener(() => OnChoiceButtonClicked(choiceIndex));

                GameObject txtObj = new GameObject("BtnText");
                txtObj.transform.SetParent(btnObj.transform, false);
                TextMeshProUGUI txt = txtObj.AddComponent<TextMeshProUGUI>();
                txt.font = defaultFont;
                txt.fontSize = 28f * currentFontMultiplier;
                txt.color = new Color(0.98f, 0.92f, 0.75f);
                txt.alignment = TextAlignmentOptions.MidlineLeft;
                txt.text = $"[{i + 1}] Choice option";
                RectTransform tr = txtObj.GetComponent<RectTransform>();
                tr.anchorMin = Vector2.zero;
                tr.anchorMax = Vector2.one;
                tr.anchoredPosition = new Vector2(20f, 0f);
                tr.sizeDelta = new Vector2(-40f, 0f);

                choiceButtons[i] = btn;
                choiceButtonTexts[i] = txt;
            }

            dialogueChoicesContainer.SetActive(false);
            dialoguePanel.SetActive(false);
        }

        private void BuildPausePanel(Transform parent)
        {
            pausePanel = new GameObject("Pause_SettingsModal");
            pausePanel.transform.SetParent(parent, false);

            RectTransform rt = pausePanel.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(960f, 700f);

            Image bg = pausePanel.AddComponent<Image>();
            bg.sprite = CreateColorSprite(new Color(0.06f, 0.07f, 0.09f, 0.98f));

            // Title (>= 56 px)
            GameObject titleObj = new GameObject("PauseTitle");
            titleObj.transform.SetParent(pausePanel.transform, false);
            TextMeshProUGUI title = titleObj.AddComponent<TextMeshProUGUI>();
            title.font = defaultFont;
            title.fontSize = 56f * currentFontMultiplier;
            title.fontStyle = FontStyles.Bold;
            title.alignment = TextAlignmentOptions.Center;
            title.color = new Color(1.0f, 0.84f, 0.3f);
            title.text = "PAUSED";
            RectTransform trt = titleObj.GetComponent<RectTransform>();
            trt.anchorMin = new Vector2(0f, 1f);
            trt.anchorMax = new Vector2(1f, 1f);
            trt.pivot = new Vector2(0.5f, 1f);
            trt.anchoredPosition = new Vector2(0f, -25f);
            trt.sizeDelta = new Vector2(-60f, 65f);

            // Buttons Container
            float startY = -110f;
            CreateMenuButton(pausePanel.transform, "Resume Game", new Vector2(0f, startY), () => TogglePause());
            CreateMenuButton(pausePanel.transform, "Replay Tutorial", new Vector2(0f, startY - 60f), () =>
            {
                TogglePause();
                TutorialManager.Instance.StartTutorial();
            });
            CreateMenuButton(pausePanel.transform, "Skip Tutorial", new Vector2(0f, startY - 120f), () =>
            {
                TogglePause();
                TutorialManager.Instance.SkipTutorial();
            });
            CreateMenuButton(pausePanel.transform, "Investigation Journal [J]", new Vector2(0f, startY - 180f), () =>
            {
                TogglePause();
                if (InvestigationJournal.Instance != null) InvestigationJournal.Instance.ToggleJournal();
            });
            CreateMenuButton(pausePanel.transform, "Temple Map [M]", new Vector2(0f, startY - 240f), () =>
            {
                TogglePause();
                if (TempleMapOverlay.Instance != null) TempleMapOverlay.Instance.ToggleMap();
            });

            // Settings Controls (Text Size & Audio)
            GameObject settingsBox = new GameObject("SettingsBox");
            settingsBox.transform.SetParent(pausePanel.transform, false);
            RectTransform srt = settingsBox.AddComponent<RectTransform>();
            srt.anchorMin = new Vector2(0.5f, 0f);
            srt.anchorMax = new Vector2(0.5f, 0f);
            srt.pivot = new Vector2(0.5f, 0f);
            srt.anchoredPosition = new Vector2(0f, 30f);
            srt.sizeDelta = new Vector2(880f, 250f);
            Image sbg = settingsBox.AddComponent<Image>();
            sbg.color = new Color(0.10f, 0.12f, 0.16f, 0.9f);

            // Text Size Scale Setting
            GameObject tsLabel = new GameObject("TextScaleLabel");
            tsLabel.transform.SetParent(settingsBox.transform, false);
            textSizeLabel = tsLabel.AddComponent<TextMeshProUGUI>();
            textSizeLabel.font = defaultFont;
            textSizeLabel.fontSize = 28f * currentFontMultiplier;
            textSizeLabel.color = Color.white;
            textSizeLabel.text = $"Text Size: <color=#ffd700>{fontScaleNames[textScaleIndex]}</color>";
            RectTransform tsrt = tsLabel.GetComponent<RectTransform>();
            tsrt.anchorMin = new Vector2(0f, 1f);
            tsrt.anchorMax = new Vector2(0.6f, 1f);
            tsrt.anchoredPosition = new Vector2(30f, -20f);
            tsrt.sizeDelta = new Vector2(0f, 40f);

            // Cycle Text Scale Button
            GameObject cycleBtn = new GameObject("CycleTextScaleBtn");
            cycleBtn.transform.SetParent(settingsBox.transform, false);
            RectTransform cbrt = cycleBtn.AddComponent<RectTransform>();
            cbrt.anchorMin = new Vector2(0.65f, 1f);
            cbrt.anchorMax = new Vector2(0.95f, 1f);
            cbrt.anchoredPosition = new Vector2(0f, -20f);
            cbrt.sizeDelta = new Vector2(0f, 40f);
            Image cbImg = cycleBtn.AddComponent<Image>();
            cbImg.color = new Color(0.3f, 0.25f, 0.12f);
            Button cbtn = cycleBtn.AddComponent<Button>();
            cbtn.onClick.AddListener(CycleTextScale);

            GameObject cbTxt = new GameObject("BtnText");
            cbTxt.transform.SetParent(cycleBtn.transform, false);
            TextMeshProUGUI cbt = cbTxt.AddComponent<TextMeshProUGUI>();
            cbt.font = defaultFont;
            cbt.fontSize = 22f * currentFontMultiplier;
            cbt.alignment = TextAlignmentOptions.Center;
            cbt.color = Color.white;
            cbt.text = "Change Size";
            RectTransform cbtrt = cbTxt.GetComponent<RectTransform>();
            cbtrt.anchorMin = Vector2.zero;
            cbtrt.anchorMax = Vector2.one;
            cbtrt.sizeDelta = Vector2.zero;

            // Volume Sliders (Master, Music, SFX)
            CreateVolumeSliderRow(settingsBox.transform, "Master Volume", new Vector2(30f, -80f), AudioManager.Instance.MasterVolume, (v) => AudioManager.Instance.SetMasterVolume(v));
            CreateVolumeSliderRow(settingsBox.transform, "Music Volume", new Vector2(30f, -135f), AudioManager.Instance.MusicVolume, (v) => AudioManager.Instance.SetMusicVolume(v));
            CreateVolumeSliderRow(settingsBox.transform, "SFX Volume", new Vector2(30f, -190f), AudioManager.Instance.SFXVolume, (v) => AudioManager.Instance.SetSFXVolume(v));

            pausePanel.SetActive(false);
        }

        private GameObject CreateMenuButton(Transform parent, string label, Vector2 pos, Action onClick)
        {
            GameObject btnObj = new GameObject($"Btn_{label.Replace(" ", "")}");
            btnObj.transform.SetParent(parent, false);

            RectTransform rt = btnObj.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(560f, 48f);

            Image img = btnObj.AddComponent<Image>();
            img.color = new Color(0.12f, 0.14f, 0.18f);

            Button btn = btnObj.AddComponent<Button>();
            ColorBlock cb = btn.colors;
            cb.highlightedColor = new Color(0.32f, 0.25f, 0.10f);
            btn.colors = cb;
            btn.onClick.AddListener(() =>
            {
                AudioManager.Instance.PlaySFX("sfx_ui_click");
                onClick?.Invoke();
            });

            GameObject txtObj = new GameObject("Text");
            txtObj.transform.SetParent(btnObj.transform, false);
            TextMeshProUGUI txt = txtObj.AddComponent<TextMeshProUGUI>();
            txt.font = defaultFont;
            txt.fontSize = 28f * currentFontMultiplier;
            txt.alignment = TextAlignmentOptions.Center;
            txt.color = new Color(0.95f, 0.92f, 0.85f);
            txt.text = label;

            RectTransform trt = txtObj.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.sizeDelta = Vector2.zero;

            return btnObj;
        }

        private void CreateVolumeSliderRow(Transform parent, string label, Vector2 pos, float initVal, Action<float> onValChanged)
        {
            GameObject row = new GameObject($"Row_{label.Replace(" ", "")}");
            row.transform.SetParent(parent, false);
            RectTransform rt = row.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(-60f, 45f);

            GameObject labelObj = new GameObject("Label");
            labelObj.transform.SetParent(row.transform, false);
            TextMeshProUGUI lbl = labelObj.AddComponent<TextMeshProUGUI>();
            lbl.font = defaultFont;
            lbl.fontSize = 26f * currentFontMultiplier;
            lbl.color = new Color(0.9f, 0.88f, 0.82f);
            lbl.text = label;
            RectTransform lrt = labelObj.GetComponent<RectTransform>();
            lrt.anchorMin = new Vector2(0f, 0f);
            lrt.anchorMax = new Vector2(0.45f, 1f);
            lrt.anchoredPosition = Vector2.zero;
            lrt.sizeDelta = Vector2.zero;

            // Slider
            GameObject sliderObj = new GameObject("Slider");
            sliderObj.transform.SetParent(row.transform, false);
            RectTransform srt = sliderObj.AddComponent<RectTransform>();
            srt.anchorMin = new Vector2(0.5f, 0.5f);
            srt.anchorMax = new Vector2(0.95f, 0.5f);
            srt.pivot = new Vector2(0f, 0.5f);
            srt.anchoredPosition = Vector2.zero;
            srt.sizeDelta = new Vector2(0f, 20f);

            // Background
            GameObject bgObj = new GameObject("Background");
            bgObj.transform.SetParent(sliderObj.transform, false);
            RectTransform bgrt = bgObj.AddComponent<RectTransform>();
            bgrt.anchorMin = Vector2.zero;
            bgrt.anchorMax = Vector2.one;
            bgrt.sizeDelta = Vector2.zero;
            Image bgImg = bgObj.AddComponent<Image>();
            bgImg.color = new Color(0.2f, 0.22f, 0.28f);

            Slider slider = sliderObj.AddComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = initVal;
            slider.onValueChanged.AddListener((val) => onValChanged?.Invoke(val));
        }

        #endregion

        #region Public UI Controls

        public void SetObjective(string title, string body, string counter = null)
        {
            if (objectiveTitleText != null && !string.IsNullOrEmpty(title)) objectiveTitleText.text = title;
            if (objectiveBodyText != null && !string.IsNullOrEmpty(body)) objectiveBodyText.text = body;
            if (objectiveCounterText != null && !string.IsNullOrEmpty(counter)) objectiveCounterText.text = counter;
        }

        public void ShowInteractionPrompt(string key, string actionText)
        {
            if (promptPanel == null) return;
            if (promptKeyText != null) promptKeyText.text = key;
            if (promptActionText != null) promptActionText.text = actionText;
            promptPanel.SetActive(true);
        }

        public void HideInteractionPrompt()
        {
            if (promptPanel != null) promptPanel.SetActive(false);
        }

        public void ShowTutorialStep(int stepIndex, int totalSteps, string title, string keyIcon, string instruction, string prompt)
        {
            if (tutorialPanel == null) return;

            tutorialStepText.text = $"TUTORIAL · STEP {stepIndex} OF {totalSteps}";
            tutorialTitleText.text = title;
            tutorialKeycapText.text = keyIcon;
            tutorialBodyText.text = instruction;
            tutorialActionPromptText.text = prompt;

            tutorialPanel.SetActive(true);
        }

        public void HideTutorial()
        {
            if (tutorialPanel != null) tutorialPanel.SetActive(false);
        }

        public void ShowHowToPlay(Action onDismiss)
        {
            onHowToPlayDismiss = onDismiss;
            if (howToPlayPanel != null) howToPlayPanel.SetActive(true);
        }

        public void DismissHowToPlay()
        {
            if (howToPlayPanel != null) howToPlayPanel.SetActive(false);
            onHowToPlayDismiss?.Invoke();
            onHowToPlayDismiss = null;
        }

        public void StartDialogue(string speaker, string[] lines, string[] choices = null, Action<int> onChoice = null, Action onComplete = null)
        {
            isDialogueActive = true;
            activeDialogueLines = lines;
            currentDialogueLine = 0;
            onDialogueCompletedCallback = onComplete;
            onChoiceCallback = onChoice;

            dialogueSpeakerText.text = speaker;
            if (dialoguePortraitInitial != null)
            {
                dialoguePortraitInitial.text = !string.IsNullOrEmpty(speaker) ? speaker.Substring(0, 1).ToUpperInvariant() : "★";
            }
            dialoguePanel.SetActive(true);

            // Duck music
            AudioManager.Instance.SetDialogueDucking(true);

            ShowDialogueLine(0);
        }

        private void ShowDialogueLine(int index)
        {
            if (index < 0 || index >= activeDialogueLines.Length)
            {
                EndDialogue();
                return;
            }

            activeFullLine = activeDialogueLines[index];
            dialogueChoicesContainer.SetActive(false);

            if (typewriterCoroutine != null) StopCoroutine(typewriterCoroutine);
            typewriterCoroutine = StartCoroutine(TypewriteText(activeFullLine));
        }

        private IEnumerator TypewriteText(string text)
        {
            isTypewriting = true;
            dialogueBodyText.text = "";
            for (int i = 0; i <= text.Length; i++)
            {
                dialogueBodyText.text = text.Substring(0, i);
                yield return new WaitForSecondsRealtime(0.02f);
            }
            isTypewriting = false;
        }

        public void AdvanceDialogue()
        {
            currentDialogueLine++;
            if (currentDialogueLine < activeDialogueLines.Length)
            {
                ShowDialogueLine(currentDialogueLine);
            }
            else
            {
                EndDialogue();
            }
        }

        public void EndDialogue()
        {
            isDialogueActive = false;
            if (dialoguePanel != null) dialoguePanel.SetActive(false);
            AudioManager.Instance.SetDialogueDucking(false);
            onDialogueCompletedCallback?.Invoke();
            onDialogueCompletedCallback = null;
        }

        private void OnChoiceButtonClicked(int choiceIndex)
        {
            AudioManager.Instance.PlaySFX("sfx_ui_select");
            dialogueChoicesContainer.SetActive(false);
            onChoiceCallback?.Invoke(choiceIndex);
            onChoiceCallback = null;
            AdvanceDialogue();
        }

        public void TogglePause()
        {
            if (pausePanel == null) return;
            bool willPause = !pausePanel.activeSelf;
            pausePanel.SetActive(willPause);
            Time.timeScale = willPause ? 0f : 1f;

            if (willPause)
            {
                AudioManager.Instance.PlaySFX("sfx_ui_select");
            }
            else
            {
                AudioManager.Instance.PlaySFX("sfx_ui_click");
            }
        }

        public void CycleTextScale()
        {
            textScaleIndex = (textScaleIndex + 1) % fontMultipliers.Length;
            currentFontMultiplier = fontMultipliers[textScaleIndex];
            PlayerPrefs.SetInt("Settings_TextScaleIndex", textScaleIndex);
            PlayerPrefs.Save();

            if (textSizeLabel != null)
            {
                textSizeLabel.text = $"Text Size: <color=#ffd700>{fontScaleNames[textScaleIndex]}</color>";
            }

            ApplyFontScaleToAll();
        }

        private void ApplyFontScaleToAll()
        {
            if (objectiveTitleText != null) objectiveTitleText.fontSize = 32f * currentFontMultiplier;
            if (objectiveBodyText != null) objectiveBodyText.fontSize = 28f * currentFontMultiplier;
            if (promptKeyText != null) promptKeyText.fontSize = 32f * currentFontMultiplier;
            if (promptActionText != null) promptActionText.fontSize = 32f * currentFontMultiplier;
            if (tutorialTitleText != null) tutorialTitleText.fontSize = 56f * currentFontMultiplier;
            if (tutorialBodyText != null) tutorialBodyText.fontSize = 32f * currentFontMultiplier;
            if (dialogueSpeakerText != null) dialogueSpeakerText.fontSize = 36f * currentFontMultiplier;
            if (dialogueBodyText != null) dialogueBodyText.fontSize = 34f * currentFontMultiplier;
        }

        #endregion

        #region Event Handlers

        private void HandleClueFound(string clueId)
        {
            int currentClues = PlayerPrefs.GetInt("Clues_Found_Count", 0) + 1;
            PlayerPrefs.SetInt("Clues_Found_Count", currentClues);
            int relics = PlayerPrefs.GetInt("Relics_Recovered_Count", 0);
            if (objectiveCounterText != null)
            {
                objectiveCounterText.text = $"Stolen Relics Recovered: {relics}/3  |  Clues Found: {currentClues}/5";
            }
        }

        private void HandlePuzzleSolved(string puzzleId)
        {
            SetObjective("The Passage Unsealed", "Enter the revealed passage and confront the smugglers.", null);
        }

        private void HandleDialogueEvent(string charId)
        {
            // Handled when dialogues trigger
        }

        #endregion
    }
}
