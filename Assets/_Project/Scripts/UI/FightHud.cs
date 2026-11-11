using TMPro;
using UnityEngine;
using UnityEngine.UI;
using LegacyOfTemples.Core;
using LegacyOfTemples.Gameplay;

namespace LegacyOfTemples.UI
{
    /// <summary>Fight HUD: Aryan's health and stamina (top-left), enemy Guard bars (bottom-centre),
    /// a hit marker, and a red flash on the side the damage came from.</summary>
    public sealed class FightHud : MonoBehaviour
    {
        public static FightHud Instance { get; private set; }
        public bool Visible { get; private set; }
        private CanvasGroup group; private RectTransform hp, st; private Image hpImg;
        private readonly RectTransform[] guard = new RectTransform[2]; private readonly TMP_Text[] guardName = new TMP_Text[2]; private readonly GameObject[] guardRow = new GameObject[2];
        private readonly Image[] edge = new Image[4]; private TMP_Text marker; private Image low;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (Instance != null) return;
            var go = new GameObject("FightHud"); DontDestroyOnLoad(go);
            Instance = go.AddComponent<FightHud>();
        }

        private static RectTransform Rect(string n, Transform p) { var g = new GameObject(n, typeof(RectTransform)); g.transform.SetParent(p, false); return (RectTransform)g.transform; }

        private RectTransform Bar(string name, Transform parent, Vector2 size, Color fill, out Image fillImg)
        {
            var bg = Rect(name, parent); bg.sizeDelta = size; var bi = bg.gameObject.AddComponent<Image>(); bi.color = new Color(0f, 0f, 0f, 0.75f); bi.raycastTarget = false;
            ScreenTextRenderer.AddBorder(bg, new Color(1f, 0.78f, 0.34f, 0.8f), 2f);
            var f = Rect("Fill", bg); f.anchorMin = Vector2.zero; f.anchorMax = Vector2.one; f.offsetMin = new Vector2(2, 2); f.offsetMax = new Vector2(-2, -2); f.pivot = new Vector2(0, 0.5f);
            fillImg = f.gameObject.AddComponent<Image>(); fillImg.color = fill; fillImg.raycastTarget = false;
            return f;
        }

        private void Awake()
        {
            var c = gameObject.AddComponent<Canvas>(); c.renderMode = RenderMode.ScreenSpaceOverlay; c.sortingOrder = 230;
            var sc = gameObject.AddComponent<CanvasScaler>(); sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; sc.referenceResolution = new Vector2(1920, 1080); sc.matchWidthOrHeight = 0.5f;
            group = gameObject.AddComponent<CanvasGroup>(); group.alpha = 0f; group.blocksRaycasts = false; group.interactable = false;

            // damage direction edges (left, right, top, bottom) and low-health tint sit behind everything
            low = Rect("LowHealth", transform).gameObject.AddComponent<Image>(); low.raycastTarget = false; low.color = new Color(0.6f, 0f, 0f, 0f);
            var lr = low.rectTransform; lr.anchorMin = Vector2.zero; lr.anchorMax = Vector2.one; lr.offsetMin = lr.offsetMax = Vector2.zero;
            for (int i = 0; i < 4; i++)
            {
                edge[i] = Rect("DamageEdge" + i, transform).gameObject.AddComponent<Image>(); edge[i].raycastTarget = false; edge[i].color = new Color(0.9f, 0.05f, 0.05f, 0f);
                var r = edge[i].rectTransform;
                if (i == 0) { r.anchorMin = new Vector2(0, 0); r.anchorMax = new Vector2(0.06f, 1); }
                else if (i == 1) { r.anchorMin = new Vector2(0.94f, 0); r.anchorMax = new Vector2(1, 1); }
                else if (i == 2) { r.anchorMin = new Vector2(0, 0.92f); r.anchorMax = new Vector2(1, 1); }
                else { r.anchorMin = new Vector2(0, 0); r.anchorMax = new Vector2(1, 0.08f); }
                r.offsetMin = r.offsetMax = Vector2.zero;
            }

            var block = Rect("PlayerBars", transform); block.anchorMin = block.anchorMax = block.pivot = new Vector2(0, 1); block.anchoredPosition = new Vector2(32, -32); block.sizeDelta = new Vector2(420, 96);
            ScreenTextRenderer.MakePanel(block, "PlayerBarsPanel", out var panel); panel.anchorMin = Vector2.zero; panel.anchorMax = Vector2.one; panel.offsetMin = panel.offsetMax = Vector2.zero;
            var name = ScreenTextRenderer.MakeText(block, "FightPlayerName", 24, ScreenTextRenderer.Gold, true, TextAlignmentOptions.TopLeft);
            name.text = "ARYAN"; name.raycastTarget = false; var nr = name.rectTransform; nr.anchorMin = new Vector2(0, 1); nr.anchorMax = new Vector2(1, 1); nr.pivot = new Vector2(0, 1); nr.anchoredPosition = new Vector2(16, -8); nr.sizeDelta = new Vector2(-32, 30);
            hp = Bar("HealthBar", block, new Vector2(388, 22), new Color(0.56f, 0.89f, 0.66f), out hpImg);
            var hb = (RectTransform)hp.parent; hb.anchorMin = hb.anchorMax = hb.pivot = new Vector2(0, 1); hb.anchoredPosition = new Vector2(16, -42);
            st = Bar("StaminaBar", block, new Vector2(388, 14), new Color(1f, 0.78f, 0.34f), out _);
            var sb = (RectTransform)st.parent; sb.anchorMin = sb.anchorMax = sb.pivot = new Vector2(0, 1); sb.anchoredPosition = new Vector2(16, -70);

            for (int i = 0; i < 2; i++)
            {
                var row = Rect("EnemyRow" + i, transform); guardRow[i] = row.gameObject;
                row.anchorMin = row.anchorMax = row.pivot = new Vector2(0.5f, 0); row.anchoredPosition = new Vector2(0, 20 + i * 62); row.sizeDelta = new Vector2(640, 54);
                ScreenTextRenderer.MakePanel(row, "EnemyRowPanel", out var rp); rp.anchorMin = Vector2.zero; rp.anchorMax = Vector2.one; rp.offsetMin = rp.offsetMax = Vector2.zero;
                guardName[i] = ScreenTextRenderer.MakeText(row, "EnemyName" + i, 22, ScreenTextRenderer.Ivory, true, TextAlignmentOptions.Left);
                guardName[i].raycastTarget = false; var gr = guardName[i].rectTransform; gr.anchorMin = new Vector2(0, 0); gr.anchorMax = new Vector2(0, 1); gr.pivot = new Vector2(0, 0.5f); gr.anchoredPosition = new Vector2(14, 0); gr.sizeDelta = new Vector2(250, 0);
                guardName[i].textWrappingMode = TextWrappingModes.NoWrap; guardName[i].overflowMode = TextOverflowModes.Ellipsis;
                guard[i] = Bar("GuardBar" + i, row, new Vector2(350, 18), new Color(1f, 0.71f, 0.66f), out _);
                var gb = (RectTransform)guard[i].parent; gb.anchorMin = gb.anchorMax = gb.pivot = new Vector2(1, 0.5f); gb.anchoredPosition = new Vector2(-14, 0);
            }

            marker = ScreenTextRenderer.MakeText(transform, "HitMarker", 44, Color.white, true, TextAlignmentOptions.Center);
            marker.text = "X"; marker.raycastTarget = false; var mr = marker.rectTransform; mr.anchorMin = mr.anchorMax = mr.pivot = new Vector2(0.5f, 0.5f); mr.sizeDelta = new Vector2(80, 60); mr.anchoredPosition = new Vector2(0, 40);
        }

