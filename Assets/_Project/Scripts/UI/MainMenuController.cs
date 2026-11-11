using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using LegacyOfTemples.Core;
using LegacyOfTemples.Gameplay;

namespace LegacyOfTemples.UI
{
    /// <summary>
    /// Main menu built with TextMeshPro + uGUI buttons (mouse, keyboard arrows/Enter, gamepad).
    /// Screens: Main, Settings (text size, high contrast, volumes, subtitles, quality), Controls, Credits.
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        private enum Screen { Main, Settings, Controls, Credits, Gameplay }

        private Canvas canvas;
        private RectTransform column;
        private TextMeshProUGUI infoText;
        private RectTransform infoPanel;
        private readonly List<GameObject> built = new List<GameObject>();
        private Screen current = Screen.Main;

        private static readonly string[] TextSizes = { "Small", "Medium", "Large", "Extra Large" };

        private void Start()
        {
            GroundingEvidenceCapture.Bootstrap();
            if (!PlayerPrefs.HasKey("LevelUnlocked_1")) { PlayerPrefs.SetInt("LevelUnlocked_1", 1); PlayerPrefs.Save(); }
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
            BuildCanvas();
            Show(Screen.Main);
        }

        private void BuildCanvas()
        {
            var go = new GameObject("MainMenuCanvas");
            canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 120;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            if (FindAnyObjectByType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
                es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            }

            // Left column panel
            ScreenTextRenderer.MakePanel(go.transform, "MenuPanel", out column);
            column.anchorMin = new Vector2(0, 0.5f); column.anchorMax = new Vector2(0, 0.5f); column.pivot = new Vector2(0, 0.5f);
            column.anchoredPosition = new Vector2(80, 0); column.sizeDelta = new Vector2(640, 0);
            var vlg = column.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(40, 40, 28, 28); vlg.spacing = 10;
            vlg.childControlHeight = true; vlg.childControlWidth = true; vlg.childForceExpandHeight = false;
            column.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Right info panel (controls / credits)
            ScreenTextRenderer.MakePanel(go.transform, "InfoPanel", out infoPanel);
            infoPanel.anchorMin = infoPanel.anchorMax = new Vector2(1, 0.5f); infoPanel.pivot = new Vector2(1, 0.5f);
            infoPanel.anchoredPosition = new Vector2(-80, 0); infoPanel.sizeDelta = new Vector2(1020, 0);
            var ivlg = infoPanel.gameObject.AddComponent<VerticalLayoutGroup>();
            ivlg.padding = new RectOffset(40, 40, 36, 36); ivlg.childControlHeight = true; ivlg.childControlWidth = true; ivlg.childForceExpandHeight = false;
            infoPanel.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            infoText = ScreenTextRenderer.MakeText(infoPanel, "InfoBody", 30, ScreenTextRenderer.Ivory, false);
        }

        private void Clear()
        {
            foreach (var g in built) Destroy(g);
            built.Clear();
        }

        private TextMeshProUGUI AddText(string name, string text, float size, Color c, bool title, TextAlignmentOptions align = TextAlignmentOptions.TopLeft)
        {
            var t = ScreenTextRenderer.MakeText(column, name, size * ScreenTextRenderer.Scale, c, title, align);
            t.text = text; built.Add(t.gameObject);
            return t;
        }

        private Button AddButton(string label, Action onClick, bool interactable = true)
        {
            var go = new GameObject("Btn_" + label, typeof(RectTransform));
            go.transform.SetParent(column, false);
            var img = go.AddComponent<Image>(); img.color = new Color(0.12f, 0.09f, 0.05f, 1f);
            var le = go.AddComponent<LayoutElement>(); le.minHeight = 64 * ScreenTextRenderer.Scale;
            var b = go.AddComponent<Button>();
            var cb = b.colors; cb.normalColor = Color.white; cb.highlightedColor = new Color(1f, 0.85f, 0.5f); cb.selectedColor = new Color(1f, 0.85f, 0.5f); cb.pressedColor = new Color(1f, 0.7f, 0.3f); cb.disabledColor = new Color(0.5f, 0.5f, 0.5f);
            b.colors = cb; b.interactable = interactable;
            ScreenTextRenderer.AddBorder((RectTransform)go.transform, new Color(1f, 0.78f, 0.34f, 0.8f), 2f);
            var t = ScreenTextRenderer.MakeText(go.transform, "BtnKeyHint", 34 * ScreenTextRenderer.Scale, interactable ? ScreenTextRenderer.Ivory : new Color(0.85f, 0.82f, 0.75f), false, TextAlignmentOptions.Center);
            var rt = t.rectTransform; rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = new Vector2(12, 4); rt.offsetMax = new Vector2(-12, -4);
            t.text = label;
            b.onClick.AddListener(() => { AudioManager.Instance?.PlaySFX("sfx_ui_select"); onClick(); });
            built.Add(go);
            return b;
        }

