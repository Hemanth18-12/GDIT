using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using LegacyOfTemples.Core;
using LegacyOfTemples.Gameplay;

namespace LegacyOfTemples.UI
{
    /// <summary>Pause menu (Esc) with real buttons, keyboard/gamepad navigation and an auto-sized layout.</summary>
    public sealed class HeritagePauseMenu : MonoBehaviour
    {
        private static readonly string[] TextSizes = { "Small", "Medium", "Large", "Extra Large" };
        private RectTransform panel;
        private RectTransform viewport;
        private ScrollRect scroll;
        private readonly List<GameObject> items = new List<GameObject>();
        private bool shown;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            var go = new GameObject("HeritagePauseMenu");
            DontDestroyOnLoad(go);
            go.AddComponent<HeritagePauseMenu>();
        }

        private void Awake()
        {
            var c = gameObject.AddComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay; c.sortingOrder = 280;
            var sc = gameObject.AddComponent<CanvasScaler>();
            sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; sc.referenceResolution = new Vector2(1920, 1080); sc.matchWidthOrHeight = 0.5f;
            gameObject.AddComponent<GraphicRaycaster>();
            ScreenTextRenderer.MakePanel(transform, "PauseMenuViewport", out viewport);
            viewport.anchorMin = new Vector2(0.5f, 0.06f); viewport.anchorMax = new Vector2(0.5f, 0.94f);
            viewport.pivot = new Vector2(0.5f, 0.5f); viewport.sizeDelta = new Vector2(820f, 0f);
            viewport.gameObject.AddComponent<RectMask2D>();
            scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false; scroll.vertical = true; scroll.scrollSensitivity = 42f;
            scroll.movementType = ScrollRect.MovementType.Clamped; scroll.viewport = viewport;
            var content = new GameObject("PauseMenuContent", typeof(RectTransform));
            content.transform.SetParent(viewport, false); panel = content.GetComponent<RectTransform>();
            panel.anchorMin = new Vector2(0f, 1f); panel.anchorMax = Vector2.one; panel.pivot = new Vector2(0.5f, 1f);
            panel.sizeDelta = Vector2.zero; scroll.content = panel;
            var v = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            v.padding = new RectOffset(40, 40, 32, 32); v.spacing = 8; v.childControlHeight = true; v.childControlWidth = true; v.childForceExpandHeight = false;
            panel.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            viewport.gameObject.SetActive(false);
        }

        private void Update()
        {
            bool open = UIManager.Instance != null && UIManager.Instance.IsPauseOpen;
            if (open && !shown) Build();
            if (!open && shown) { viewport.gameObject.SetActive(false); shown = false; showControls = false; }
        }

