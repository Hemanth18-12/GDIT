using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using LegacyOfTemples.Characters;
using LegacyOfTemples.Core;
using LegacyOfTemples.Gameplay;

namespace LegacyOfTemples.UI
{
    /// <summary>One on-screen button. Works with mouse clicks and touch (Input System UI pointer events).</summary>
    public sealed class TouchButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler
    {
        public string action; public bool hold; public Image ring, fill, cooldown; public TMP_Text label;
        public bool Pressed { get; private set; }
        public int Presses { get; private set; }
        public static int Hovering;
        private bool over;

        public void OnPointerDown(PointerEventData e)
        {
            Pressed = true; Presses++;
            if (hold) { if (action == "block") VirtualInput.Block = true; if (action == "crouch") VirtualInput.CrouchHeld = !VirtualInput.CrouchHeld; }
            else VirtualInput.Press(action);
        }
        public void OnPointerUp(PointerEventData e) { Pressed = false; if (hold && action == "block") VirtualInput.Block = false; }
        public void OnPointerEnter(PointerEventData e) { if (!over) { over = true; Hovering++; } }
        public void OnPointerExit(PointerEventData e) { if (over) { over = false; Hovering--; } }
        private void OnDisable() { if (over) { over = false; Hovering--; } if (Pressed && hold && action == "block") VirtualInput.Block = false; Pressed = false; }

        private void Update()
        {
            bool lit = Pressed || (action == "crouch" && VirtualInput.CrouchHeld);
            transform.localScale = Vector3.one * (Pressed ? 0.92f : 1f);
            fill.color = lit ? new Color(1f, 0.78f, 0.34f, 0.55f) : new Color(0f, 0f, 0f, 0.45f);
        }
    }

