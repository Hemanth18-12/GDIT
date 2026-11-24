using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using LegacyOfTemples.Core;
using LegacyOfTemples.Gameplay;

namespace LegacyOfTemples.UI
{
    /// <summary>
    /// Loading screen between maps: full-screen image with a slow zoom, thin gold bar driven by the real
    /// async load, chapter title, Dawn Clock line and one rotating suspense tip, with fades.
    /// </summary>
    public sealed class LoadingScreen : MonoBehaviour
    {
        public static LoadingScreen Instance { get; private set; }
        public static int LoadsShown { get; private set; }
        public static string LastImage { get; private set; } = "";
        public static float LastProgress { get; private set; }
        public const float MinSeconds = 2.2f;

        private CanvasGroup group; private RawImage image; private RectTransform barFill, track, glow, tipPill; private Image glowImg; private TMP_Text tipText, pctText, titleText, loadingText;
        private Texture2D[] images; private Texture2D fallback; private string[] tips; private int tipIndex; private float tipAt;



        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (Instance != null) return;
            var go = new GameObject("LoadingScreen"); DontDestroyOnLoad(go);
            Instance = go.AddComponent<LoadingScreen>();
        }

        private void Awake()
        {
            var c = gameObject.AddComponent<Canvas>(); c.renderMode = RenderMode.ScreenSpaceOverlay; c.sortingOrder = 900;
            var sc = gameObject.AddComponent<CanvasScaler>(); sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; sc.referenceResolution = new Vector2(1920, 1080); sc.matchWidthOrHeight = 0.5f;
            group = gameObject.AddComponent<CanvasGroup>(); group.alpha = 0f; group.blocksRaycasts = false;

            var black = Rect("Black", transform); black.gameObject.AddComponent<Image>().color = Color.black; Stretch(black);
            var imgRt = Rect("LoadingImage", transform); image = imgRt.gameObject.AddComponent<RawImage>(); Stretch(imgRt);
            fallback = Gradient(new Color(0.05f, 0.035f, 0.025f, 1f), new Color(0.23f, 0.15f, 0.09f, 1f));

            track = Rect("LoadingBarTrack", transform); track.gameObject.AddComponent<Image>().color = new Color(0.07f, 0.045f, 0.02f, 1f);
            ScreenTextRenderer.AddBorder(track, new Color(0.83f, 0.65f, 0.29f, 0.95f), 2f);
            barFill = Rect("LoadingBarFill", track); barFill.gameObject.AddComponent<Image>().color = new Color(1f, 0.74f, 0.22f, 1f);
            barFill.anchorMin = Vector2.zero; barFill.anchorMax = new Vector2(0, 1); barFill.offsetMin = new Vector2(3, 3); barFill.offsetMax = new Vector2(0, -3);
            glow = Rect("LoadingBarGlow", track); glowImg = glow.gameObject.AddComponent<Image>(); glowImg.raycastTarget = false;
            var gt = new Texture2D(32, 1, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int x = 0; x < 32; x++) { float a = 1f - Mathf.Abs(x - 15.5f) / 15.5f; gt.SetPixel(x, 0, new Color(1f, 0.95f, 0.7f, a * a)); }
            gt.Apply(); glowImg.sprite = Sprite.Create(gt, new UnityEngine.Rect(0, 0, 32, 1), new Vector2(0.5f, 0.5f));

            var grad = Rect("BottomGradient", transform); var gi = grad.gameObject.AddComponent<RawImage>(); gi.texture = Gradient(new Color(0f, 0f, 0f, 0.92f), new Color(0f, 0f, 0f, 0f)); gi.raycastTarget = false;
            grad.anchorMin = new Vector2(0f, 0f); grad.anchorMax = new Vector2(1f, 0.30f); grad.offsetMin = grad.offsetMax = Vector2.zero; grad.SetSiblingIndex(2);
            titleText = ScreenTextRenderer.MakeText(transform, "LoadingTitle", 46, ScreenTextRenderer.Gold, true, TextAlignmentOptions.BottomLeft); titleText.raycastTarget = false;
            titleText.text = "LEGACY OF THE TEMPLE"; titleText.characterSpacing = 8f; titleText.textWrappingMode = TextWrappingModes.NoWrap;
            loadingText = ScreenTextRenderer.MakeText(transform, "LoadingLabel", 24, ScreenTextRenderer.Ivory, false, TextAlignmentOptions.BottomRight); loadingText.raycastTarget = false; loadingText.textWrappingMode = TextWrappingModes.NoWrap;
            tipPill = Rect("LoadingTipPill", transform); tipPill.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0f);
            tipText = ScreenTextRenderer.MakeText(tipPill, "LoadingTip", 26, ScreenTextRenderer.Ivory, false, TextAlignmentOptions.Left);
            Stretch(tipText.rectTransform); tipText.rectTransform.offsetMin = new Vector2(16, 0); tipText.rectTransform.offsetMax = new Vector2(-16, 0);
            tipText.fontStyle = FontStyles.Italic; tipText.textWrappingMode = TextWrappingModes.NoWrap; tipText.enableAutoSizing = true; tipText.fontSizeMax = 26; tipText.fontSizeMin = 16; tipText.raycastTarget = false;
            pctText = ScreenTextRenderer.MakeText(transform, "LoadingPercent", 28, ScreenTextRenderer.Gold, true, TextAlignmentOptions.Right); pctText.raycastTarget = false; pctText.textWrappingMode = TextWrappingModes.NoWrap;