        private void Build()
        {
            shown = true;
            foreach (var g in items) { g.SetActive(false); Destroy(g); }
            items.Clear();
            viewport.gameObject.SetActive(true);
            var t = ScreenTextRenderer.MakeText(panel, "PauseTitle", ScreenTextRenderer.TitleSize * ScreenTextRenderer.Scale, ScreenTextRenderer.Gold, true, TextAlignmentOptions.Center);
            t.text = "PAUSED"; items.Add(t.gameObject);
            if (showControls)
            {
                t.text = "CONTROLS";
                var ct = ScreenTextRenderer.MakeText(panel, "PauseControlsBody", 21 * ScreenTextRenderer.Scale, ScreenTextRenderer.Ivory, false, TextAlignmentOptions.Left); ct.lineSpacing = -8f;
                ct.text = ControlsText.Body; ct.textWrappingMode = TextWrappingModes.Normal; items.Add(ct.gameObject);
                var back = Button("BACK", () => { showControls = false; Rebuild(); });
                EventSystem.current?.SetSelectedGameObject(back);
                return;
            }
            var first = Button("RESUME", () => UIManager.Instance.TogglePause());
            Button("CONTROLS", () => { showControls = true; Rebuild(); });
            Button("GUIDE: WHAT DO I DO?", () => { UIManager.Instance.TogglePause(); GuideSystem.Instance?.Toggle(); });
            int ts = PlayerPrefs.GetInt("Settings_TextScaleIndex", 1);
            Button("Text size: " + TextSizes[Mathf.Clamp(ts, 0, 3)], () => { UIManager.Instance.CycleTextScale(); Rebuild(); });
            Button("High contrast: " + (ScreenTextRenderer.HighContrast ? "ON" : "OFF"), () => { UIManager.Instance.ToggleHighContrast(); Rebuild(); });
            Button("RETRY SECTION HERE", () => { UIManager.Instance.TogglePause(); DawnClock.Instance?.RetryOnSpot(false); });
            var audio = AudioManager.Instance;
            VolumeSlider("Master", audio.MasterVolume, audio.SetMasterVolume);
            VolumeSlider("Music", audio.MusicVolume, audio.SetMusicVolume);
            VolumeSlider("Ambience", audio.AmbienceVolume, audio.SetAmbienceVolume);
            VolumeSlider("Effects", audio.SFXVolume, audio.SetSFXVolume);
            VolumeSlider("Voice", VoicePlayer.Volume, audio.SetVoiceVolume);
            Button("Difficulty: " + GameOptions.DifficultyNames[GameOptions.Difficulty], () => { GameOptions.Difficulty = (GameOptions.Difficulty + 1) % 3; Rebuild(); });
            Button("Voice lines: " + GameOptions.VoiceFrequencyNames[GameOptions.VoiceFrequency], () => { GameOptions.VoiceFrequency = (GameOptions.VoiceFrequency + 1) % 3; Rebuild(); });
            Button("Subtitles: " + (GameOptions.Subtitles ? "ON" : "OFF"), () => { GameOptions.Subtitles = !GameOptions.Subtitles; Rebuild(); });
            Button("Demo mode (F9 menu): " + (GameOptions.DemoMode ? "On" : "Off"), () => { GameOptions.DemoMode = !GameOptions.DemoMode; Rebuild(); });
            Button("Weather effects: " + GameOptions.WeatherNames[GameOptions.WeatherQuality], () => { GameOptions.WeatherQuality = (GameOptions.WeatherQuality + 1) % 3; Rebuild(); });
            Button("Graphics: " + GameOptions.GraphicsNames[GameOptions.Graphics], () => { GameOptions.Graphics = (GameOptions.Graphics + 1) % 4; Rebuild(); });
            Button("Mouse speed: " + GameOptions.SensitivityNames[GameOptions.SensitivityIndex], () => { GameOptions.SensitivityIndex = (GameOptions.SensitivityIndex + 1) % 4; Rebuild(); });
            Button("On-screen controls: " + GameOptions.TouchNames[GameOptions.TouchMode], () => { GameOptions.TouchMode = (GameOptions.TouchMode + 1) % 3; Rebuild(); });
            Button("SAVE AND QUIT TO MENU", () =>
            {
                StoryDialogueSystem.Instance?.Publish();
                PlayerPrefs.Save(); Time.timeScale = 1f; UIManager.Instance.TogglePause();
                SceneManager.LoadScene("MainMenu");
            });
            var stamp = Resources.Load<TextAsset>("build_stamp");
            var bs = ScreenTextRenderer.MakeText(panel, "BuildStamp", 20 * ScreenTextRenderer.Scale, ScreenTextRenderer.Ivory, false, TextAlignmentOptions.Center);
            bs.text = stamp != null ? stamp.text : "build (editor)"; items.Add(bs.gameObject);
            EventSystem.current?.SetSelectedGameObject(first);
            Canvas.ForceUpdateCanvases(); scroll.verticalNormalizedPosition = 1f;
        }

        private bool showControls;
        private void Rebuild() { shown = false; }

        private void LateUpdate()
        {
            // Keyboard/gamepad selection scrolls into view, as mouse-wheel scrolling does.
            if (!shown || EventSystem.current?.currentSelectedGameObject == null) return;
            var selected = EventSystem.current.currentSelectedGameObject.transform as RectTransform;
            if (selected == null || !selected.IsChildOf(panel)) return;
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            var gamepad = UnityEngine.InputSystem.Gamepad.current;
            bool navigating = keyboard != null && (keyboard.upArrowKey.isPressed || keyboard.downArrowKey.isPressed || keyboard.tabKey.isPressed)
                || gamepad != null && (gamepad.dpad.ReadValue().sqrMagnitude > 0.1f || gamepad.leftStick.ReadValue().sqrMagnitude > 0.1f);
            if (!navigating) return;
            Bounds bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(viewport, selected);
            float shift = bounds.min.y < viewport.rect.yMin ? viewport.rect.yMin - bounds.min.y : bounds.max.y > viewport.rect.yMax ? viewport.rect.yMax - bounds.max.y : 0f;
            panel.anchoredPosition += Vector2.up * shift;
        }