        private static void SetFill(RectTransform fill, float v) { fill.anchorMax = new Vector2(Mathf.Clamp01(v), 1f); fill.offsetMax = new Vector2(v >= 0.999f ? -2 : 0, -2); }

        private void Update()
        {
            var f = FightDirector.Instance;
            bool ui = InputGate.UiOpen != null && InputGate.UiOpen();
            Visible = f != null && CombatState.InCombat && f.Phase > 0 && !ui && !SceneLoader.IsLoading;
            group.alpha = Mathf.MoveTowards(group.alpha, Visible ? 1f : 0f, Time.unscaledDeltaTime * 4f);
            if (f == null || group.alpha <= 0f) return;
            SetFill(hp, f.Health / 100f); SetFill(st, f.Stamina / 100f);
            hpImg.color = f.Health < 30f ? new Color(1f, 0.45f, 0.4f) : new Color(0.56f, 0.89f, 0.66f);
            var enemies = new[] { f.Bhaskar, f.Kishore };
            for (int i = 0; i < 2; i++)
            {
                var e = enemies[i];
                bool show = e != null && e.State != EnemyState.Idle && e.State != EnemyState.Ally && !(i == 1 && e.State == EnemyState.Down) && !(f.Phase == 3);
                guardRow[i].SetActive(show);
                if (!show) continue;
                guardName[i].text = (i == 0 ? "BHASKAR" : "KISHORE") + (e == f.Target ? "  <" : "") + (i == 0 ? "   P" + f.Phase : "");
                SetFill(guard[i], e.MaxGuard > 0f ? e.Guard / e.MaxGuard : 0f);
            }
            marker.alpha = Mathf.Clamp01(1f - (Time.unscaledTime - f.LastHitMarker) / 0.18f);
            // directional damage flash
            float k = Mathf.Clamp01(1f - (Time.time - f.LastDamageTime) / 0.6f);
            var cam = Camera.main; Vector3 d = f.LastDamageDir;
            float right = cam != null ? Vector3.Dot(cam.transform.right, d) : 0f, fwd = cam != null ? Vector3.Dot(cam.transform.forward, d) : 1f;
            edge[0].color = new Color(0.9f, 0.05f, 0.05f, k * Mathf.Clamp01(-right) * 0.7f);
            edge[1].color = new Color(0.9f, 0.05f, 0.05f, k * Mathf.Clamp01(right) * 0.7f);
            edge[2].color = new Color(0.9f, 0.05f, 0.05f, k * Mathf.Clamp01(fwd) * 0.6f);
            edge[3].color = new Color(0.9f, 0.05f, 0.05f, k * Mathf.Clamp01(-fwd) * 0.6f);
            low.color = new Color(0.6f, 0f, 0f, f.Health < 30f ? 0.10f + 0.06f * Mathf.Sin(Time.unscaledTime * 6f) : 0f);
        }
    }
}
