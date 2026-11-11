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

        #region Theme Colors & Styling
        // Authentic Odishan Heritage Mystery Palette
        public static readonly Color ColorTextWarmIvory = new Color(1.0f, 0.957f, 0.839f, 1.0f);     // #FFF4D6
        public static readonly Color ColorHeaderAntiqueGold = new Color(1.0f, 0.784f, 0.341f, 1.0f); // #FFC857
        public static readonly Color ColorClueCyan = new Color(0.361f, 0.882f, 0.902f, 1.0f);          // #5CE1E6
        public static readonly Color ColorWarningCoral = new Color(1.0f, 0.706f, 0.659f, 1.0f);        // #FFB4A8 (lightened coral, 7:1)

        public static readonly Color ColorPanelBgNormal = new Color(0.0f, 0.0f, 0.0f, 0.85f);        // #000000 @ 85% (>= 7:1 for every text colour)
        public static readonly Color ColorPanelBgHighContrast = new Color(0.0f, 0.0f, 0.0f, 0.98f);  // #000000 @ 98%
        public static readonly Color ColorBorderGold = new Color(1.0f, 0.784f, 0.341f, 0.85f);       // Thin Gold Border
        #endregion

        [Header("Font Assets")]
        private TMP_FontAsset defaultFont;

        // Canvas & Scaler
        private Canvas mainCanvas;
        private CanvasScaler canvasScaler;

        // UI Panels
        private GameObject hudPanel;
        private Image hudPanelBg;
        private TextMeshProUGUI objectiveTitleText;
        private TextMeshProUGUI objectiveBodyText;
        private TextMeshProUGUI objectiveCounterText;

        private GameObject promptPanel;
        private TextMeshProUGUI bottomHintText;
        private Image promptPanelBg;
        private TextMeshProUGUI promptKeyText;
        private TextMeshProUGUI promptActionText;

        private GameObject tutorialPanel;
        private Image tutorialPanelBg;
        private TextMeshProUGUI tutorialStepText;
        private TextMeshProUGUI tutorialTitleText;
        private TextMeshProUGUI tutorialKeycapText;
        private TextMeshProUGUI tutorialBodyText;
        private TextMeshProUGUI tutorialActionPromptText;

        private GameObject howToPlayPanel;
        private Image howToPlayPanelBg;
        private Action onHowToPlayDismiss;

        private GameObject dialoguePanel;
        private Image dialoguePanelBg;
        private TextMeshProUGUI dialogueSpeakerText;
        private TextMeshProUGUI dialoguePortraitInitial;
        private TextMeshProUGUI dialogueBodyText;
        private TextMeshProUGUI dialoguePromptText;
        private GameObject dialogueChoicesContainer;
        private Button[] choiceButtons;
        private TextMeshProUGUI[] choiceButtonTexts;

        private GameObject pausePanel;
        private Image pausePanelBg;
        private Slider masterSlider;
        private Slider musicSlider;
        private Slider sfxSlider;
        private TextMeshProUGUI textSizeLabel;
        private TextMeshProUGUI highContrastLabel;

        // Bottom Controls Hint Bar
        private GameObject bottomHintBar;
        private Image bottomHintBarBg;

        // Photo Mode Toast
        private GameObject photoModeToast;
        private TextMeshProUGUI photoModeToastText;
        private float photoModeToastTimer = 0f;

        // Dialogue State
        private bool isDialogueActive = false;
        private string[] activeDialogueLines;
        private string[] activeDialogueChoices;
        private int currentDialogueLine = 0;
        private bool isTypewriting = false;
        private Coroutine typewriterCoroutine;
        private string activeFullLine = "";
        private Action onDialogueCompletedCallback;
        private Action<int> onChoiceCallback;

        // High Contrast & Text Scale
        private bool isHighContrast = false;
        private bool isPhotoMode = false;
        private float currentFontMultiplier = 1.0f;
        private int textScaleIndex = 1; // 0: Small (0.85), 1: Med (1.0), 2: Large (1.15), 3: XL (1.30)
        private readonly float[] fontMultipliers = new float[] { 0.9f, 1.0f, 1.12f, 1.25f };
        private readonly string[] fontScaleNames = new string[] { "Small (85%)", "Medium (100%)", "Large (115%)", "Extra Large (130%)" };

        private readonly List<Image> allPanelBackgrounds = new List<Image>();
        private readonly List<TextMeshProUGUI> allTextElements = new List<TextMeshProUGUI>();

        public bool IsDialogueActive => isDialogueActive;
        public bool IsTutorialActive => tutorialPanel != null && tutorialPanel.activeSelf;
        public bool IsPaused => pausePanel != null && pausePanel.activeSelf;
        public bool IsPhotoMode => isPhotoMode;

        private void Awake()
        {
            LegacyOfTemples.Core.InputGate.UiOpen = () =>
            {
                var u = Instance; // the lambda must read the live singleton, not a destroyed duplicate
                if (u == null) return false;
                var tm = TutorialManager.Instance;
                bool tutorialCard = tm != null && tm.IsActive && tm.CurrentStep == TutorialManager.TutorialStep.Welcome;
                bool dim = false;
                foreach (var spec in ScreenText.Panels.Values) if (spec.dimBackground && ScreenText.IsLive(spec.lastFrame)) { dim = true; break; }
                return u.isDialogueActive || tutorialCard || dim || u.IsPauseOpen || GameIntroOverlay.AnyOpen
                    || (u.howToPlayPanel != null && u.howToPlayPanel.activeSelf)
                    || (GuideSystem.Instance != null && GuideSystem.Instance.IsOpen)
                    || (InvestigationJournal.Instance != null && InvestigationJournal.Instance.IsOpen)
                    || (TempleMapOverlay.Instance != null && TempleMapOverlay.Instance.IsOpen);
            };
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
            isHighContrast = PlayerPrefs.GetInt("Settings_HighContrast", 0) == 1;

            BuildCanvasHierarchy();
            SubscribeEvents();
            ApplyHighContrastTheme();
        }

        private void OnDestroy()
        {
            UnsubscribeEvents();
        }

        private void LoadFont()
        {
            defaultFont = HeritageFonts.Body;
            if (defaultFont == null)
            {
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
            EventBus.OnRequestDialogueEnd += CancelDialogue;
            EventBus.OnShowcaseTourToggled += HideAllUIForShowcase;
            EventBus.OnOverlayMenuToggled += HandleOverlayMenuToggled;
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
            EventBus.OnRequestDialogueEnd -= CancelDialogue;
            EventBus.OnShowcaseTourToggled -= HideAllUIForShowcase;
            EventBus.OnOverlayMenuToggled -= HandleOverlayMenuToggled;
        }

        private void HandleOverlayMenuToggled(bool isOpen)
        {
            SetHudActive(!isOpen);
            if (bottomHintBar != null) bottomHintBar.SetActive(!isOpen && !isPhotoMode);
        }

        private void LateUpdate() { ThemeAndLayout(); }

        /// <summary>True when another panel is open (or closed on this frame) so Esc must not also open Pause.</summary>
        private static bool OverlayOwnsEscape()
        {
            if (EventBus.EscapeConsumedFrame >= Time.frameCount - 1) return true;
            if (InvestigationJournal.Instance != null && InvestigationJournal.Instance.IsOpen) return true;
            if (TempleMapOverlay.Instance != null && TempleMapOverlay.Instance.IsOpen) return true;
            if (ChakraPuzzleController.Active != null && ChakraPuzzleController.Active.IsInteracting) return true;
            foreach (var c in FindObjectsByType<ClueInspectable>()) if (c.IsInspecting) return true;
            return false;
        }

        private void Update()
        {
            var kb = Keyboard.current;

            // Photo Mode Toggle [P]
            if (kb != null && kb.pKey.wasPressedThisFrame)
            {
                TogglePhotoMode();
            }

            if (isPhotoMode)
            {
                if (photoModeToastTimer > 0f)
                {
                    photoModeToastTimer -= Time.unscaledDeltaTime;
                    if (photoModeToastTimer <= 0f && photoModeToast != null)
                    {
                        photoModeToast.SetActive(false);
                    }
                }
                return;
            }


            // Pause Toggle (Esc)
            if (kb != null && kb.escapeKey.wasPressedThisFrame && !OverlayOwnsEscape())
            {
                if (howToPlayPanel != null && howToPlayPanel.activeSelf)
                {
                    DismissHowToPlay();
                }
                else if (isDialogueActive)
                {
                    // Do nothing
                }
                else
                {
                    TogglePause();
                }
            }

            // Dialogue Advance or Choice selection
            if (isDialogueActive)
            {
                if (dialogueChoicesContainer != null && dialogueChoicesContainer.activeSelf)
                {
                    // Choice selection keys [1], [2], [3]
                    if (kb != null)
                    {
                        if ((kb.digit1Key.wasPressedThisFrame || kb.numpad1Key.wasPressedThisFrame) && choiceButtons.Length > 0 && choiceButtons[0].gameObject.activeSelf)
                        {
                            OnChoiceButtonClicked(0);
                        }
                        else if ((kb.digit2Key.wasPressedThisFrame || kb.numpad2Key.wasPressedThisFrame) && choiceButtons.Length > 1 && choiceButtons[1].gameObject.activeSelf)
                        {
                            OnChoiceButtonClicked(1);
                        }
                        else if ((kb.digit3Key.wasPressedThisFrame || kb.numpad3Key.wasPressedThisFrame) && choiceButtons.Length > 2 && choiceButtons[2].gameObject.activeSelf)
                        {
                            OnChoiceButtonClicked(2);
                        }
                    }
                }
                else
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

            // 8. Persistent Bottom Controls Hint Bar
            BuildBottomHintBar(canvasGO.transform);

            // 9. Photo Mode Toast
            BuildPhotoModeToast(canvasGO.transform);
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

        private void AddGoldBorder(GameObject panel)
        {
            GameObject border = new GameObject("GoldBorder");
            border.transform.SetParent(panel.transform, false);
            RectTransform brt = border.AddComponent<RectTransform>();
            brt.anchorMin = Vector2.zero;
            brt.anchorMax = Vector2.one;
            brt.sizeDelta = Vector2.zero;

            // 4 outline edges (2px)
            CreateBorderEdge(border.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -1), new Vector2(0, 2)); // Top
            CreateBorderEdge(border.transform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(0, 2));  // Bottom
            CreateBorderEdge(border.transform, new Vector2(0, 0), new Vector2(0, 1), new Vector2(1, 0), new Vector2(2, 0));  // Left
            CreateBorderEdge(border.transform, new Vector2(1, 0), new Vector2(1, 1), new Vector2(-1, 0), new Vector2(2, 0)); // Right
        }

        private void CreateBorderEdge(Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 size)
        {
            GameObject edge = new GameObject("Edge");
            edge.transform.SetParent(parent, false);
            RectTransform rt = edge.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.sizeDelta = size;
            rt.anchoredPosition = Vector2.zero;
            Image img = edge.AddComponent<Image>();
            img.color = ColorBorderGold;
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
            rt.sizeDelta = new Vector2(720f, 200f);

            hudPanelBg = hudPanel.AddComponent<Image>();
            hudPanelBg.sprite = CreateColorSprite(ColorPanelBgNormal);
            allPanelBackgrounds.Add(hudPanelBg);
            AddGoldBorder(hudPanel);

            // Left gold accent strip
            GameObject strip = new GameObject("AccentStrip");
            strip.transform.SetParent(hudPanel.transform, false);
            RectTransform srt = strip.AddComponent<RectTransform>();
            srt.anchorMin = new Vector2(0f, 0f);
            srt.anchorMax = new Vector2(0f, 1f);
            srt.pivot = new Vector2(0f, 0.5f);
            srt.sizeDelta = new Vector2(6f, 0f);
            srt.anchoredPosition = Vector2.zero;
            Image stripImg = strip.AddComponent<Image>();
            stripImg.color = ColorHeaderAntiqueGold;

            // Header TMP (>= 34 px)
            GameObject titleObj = new GameObject("ObjectiveTitle");
            titleObj.transform.SetParent(hudPanel.transform, false);
            objectiveTitleText = titleObj.AddComponent<TextMeshProUGUI>();
            objectiveTitleText.font = defaultFont;
            objectiveTitleText.fontSize = 40f * currentFontMultiplier;
            objectiveTitleText.fontStyle = FontStyles.Bold;
            objectiveTitleText.color = ColorHeaderAntiqueGold;
            objectiveTitleText.text = "CURRENT OBJECTIVE";
            RectTransform trt = titleObj.GetComponent<RectTransform>();
            trt.anchorMin = new Vector2(0f, 1f);
            trt.anchorMax = new Vector2(1f, 1f);
            trt.pivot = new Vector2(0f, 1f);
            trt.anchoredPosition = new Vector2(24f, -14f);
            trt.sizeDelta = new Vector2(-48f, 36f);
            allTextElements.Add(objectiveTitleText);

            // Body TMP (>= 30 px)
            GameObject bodyObj = new GameObject("ObjectiveBody");
            bodyObj.transform.SetParent(hudPanel.transform, false);
            objectiveBodyText = bodyObj.AddComponent<TextMeshProUGUI>();
            objectiveBodyText.font = defaultFont;
            objectiveBodyText.fontSize = 34f * currentFontMultiplier;
            objectiveBodyText.color = ColorTextWarmIvory;
            objectiveBodyText.text = "Speak with Head Priest Ananda at the courtyard steps.";
            objectiveBodyText.textWrappingMode = TextWrappingModes.Normal;
            RectTransform brt = bodyObj.GetComponent<RectTransform>();
            brt.anchorMin = new Vector2(0f, 1f);
            brt.anchorMax = new Vector2(1f, 1f);
            brt.pivot = new Vector2(0f, 1f);
            brt.anchoredPosition = new Vector2(24f, -54f);
            brt.sizeDelta = new Vector2(-48f, 85f);
            allTextElements.Add(objectiveBodyText);

            // Counter TMP
            GameObject counterObj = new GameObject("ObjectiveCounter");
            counterObj.transform.SetParent(hudPanel.transform, false);
            objectiveCounterText = counterObj.AddComponent<TextMeshProUGUI>();
            objectiveCounterText.font = defaultFont;
            objectiveCounterText.fontSize = 34f * currentFontMultiplier;
            objectiveCounterText.fontStyle = FontStyles.Italic;
            objectiveCounterText.color = ColorClueCyan;
            objectiveCounterText.text = "Stolen Relics Recovered: 0/3  |  Clues Found: 0/5";
            objectiveCounterText.textWrappingMode = TextWrappingModes.NoWrap;
            RectTransform crt = counterObj.GetComponent<RectTransform>();
            crt.anchorMin = new Vector2(0f, 0f);
            crt.anchorMax = new Vector2(1f, 0f);
            crt.pivot = new Vector2(0f, 0f);
            crt.anchoredPosition = new Vector2(24f, 14f);
            crt.sizeDelta = new Vector2(-48f, 32f);
            allTextElements.Add(objectiveCounterText);
        }

        private void BuildPromptPanel(Transform parent)
        {
            promptPanel = new GameObject("Prompt_InteractionPanel");
            promptPanel.transform.SetParent(parent, false);

            RectTransform rt = promptPanel.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0f, 95f);
            rt.sizeDelta = new Vector2(720f, 72f);

            promptPanelBg = promptPanel.AddComponent<Image>();
            promptPanelBg.sprite = CreateColorSprite(ColorPanelBgNormal);
            allPanelBackgrounds.Add(promptPanelBg);
            AddGoldBorder(promptPanel);

            // Keycap Box
            GameObject keyObj = new GameObject("PromptKeycap");
            keyObj.transform.SetParent(promptPanel.transform, false);
            RectTransform krt = keyObj.AddComponent<RectTransform>();
            krt.anchorMin = new Vector2(0f, 0.5f);
            krt.anchorMax = new Vector2(0f, 0.5f);
            krt.pivot = new Vector2(0f, 0.5f);
            krt.anchoredPosition = new Vector2(16f, 0f);
            krt.sizeDelta = new Vector2(64f, 50f);
            Image keyBg = keyObj.AddComponent<Image>();
            keyBg.color = new Color(0.25f, 0.20f, 0.12f, 1.0f);

            GameObject keyTextObj = new GameObject("KeyText");
            keyTextObj.transform.SetParent(keyObj.transform, false);
            promptKeyText = keyTextObj.AddComponent<TextMeshProUGUI>();
            promptKeyText.font = defaultFont;
            promptKeyText.fontSize = 34f * currentFontMultiplier;
            promptKeyText.fontStyle = FontStyles.Bold;
            promptKeyText.alignment = TextAlignmentOptions.Center;
            promptKeyText.color = ColorHeaderAntiqueGold;
            promptKeyText.text = "E";
            RectTransform ktrt = keyTextObj.GetComponent<RectTransform>();
            ktrt.anchorMin = Vector2.zero;
            ktrt.anchorMax = Vector2.one;
            ktrt.sizeDelta = Vector2.zero;
            allTextElements.Add(promptKeyText);

            // Prompt Action Text (>= 34 px)
            GameObject actionObj = new GameObject("PromptAction");
            actionObj.transform.SetParent(promptPanel.transform, false);
            promptActionText = actionObj.AddComponent<TextMeshProUGUI>();
            promptActionText.font = defaultFont;
            promptActionText.fontSize = 34f * currentFontMultiplier;
            promptActionText.fontStyle = FontStyles.Bold;
            promptActionText.alignment = TextAlignmentOptions.MidlineLeft;
            promptActionText.color = ColorTextWarmIvory;
            promptActionText.textWrappingMode = TextWrappingModes.NoWrap;
            promptActionText.overflowMode = TextOverflowModes.Ellipsis;
            promptActionText.text = "Talk to Priest Ananda";
            RectTransform art = actionObj.GetComponent<RectTransform>();
            art.anchorMin = new Vector2(0f, 0f);
            art.anchorMax = new Vector2(1f, 1f);
            art.offsetMin = new Vector2(88f, 0f);
            art.offsetMax = new Vector2(-20f, 0f);
            allTextElements.Add(promptActionText);

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
            rt.sizeDelta = new Vector2(1080f, 540f);

            tutorialPanelBg = tutorialPanel.AddComponent<Image>();
            tutorialPanelBg.sprite = CreateColorSprite(ColorPanelBgNormal);
            allPanelBackgrounds.Add(tutorialPanelBg);
            AddGoldBorder(tutorialPanel);

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
            hbg.color = new Color(0.18f, 0.14f, 0.08f, 0.95f);

            // Step Progress TMP (>= 30 px)
            GameObject stepObj = new GameObject("TutorialStepText");
            stepObj.transform.SetParent(header.transform, false);
            tutorialStepText = stepObj.AddComponent<TextMeshProUGUI>();
            tutorialStepText.font = defaultFont;
            tutorialStepText.fontSize = 32f * currentFontMultiplier;
            tutorialStepText.fontStyle = FontStyles.Bold;
            tutorialStepText.color = ColorHeaderAntiqueGold;
            tutorialStepText.text = "TUTORIAL · STEP 1 OF 12";
            tutorialStepText.alignment = TextAlignmentOptions.MidlineLeft;
            RectTransform srt = stepObj.GetComponent<RectTransform>();
            srt.anchorMin = new Vector2(0f, 0f);
            srt.anchorMax = new Vector2(1f, 1f);
            srt.anchoredPosition = new Vector2(30f, 0f);
            srt.sizeDelta = new Vector2(-60f, 0f);
            allTextElements.Add(tutorialStepText);

            // Title TMP (>= 64 px)
            GameObject titleObj = new GameObject("TutorialTitle");
            titleObj.transform.SetParent(tutorialPanel.transform, false);
            tutorialTitleText = titleObj.AddComponent<TextMeshProUGUI>();
            tutorialTitleText.font = defaultFont;
            tutorialTitleText.fontSize = 64f * currentFontMultiplier;
            tutorialTitleText.fontStyle = FontStyles.Bold;
            tutorialTitleText.color = ColorHeaderAntiqueGold;
            tutorialTitleText.alignment = TextAlignmentOptions.Center;
            tutorialTitleText.text = "WELCOME TO PURI";
            RectTransform trt = titleObj.GetComponent<RectTransform>();
            trt.anchorMin = new Vector2(0f, 1f);
            trt.anchorMax = new Vector2(1f, 1f);
            trt.pivot = new Vector2(0.5f, 1f);
            trt.anchoredPosition = new Vector2(0f, -95f);
            trt.sizeDelta = new Vector2(-60f, 80f);
            allTextElements.Add(tutorialTitleText);

            // Keycap Display TMP (>= 34 px)
            GameObject keycapObj = new GameObject("TutorialKeycap");
            keycapObj.transform.SetParent(tutorialPanel.transform, false);
            tutorialKeycapText = keycapObj.AddComponent<TextMeshProUGUI>();
            tutorialKeycapText.font = defaultFont;
            tutorialKeycapText.fontSize = 46f * currentFontMultiplier;
            tutorialKeycapText.fontStyle = FontStyles.Bold;
            tutorialKeycapText.alignment = TextAlignmentOptions.Center;
            tutorialKeycapText.color = ColorClueCyan;
            tutorialKeycapText.text = "[ W ]  [ A ]  [ S ]  [ D ]";
            RectTransform krt = keycapObj.GetComponent<RectTransform>();
            krt.anchorMin = new Vector2(0f, 0.5f);
            krt.anchorMax = new Vector2(1f, 0.5f);
            krt.pivot = new Vector2(0.5f, 0.5f);
            krt.anchoredPosition = new Vector2(0f, 25f);
            krt.sizeDelta = new Vector2(-60f, 65f);
            allTextElements.Add(tutorialKeycapText);

            // Body TMP (>= 30 px)
            GameObject bodyObj = new GameObject("TutorialBody");
            bodyObj.transform.SetParent(tutorialPanel.transform, false);
            tutorialBodyText = bodyObj.AddComponent<TextMeshProUGUI>();
            tutorialBodyText.font = defaultFont;
            tutorialBodyText.fontSize = 32f * currentFontMultiplier;
            tutorialBodyText.alignment = TextAlignmentOptions.Center;
            tutorialBodyText.color = ColorTextWarmIvory;
            tutorialBodyText.textWrappingMode = TextWrappingModes.Normal;
            tutorialBodyText.text = "Three sacred fictional heritage relics have vanished overnight from the temple storage chamber.";
            RectTransform brt = bodyObj.GetComponent<RectTransform>();
            brt.anchorMin = new Vector2(0f, 0f);
            brt.anchorMax = new Vector2(1f, 0.5f);
            brt.pivot = new Vector2(0.5f, 1f);
            brt.anchoredPosition = new Vector2(0f, -20f);
            brt.sizeDelta = new Vector2(-80f, 130f);
            allTextElements.Add(tutorialBodyText);

            // Prompt TMP (>= 30 px)
            GameObject promptObj = new GameObject("TutorialActionPrompt");
            promptObj.transform.SetParent(tutorialPanel.transform, false);
            tutorialActionPromptText = promptObj.AddComponent<TextMeshProUGUI>();
            tutorialActionPromptText.font = defaultFont;
            tutorialActionPromptText.fontSize = 34f * currentFontMultiplier;
            tutorialActionPromptText.fontStyle = FontStyles.Bold;
            tutorialActionPromptText.alignment = TextAlignmentOptions.Center;
            tutorialActionPromptText.color = ColorWarningCoral;
            tutorialActionPromptText.text = "Press ENTER or Space to Begin Tutorial...";
            RectTransform prt = promptObj.GetComponent<RectTransform>();
            prt.anchorMin = new Vector2(0f, 0f);
            prt.anchorMax = new Vector2(1f, 0f);
            prt.pivot = new Vector2(0.5f, 0f);
            prt.anchoredPosition = new Vector2(0f, 25f);
            prt.sizeDelta = new Vector2(-60f, 45f);
            allTextElements.Add(tutorialActionPromptText);

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
            rt.sizeDelta = new Vector2(1280f, 800f);

            howToPlayPanelBg = howToPlayPanel.AddComponent<Image>();
            howToPlayPanelBg.sprite = CreateColorSprite(ColorPanelBgNormal);
            allPanelBackgrounds.Add(howToPlayPanelBg);
            AddGoldBorder(howToPlayPanel);

            // Title
            GameObject titleObj = new GameObject("CardTitle");
            titleObj.transform.SetParent(howToPlayPanel.transform, false);
            TextMeshProUGUI title = titleObj.AddComponent<TextMeshProUGUI>();
            title.font = defaultFont;
            title.fontSize = 64f * currentFontMultiplier;
            title.fontStyle = FontStyles.Bold;
            title.alignment = TextAlignmentOptions.Center;
            title.color = ColorHeaderAntiqueGold;
            title.text = "HOW TO PLAY & INVESTIGATION BRIEF";
            RectTransform trt = titleObj.GetComponent<RectTransform>();
            trt.anchorMin = new Vector2(0f, 1f);
            trt.anchorMax = new Vector2(1f, 1f);
            trt.pivot = new Vector2(0.5f, 1f);
            trt.anchoredPosition = new Vector2(0f, -20f);
            trt.sizeDelta = new Vector2(-60f, 48f);
            allTextElements.Add(title);

            // 3-Line Goal Summary & Keycap Grid (>= 30 px)
            GameObject bodyObj = new GameObject("CardBody");
            bodyObj.transform.SetParent(howToPlayPanel.transform, false);
            TextMeshProUGUI body = bodyObj.AddComponent<TextMeshProUGUI>();
            body.font = defaultFont;
            body.fontSize = 30f * currentFontMultiplier;
            body.alignment = TextAlignmentOptions.TopLeft;
            body.color = ColorTextWarmIvory;
            body.textWrappingMode = TextWrappingModes.Normal;
            body.text =
                "<b>INVESTIGATION MISSION GOAL:</b>\n" +
                "• <b>Goal 1:</b> Three sacred fictional heritage relics were stolen overnight from the temple storage hall.\n" +
                "• <b>Goal 2:</b> Investigate forensic clues, interview witnesses, and decipher the ancient Surya Chakra puzzle wall.\n" +
                "• <b>Goal 3:</b> Track the thieves to their hideout and peacefully recover the sacred pieces before the festival.\n\n" +
                "<b>CONTROLS & SHORTCUTS:</b>\n" +
                "  <color=#FFC857>[W] [A] [S] [D]</color> Move Investigator           <color=#FFC857>[MOUSE]</color> Look & Survey Architecture\n" +
                "  <color=#FFC857>[LEFT SHIFT]</color> Sprint Across Plaza          <color=#FFC857>[SPACE]</color> Jump Steps & Obstacles\n" +
                "  <color=#FFC857>[C / CTRL]</color> Crouch Forensic View            <color=#FFC857>[E / F]</color> Question Witnesses / Inspect Clues\n" +
                "  <color=#FFC857>[J]</color> Investigation Case Journal         <color=#FFC857>[M]</color> Temple Architectural Map Overlay\n" +
                "  <color=#FFC857>[H]</color> Toggle Help & Controls Guide       <color=#FFC857>[P]</color> Photo Mode (Clean Cinematic View)\n" +
                "  <color=#FFC857>[ESC]</color> Pause Game & Settings";

            RectTransform brt = bodyObj.GetComponent<RectTransform>();
            brt.anchorMin = new Vector2(0f, 0f);
            brt.anchorMax = new Vector2(1f, 1f);
            brt.offsetMin = new Vector2(40f, 75f);
            brt.offsetMax = new Vector2(-40f, -75f);
            allTextElements.Add(body);

            // Continue Button
            GameObject contObj = new GameObject("ContinuePrompt");
            contObj.transform.SetParent(howToPlayPanel.transform, false);
            TextMeshProUGUI contText = contObj.AddComponent<TextMeshProUGUI>();
            contText.font = defaultFont;
            contText.fontSize = 34f * currentFontMultiplier;
            contText.fontStyle = FontStyles.Bold;
            contText.alignment = TextAlignmentOptions.Center;
            contText.color = ColorClueCyan;
            contText.text = "Press ENTER or Space to Begin Your Investigation";
            RectTransform crt = contObj.GetComponent<RectTransform>();
            crt.anchorMin = new Vector2(0f, 0f);
            crt.anchorMax = new Vector2(1f, 0f);
            crt.pivot = new Vector2(0.5f, 0f);
            crt.anchoredPosition = new Vector2(0f, 20f);
            crt.sizeDelta = new Vector2(-60f, 40f);
            allTextElements.Add(contText);

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
            rt.anchoredPosition = new Vector2(0f, 75f);
            rt.sizeDelta = new Vector2(1320f, 310f);

            dialoguePanelBg = dialoguePanel.AddComponent<Image>();
            dialoguePanelBg.sprite = CreateColorSprite(ColorPanelBgNormal);
            allPanelBackgrounds.Add(dialoguePanelBg);
            AddGoldBorder(dialoguePanel);

            // Portrait Box
            GameObject portObj = new GameObject("PortraitBox");
            portObj.transform.SetParent(dialoguePanel.transform, false);
            RectTransform prt = portObj.AddComponent<RectTransform>();
            prt.anchorMin = new Vector2(0f, 0.5f);
            prt.anchorMax = new Vector2(0f, 0.5f);
            prt.pivot = new Vector2(0f, 0.5f);
            prt.anchoredPosition = new Vector2(30f, 0f);
            prt.sizeDelta = new Vector2(150f, 190f);
            Image portBg = portObj.AddComponent<Image>();
            portBg.color = new Color(0.22f, 0.18f, 0.12f, 1.0f);

            GameObject portInitialObj = new GameObject("PortraitInitial");
            portInitialObj.transform.SetParent(portObj.transform, false);
            dialoguePortraitInitial = portInitialObj.AddComponent<TextMeshProUGUI>();
            dialoguePortraitInitial.font = defaultFont;
            dialoguePortraitInitial.fontSize = 64f * currentFontMultiplier;
            dialoguePortraitInitial.fontStyle = FontStyles.Bold;
            dialoguePortraitInitial.alignment = TextAlignmentOptions.Center;
            dialoguePortraitInitial.color = ColorHeaderAntiqueGold;
            dialoguePortraitInitial.text = "★";
            RectTransform pirt = portInitialObj.GetComponent<RectTransform>();
            pirt.anchorMin = Vector2.zero;
            pirt.anchorMax = Vector2.one;
            pirt.sizeDelta = Vector2.zero;
            allTextElements.Add(dialoguePortraitInitial);

            // Speaker Name TMP (>= 36 px)
            GameObject nameObj = new GameObject("SpeakerName");
            nameObj.transform.SetParent(dialoguePanel.transform, false);
            dialogueSpeakerText = nameObj.AddComponent<TextMeshProUGUI>();
            dialogueSpeakerText.font = defaultFont;
            dialogueSpeakerText.fontSize = 40f * currentFontMultiplier;
            dialogueSpeakerText.fontStyle = FontStyles.Bold;
            dialogueSpeakerText.color = ColorHeaderAntiqueGold;
            dialogueSpeakerText.text = "Pujari Ananda";
            RectTransform nrt = nameObj.GetComponent<RectTransform>();
            nrt.anchorMin = new Vector2(0f, 1f);
            nrt.anchorMax = new Vector2(1f, 1f);
            nrt.pivot = new Vector2(0f, 1f);
            nrt.anchoredPosition = new Vector2(210f, -16f);
            nrt.sizeDelta = new Vector2(-240f, 40f);
            allTextElements.Add(dialogueSpeakerText);

            // Dialogue Body TMP (>= 36 px)
            GameObject bodyObj = new GameObject("DialogueBody");
            bodyObj.transform.SetParent(dialoguePanel.transform, false);
            dialogueBodyText = bodyObj.AddComponent<TextMeshProUGUI>();
            dialogueBodyText.font = defaultFont;
            dialogueBodyText.fontSize = 36f * currentFontMultiplier;
            dialogueBodyText.color = ColorTextWarmIvory;
            dialogueBodyText.textWrappingMode = TextWrappingModes.Normal;
            dialogueBodyText.text = "Namaste, Investigator Aryan...";
            RectTransform brt = bodyObj.GetComponent<RectTransform>();
            brt.anchorMin = new Vector2(0f, 0f);
            brt.anchorMax = new Vector2(1f, 1f);
            brt.offsetMin = new Vector2(210f, 45f);
            brt.offsetMax = new Vector2(-24f, -58f);
            allTextElements.Add(dialogueBodyText);

            // Prompt TMP (>= 28 px)
            GameObject promptObj = new GameObject("DialoguePrompt");
            promptObj.transform.SetParent(dialoguePanel.transform, false);
            dialoguePromptText = promptObj.AddComponent<TextMeshProUGUI>();
            dialoguePromptText.font = defaultFont;
            dialoguePromptText.fontSize = 34f * currentFontMultiplier;
            dialoguePromptText.fontStyle = FontStyles.Italic;
            dialoguePromptText.color = ColorClueCyan;
            dialoguePromptText.alignment = TextAlignmentOptions.MidlineRight;
            dialoguePromptText.text = "[Space / Enter / E to advance]";
            RectTransform drt = promptObj.GetComponent<RectTransform>();
            drt.anchorMin = new Vector2(0f, 0f);
            drt.anchorMax = new Vector2(1f, 0f);
            drt.pivot = new Vector2(1f, 0f);
            drt.anchoredPosition = new Vector2(-24f, 12f);
            drt.sizeDelta = new Vector2(400f, 30f);
            allTextElements.Add(dialoguePromptText);

            // Choice Buttons Container
            dialogueChoicesContainer = new GameObject("DialogueChoicesContainer");
            dialogueChoicesContainer.transform.SetParent(dialoguePanel.transform, false);
            RectTransform crt = dialogueChoicesContainer.AddComponent<RectTransform>();
            crt.anchorMin = new Vector2(0f, 0f);
            crt.anchorMax = new Vector2(1f, 1f);
            crt.anchoredPosition = new Vector2(210f, -20f);
            crt.sizeDelta = new Vector2(-240f, -40f);

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
                btrt.anchoredPosition = new Vector2(0f, -40f - i * 65f);
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
                txt.fontSize = 34f * currentFontMultiplier;
                txt.color = ColorHeaderAntiqueGold;
                txt.alignment = TextAlignmentOptions.MidlineLeft;
                txt.text = $"[{i + 1}] Choice option";
                RectTransform tr = txtObj.GetComponent<RectTransform>();
                tr.anchorMin = Vector2.zero;
                tr.anchorMax = Vector2.one;
                tr.anchoredPosition = new Vector2(20f, 0f);
                tr.sizeDelta = new Vector2(-40f, 0f);

                choiceButtons[i] = btn;
                choiceButtonTexts[i] = txt;
                allTextElements.Add(txt);
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
            rt.sizeDelta = new Vector2(980f, 740f);

            pausePanelBg = pausePanel.AddComponent<Image>();
            pausePanelBg.sprite = CreateColorSprite(ColorPanelBgNormal);
            allPanelBackgrounds.Add(pausePanelBg);
            AddGoldBorder(pausePanel);

            // Title (>= 64 px)
            GameObject titleObj = new GameObject("PauseTitle");
            titleObj.transform.SetParent(pausePanel.transform, false);
            TextMeshProUGUI title = titleObj.AddComponent<TextMeshProUGUI>();
            title.font = defaultFont;
            title.fontSize = 64f * currentFontMultiplier;
            title.fontStyle = FontStyles.Bold;
            title.alignment = TextAlignmentOptions.Center;
            title.color = ColorHeaderAntiqueGold;
            title.text = "PAUSED";
            RectTransform trt = titleObj.GetComponent<RectTransform>();
            trt.anchorMin = new Vector2(0f, 1f);
            trt.anchorMax = new Vector2(1f, 1f);
            trt.pivot = new Vector2(0.5f, 1f);
            trt.anchoredPosition = new Vector2(0f, -25f);
            trt.sizeDelta = new Vector2(-60f, 70f);
            allTextElements.Add(title);

            // Buttons Container
            float startY = -105f;
            CreateMenuButton(pausePanel.transform, "Resume Game", new Vector2(0f, startY), () => TogglePause());
            CreateMenuButton(pausePanel.transform, "Replay Tutorial", new Vector2(0f, startY - 55f), () =>
            {
                TogglePause();
                TutorialManager.Instance.StartTutorial();
            });
            CreateMenuButton(pausePanel.transform, "Skip Tutorial", new Vector2(0f, startY - 110f), () =>
            {
                TogglePause();
                TutorialManager.Instance.SkipTutorial();
            });
            CreateMenuButton(pausePanel.transform, "Investigation Journal [J]", new Vector2(0f, startY - 165f), () =>
            {
                TogglePause();
                if (InvestigationJournal.Instance != null) InvestigationJournal.Instance.ToggleJournal();
            });
            CreateMenuButton(pausePanel.transform, "Temple Map [M]", new Vector2(0f, startY - 220f), () =>
            {
                TogglePause();
                if (TempleMapOverlay.Instance != null) TempleMapOverlay.Instance.ToggleMap();
            });

            // Settings Controls Box
            GameObject settingsBox = new GameObject("SettingsBox");
            settingsBox.transform.SetParent(pausePanel.transform, false);
            RectTransform srt = settingsBox.AddComponent<RectTransform>();
            srt.anchorMin = new Vector2(0.5f, 0f);
            srt.anchorMax = new Vector2(0.5f, 0f);
            srt.pivot = new Vector2(0.5f, 0f);
            srt.anchoredPosition = new Vector2(0f, 25f);
            srt.sizeDelta = new Vector2(900f, 310f);
            Image sbg = settingsBox.AddComponent<Image>();
            sbg.color = new Color(0.10f, 0.12f, 0.16f, 0.92f);

            // Text Size Scale Setting
            GameObject tsLabel = new GameObject("TextScaleLabel");
            tsLabel.transform.SetParent(settingsBox.transform, false);
            textSizeLabel = tsLabel.AddComponent<TextMeshProUGUI>();
            textSizeLabel.font = defaultFont;
            textSizeLabel.fontSize = 30f * currentFontMultiplier;
            textSizeLabel.color = ColorTextWarmIvory;
            textSizeLabel.text = $"Text Size: <color=#FFC857>{fontScaleNames[textScaleIndex]}</color>";
            RectTransform tsrt = tsLabel.GetComponent<RectTransform>();
            tsrt.anchorMin = new Vector2(0f, 1f);
            tsrt.anchorMax = new Vector2(0.6f, 1f);
            tsrt.anchoredPosition = new Vector2(30f, -20f);
            tsrt.sizeDelta = new Vector2(0f, 40f);
            allTextElements.Add(textSizeLabel);

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
            cbt.fontSize = 30f * currentFontMultiplier;
            cbt.alignment = TextAlignmentOptions.Center;
            cbt.color = Color.white;
            cbt.text = "Change Size";
            RectTransform cbtrt = cbTxt.GetComponent<RectTransform>();
            cbtrt.anchorMin = Vector2.zero;
            cbtrt.anchorMax = Vector2.one;
            cbtrt.sizeDelta = Vector2.zero;
            allTextElements.Add(cbt);

            // High Contrast Mode Toggle Setting
            GameObject hcLabel = new GameObject("HighContrastLabel");
            hcLabel.transform.SetParent(settingsBox.transform, false);
            highContrastLabel = hcLabel.AddComponent<TextMeshProUGUI>();
            highContrastLabel.font = defaultFont;
            highContrastLabel.fontSize = 30f * currentFontMultiplier;
            highContrastLabel.color = ColorTextWarmIvory;
            highContrastLabel.text = $"High Contrast: <color=#FFC857>{(isHighContrast ? "ON" : "OFF")}</color>";
            RectTransform hcrt = hcLabel.GetComponent<RectTransform>();
            hcrt.anchorMin = new Vector2(0f, 1f);
            hcrt.anchorMax = new Vector2(0.6f, 1f);
            hcrt.anchoredPosition = new Vector2(30f, -70f);
            hcrt.sizeDelta = new Vector2(0f, 40f);
            allTextElements.Add(highContrastLabel);

            // Toggle High Contrast Button
            GameObject hcBtn = new GameObject("ToggleHighContrastBtn");
            hcBtn.transform.SetParent(settingsBox.transform, false);
            RectTransform hcbrt = hcBtn.AddComponent<RectTransform>();
            hcbrt.anchorMin = new Vector2(0.65f, 1f);
            hcbrt.anchorMax = new Vector2(0.95f, 1f);
            hcbrt.anchoredPosition = new Vector2(0f, -70f);
            hcbrt.sizeDelta = new Vector2(0f, 40f);
            Image hcImg = hcBtn.AddComponent<Image>();
            hcImg.color = new Color(0.3f, 0.25f, 0.12f);
            Button hcbtn = hcBtn.AddComponent<Button>();
            hcbtn.onClick.AddListener(ToggleHighContrast);

            GameObject hcbtTxt = new GameObject("BtnText");
            hcbtTxt.transform.SetParent(hcBtn.transform, false);
            TextMeshProUGUI hcbt = hcbtTxt.AddComponent<TextMeshProUGUI>();
            hcbt.font = defaultFont;
            hcbt.fontSize = 30f * currentFontMultiplier;
            hcbt.alignment = TextAlignmentOptions.Center;
            hcbt.color = Color.white;
            hcbt.text = "Toggle Mode";
            RectTransform hcbtrt = hcbtTxt.GetComponent<RectTransform>();
            hcbtrt.anchorMin = Vector2.zero;
            hcbtrt.anchorMax = Vector2.one;
            hcbtrt.sizeDelta = Vector2.zero;
            allTextElements.Add(hcbt);

            // Volume Sliders (Master, Music, SFX)
            CreateVolumeSliderRow(settingsBox.transform, "Master Volume", new Vector2(30f, -125f), AudioManager.Instance.MasterVolume, (v) => AudioManager.Instance.SetMasterVolume(v));
            CreateVolumeSliderRow(settingsBox.transform, "Music Volume", new Vector2(30f, -180f), AudioManager.Instance.MusicVolume, (v) => AudioManager.Instance.SetMusicVolume(v));
            CreateVolumeSliderRow(settingsBox.transform, "SFX Volume", new Vector2(30f, -235f), AudioManager.Instance.SFXVolume, (v) => AudioManager.Instance.SetSFXVolume(v));

            pausePanel.SetActive(false);
        }

        private void BuildBottomHintBar(Transform parent)
        {
            bottomHintBar = new GameObject("BottomControlsHintBar");
            bottomHintBar.transform.SetParent(parent, false);

            RectTransform rt = bottomHintBar.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0f, 12f);
            rt.sizeDelta = new Vector2(1740f, 44f);

            bottomHintBarBg = bottomHintBar.AddComponent<Image>();
            bottomHintBarBg.sprite = CreateColorSprite(ColorPanelBgNormal);
            allPanelBackgrounds.Add(bottomHintBarBg);
            AddGoldBorder(bottomHintBar);

            GameObject textObj = new GameObject("HintText");
            textObj.transform.SetParent(bottomHintBar.transform, false);
            TextMeshProUGUI hintText = textObj.AddComponent<TextMeshProUGUI>();
            hintText.font = defaultFont;
            hintText.fontSize = 32f * currentFontMultiplier;
            hintText.textWrappingMode = TextWrappingModes.Normal;
            hintText.alignment = TextAlignmentOptions.Center;
            bottomHintText = hintText;
            hintText.color = ColorTextWarmIvory;
            hintText.text = "<color=#FFC857>[WASD]</color> Move  •  <color=#FFC857>[Shift]</color> Run  •  <color=#FFC857>[E]</color> Talk / Use  •  <color=#FFC857>[F]</color> Inspect  •  <color=#FFC857>[J]</color> Journal  •  <color=#FFC857>[M]</color> Map  •  <color=#FFC857>[H]</color> Guide  •  <color=#FFC857>[Esc]</color> Pause";

            RectTransform trt = textObj.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.sizeDelta = Vector2.zero;
            allTextElements.Add(hintText);
        }

        private void BuildPhotoModeToast(Transform parent)
        {
            photoModeToast = new GameObject("PhotoModeToast");
            photoModeToast.transform.SetParent(parent, false);

            RectTransform rt = photoModeToast.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -20f);
            rt.sizeDelta = new Vector2(620f, 50f);

            Image bg = photoModeToast.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.85f);

            GameObject txtObj = new GameObject("ToastText");
            txtObj.transform.SetParent(photoModeToast.transform, false);
            photoModeToastText = txtObj.AddComponent<TextMeshProUGUI>();
            photoModeToastText.font = defaultFont;
            photoModeToastText.fontSize = 30f;
            photoModeToastText.alignment = TextAlignmentOptions.Center;
            photoModeToastText.color = ColorHeaderAntiqueGold;
            photoModeToastText.text = "PHOTO MODE: UI Hidden. Press [P] to restore.";

            RectTransform trt = txtObj.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.sizeDelta = Vector2.zero;

            photoModeToast.SetActive(false);
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
            rt.sizeDelta = new Vector2(580f, 48f);

            Image img = btnObj.AddComponent<Image>();
            img.color = new Color(0.12f, 0.14f, 0.18f, 0.95f);

            Button btn = btnObj.AddComponent<Button>();
            ColorBlock cb = btn.colors;
            cb.highlightedColor = new Color(0.35f, 0.28f, 0.12f);
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
            txt.fontSize = 30f * currentFontMultiplier;
            txt.alignment = TextAlignmentOptions.Center;
            txt.color = ColorTextWarmIvory;
            txt.text = label;

            RectTransform trt = txtObj.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.sizeDelta = Vector2.zero;
            allTextElements.Add(txt);

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
            lbl.fontSize = 30f * currentFontMultiplier;
            lbl.color = ColorTextWarmIvory;
            lbl.text = label;
            RectTransform lrt = labelObj.GetComponent<RectTransform>();
            lrt.anchorMin = new Vector2(0f, 0f);
            lrt.anchorMax = new Vector2(0.45f, 1f);
            lrt.anchoredPosition = Vector2.zero;
            lrt.sizeDelta = Vector2.zero;
            allTextElements.Add(lbl);

            // Slider
            GameObject sliderObj = new GameObject("Slider");
            sliderObj.transform.SetParent(row.transform, false);
            RectTransform srt = sliderObj.AddComponent<RectTransform>();
            srt.anchorMin = new Vector2(0.5f, 0.5f);
            srt.anchorMax = new Vector2(0.95f, 0.5f);
            srt.pivot = new Vector2(0f, 0.5f);
            srt.anchoredPosition = Vector2.zero;
            srt.sizeDelta = new Vector2(0f, 20f);

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
            if (isPhotoMode || promptPanel == null) return;
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
            if (isPhotoMode || tutorialPanel == null) return;

            tutorialStepText.text = $"TUTORIAL · STEP {stepIndex} OF {totalSteps}";
            tutorialTitleText.text = title;
            tutorialKeycapText.text = keyIcon;
            tutorialBodyText.text = instruction;
            tutorialActionPromptText.text = prompt;

            tutorialPanel.SetActive(true);
            if (hudPanel != null) hudPanel.SetActive(false);
            if (bottomHintBar != null) bottomHintBar.SetActive(false);
        }

        public void HideTutorial()
        {
            if (tutorialPanel != null) tutorialPanel.SetActive(false);
            if (hudPanel != null && !isPhotoMode) hudPanel.SetActive(true);
            if (bottomHintBar != null && !isPhotoMode) bottomHintBar.SetActive(true);
        }

        public void ShowHowToPlay(Action onDismiss)
        {
            if (isPhotoMode) return;
            onHowToPlayDismiss = onDismiss;
            if (howToPlayPanel != null) howToPlayPanel.SetActive(true);
            if (hudPanel != null) hudPanel.SetActive(false);
            if (bottomHintBar != null) bottomHintBar.SetActive(false);
        }

        public void DismissHowToPlay()
        {
            if (howToPlayPanel != null) howToPlayPanel.SetActive(false);
            if (hudPanel != null && !isPhotoMode) hudPanel.SetActive(true);
            if (bottomHintBar != null && !isPhotoMode) bottomHintBar.SetActive(true);
            onHowToPlayDismiss?.Invoke();
            onHowToPlayDismiss = null;
        }

        /// <summary>Voice-ready hook: (speaker, text) for each displayed line (subtitles are the dialogue box).</summary>
        public static event Action<string, string> OnSubtitleLine;

        public void StartDialogue(string speaker, string[] lines, string[] choices = null, Action<int> onChoice = null, Action onComplete = null)
        {
            if (isPhotoMode) TogglePhotoMode();

            isDialogueActive = true;
            if (TempleMapOverlay.Instance != null && TempleMapOverlay.Instance.IsOpen) TempleMapOverlay.Instance.ToggleMap(); // map never covers a conversation
            activeDialogueLines = lines;
            activeDialogueChoices = choices;
            currentDialogueLine = 0;
            onDialogueCompletedCallback = onComplete;
            onChoiceCallback = onChoice;

            dialogueSpeakerText.text = speaker;
            if (dialoguePortraitInitial != null)
            {
                dialoguePortraitInitial.text = !string.IsNullOrEmpty(speaker) ? speaker.Substring(0, 1).ToUpperInvariant() : "★";
            }
            dialoguePanel.SetActive(true);
            if (bottomHintBar != null) bottomHintBar.SetActive(false);

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
            int colon = activeFullLine.IndexOf(": ");
            if (colon > 0 && colon < 32)
            {
                string who = activeFullLine.Substring(0, colon);
                activeFullLine = activeFullLine.Substring(colon + 2);
                dialogueSpeakerText.text = who;
                if (dialoguePortraitInitial != null) dialoguePortraitInitial.text = who.Substring(0, 1).ToUpperInvariant();
                EventBus.RaiseDialogueLine(who);
                OnSubtitleLine?.Invoke(who, activeFullLine);
            }
            else EventBus.RaiseDialogueLine(dialogueSpeakerText.text);
            dialogueChoicesContainer.SetActive(false);

            if (typewriterCoroutine != null) StopCoroutine(typewriterCoroutine);
            typewriterCoroutine = StartCoroutine(TypewriteText(activeFullLine, index == activeDialogueLines.Length - 1));
        }

        private IEnumerator TypewriteText(string text, bool isLastLine)
        {
            isTypewriting = true;
            dialogueBodyText.text = "";
            for (int i = 0; i <= text.Length; i++)
            {
                dialogueBodyText.text = text.Substring(0, i);
                yield return new WaitForSecondsRealtime(0.015f);
            }
            isTypewriting = false;

            // If last line has choices, display choice buttons
            if (isLastLine && activeDialogueChoices != null && activeDialogueChoices.Length > 0)
            {
                ShowDialogueChoices();
            }
        }

        private void ShowDialogueChoices()
        {
            dialogueChoicesContainer.SetActive(true);
            for (int i = 0; i < choiceButtons.Length; i++)
            {
                if (i < activeDialogueChoices.Length)
                {
                    choiceButtons[i].gameObject.SetActive(true);
                    choiceButtonTexts[i].text = $"[{i + 1}] {activeDialogueChoices[i]}";
                }
                else
                {
                    choiceButtons[i].gameObject.SetActive(false);
                }
            }
        }

        public void AdvanceDialogue()
        {
            // Never skip a choice: on the last line the choices must be shown and answered.
            if (currentDialogueLine >= activeDialogueLines.Length - 1 && activeDialogueChoices != null && activeDialogueChoices.Length > 0)
            {
                if (!dialogueChoicesContainer.activeSelf) ShowDialogueChoices();
                return;
            }
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

        /// <summary>Close the dialogue box without invoking completion callbacks (conversation cancelled).</summary>
        public void CancelDialogue()
        {
            onDialogueCompletedCallback = null; onChoiceCallback = null;
            if (typewriterCoroutine != null) StopCoroutine(typewriterCoroutine);
            isTypewriting = false;
            EndDialogue();
        }

        public void EndDialogue()
        {
            isDialogueActive = false;
            if (dialoguePanel != null) dialoguePanel.SetActive(false);
            if (bottomHintBar != null && !isPhotoMode) bottomHintBar.SetActive(true);
            AudioManager.Instance.SetDialogueDucking(false);
            onDialogueCompletedCallback?.Invoke();
            onDialogueCompletedCallback = null;
        }

        private void OnChoiceButtonClicked(int choiceIndex)
        {
            AudioManager.Instance.PlaySFX("sfx_ui_select");
            dialogueChoicesContainer.SetActive(false);
            Action<int> cb = onChoiceCallback;
            onChoiceCallback = null;
            cb?.Invoke(choiceIndex);
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
                textSizeLabel.text = $"Text Size: <color=#FFC857>{fontScaleNames[textScaleIndex]}</color>";
            }

            ApplyFontScaleToAll();
        }

        public void ToggleHighContrast()
        {
            isHighContrast = !isHighContrast;
            PlayerPrefs.SetInt("Settings_HighContrast", isHighContrast ? 1 : 0);
            PlayerPrefs.Save();

            if (highContrastLabel != null)
            {
                highContrastLabel.text = $"High Contrast: <color=#FFC857>{(isHighContrast ? "ON" : "OFF")}</color>";
            }

            ApplyHighContrastTheme();
        }

        private void ApplyHighContrastTheme()
        {
            Color panelBg = isHighContrast ? ColorPanelBgHighContrast : ColorPanelBgNormal;
            Color bodyColor = isHighContrast ? Color.white : ColorTextWarmIvory;
            Color headerColor = isHighContrast ? Color.yellow : ColorHeaderAntiqueGold;

            foreach (var img in allPanelBackgrounds)
            {
                if (img != null) { img.sprite = null; img.color = panelBg; } // one source of alpha (sprite x color was 72%)
            }

            if (objectiveTitleText != null) objectiveTitleText.color = headerColor;
            if (objectiveBodyText != null) objectiveBodyText.color = bodyColor;
            if (dialogueSpeakerText != null) dialogueSpeakerText.color = headerColor;
            if (dialogueBodyText != null) dialogueBodyText.color = bodyColor;
            if (tutorialTitleText != null) tutorialTitleText.color = headerColor;
            if (tutorialBodyText != null) tutorialBodyText.color = bodyColor;
        }

        public void TogglePhotoMode()
        {
            isPhotoMode = !isPhotoMode;
            EventBus.IsPhotoModeActive = isPhotoMode;

            if (hudPanel != null) hudPanel.SetActive(!isPhotoMode);
            if (promptPanel != null && isPhotoMode) promptPanel.SetActive(false);
            if (tutorialPanel != null && isPhotoMode) tutorialPanel.SetActive(false);
            if (howToPlayPanel != null && isPhotoMode) howToPlayPanel.SetActive(false);
            if (bottomHintBar != null) bottomHintBar.SetActive(!isPhotoMode);

            if (isPhotoMode && photoModeToast != null)
            {
                photoModeToast.SetActive(true);
                photoModeToastTimer = 3.0f;
            }
            else if (photoModeToast != null)
            {
                photoModeToast.SetActive(false);
            }
        }

        public void SetHudActive(bool active)
        {
            if (hudPanel != null && !isPhotoMode)
            {
                hudPanel.SetActive(active);
            }
        }

        public void HideAllUIForShowcase(bool hide)
        {
            if (mainCanvas != null)
            {
                mainCanvas.enabled = !hide;
            }
        }

        private bool themeApplied;
        private readonly Dictionary<TMP_Text, float> baseFontSizes = new Dictionary<TMP_Text, float>();

        private void ApplyFontScaleToAll()
        {
            if (mainCanvas == null) return;
            foreach (var t in mainCanvas.GetComponentsInChildren<TMP_Text>(true))
            {
                if (!baseFontSizes.TryGetValue(t, out float b)) continue;
                t.fontSize = b * currentFontMultiplier;
            }
        }

        /// <summary>Theme + layout pass: fonts, outline, base sizes, and stacking so text never overlaps.</summary>
        private void ThemeAndLayout()
        {
            if (mainCanvas == null) return;
            if (!themeApplied) { themeApplied = true; ApplyHighContrastTheme(); }
            foreach (var t in mainCanvas.GetComponentsInChildren<TMP_Text>(true))
            {
                if (baseFontSizes.ContainsKey(t)) continue;
                string n = t.name;
                baseFontSizes[t] = BaseSizeFor(n, t.fontSize / Mathf.Max(0.01f, currentFontMultiplier));
                t.fontSize = baseFontSizes[t] * currentFontMultiplier;
                bool title = n.Contains("Title") || n == "SpeakerName" || n == "PortraitInitial";
                HeritageFonts.Apply(t, title);
            }
            float canvasW = ((RectTransform)mainCanvas.transform).rect.width, canvasH = ((RectTransform)mainCanvas.transform).rect.height;
            if (hudPanel != null) { var hp = (RectTransform)hudPanel.transform; hp.sizeDelta = new Vector2(Mathf.Max(420f, canvasW * 0.28f), hp.sizeDelta.y); }
            StackPanel(hudPanel, 14f, 14f, 24f, 18f, 4f, 0f, objectiveTitleText, objectiveBodyText, objectiveCounterText);
            if (isDialogueActive)
            {
                if (promptPanel != null) promptPanel.SetActive(false);
                if (bottomHintBar != null) bottomHintBar.SetActive(false);
            }
            bool modal = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "MainMenu" || (GuideSystem.Instance != null && GuideSystem.Instance.IsOpen)
                || isDialogueActive || (tutorialPanel != null && tutorialPanel.activeSelf) || (howToPlayPanel != null && howToPlayPanel.activeSelf)
                || (InvestigationJournal.Instance != null && InvestigationJournal.Instance.IsOpen) || (TempleMapOverlay.Instance != null && TempleMapOverlay.Instance.IsOpen)
                || IsPauseOpen;
            foreach (var spec in ScreenText.Panels.Values) if (spec.dimBackground && ScreenText.IsLive(spec.lastFrame)) modal = true;
            SetVisible(hudPanel, !modal);
            SetVisible(bottomHintBar, !modal);
            SetVisible(dialoguePanel, !modal || isDialogueActive);
            ScreenText.HideLabels = modal;
            // Reserved zones for world labels (reference units, origin bottom-left).
            ScreenText.Reserved.Clear();
            if (hudPanel != null && !modal) { var hp = ((RectTransform)hudPanel.transform).rect; ScreenText.Reserved.Add(new Rect(24f, canvasH - 24f - hp.height - 8f, hp.width + 16f, hp.height + 16f)); }
            ScreenText.Reserved.Add(new Rect(0f, 0f, canvasW, 120f));            // controls bar + guide button strip
            MirrorPanel(howToPlayPanel, "howtoplay", PanelAnchor.Center, 1500f, true, 70);
            MirrorPanel(tutorialPanel, "tutorial", PanelAnchor.Center, 1300f, false, 65);
            if (!modal) MirrorPanel(promptPanel, "prompt", PanelAnchor.BottomCenter, 900f, false, 5);
            Hide(pausePanel); // drawn by HeritagePauseMenu (uGUI buttons + auto layout)
            if (dialoguePromptText != null)
            {
                var drt = dialoguePromptText.rectTransform;
                drt.anchorMin = new Vector2(1f, 0f); drt.anchorMax = new Vector2(1f, 0f); drt.pivot = new Vector2(1f, 0f);
                drt.anchoredPosition = new Vector2(-30f, 10f);
                drt.sizeDelta = new Vector2(Mathf.Ceil(dialoguePromptText.GetPreferredValues(dialoguePromptText.text, 2000f, 0f).x) + 8f, Mathf.Ceil(dialoguePromptText.fontSize * 1.4f));
            }
            if (bottomHintBar != null && bottomHintText != null)
            {
                // Single line, centred, auto-shrinks, never under the guide button (bottom-right).
                var hrt = (RectTransform)bottomHintBar.transform;
                float w = Mathf.Min(1300f, canvasW - 2f * 300f);
                hrt.sizeDelta = new Vector2(w, 46f);
                hrt.anchoredPosition = new Vector2(0f, 16f);
                bottomHintText.textWrappingMode = TextWrappingModes.NoWrap;
                bottomHintText.enableAutoSizing = true;
                bottomHintText.fontSizeMax = 24f * currentFontMultiplier; bottomHintText.fontSizeMin = 14f;
                var trt = bottomHintText.rectTransform; trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one; trt.offsetMin = new Vector2(16f, 4f); trt.offsetMax = new Vector2(-16f, -4f);
            }
            if (promptPanel != null && isDialogueActive && promptPanel.activeSelf) promptPanel.SetActive(false);
            if (dialoguePanel != null && dialoguePanel.activeSelf)
            {
                float choices = 0f;
                if (dialogueChoicesContainer != null && dialogueChoicesContainer.activeSelf && choiceButtons != null)
                {
                    int shown = 0;
                    foreach (var b in choiceButtons) if (b != null && b.gameObject.activeSelf) shown++;
                    choices = shown * 74f + 10f;
                }
                StackPanel(dialoguePanel, 16f, 64f + choices, 210f, 30f, 6f, 220f, dialogueSpeakerText, dialogueBodyText);
                if (dialogueChoicesContainer != null && choices > 0f)
                {
                    var crt = (RectTransform)dialogueChoicesContainer.transform;
                    crt.anchorMin = new Vector2(0f, 0f); crt.anchorMax = new Vector2(1f, 0f); crt.pivot = new Vector2(0f, 0f);
                    crt.offsetMin = new Vector2(210f, 74f); crt.offsetMax = new Vector2(-30f, 74f + choices);
                    for (int i = 0; i < choiceButtons.Length; i++)
                    {
                        if (choiceButtons[i] == null) continue;
                        var brt = (RectTransform)choiceButtons[i].transform;
                        brt.anchorMin = new Vector2(0f, 1f); brt.anchorMax = new Vector2(1f, 1f); brt.pivot = new Vector2(0f, 1f);
                        brt.anchoredPosition = new Vector2(0f, -i * 74f); brt.sizeDelta = new Vector2(0f, 64f);
                    }
                }
            }
        }

        public bool IsPauseOpen => pausePanel != null && pausePanel.activeSelf;

        private static float BaseSizeFor(string n, float current)
        {
            switch (n)
            {
                case "ObjectiveTitle": return 28f;
                case "ObjectiveBody": return 26f;
                case "ObjectiveCounter": return 24f;
                case "PromptAction": return 28f;
                case "KeyText": return 24f;
                case "SpeakerName": return 30f;
                case "DialogueBody": return 30f;
                case "DialoguePrompt": return 24f;
                case "PortraitInitial": return 56f;
                case "HintText": return 24f;
                case "ToastText": return 26f;
                case "BtnText": return 26f;
            }
            if (n.Contains("Title")) return 56f;
            if (n.Contains("Keycap")) return 32f;
            return Mathf.Min(current, 26f);
        }

        private static void SetVisible(GameObject panel, bool visible)
        {
            if (panel == null) return;
            var cg = (panel.TryGetComponent<CanvasGroup>(out var __c) ? __c : panel.AddComponent<CanvasGroup>());
            cg.alpha = visible ? 1f : 0f; cg.blocksRaycasts = visible;
        }

        private static void Hide(GameObject panel)
        {
            if (panel == null) return;
            var cg = (panel.TryGetComponent<CanvasGroup>(out var __c) ? __c : panel.AddComponent<CanvasGroup>());
            cg.alpha = 0f; cg.blocksRaycasts = false; cg.interactable = false;
        }

        /// <summary>Hides a fixed-layout legacy panel and redraws its texts with the auto-layout renderer.</summary>
        private static void MirrorPanel(GameObject panel, string id, PanelAnchor anchor, float width, bool dim, int order)
        {
            if (panel == null) return;
            Hide(panel);
            if (!panel.activeInHierarchy) return;
            string title = null; var body = new System.Text.StringBuilder(); string footer = null;
            foreach (var t in panel.GetComponentsInChildren<TMP_Text>(false))
            {
                if (string.IsNullOrWhiteSpace(t.text)) continue;
                string n = t.name;
                if (title == null && n.Contains("Title")) title = t.text;
                else if (n.Contains("Prompt") && n != "PromptAction") footer = t.text;
                else if (n == "KeyText") body.Append(ScreenText.Key(t.text)).Append(' ');
                else body.Append(t.text).Append(n == "TutorialStepText" || n == "TutorialKeycap" ? "\n" : "\n\n");
            }
            ScreenText.Panel(id, title, body.ToString().TrimEnd(), anchor, width, footer, dim, order);
        }

        /// <summary>Places texts top-to-bottom by their real preferred height and resizes the panel to fit.</summary>
        private static void StackPanel(GameObject panel, float padTop, float padBottom, float left, float right, float spacing, float minHeight, params TMP_Text[] items)
        {
            if (panel == null) return;
            var prt = (RectTransform)panel.transform;
            float width = prt.rect.width - left - right;
            if (width <= 10f) return;
            float y = padTop;
            foreach (var t in items)
            {
                if (t == null || !t.gameObject.activeSelf || string.IsNullOrEmpty(t.text)) continue;
                t.textWrappingMode = TextWrappingModes.Normal;
                t.overflowMode = TextOverflowModes.Overflow;
                float h = Mathf.Ceil(t.GetPreferredValues(t.text, width, 0f).y) + 4f;
                var rt = t.rectTransform;
                rt.anchorMin = new Vector2(0f, 1f); rt.anchorMax = new Vector2(1f, 1f); rt.pivot = new Vector2(0f, 1f);
                rt.offsetMin = new Vector2(left, -(y + h)); rt.offsetMax = new Vector2(-right, -y);
                y += h + spacing;
            }
            float total = Mathf.Max(minHeight, y - spacing + padBottom);
            prt.sizeDelta = new Vector2(prt.sizeDelta.x, total);
        }

        #endregion

        #region Event Handlers

        private void HandleClueFound(string clueId)
        {
            int currentClues = PlayerPrefs.GetInt("Clues_Found_Count", 0) + 1;
            PlayerPrefs.SetInt("Clues_Found_Count", currentClues);
            int relics = PlayerPrefs.GetInt("Relics_Recovered_Count", 0);
            StoryDialogueSystem.Instance?.Publish();
        }

        private void HandlePuzzleSolved(string puzzleId)
        {
            StoryDialogueSystem.Instance?.Publish();
        }

        private void HandleDialogueEvent(string charId)
        {
            // Handled when dialogues trigger
        }

        #endregion
    }
}