        private void VolumeSlider(string channel, float value, Action<float> changed)
        {
            var row = new GameObject("Volume_" + channel, typeof(RectTransform)); row.transform.SetParent(panel, false);
            items.Add(row); row.AddComponent<LayoutElement>().minHeight = 86f * ScreenTextRenderer.Scale;
            var label = ScreenTextRenderer.MakeText(row.transform, channel + " volume", 26f * ScreenTextRenderer.Scale, ScreenTextRenderer.Ivory, false);
            var lr = label.rectTransform; lr.anchorMin = new Vector2(0f, 0.5f); lr.anchorMax = Vector2.one; lr.offsetMin = new Vector2(6f, 0f); lr.offsetMax = new Vector2(-6f, 0f);
            label.text = $"{channel} volume: {value:P0}";
            var track = new GameObject("Slider_" + channel, typeof(RectTransform)); track.transform.SetParent(row.transform, false);
            var rt = track.GetComponent<RectTransform>(); rt.anchorMin = new Vector2(0f, 0f); rt.anchorMax = new Vector2(1f, 0.45f); rt.offsetMin = new Vector2(12f, 8f); rt.offsetMax = new Vector2(-12f, -2f);
            var bg = track.AddComponent<Image>(); bg.color = new Color(0.25f, 0.22f, 0.16f);
            var slider = track.AddComponent<Slider>(); slider.minValue = 0f; slider.maxValue = 1f;
            var fill = new GameObject("Fill", typeof(RectTransform)); fill.transform.SetParent(track.transform, false);
            var fr = fill.GetComponent<RectTransform>(); fr.anchorMin = Vector2.zero; fr.anchorMax = Vector2.one; fr.offsetMin = fr.offsetMax = Vector2.zero;
            fill.AddComponent<Image>().color = ScreenTextRenderer.Gold;
            var handle = new GameObject("Handle", typeof(RectTransform)); handle.transform.SetParent(track.transform, false);
            var hr = handle.GetComponent<RectTransform>(); hr.sizeDelta = new Vector2(22f, 32f);
            var image = handle.AddComponent<Image>(); image.color = ScreenTextRenderer.Ivory;
            slider.fillRect = fr; slider.handleRect = hr; slider.targetGraphic = image; slider.SetValueWithoutNotify(value);
            slider.onValueChanged.AddListener(v => { changed(v); label.text = $"{channel} volume: {v:P0}"; });
        }

        private GameObject Button(string label, Action onClick)
        {
            var go = new GameObject("Btn_" + label, typeof(RectTransform));
            go.transform.SetParent(panel, false);
            var img = go.AddComponent<Image>(); img.color = new Color(0.12f, 0.09f, 0.05f, 1f);
            go.AddComponent<LayoutElement>().minHeight = 58 * ScreenTextRenderer.Scale;
            var b = go.AddComponent<Button>();
            var cb = b.colors; cb.highlightedColor = new Color(1f, 0.85f, 0.5f); cb.selectedColor = new Color(1f, 0.85f, 0.5f); b.colors = cb;
            ScreenTextRenderer.AddBorder((RectTransform)go.transform, new Color(1f, 0.78f, 0.34f, 0.8f), 2f);
            var t = ScreenTextRenderer.MakeText(go.transform, "BtnKeyHint", 30 * ScreenTextRenderer.Scale, ScreenTextRenderer.Ivory, false, TextAlignmentOptions.Center);
            var rt = t.rectTransform; rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = new Vector2(12, 4); rt.offsetMax = new Vector2(-12, -4);
            t.text = label;
            b.onClick.AddListener(() => onClick());
            items.Add(go);
            return go;
        }
    }
}
