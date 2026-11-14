using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;
using LegacyOfTemples.Core;

namespace LegacyOfTemples.UI
{
    /// <summary>
    /// When the game starts, the intro video (StreamingAssets/Intro/intro.mp4: temple reveal and title, the robbery, the
    /// Pujari's discovery, recorded from the game itself) plays full screen before the main menu. Hold Esc to skip.
    /// </summary>
    public sealed class StartupIntroVideo : MonoBehaviour
    {
        public static bool Playing { get; private set; }
        public static bool Played { get; private set; }
        private VideoPlayer vp; private RenderTexture rt; private float held, started; private EventSystem es;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            if (Played || SceneManager.GetActiveScene().name != "MainMenu") return;
            var args = System.Environment.GetCommandLineArgs();
            if (System.Array.IndexOf(args, "-noIntroVideo") >= 0 || System.Array.IndexOf(args, "-introVideoPart") >= 0) return;
            string path = Path.Combine(Application.streamingAssetsPath, "Intro", "intro.mp4");
            if (!File.Exists(path)) { Debug.Log("[IntroVideo] no video at " + path); return; }
            new GameObject("StartupIntroVideo").AddComponent<StartupIntroVideo>().Begin(path);
        }

        private void Begin(string path)
        {
            Playing = true; Played = true; started = Time.unscaledTime;
            var c = gameObject.AddComponent<Canvas>(); c.renderMode = RenderMode.ScreenSpaceOverlay; c.sortingOrder = 2000;
            var sc = gameObject.AddComponent<CanvasScaler>(); sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; sc.referenceResolution = new Vector2(1920, 1080); sc.matchWidthOrHeight = 0.5f;
            var bg = new GameObject("Black", typeof(RectTransform)).AddComponent<Image>(); bg.transform.SetParent(transform, false); bg.color = Color.black; Full(bg.rectTransform);
            rt = new RenderTexture(1920, 1080, 0); var img = new GameObject("Video", typeof(RectTransform)).AddComponent<RawImage>(); img.transform.SetParent(transform, false); img.texture = rt; Full(img.rectTransform);
            var fit = img.gameObject.AddComponent<AspectRatioFitter>(); fit.aspectMode = AspectRatioFitter.AspectMode.FitInParent; fit.aspectRatio = 16f / 9f;
            var hint = ScreenTextRenderer.MakeText(transform, "VideoSkipKeyHint", 26, ScreenTextRenderer.Ivory, false, TMPro.TextAlignmentOptions.TopRight);
            hint.text = "HOLD " + ScreenText.Key("Esc") + " TO SKIP"; var hr = hint.rectTransform; hr.anchorMin = hr.anchorMax = hr.pivot = new Vector2(1f, 1f); hr.anchoredPosition = new Vector2(-40f, -32f); hr.sizeDelta = new Vector2(420f, 40f);
            vp = gameObject.AddComponent<VideoPlayer>(); vp.url = "file://" + path; vp.renderMode = VideoRenderMode.RenderTexture; vp.targetTexture = rt;
            vp.audioOutputMode = VideoAudioOutputMode.Direct; vp.isLooping = false; vp.playOnAwake = false; vp.skipOnDrop = true;
            vp.loopPointReached += _ => End(); vp.errorReceived += (_, e) => { Debug.LogWarning("[IntroVideo] " + e); End(); };
            vp.Play();
            es = EventSystem.current; if (es != null) es.enabled = false;   // the menu cannot be clicked through the video
            AudioManager.Instance?.StopBGM();
        }

        private static void Full(RectTransform r) { r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero; }

        private void Update()
        {
            if (!Playing) return;
            var kb = Keyboard.current; var gp = Gamepad.current;
            bool hold = (kb != null && kb.escapeKey.isPressed) || (gp != null && gp.startButton.isPressed);
            held = hold ? held + Time.unscaledDeltaTime : 0f;
            if (held > 1f || Time.unscaledTime - started > 60f) End();
        }

        public void End()
        {
            if (!Playing) return;
            Playing = false; if (vp != null) vp.Stop();
            if (es != null) es.enabled = true;
            AudioManager.Instance?.PlayBGM("bgm_menu");
            if (rt != null) rt.Release();
            Destroy(gameObject);
        }
    }
}