            images = Resources.LoadAll<Texture2D>("UI/Loading");
            System.Array.Sort(images, (a, b) => string.CompareOrdinal(a.name, b.name));
            SceneLoader.Handler = scene => StartCoroutine(Run(scene));
        }

        private static RectTransform Rect(string n, Transform parent) { var g = new GameObject(n, typeof(RectTransform)); g.transform.SetParent(parent, false); return (RectTransform)g.transform; }
        private static void Stretch(RectTransform r) { r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero; }
        private static Texture2D Gradient(Color bottom, Color top)
        {
            var t = new Texture2D(1, 64, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < 64; y++) t.SetPixel(0, y, Color.Lerp(bottom, top, Mathf.SmoothStep(0, 1, y / 63f)));
            t.Apply(); return t;
        }

        /// <summary>Which chapter a load to this scene begins (Courtyard is both chapter 1 and the ending).</summary>
        public static StoryChapter ChapterFor(string scene)
        {
            var story = StoryDialogueSystem.Instance;
            bool late = story != null && story.Current != null && story.Current.chapter != "ch1" && story.ObjectiveIndex > 0;
            StoryChapter found = null;
            foreach (var c in StoryData.File.chapters ?? new StoryChapter[0])
                if (c.scene == scene) { found = c; if (!(scene == Constants.Courtyard && late)) break; }
            return found;
        }

        private IEnumerator Run(string scene)
        {
            SceneLoader.IsLoading = true; LoadsShown++;
            int pick = PlayerPrefs.GetInt("Loading_ImageIndex", 0);
            Texture2D tex = images != null && images.Length > 0 ? images[pick % images.Length] : null;
            PlayerPrefs.SetInt("Loading_ImageIndex", pick + 1);
            if (tex == null) Debug.LogWarning("[LoadingScreen] No pictures in Resources/UI/Loading. Showing the dark gradient.");
            LastImage = tex != null ? tex.name : "(gradient fallback)";
            image.texture = tex != null ? tex : fallback;
            tips = StoryData.File.tips ?? new string[0];
            tipIndex = PlayerPrefs.GetInt("Loading_TipIndex", 0); PlayerPrefs.SetInt("Loading_TipIndex", tipIndex + 2); NextTip();
            SetBar(0f); group.blocksRaycasts = true;
            float aspect = tex != null ? (float)tex.width / tex.height : 16f / 9f;

            float t0 = Time.unscaledTime;
            for (float t = 0; t < 0.35f; t += Time.unscaledDeltaTime) { group.alpha = t / 0.35f; Place(aspect); yield return null; }
            group.alpha = 1f;
            Time.timeScale = 1f;
            var op = SceneManager.LoadSceneAsync(scene);
            op.allowSceneActivation = false;
            float shown = 0f;
            while (op.progress < 0.9f || Time.unscaledTime - t0 < MinSeconds || shown < 0.995f)
            {
                // the bar follows the real load (AsyncOperation.progress), smoothed, and never moves backwards
                float real = Mathf.Clamp01(op.progress / 0.9f) * Mathf.Clamp01((Time.unscaledTime - t0) / MinSeconds * 1.05f);
                shown = Mathf.Max(shown, Mathf.MoveTowards(shown, real, Time.unscaledDeltaTime * 0.9f));
                if (op.progress >= 0.9f && Time.unscaledTime - t0 >= MinSeconds) shown = Mathf.MoveTowards(shown, 1f, Time.unscaledDeltaTime * 2.5f);
                SetBar(shown); Place(aspect);
                if (Time.unscaledTime > tipAt) NextTip();
                yield return null;
            }
            SetBar(1f); Place(aspect);
            yield return new WaitForSecondsRealtime(0.25f); // let the full bar be seen
            op.allowSceneActivation = true;
            while (!op.isDone) { Place(aspect); yield return null; }
            yield return null;
            SceneLoader.IsLoading = false;
            for (float t = 0; t < 0.4f; t += Time.unscaledDeltaTime) { group.alpha = 1f - t / 0.4f; yield return null; }
            group.alpha = 0f; group.blocksRaycasts = false;
        }

        private void NextTip()
        {
            tipAt = Time.unscaledTime + 1.6f;
            tipText.text = tips != null && tips.Length > 0 ? "Tip: " + tips[tipIndex++ % tips.Length] : "";
        }

        private void SetBar(float p)
        {
            LastProgress = p; p = Mathf.Clamp01(p);
            barFill.anchorMax = new Vector2(p, 1f); barFill.offsetMax = new Vector2(p >= 0.999f ? -3 : 0, -3);
            glow.anchorMin = new Vector2(p, 0f); glow.anchorMax = new Vector2(p, 1f); glow.sizeDelta = new Vector2(90f, 22f); glow.anchoredPosition = Vector2.zero;
            glowImg.color = new Color(1f, 1f, 1f, p > 0.005f && p < 0.999f ? 0.55f + 0.35f * Mathf.Sin(Time.unscaledTime * 7f) : 0f);
            pctText.text = Mathf.RoundToInt(p * 100f) + "%"; if (loadingText != null) loadingText.text = "Loading" + new string('.', 1 + (int)(Time.unscaledTime * 2f) % 3);
        }

        /// <summary>Cover-fit the picture without stretching it (painted UI was cropped out of the art); the game's own UI sits in the bottom 15%.</summary>
        private void Place(float aspect)
        {
            float screen = (float)Screen.width / Mathf.Max(1, Screen.height);
            float w = 1f, h = 1f;
            if (aspect > screen) w = screen / aspect; else h = aspect / screen;
            image.uvRect = new UnityEngine.Rect((1f - w) * 0.5f, (1f - h) * 0.5f, w, h);
            void At(RectTransform r, float x0, float x1, float y, float height) { r.anchorMin = new Vector2(x0, 0f); r.anchorMax = new Vector2(x1, 0f); r.pivot = new Vector2(0.5f, 0.5f); r.sizeDelta = new Vector2(0f, height); r.anchoredPosition = new Vector2(0f, y); }
            At(titleText.rectTransform, 0.06f, 0.70f, 132f, 60f);
            At(loadingText.rectTransform, 0.60f, 0.88f, 104f, 34f);
            At(track, 0.06f, 0.94f, 76f, 14f);
            At(tipPill, 0.06f, 0.94f, 40f, 36f);
            var pr = pctText.rectTransform; pr.anchorMin = pr.anchorMax = new Vector2(0.94f, 0f); pr.pivot = new Vector2(1f, 0.5f); pr.sizeDelta = new Vector2(110f, 34f); pr.anchoredPosition = new Vector2(0f, 104f);
            pctText.alignment = TextAlignmentOptions.Right;
        }
    }
}