        private void Show(Screen s)
        {
            current = s;
            Clear();
            infoPanel.gameObject.SetActive(false);
            Button first = null;
            switch (s)
            {
                case Screen.Main:
                    AddText("MenuTitle", "LEGACY OF THE TEMPLE", 64, ScreenTextRenderer.Gold, true, TextAlignmentOptions.Center);
                    AddText("MenuSubtitle", "A heritage mystery", 32, ScreenTextRenderer.Ivory, false, TextAlignmentOptions.Center);
                    bool hasSave = PlayerPrefs.HasKey("LastPlayedScene") && PlayerPrefs.GetInt("Story_Objective", 0) > 0;
                    first = hasSave ? AddButton("CONTINUE", Continue) : null;
                    var ng = AddButton("NEW GAME", NewGame);
                    if (first == null) first = ng;
                    AddButton("SHOWCASE TOUR", () => { PlayerPrefs.SetInt("ShowcaseTourActive", 1); PlayerPrefs.Save(); SceneManager.LoadScene(Constants.Courtyard); });
                    AddButton("SETTINGS", () => Show(Screen.Settings));
                    AddButton("CONTROLS", () => Show(Screen.Controls));
                    AddButton("CREDITS", () => Show(Screen.Credits));
                    AddButton("QUIT", Application.Quit);
                    break;
                case Screen.Settings:
                    AddText("MenuTitle", "SETTINGS", 64, ScreenTextRenderer.Gold, true, TextAlignmentOptions.Center);
                    int ts = PlayerPrefs.GetInt("Settings_TextScaleIndex", 1);
                    first = AddButton("Text size: " + TextSizes[Mathf.Clamp(ts, 0, 3)], () => { PlayerPrefs.SetInt("Settings_TextScaleIndex", (ts + 1) % 4); PlayerPrefs.Save(); Show(Screen.Settings); });
                    bool hc = ScreenTextRenderer.HighContrast;
                    AddButton("High contrast: " + (hc ? "ON" : "OFF"), () => { PlayerPrefs.SetInt("Settings_HighContrast", hc ? 0 : 1); PlayerPrefs.Save(); Show(Screen.Settings); });
                    AddVolume("Master volume", "Settings_MasterVol", 1f, v => AudioManager.Instance?.SetMasterVolume(v));
                    AddVolume("Music volume", "Settings_MusicVol", 0.8f, v => AudioManager.Instance?.SetMusicVolume(v));
                    AddVolume("Effects volume", "Settings_SfxVol", 0.9f, v => AudioManager.Instance?.SetSFXVolume(v));
                    AddVolume("Voice volume", "Settings_VoiceVol", 1f, v => { });
                    bool sub = PlayerPrefs.GetInt("Settings_Subtitles", 1) == 1;
                    AddButton("Subtitles: " + (sub ? "ON" : "OFF"), () => { PlayerPrefs.SetInt("Settings_Subtitles", sub ? 0 : 1); PlayerPrefs.Save(); Show(Screen.Settings); });
                    int q = QualitySettings.GetQualityLevel();
                    AddButton("Graphics: " + QualitySettings.names[q], () => { QualitySettings.SetQualityLevel((q + 1) % QualitySettings.names.Length, true); Show(Screen.Settings); });
                    AddButton("GAMEPLAY AND CONTROLS OPTIONS", () => Show(Screen.Gameplay));
                    AddButton("BACK", () => Show(Screen.Main));
                    break;
                case Screen.Gameplay:
                    AddText("MenuTitle", "GAMEPLAY", 64, ScreenTextRenderer.Gold, true, TextAlignmentOptions.Center);
                    first = AddButton("Difficulty: " + GameOptions.DifficultyNames[GameOptions.Difficulty], () => { GameOptions.Difficulty = (GameOptions.Difficulty + 1) % 3; Show(Screen.Gameplay); });
                    AddButton("Weather effects: " + GameOptions.WeatherNames[GameOptions.WeatherQuality], () => { GameOptions.WeatherQuality = (GameOptions.WeatherQuality + 1) % 3; Show(Screen.Gameplay); });
                    AddButton("On-screen controls: " + GameOptions.TouchNames[GameOptions.TouchMode], () => { GameOptions.TouchMode = (GameOptions.TouchMode + 1) % 3; Show(Screen.Gameplay); });
                    AddButton("Button size: " + GameOptions.SizeNames[GameOptions.TouchSize], () => { GameOptions.TouchSize = (GameOptions.TouchSize + 1) % 3; Show(Screen.Gameplay); });
                    AddButton("Button opacity: " + GameOptions.OpacityNames[GameOptions.TouchOpacityIndex], () => { GameOptions.TouchOpacityIndex = (GameOptions.TouchOpacityIndex + 1) % 3; Show(Screen.Gameplay); });
                    AddButton("BACK", () => Show(Screen.Settings));
                    break;
                case Screen.Controls:
                    AddText("MenuTitle", "CONTROLS", 64, ScreenTextRenderer.Gold, true, TextAlignmentOptions.Center);
                    first = AddButton("BACK", () => Show(Screen.Main));
                    infoPanel.gameObject.SetActive(true);
                    infoText.fontSize = 32 * ScreenTextRenderer.Scale;
                    infoText.text =
                        $"{K("W A S D")} Walk      {K("Shift")} Run      {K("C")} Crouch      {K("Space")} Jump\n" +
                        $"{K("Mouse")} Look around\n" +
                        $"{K("E")} Talk / use      {K("F")} Inspect a clue\n" +
                        $"{K("1")} {K("2")} {K("3")} Choose an answer in conversations\n" +
                        $"{K("H")} or {K("Tab")} or the {K("? GUIDE")} button: current goal, hints, Show me the way\n" +
                        $"{K("J")} Case journal      {K("M")} Map      {K("P")} Photo mode      {K("Esc")} Pause\n\n" +
                        $"<color={ScreenText.Gold}>Gamepad</color>\nLeft stick walk, A talk/confirm, X inspect, B crouch/back,\nD-pad left Guide, D-pad down Map, View button Journal, Start Pause";
                    break;
                case Screen.Credits:
                    AddText("MenuTitle", "CREDITS", 64, ScreenTextRenderer.Gold, true, TextAlignmentOptions.Center);
                    first = AddButton("BACK", () => Show(Screen.Main));
                    infoPanel.gameObject.SetActive(true);
                    infoText.fontSize = 30 * ScreenTextRenderer.Scale;
                    infoText.text =
                        "<b>Legacy of the Temple</b>\nDirection, environments and characters: Hemanth\n" +
                        "Characters: Investigator Aryan, Pujari Ananda, Meera, Bhaskar and Kishore\n" +
                        "Animation: Mixamo (Adobe)    Fonts: Cinzel and Noto Sans (SIL Open Font License)\n" +
                        "Audio and other assets: see ATTRIBUTION.md\n\n" +
                        $"<color={ScreenText.Gold}>A note on the story</color>\nThe story and the three missing pieces (a brass ceremonial vessel, a carved sandstone rosette and a palm-leaf manuscript) are fictional. " +
                        "The temple shrine in the background is shown with respect and is never touched or taken in the game.";
                    break;
            }
            if (first != null) EventSystem.current?.SetSelectedGameObject(first.gameObject);
        }

