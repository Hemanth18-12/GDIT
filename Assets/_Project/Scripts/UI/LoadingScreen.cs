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
        public const float MinSeconds = 2.5f;

        private CanvasGroup group; private RawImage image; private RectTransform barFill; private TMP_Text chapterText, titleText, clockText, tipText;
        private LoadingImageSet set; private Texture2D fallback, gradient;

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
            gradient = Gradient(new Color(0, 0, 0, 0.9f), new Color(0, 0, 0, 0f));
            var grad = Rect("BottomGradient", transform); var gi = grad.gameObject.AddComponent<RawImage>(); gi.texture = gradient;
            grad.anchorMin = new Vector2(0, 0); grad.anchorMax = new Vector2(1, 0.42f); grad.offsetMin = grad.offsetMax = Vector2.zero;
            fallback = Gradient(new Color(0.05f, 0.035f, 0.025f, 1f), new Color(0.23f, 0.15f, 0.09f, 1f));

            float s = ScreenTextRenderer.Scale;
            chapterText = Text("LoadingChapter", 28 * s, ScreenTextRenderer.Gold, true, 300);
            titleText = Text("LoadingTitle", 56 * s, ScreenTextRenderer.Ivory, true, 236);
            clockText = Text("LoadingClock", 30 * s, ScreenTextRenderer.Gold, false, 176);
            tipText = Text("LoadingTip", 24 * s, ScreenTextRenderer.Ivory, false, 112); tipText.fontStyle = FontStyles.Italic;

            var track = Rect("LoadingBarTrack", transform); track.gameObject.AddComponent<Image>().color = new Color(1, 1, 1, 0.2f);
            track.anchorMin = new Vector2(0, 0); track.anchorMax = new Vector2(0.6f, 0); track.pivot = new Vector2(0, 0);
            track.offsetMin = new Vector2(96, 72); track.offsetMax = new Vector2(96, 76);
            barFill = Rect("LoadingBarFill", track); barFill.gameObject.AddComponent<Image>().color = new Color(0.83f, 0.65f, 0.29f, 1f);
            barFill.anchorMin = Vector2.zero; barFill.anchorMax = new Vector2(0, 1); barFill.offsetMin = barFill.offsetMax = Vector2.zero;

            set = Resources.Load<LoadingImageSet>("UI/LoadingImages");
            SceneLoader.Handler = scene => StartCoroutine(Run(scene));
        }

        private static RectTransform Rect(string n, Transform parent) { var g = new GameObject(n, typeof(RectTransform)); g.transform.SetParent(parent, false); return (RectTransform)g.transform; }
        private static void Stretch(RectTransform r) { r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero; }
        private TMP_Text Text(string n, float size, Color c, bool title, float y)
        {
            var t = ScreenTextRenderer.MakeText(transform, n, size, c, title, TextAlignmentOptions.BottomLeft);
            var r = t.rectTransform; r.anchorMin = new Vector2(0, 0); r.anchorMax = new Vector2(1, 0); r.pivot = new Vector2(0, 0);
            r.offsetMin = new Vector2(96, y); r.offsetMax = new Vector2(-96, y + size * 1.3f);
            t.textWrappingMode = TextWrappingModes.NoWrap; t.overflowMode = TextOverflowModes.Ellipsis; t.raycastTarget = false;
            return t;
        }
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
            var ch = ChapterFor(scene);
            Texture2D tex = ch != null && set != null ? set.Find(ch.image) : null;
            if (tex == null) Debug.LogWarning($"[LoadingScreen] No image for '{scene}' (expected '{(ch != null ? ch.image : "?")}' in Assets/_Project/UI/Loading). Showing the dark gradient.");
            LastImage = tex != null ? tex.name : "(gradient fallback)";
            image.texture = tex != null ? tex : fallback;
            chapterText.text = ch == null ? "" : ch.n >= 6 ? "" : "CHAPTER " + ch.n;
            titleText.text = ch == null ? scene.ToUpperInvariant() : ch.title;
            clockText.text = DawnClock.Instance != null && ch != null && ch.n < 6 ? DawnClock.Instance.DawnIn : (ch != null ? "05:45" : "");
            var tips = StoryData.File.tips ?? new string[0];
            int ti = PlayerPrefs.GetInt("Loading_TipIndex", 0);
            tipText.text = tips.Length > 0 ? "\"" + tips[ti % tips.Length] + "\"" : "";
            PlayerPrefs.SetInt("Loading_TipIndex", ti + 1);
            SetBar(0f); group.blocksRaycasts = true;
            float aspect = tex != null ? (float)tex.width / tex.height : 16f / 9f;

            float t0 = Time.unscaledTime;
            for (float t = 0; t < 0.4f; t += Time.unscaledDeltaTime) { group.alpha = t / 0.4f; Zoom(t0, aspect); yield return null; }
            group.alpha = 1f;
            Time.timeScale = 1f;
            var op = SceneManager.LoadSceneAsync(scene);
            op.allowSceneActivation = false;
            float shown = 0f;
            while (op.progress < 0.9f || Time.unscaledTime - t0 < MinSeconds || shown < 0.995f)
            {
                float real = Mathf.Clamp01(op.progress / 0.9f) * Mathf.Clamp01((Time.unscaledTime - t0) / MinSeconds * 1.05f);
                shown = Mathf.Max(shown, Mathf.MoveTowards(shown, real, Time.unscaledDeltaTime * 1.5f)); // never backwards
                if (op.progress >= 0.9f && Time.unscaledTime - t0 >= MinSeconds) shown = Mathf.MoveTowards(shown, 1f, Time.unscaledDeltaTime * 3f);
                SetBar(shown); Zoom(t0, aspect);
                yield return null;
            }
            SetBar(1f);
            op.allowSceneActivation = true;
            while (!op.isDone) { Zoom(t0, aspect); yield return null; }
            yield return null;
            SceneLoader.IsLoading = false;
            for (float t = 0; t < 0.4f; t += Time.unscaledDeltaTime) { group.alpha = 1f - t / 0.4f; Zoom(t0, aspect); yield return null; }
            group.alpha = 0f; group.blocksRaycasts = false;
        }

        private void SetBar(float p) { LastProgress = p; barFill.anchorMax = new Vector2(Mathf.Clamp01(p), 1f); }

        /// <summary>Cover-fit the image and zoom slowly from 100% to 106% over 12 s.</summary>
        private void Zoom(float t0, float aspect)
        {
            float z = 1f + 0.06f * Mathf.Clamp01((Time.unscaledTime - t0) / 12f);
            float screen = (float)Screen.width / Mathf.Max(1, Screen.height);
            float w = 1f, h = 1f;
            if (aspect > screen) w = screen / aspect; else h = aspect / screen;
            w /= z; h /= z;
            image.uvRect = new Rect((1f - w) * 0.5f, (1f - h) * 0.5f, w, h);
        }
    }
}
