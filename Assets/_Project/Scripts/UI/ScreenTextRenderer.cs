using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using LegacyOfTemples.Core;

namespace LegacyOfTemples.UI
{
    /// <summary>
    /// Draws every LegacyOfTemples.Core.ScreenText request with TextMeshPro: auto-sized panels
    /// (layout groups + content size fitters, word wrap, scroll when too tall), shared fonts and palette,
    /// minimum sizes at 1080p, Text Size and High Contrast settings, and non-overlapping world labels.
    /// </summary>
    public sealed class ScreenTextRenderer : MonoBehaviour
    {
        public static ScreenTextRenderer Instance { get; private set; }

        private static readonly float[] Multipliers = { 0.9f, 1.0f, 1.12f, 1.25f };
        public const float TitleSize = 56f, HeadingSize = 28f, BodySize = 26f, FooterSize = 24f, LabelSize = 22f;

        private Canvas canvas;
        private RectTransform root;
        private Image dim;
        private readonly Dictionary<string, PanelView> panels = new Dictionary<string, PanelView>();
        private readonly Dictionary<string, LabelView> labels = new Dictionary<string, LabelView>();

        private sealed class PanelView
        {
            public RectTransform rt; public Image bg; public TMP_Text title, body, footer;
            public RectTransform viewport; public RectTransform content; public LayoutElement viewportLE;
            public float scroll;
        }
        private sealed class LabelView { public RectTransform rt; public TMP_Text text; public Image bg; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (Instance != null) return;
            var go = new GameObject("ScreenTextRenderer");
            DontDestroyOnLoad(go);
            go.AddComponent<ScreenTextRenderer>();
        }

        public static float Scale => Multipliers[Mathf.Clamp(PlayerPrefs.GetInt("Settings_TextScaleIndex", 1), 0, 3)];
        public static bool HighContrast => PlayerPrefs.GetInt("Settings_HighContrast", 0) == 1;
        public static Color PanelColor => HighContrast ? new Color(0f, 0f, 0f, 0.97f) : new Color(0f, 0f, 0f, 0.85f);
        public static readonly Color Gold = new Color(1f, 0.784f, 0.341f), Ivory = new Color(1f, 0.957f, 0.839f);

        private void Awake()
        {
            Instance = this;
            canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 300;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            gameObject.AddComponent<GraphicRaycaster>();
            root = (RectTransform)transform;

            var dimGo = new GameObject("Dim", typeof(RectTransform));
            dimGo.transform.SetParent(transform, false);
            var drt = (RectTransform)dimGo.transform; drt.anchorMin = Vector2.zero; drt.anchorMax = Vector2.one; drt.offsetMin = drt.offsetMax = Vector2.zero;
            dim = dimGo.AddComponent<Image>(); dim.color = new Color(0, 0, 0, 0.55f); dim.raycastTarget = false;
            dimGo.SetActive(false);
        }

        public static TextMeshProUGUI MakeText(Transform parent, string name, float size, Color color, bool title, TextAlignmentOptions align = TextAlignmentOptions.TopLeft)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<TextMeshProUGUI>();
            HeritageFonts.Apply(t, title);
            t.fontSize = size; t.color = color; t.alignment = align;
            t.textWrappingMode = TextWrappingModes.Normal; t.richText = true; t.raycastTarget = false;
            return t;
        }