        private void AddVolume(string label, string key, float def, Action<float> apply)
        {
            float v = PlayerPrefs.GetFloat(key, def);
            AddButton($"{label}: {Mathf.RoundToInt(v * 100)}%  (click to change)", () =>
            {
                float nv = v >= 0.99f ? 0f : Mathf.Min(1f, v + 0.25f);
                PlayerPrefs.SetFloat(key, nv); PlayerPrefs.Save(); apply(nv); Show(Screen.Settings);
            });
        }

        private static string K(string k) => ScreenText.Key(k);

        private void Update()
        {
            var kb = Keyboard.current; var gp = Gamepad.current;
            bool back = (kb != null && kb.escapeKey.wasPressedThisFrame) || (gp != null && gp.buttonEast.wasPressedThisFrame);
            if (back && current != Screen.Main) Show(Screen.Main);
            if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject == null && built.Count > 0)
            {
                bool nav = (kb != null && (kb.downArrowKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame || kb.tabKey.wasPressedThisFrame)) || (gp != null && gp.leftStick.ReadValue().sqrMagnitude > 0.3f);
                if (nav) foreach (var g in built) if (g.GetComponent<Button>() != null) { EventSystem.current.SetSelectedGameObject(g); break; }
            }
        }

        private static void NewGame()
        {
            StoryDialogueSystem.ResetForNewGame();
            PlayerPrefs.SetInt("OpeningCinematicActive", 1);
            PlayerPrefs.SetInt("ShowcaseTourActive", 0);
            PlayerPrefs.SetInt("Force_Tutorial_On_Start", 1);
            PlayerPrefs.Save();
            SceneLoader.Load(Constants.Courtyard);
        }

        private static void Continue()
        {
            PlayerPrefs.SetInt("OpeningCinematicActive", 0);
            PlayerPrefs.SetInt("ShowcaseTourActive", 0);
            PlayerPrefs.Save();
            SceneLoader.Load(PlayerPrefs.GetString("LastPlayedScene", Constants.Courtyard));
        }
    }
}
