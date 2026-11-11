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
            ScreenTextRenderer.MakePanel(transform, "PauseMenuPanel", out panel);
            panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(0.5f, 0.5f);
            panel.sizeDelta = new Vector2(760, 0);
            var v = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            v.padding = new RectOffset(40, 40, 32, 32); v.spacing = 8; v.childControlHeight = true; v.childControlWidth = true; v.childForceExpandHeight = false;
            panel.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            panel.gameObject.SetActive(false);
        }

        private void Update()
        {
            bool open = UIManager.Instance != null && UIManager.Instance.IsPauseOpen;
            if (open && !shown) Build();
            if (!open && shown) { panel.gameObject.SetActive(false); shown = false; }
        }

        private void Build()
        {
            shown = true;
            foreach (var g in items) Destroy(g);
            items.Clear();
            panel.gameObject.SetActive(true);
            var t = ScreenTextRenderer.MakeText(panel, "PauseTitle", ScreenTextRenderer.TitleSize * ScreenTextRenderer.Scale, ScreenTextRenderer.Gold, true, TextAlignmentOptions.Center);
            t.text = "PAUSED"; items.Add(t.gameObject);
            var first = Button("RESUME", () => UIManager.Instance.TogglePause());
            Button("GUIDE: WHAT DO I DO?", () => { UIManager.Instance.TogglePause(); GuideSystem.Instance?.Toggle(); });
            int ts = PlayerPrefs.GetInt("Settings_TextScaleIndex", 1);
            Button("Text size: " + TextSizes[Mathf.Clamp(ts, 0, 3)], () => { UIManager.Instance.CycleTextScale(); Rebuild(); });
            Button("High contrast: " + (ScreenTextRenderer.HighContrast ? "ON" : "OFF"), () => { UIManager.Instance.ToggleHighContrast(); Rebuild(); });
            float mv = PlayerPrefs.GetFloat("Settings_MasterVol", 1f);
            Button($"Volume: {Mathf.RoundToInt(mv * 100)}%", () =>
            {
                float nv = mv >= 0.99f ? 0f : Mathf.Min(1f, mv + 0.25f);
                PlayerPrefs.SetFloat("Settings_MasterVol", nv); PlayerPrefs.Save(); AudioManager.Instance?.SetMasterVolume(nv); Rebuild();
            });
            float vv = VoicePlayer.Volume;
            Button($"Voice: {Mathf.RoundToInt(vv * 100)}%", () => { VoicePlayer.Volume = vv >= 0.99f ? 0f : Mathf.Min(1f, vv + 0.25f); Rebuild(); });
            Button("Difficulty: " + GameOptions.DifficultyNames[GameOptions.Difficulty], () => { GameOptions.Difficulty = (GameOptions.Difficulty + 1) % 3; Rebuild(); });
            Button("Weather effects: " + GameOptions.WeatherNames[GameOptions.WeatherQuality], () => { GameOptions.WeatherQuality = (GameOptions.WeatherQuality + 1) % 3; Rebuild(); });
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
        }

        private void Rebuild() { shown = false; }

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