        public static Image MakePanel(Transform parent, string name, out RectTransform rt)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            rt = (RectTransform)go.transform;
            var img = go.AddComponent<Image>();
            img.color = PanelColor;
            AddBorder(rt, new Color(Gold.r, Gold.g, Gold.b, 0.9f), 2f);
            return img;
        }

        /// <summary>Thin border made of four edge images (does not tint the panel like Outline does).</summary>
        public static void AddBorder(RectTransform rt, Color c, float w)
        {
            Vector2[,] edges = { { new Vector2(0, 1), new Vector2(1, 1) }, { new Vector2(0, 0), new Vector2(1, 0) }, { new Vector2(0, 0), new Vector2(0, 1) }, { new Vector2(1, 0), new Vector2(1, 1) } };
            for (int i = 0; i < 4; i++)
            {
                var e = new GameObject("Border" + i, typeof(RectTransform));
                e.transform.SetParent(rt, false);
                var ert = (RectTransform)e.transform;
                ert.anchorMin = edges[i, 0]; ert.anchorMax = edges[i, 1];
                ert.sizeDelta = i < 2 ? new Vector2(0, w) : new Vector2(w, 0);
                ert.anchoredPosition = Vector2.zero;
                var im = e.AddComponent<Image>(); im.color = c; im.raycastTarget = false;
                e.AddComponent<LayoutElement>().ignoreLayout = true;
            }
        }

        private PanelView CreatePanel(string id)
        {
            var v = new PanelView();
            v.bg = MakePanel(root, "Panel_" + id, out v.rt);
            var vlg = v.rt.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(36, 36, 28, 28); vlg.spacing = 14;
            vlg.childControlHeight = true; vlg.childControlWidth = true; vlg.childForceExpandHeight = false; vlg.childForceExpandWidth = true;
            var csf = v.rt.gameObject.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            v.title = MakeText(v.rt, "PanelTitle", HeadingSize, Gold, true);

            var vp = new GameObject("Viewport", typeof(RectTransform));
            vp.transform.SetParent(v.rt, false);
            v.viewport = (RectTransform)vp.transform;
            vp.AddComponent<RectMask2D>();
            v.viewportLE = vp.AddComponent<LayoutElement>();
            var content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(vp.transform, false);
            v.content = (RectTransform)content.transform;
            v.content.anchorMin = new Vector2(0, 1); v.content.anchorMax = new Vector2(1, 1); v.content.pivot = new Vector2(0.5f, 1);
            v.body = MakeText(v.content, "PanelBody", BodySize, Ivory, false);
            var brt = v.body.rectTransform; brt.anchorMin = new Vector2(0, 1); brt.anchorMax = new Vector2(1, 1); brt.pivot = new Vector2(0.5f, 1);

            v.footer = MakeText(v.rt, "PanelKeyHint", FooterSize, Ivory, false, TextAlignmentOptions.Center);
            panels[id] = v;
            return v;
        }

        private void LateUpdate()
        {
            float s = Scale;
            bool anyDim = false;
            var live = new List<PanelSpec>();
            foreach (var spec in ScreenText.Panels.Values) if (ScreenText.IsLive(spec.lastFrame)) live.Add(spec);
            live.Sort((a, b) => a.order.CompareTo(b.order));
            // A big (dimmed) panel is exclusive: small side panels and prompts hide while it is up.
            if (live.Exists(p => p.dimBackground)) live.RemoveAll(p => !p.dimBackground);

            foreach (var kv in panels) kv.Value.rt.gameObject.SetActive(false);
            var stackOffset = new Dictionary<PanelAnchor, float>();
            float screenW = root.rect.width, screenH = root.rect.height;

            foreach (var spec in live)
            {
                if (!panels.TryGetValue(spec.id, out var v)) v = CreatePanel(spec.id);
                v.rt.gameObject.SetActive(true);
                v.rt.SetAsLastSibling();
                v.bg.color = PanelColor;
                bool big = spec.anchor == PanelAnchor.Center;
                anyDim |= spec.dimBackground;

                v.title.gameObject.SetActive(!string.IsNullOrEmpty(spec.title));
                v.title.text = spec.title ?? "";
                v.title.fontSize = (big ? TitleSize : HeadingSize) * s;
                v.title.alignment = big ? TextAlignmentOptions.Top : TextAlignmentOptions.TopLeft;
                v.title.name = big ? "PanelTitle" : "PanelObjectiveHeading";
                v.body.text = spec.body ?? "";
                v.body.fontSize = BodySize * s;
                v.body.color = HighContrast ? Color.white : Ivory;
                v.footer.gameObject.SetActive(!string.IsNullOrEmpty(spec.footer));
                v.footer.text = spec.footer ?? "";
                v.footer.fontSize = FooterSize * s;

                float width = Mathf.Min(spec.width, screenW - 64f);
                v.rt.sizeDelta = new Vector2(width, v.rt.sizeDelta.y);
                float inner = width - 72f;
                float bodyH = string.IsNullOrEmpty(v.body.text) ? 0f : v.body.GetPreferredValues(v.body.text, inner, 0f).y + 6f;
                v.body.rectTransform.sizeDelta = new Vector2(0f, bodyH);
                v.content.sizeDelta = new Vector2(0f, bodyH);
                // Height budget from the last laid-out frame: everything except the viewport.
                float chrome = Mathf.Max(0f, v.rt.rect.height - v.viewportLE.preferredHeight);
                chrome = Mathf.Max(chrome, 260f);
                float maxH = Mathf.Min(Mathf.Max(spec.maxHeight, 900f), screenH - (spec.anchor == PanelAnchor.Center ? 120f : 330f));
                float room = Mathf.Max(120f, maxH - chrome);
                float vpH = Mathf.Min(bodyH, room);
                v.viewportLE.preferredHeight = vpH; v.viewportLE.minHeight = vpH;
                v.viewport.gameObject.SetActive(bodyH > 0f);

                // Scroll long content with mouse wheel / PageUp / PageDown / arrow keys.
                if (bodyH > room)
                {
                    float delta = 0f;
                    var m = Mouse.current; var kb = Keyboard.current;
                    if (m != null) delta -= m.scroll.ReadValue().y * 0.5f;
                    if (kb != null) { if (kb.pageDownKey.isPressed || kb.downArrowKey.isPressed) delta += 900f * Time.unscaledDeltaTime; if (kb.pageUpKey.isPressed || kb.upArrowKey.isPressed) delta -= 900f * Time.unscaledDeltaTime; }
                    v.scroll = Mathf.Clamp(v.scroll + delta, 0f, bodyH - room);
                    if (v.footer.gameObject.activeSelf && !v.footer.text.Contains("scroll")) v.footer.text += "   " + ScreenText.Key("Mouse wheel") + " scroll";
                }
                else v.scroll = 0f;
                v.content.anchoredPosition = new Vector2(0f, v.scroll);

                LayoutRebuilder.ForceRebuildLayoutImmediate(v.rt);
                float h = v.rt.rect.height;
                if (h > screenH - 48f && vpH > 120f)
                {   // first-frame chrome estimate was stale: shrink the scroll viewport so the panel fits
                    vpH = Mathf.Max(120f, vpH - (h - (screenH - 48f)));
                    v.viewportLE.preferredHeight = vpH; v.viewportLE.minHeight = vpH;
                    LayoutRebuilder.ForceRebuildLayoutImmediate(v.rt);
                    h = v.rt.rect.height;
                }
                stackOffset.TryGetValue(spec.anchor, out float off);
                Place(v.rt, spec.anchor, off);
                stackOffset[spec.anchor] = off + h + 16f;
            }
            dim.gameObject.SetActive(anyDim);
            if (anyDim) dim.transform.SetAsFirstSibling();

            UpdateLabels(s);
        }

        private static void Place(RectTransform rt, PanelAnchor a, float offset)
        {
            Vector2 anchor, pivot, pos;
            const float m = 32f;
            switch (a)
            {
                case PanelAnchor.TopLeft: anchor = new Vector2(0, 1); pivot = new Vector2(0, 1); pos = new Vector2(m, -m - offset); break;
                case PanelAnchor.TopCenter: anchor = new Vector2(0.5f, 1); pivot = new Vector2(0.5f, 1); pos = new Vector2(0, -m - offset); break;
                case PanelAnchor.TopRight: anchor = new Vector2(1, 1); pivot = new Vector2(1, 1); pos = new Vector2(-m, -m - offset); break;
                case PanelAnchor.BottomCenter: anchor = new Vector2(0.5f, 0); pivot = new Vector2(0.5f, 0); pos = new Vector2(0, 150 + offset); break;
                case PanelAnchor.BottomLeft: anchor = new Vector2(0, 0); pivot = new Vector2(0, 0); pos = new Vector2(m, 110 + offset); break;
                case PanelAnchor.BottomRight: anchor = new Vector2(1, 0); pivot = new Vector2(1, 0); pos = new Vector2(-m, 200 + offset); break;
                default: anchor = new Vector2(0.5f, 0.5f); pivot = new Vector2(0.5f, 0.5f); pos = new Vector2(0, -offset); break;
            }
            rt.anchorMin = rt.anchorMax = anchor; rt.pivot = pivot; rt.anchoredPosition = pos;
        }

        private void UpdateLabels(float s)
        {
            foreach (var kv in labels) kv.Value.rt.gameObject.SetActive(false);
            var cam = Camera.main;
            if (cam == null || ScreenText.HideLabels) return;
            var placed = new List<Rect>(ScreenText.Reserved);
            var live = new List<WorldLabelSpec>();
            foreach (var l in ScreenText.Labels.Values) if (ScreenText.IsLive(l.lastFrame)) live.Add(l);
            live.Sort((a, b) => (a.worldPos - cam.transform.position).sqrMagnitude.CompareTo((b.worldPos - cam.transform.position).sqrMagnitude));
            float k = root.rect.height / Mathf.Max(1f, Screen.height);
            foreach (var spec in live)
            {
                Vector3 sp = cam.WorldToScreenPoint(spec.worldPos);
                if (sp.z <= 0.2f || sp.x < 0f || sp.x > Screen.width || sp.y < 0f || sp.y > Screen.height * 0.92f) continue;
                if (!labels.TryGetValue(spec.id, out var v))
                {
                    v = new LabelView();
                    v.bg = MakePanel(root, "WorldLabel_" + spec.id, out v.rt);
                    var hlg = v.rt.gameObject.AddComponent<HorizontalLayoutGroup>();
                    hlg.padding = new RectOffset(16, 16, 6, 6); hlg.childControlWidth = true; hlg.childControlHeight = true;
                    var csf = v.rt.gameObject.AddComponent<ContentSizeFitter>();
                    csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize; csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                    v.text = MakeText(v.rt, "LabelText", LabelSize, Ivory, false, TextAlignmentOptions.Center);
                    v.text.textWrappingMode = TextWrappingModes.NoWrap;
                    v.rt.pivot = new Vector2(0.5f, 0f);
                    v.rt.anchorMin = v.rt.anchorMax = Vector2.zero;
                    labels[spec.id] = v;
                }
                v.rt.gameObject.SetActive(true);
                v.bg.color = new Color(0f, 0f, 0f, PanelColor.a * spec.color.a);
                v.text.text = spec.text; v.text.color = spec.color; v.text.fontSize = LabelSize * s;
                LayoutRebuilder.ForceRebuildLayoutImmediate(v.rt);
                Vector2 size = v.rt.rect.size;
                Vector2 pos = new Vector2(sp.x * k, sp.y * k);
                var r = new Rect(pos.x - size.x / 2, pos.y, size.x, size.y);
                for (int guard = 0; guard < 8; guard++)
                {
                    bool hit = false;
                    foreach (var p in placed) if (p.Overlaps(r)) { r.y = p.yMax + 6f; hit = true; }
                    if (!hit) break;
                }
                bool blocked = false;
                foreach (var z in ScreenText.Reserved) if (z.Overlaps(r)) { blocked = true; break; }
                if (blocked || r.yMax > root.rect.height - 8f || r.xMin < 8f || r.xMax > root.rect.width - 8f) { v.rt.gameObject.SetActive(false); continue; }
                placed.Add(r);
                v.rt.anchoredPosition = new Vector2(r.center.x, r.y);
            }
        }
    }
}