    /// <summary>
    /// On-screen controls in the style of mobile action games (own art, generated at runtime):
    /// floating joystick with a sprint ring on the left, a cluster of gold-ring buttons on the right
    /// (ATTACK, HEAVY, BLOCK, DODGE, JUMP, CROUCH, FLASH, contextual INTERACT), small Journal / Map / Guide / Pause
    /// buttons on the left edge, and drag on the right half to turn the camera.
    /// Settings: "On-screen controls" Off / In fights (default) / Always, plus size and opacity.
    /// </summary>
    public sealed class TouchControls : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public static TouchControls Instance { get; private set; }
        public bool Visible { get; private set; }
        public readonly Dictionary<string, TouchButton> Buttons = new Dictionary<string, TouchButton>();
        private CanvasGroup group; private RectTransform cluster, column, stickBase, stickKnob, sprintRing, lookArea, stickArea;
        private Sprite disc, ringSprite; private int stickPointer = -99; private Vector2 stickOrigin;
        private PlayerController player;
        private const float StickRadius = 110f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (Instance != null) return;
            var go = new GameObject("TouchControls"); DontDestroyOnLoad(go);
            Instance = go.AddComponent<TouchControls>();
        }

        private static Sprite MakeSprite(bool ring)
        {
            const int n = 128; var t = new Texture2D(n, n, TextureFormat.RGBA32, false);
            for (int y = 0; y < n; y++) for (int x = 0; x < n; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), Vector2.one * n * 0.5f) / (n * 0.5f);
                float a = ring ? Mathf.Clamp01((1f - Mathf.Abs(d - 0.93f) / 0.055f)) : Mathf.Clamp01((0.9f - d) * 40f);
                t.SetPixel(x, y, new Color(1, 1, 1, a));
            }
            t.Apply(); return Sprite.Create(t, new Rect(0, 0, n, n), Vector2.one * 0.5f, 100f);
        }

        private static RectTransform Rect(string n, Transform p) { var g = new GameObject(n, typeof(RectTransform)); g.transform.SetParent(p, false); return (RectTransform)g.transform; }

        private void Awake()
        {
            var c = gameObject.AddComponent<Canvas>(); c.renderMode = RenderMode.ScreenSpaceOverlay; c.sortingOrder = 240;
            var sc = gameObject.AddComponent<CanvasScaler>(); sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; sc.referenceResolution = new Vector2(1920, 1080); sc.matchWidthOrHeight = 0.5f;
            gameObject.AddComponent<GraphicRaycaster>();
            group = gameObject.AddComponent<CanvasGroup>(); group.alpha = 0f; group.blocksRaycasts = false;
            disc = MakeSprite(false); ringSprite = MakeSprite(true);

            // drag areas (behind the buttons): right = camera, left = floating joystick. This component receives their pointer events.
            lookArea = Rect("LookArea", transform); lookArea.anchorMin = new Vector2(0.42f, 0.1f); lookArea.anchorMax = new Vector2(1f, 0.82f); lookArea.offsetMin = lookArea.offsetMax = Vector2.zero;
            lookArea.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, 0);
            stickArea = Rect("StickArea", transform); stickArea.anchorMin = new Vector2(0.05f, 0.1f); stickArea.anchorMax = new Vector2(0.42f, 0.7f); stickArea.offsetMin = stickArea.offsetMax = Vector2.zero;
            stickArea.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, 0);

            stickBase = Rect("StickBase", transform); stickBase.sizeDelta = Vector2.one * StickRadius * 2f; stickBase.anchorMin = stickBase.anchorMax = Vector2.zero;
            var sb = stickBase.gameObject.AddComponent<Image>(); sb.sprite = disc; sb.color = new Color(0, 0, 0, 0.35f); sb.raycastTarget = false;
            sprintRing = Rect("SprintRing", stickBase); sprintRing.anchorMin = Vector2.zero; sprintRing.anchorMax = Vector2.one; sprintRing.offsetMin = Vector2.one * -14f; sprintRing.offsetMax = Vector2.one * 14f;
            var sr = sprintRing.gameObject.AddComponent<Image>(); sr.sprite = ringSprite; sr.color = new Color(1f, 0.78f, 0.34f, 0.5f); sr.raycastTarget = false;
            stickKnob = Rect("StickKnob", stickBase); stickKnob.sizeDelta = Vector2.one * 90f;
            var sk = stickKnob.gameObject.AddComponent<Image>(); sk.sprite = disc; sk.color = new Color(1f, 0.78f, 0.34f, 0.8f); sk.raycastTarget = false;
            stickBase.gameObject.SetActive(false);

            cluster = Rect("ButtonCluster", transform); cluster.anchorMin = cluster.anchorMax = cluster.pivot = new Vector2(1, 0); cluster.anchoredPosition = Vector2.zero; cluster.sizeDelta = new Vector2(620, 540);
            Add(cluster, "attack", "ATTACK", 190, 300, 170, false, 30);
            Add(cluster, "heavy", "HEAVY", 372, 212, 112, false, 22);
            Add(cluster, "block", "BLOCK", 388, 362, 112, true, 22);
            Add(cluster, "dodge", "DODGE", 250, 468, 104, false, 21);
            Add(cluster, "jump", "JUMP", 84, 462, 98, false, 21);
            Add(cluster, "crouch", "CROUCH", 72, 168, 92, true, 18);
            Add(cluster, "flash", "FLASH", 512, 286, 96, false, 20);
            Add(cluster, "interact", "USE", 524, 428, 104, false, 24);

            column = Rect("MenuColumn", transform); column.anchorMin = column.anchorMax = column.pivot = new Vector2(0, 0.5f); column.anchoredPosition = new Vector2(20, 40); column.sizeDelta = new Vector2(76, 310);
            Add(column, "pause", "II", 38, 272, 66, false, 22);
            Add(column, "guide", "?", 38, 196, 66, false, 26);
            Add(column, "map", "MAP", 38, 120, 66, false, 16);
            Add(column, "journal", "LOG", 38, 44, 66, false, 16);
        }

        /// <summary>x is the distance of the centre from the parent's right edge, y from its bottom edge.</summary>
        private void Add(RectTransform parent, string action, string text, float x, float y, float size, bool hold, float font)
        {
            var rt = Rect("Touch_" + action, parent); rt.anchorMin = rt.anchorMax = new Vector2(1, 0); rt.pivot = Vector2.one * 0.5f; rt.anchoredPosition = new Vector2(-x, y); rt.sizeDelta = Vector2.one * size;
            var fill = rt.gameObject.AddComponent<Image>(); fill.sprite = disc; fill.color = new Color(0, 0, 0, 0.45f);
            var ring = Rect("Ring", rt).gameObject.AddComponent<Image>(); ring.sprite = ringSprite; ring.color = new Color(1f, 0.78f, 0.34f, 0.95f); ring.raycastTarget = false;
            ring.rectTransform.anchorMin = Vector2.zero; ring.rectTransform.anchorMax = Vector2.one; ring.rectTransform.offsetMin = ring.rectTransform.offsetMax = Vector2.zero;
            var cd = Rect("Cooldown", rt).gameObject.AddComponent<Image>(); cd.sprite = disc; cd.color = new Color(0, 0, 0, 0.6f); cd.raycastTarget = false;
            cd.type = Image.Type.Filled; cd.fillMethod = Image.FillMethod.Radial360; cd.fillOrigin = 2; cd.fillAmount = 0f;
            cd.rectTransform.anchorMin = Vector2.zero; cd.rectTransform.anchorMax = Vector2.one; cd.rectTransform.offsetMin = cd.rectTransform.offsetMax = Vector2.zero;
            var label = ScreenTextRenderer.MakeText(rt, "TouchLabel_" + action, font, ScreenTextRenderer.Ivory, true, TextAlignmentOptions.Center);
            label.text = text; label.raycastTarget = false; label.textWrappingMode = TextWrappingModes.NoWrap;
            label.rectTransform.anchorMin = Vector2.zero; label.rectTransform.anchorMax = Vector2.one; label.rectTransform.offsetMin = label.rectTransform.offsetMax = Vector2.zero;
            var b = rt.gameObject.AddComponent<TouchButton>(); b.action = action; b.hold = hold; b.ring = ring; b.fill = fill; b.cooldown = cd; b.label = label;
            Buttons[action] = b;
        }

        /// <summary>Simulate a full click on a button (used by tests and the screenshot bot).</summary>
        public bool Click(string action)
        {
            if (!Buttons.TryGetValue(action, out var b) || !b.gameObject.activeInHierarchy || EventSystem.current == null) return false;
            var e = new PointerEventData(EventSystem.current) { position = RectTransformUtility.WorldToScreenPoint(null, b.transform.position) };
            ExecuteEvents.Execute(b.gameObject, e, ExecuteEvents.pointerDownHandler);
            ExecuteEvents.Execute(b.gameObject, e, ExecuteEvents.pointerUpHandler);
            return true;
        }

        // ---- drag areas ----
        public void OnPointerDown(PointerEventData e)
        {
            if (!RectTransformUtility.RectangleContainsScreenPoint(stickArea, e.position) || stickPointer != -99) return;
            stickPointer = e.pointerId; stickOrigin = e.position;
            stickBase.gameObject.SetActive(true); stickBase.position = e.position; stickKnob.anchoredPosition = Vector2.zero;
        }
        public void OnDrag(PointerEventData e)
        {
            if (e.pointerId == stickPointer)
            {
                float scale = transform.lossyScale.x; Vector2 d = (e.position - stickOrigin) / Mathf.Max(0.01f, scale);
                float m = d.magnitude; Vector2 clamped = m > StickRadius ? d / m * StickRadius : d;
                stickKnob.anchoredPosition = clamped;
                VirtualInput.Move = m < 14f ? Vector2.zero : clamped / StickRadius;
                VirtualInput.Sprint = m > StickRadius * 0.95f; // pushed out to the sprint ring
            }
            else VirtualInput.Look += e.delta * 0.22f;
        }
        public void OnPointerUp(PointerEventData e)
        {
            if (e.pointerId != stickPointer) return;
            stickPointer = -99; VirtualInput.Move = Vector2.zero; VirtualInput.Sprint = false; stickBase.gameObject.SetActive(false);
        }

        private static bool Gameplay(string s) => s == Constants.Courtyard || s == Constants.Sanctum || s == Constants.ShrineCrime || s == Constants.PuzzleWall || s == Constants.JunglePath;

        private void Update()
        {
            int mode = GameOptions.TouchMode;
            bool ui = InputGate.UiOpen != null && InputGate.UiOpen();
            bool want = Gameplay(SceneManager.GetActiveScene().name) && !SceneLoader.IsLoading && !ui && Time.timeScale > 0f &&
                        (mode == 2 || (mode == 1 && CombatState.InCombat));
            if (want && EventSystem.current == null) { var es = new GameObject("EventSystem"); es.AddComponent<EventSystem>(); es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>(); }
            if (Visible && !want) { stickPointer = -99; VirtualInput.Move = Vector2.zero; VirtualInput.Sprint = false; VirtualInput.Block = false; stickBase.gameObject.SetActive(false); }
            Visible = want; VirtualInput.TouchVisible = want;
            group.alpha = want ? GameOptions.TouchOpacity + 0.2f : 0f; group.blocksRaycasts = want;
            FightDirector.PointerOverUi = want && (TouchButton.Hovering > 0 || stickPointer != -99);
            if (!want) return;
            cluster.localScale = Vector3.one * GameOptions.TouchScale; column.localScale = Vector3.one * GameOptions.TouchScale;
            sprintRing.GetComponent<Image>().color = new Color(1f, 0.78f, 0.34f, VirtualInput.Sprint ? 1f : 0.4f);

            bool fight = CombatState.InCombat;
            if (player == null) player = FindAnyObjectByType<PlayerController>();
            var f = FightDirector.Instance;
            bool canUse = player != null && player.CurrentInteractable != null;
            if (fight && f != null) canUse |= f.Grappling || (f.Bhaskar != null && f.Bhaskar.State == EnemyState.Staggered) || (f.Kishore != null && f.Kishore.State == EnemyState.Staggered);
            Buttons["interact"].gameObject.SetActive(canUse);
            foreach (string a in new[] { "attack", "heavy", "block", "dodge" }) Buttons[a].gameObject.SetActive(fight);
            Buttons["crouch"].gameObject.SetActive(!fight);
            Buttons["flash"].label.text = fight ? "FLASH" : "LIGHT";
            Buttons["flash"].cooldown.fillAmount = fight ? 1f - Flashlight.FlashReady01 : 0f;
            Buttons["dodge"].cooldown.fillAmount = fight && f != null ? 1f - f.DodgeReady01 : 0f;
            Buttons["heavy"].cooldown.fillAmount = fight && f != null ? 1f - Mathf.Clamp01(f.Stamina / 20f) : 0f;
        }
    }
}
