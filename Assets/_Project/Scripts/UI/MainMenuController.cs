using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using LegacyOfTemples.Core;

namespace LegacyOfTemples.UI
{
    public class MainMenuController : MonoBehaviour
    {
        private enum MenuState
        {
            Main,
            LevelSelect,
            Settings,
            Controls,
            Help,
            Credits
        }

        private MenuState currentState = MenuState.Main;

        // Settings variables
        private float masterVolume = 1.0f;
        private float musicVolume = 0.8f;
        private float sfxVolume = 0.9f;
        private float mouseSensitivity = 1.0f;
        private bool subtitlesEnabled = true;
        private int qualityLevel = 2; // Medium

        // GUI Styles
        private GUIStyle titleStyle;
        private GUIStyle subtitleStyle;
        private GUIStyle buttonStyle;
        private GUIStyle buttonDisabledStyle;
        private GUIStyle panelStyle;
        private GUIStyle labelStyle;
        private GUIStyle headerStyle;
        private Texture2D panelTex;
        private Texture2D btnTex;
        private Texture2D btnHoverTex;
        private bool stylesInitialized = false;

        private void Start()
        {
            GroundingEvidenceCapture.Bootstrap();
            // Load saved settings
            masterVolume = PlayerPrefs.GetFloat("Settings_MasterVol", 1.0f);
            musicVolume = PlayerPrefs.GetFloat("Settings_MusicVol", 0.8f);
            sfxVolume = PlayerPrefs.GetFloat("Settings_SfxVol", 0.9f);
            mouseSensitivity = PlayerPrefs.GetFloat("Settings_MouseSens", 1.0f);
            subtitlesEnabled = PlayerPrefs.GetInt("Settings_Subtitles", 1) == 1;
            qualityLevel = QualitySettings.GetQualityLevel();

            // Unlock Level 1 by default
            if (!PlayerPrefs.HasKey("LevelUnlocked_1"))
            {
                PlayerPrefs.SetInt("LevelUnlocked_1", 1);
                PlayerPrefs.Save();
            }
        }

        private void InitStyles()
        {
            if (stylesInitialized) return;

            panelTex = new Texture2D(2, 2);
            Color[] panelColors = new Color[4];
            for (int i = 0; i < 4; i++) panelColors[i] = new Color(0.08f, 0.07f, 0.06f, 0.92f);
            panelTex.SetPixels(panelColors);
            panelTex.Apply();

            btnTex = new Texture2D(2, 2);
            Color[] btnColors = new Color[4];
            for (int i = 0; i < 4; i++) btnColors[i] = new Color(0.22f, 0.18f, 0.14f, 0.95f);
            btnTex.SetPixels(btnColors);
            btnTex.Apply();

            btnHoverTex = new Texture2D(2, 2);
            Color[] hoverColors = new Color[4];
            for (int i = 0; i < 4; i++) hoverColors[i] = new Color(0.45f, 0.35f, 0.20f, 0.98f);
            btnHoverTex.SetPixels(hoverColors);
            btnHoverTex.Apply();

            float scale = Mathf.Max(1.0f, Screen.height / 1080f);

            panelStyle = new GUIStyle(GUI.skin.box);
            panelStyle.normal.background = panelTex;

            titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.RoundToInt(56 * scale),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            titleStyle.normal.textColor = new Color(1.0f, 0.84f, 0.38f); // Antique Gold

            subtitleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.RoundToInt(28 * scale),
                fontStyle = FontStyle.Italic,
                alignment = TextAnchor.MiddleCenter
            };
            subtitleStyle.normal.textColor = new Color(0.85f, 0.78f, 0.68f);

            headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.RoundToInt(38 * scale),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            headerStyle.normal.textColor = new Color(1.0f, 0.84f, 0.38f);

            buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = Mathf.RoundToInt(30 * scale),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            buttonStyle.normal.background = btnTex;
            buttonStyle.hover.background = btnHoverTex;
            buttonStyle.normal.textColor = new Color(0.95f, 0.90f, 0.80f);
            buttonStyle.hover.textColor = new Color(1.0f, 0.92f, 0.50f);

            buttonDisabledStyle = new GUIStyle(buttonStyle);
            buttonDisabledStyle.normal.textColor = new Color(0.45f, 0.42f, 0.38f);

            labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.RoundToInt(28 * scale),
                alignment = TextAnchor.MiddleLeft
            };
            labelStyle.normal.textColor = new Color(0.90f, 0.85f, 0.75f);

            stylesInitialized = true;
        }

        private void OnGUI()
        {
            InitStyles();

            switch (currentState)
            {
                case MenuState.Main:
                    DrawMainMenu();
                    break;
                case MenuState.LevelSelect:
                    DrawLevelSelect();
                    break;
                case MenuState.Settings:
                    DrawSettings();
                    break;
                case MenuState.Controls:
                    DrawControls();
                    break;
                case MenuState.Help:
                    DrawHelp();
                    break;
                case MenuState.Credits:
                    DrawCredits();
                    break;
            }
        }

        private void DrawMainMenu()
        {
            float scale = Mathf.Max(1.0f, Screen.height / 1080f);
            float width = 520f * scale;
            float height = 780f * scale;
            float x = 60f * scale;
            float y = (Screen.height - height) * 0.5f;

            GUI.Box(new Rect(x, y, width, height), GUIContent.none, panelStyle);

            GUILayout.BeginArea(new Rect(x + 25f * scale, y + 25f * scale, width - 50f * scale, height - 50f * scale));

            GUILayout.Label("LEGACY OF THE TEMPLE", titleStyle);
            GUILayout.Label("A Cinematic Heritage Mystery", subtitleStyle);
            GUILayout.Space(18f * scale);

            if (GUILayout.Button("NEW GAME", buttonStyle, GUILayout.Height(50f * scale)))
            {
                PlayerPrefs.SetInt("OpeningCinematicActive", 1);
                PlayerPrefs.SetInt("ShowcaseTourActive", 0);
                PlayerPrefs.Save();
                SceneManager.LoadScene(Constants.Courtyard);
            }

            GUILayout.Space(8f * scale);

            if (GUILayout.Button("SHOWCASE TOUR", buttonStyle, GUILayout.Height(50f * scale)))
            {
                PlayerPrefs.SetInt("ShowcaseTourActive", 1);
                PlayerPrefs.Save();
                SceneManager.LoadScene(Constants.Courtyard);
            }

            GUILayout.Space(8f * scale);

            bool hasSavedGame = PlayerPrefs.HasKey("LastPlayedScene");
            GUI.enabled = hasSavedGame;
            if (GUILayout.Button("CONTINUE", hasSavedGame ? buttonStyle : buttonDisabledStyle, GUILayout.Height(50f * scale)))
            {
                string lastScene = PlayerPrefs.GetString("LastPlayedScene", Constants.Courtyard);
                SceneManager.LoadScene(lastScene);
            }
            GUI.enabled = true;

            GUILayout.Space(8f * scale);

            if (GUILayout.Button("LEVEL SELECTION", buttonStyle, GUILayout.Height(50f * scale)))
            {
                currentState = MenuState.LevelSelect;
            }

            GUILayout.Space(8f * scale);

            if (GUILayout.Button("SETTINGS", buttonStyle, GUILayout.Height(50f * scale)))
            {
                currentState = MenuState.Settings;
            }

            GUILayout.Space(8f * scale);

            if (GUILayout.Button("CONTROLS", buttonStyle, GUILayout.Height(50f * scale)))
            {
                currentState = MenuState.Controls;
            }

            GUILayout.Space(8f * scale);

            if (GUILayout.Button("HELP", buttonStyle, GUILayout.Height(50f * scale)))
            {
                currentState = MenuState.Help;
            }

            GUILayout.Space(8f * scale);

            if (GUILayout.Button("CREDITS", buttonStyle, GUILayout.Height(50f * scale)))
            {
                currentState = MenuState.Credits;
            }

            GUILayout.Space(8f * scale);

            if (GUILayout.Button("EXIT GAME", buttonStyle, GUILayout.Height(38f * scale)))
            {
                Application.Quit();
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#endif
            }

            GUILayout.EndArea();
        }

        private void DrawLevelSelect()
        {
            float width = 520f;
            float height = 440f;
            float x = (Screen.width - width) * 0.5f;
            float y = (Screen.height - height) * 0.5f;

            GUI.Box(new Rect(x, y, width, height), GUIContent.none, panelStyle);
            GUILayout.BeginArea(new Rect(x + 25f, y + 20f, width - 50f, height - 40f));

            GUILayout.Label("LEVEL SELECTION", headerStyle);
            GUILayout.Space(15f);

            // Level 1
            bool lvl1Unlocked = PlayerPrefs.GetInt("LevelUnlocked_1", 1) == 1;
            DrawLevelCard("LEVEL 1: THE MISSING HERITAGE", "Arrival at the dawn-lit temple courtyard & sanctum investigation.", lvl1Unlocked, Constants.Courtyard);

            GUILayout.Space(12f);

            // Level 2
            bool lvl2Unlocked = PlayerPrefs.GetInt("LevelUnlocked_2", 0) == 1;
            DrawLevelCard("LEVEL 2: THE TRAIL OF THE ROBBERS", "Outer shrine crime scene, muddy tracks & the ancient puzzle wall.", lvl2Unlocked, Constants.ShrineCrime);

            GUILayout.Space(12f);

            // Level 3
            bool lvl3Unlocked = PlayerPrefs.GetInt("LevelUnlocked_3", 0) == 1;
            DrawLevelCard("LEVEL 3: THE FINAL TRUTH", "Misty forest ravine ruins, confronting the thieves & relic recovery.", lvl3Unlocked, Constants.JunglePath);

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("BACK TO MAIN MENU", buttonStyle, GUILayout.Height(36f)))
            {
                currentState = MenuState.Main;
            }

            GUILayout.EndArea();
        }

        private void DrawLevelCard(string title, string desc, bool isUnlocked, string sceneName)
        {
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.BeginHorizontal();
            GUILayout.Label(title, isUnlocked ? headerStyle : subtitleStyle);
            GUILayout.FlexibleSpace();
            if (isUnlocked)
            {
                if (GUILayout.Button("PLAY", buttonStyle, GUILayout.Width(80f), GUILayout.Height(28f)))
                {
                    SceneManager.LoadScene(sceneName);
                }
            }
            else
            {
                GUILayout.Label("🔒 LOCKED", subtitleStyle);
            }
            GUILayout.EndHorizontal();
            GUILayout.Label(desc, labelStyle);
            GUILayout.EndVertical();
        }

        private void DrawSettings()
        {
            float width = 480f;
            float height = 480f;
            float x = (Screen.width - width) * 0.5f;
            float y = (Screen.height - height) * 0.5f;

            GUI.Box(new Rect(x, y, width, height), GUIContent.none, panelStyle);
            GUILayout.BeginArea(new Rect(x + 30f, y + 20f, width - 60f, height - 40f));

            GUILayout.Label("SETTINGS", headerStyle);
            GUILayout.Space(15f);

            // Master Volume
            GUILayout.Label($"Master Volume: {(int)(masterVolume * 100)}%", labelStyle);
            masterVolume = GUILayout.HorizontalSlider(masterVolume, 0f, 1f);
            AudioListener.volume = masterVolume;

            GUILayout.Space(10f);

            // Music Volume
            GUILayout.Label($"Music Volume: {(int)(musicVolume * 100)}%", labelStyle);
            musicVolume = GUILayout.HorizontalSlider(musicVolume, 0f, 1f);

            GUILayout.Space(10f);

            // SFX Volume
            GUILayout.Label($"SFX Volume: {(int)(sfxVolume * 100)}%", labelStyle);
            sfxVolume = GUILayout.HorizontalSlider(sfxVolume, 0f, 1f);

            GUILayout.Space(10f);

            // Mouse Look Sensitivity
            GUILayout.Label($"Mouse Look Sensitivity: {mouseSensitivity:F1}x", labelStyle);
            mouseSensitivity = GUILayout.HorizontalSlider(mouseSensitivity, 0.2f, 3.0f);

            GUILayout.Space(10f);

            // Subtitles Toggle
            subtitlesEnabled = GUILayout.Toggle(subtitlesEnabled, " Enable Dialogue Subtitles", labelStyle);

            GUILayout.Space(10f);

            // Graphics Quality
            GUILayout.Label($"Graphics Quality: {QualitySettings.names[QualitySettings.GetQualityLevel()]}", labelStyle);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Low", buttonStyle)) QualitySettings.SetQualityLevel(0);
            if (GUILayout.Button("Medium", buttonStyle)) QualitySettings.SetQualityLevel(1);
            if (GUILayout.Button("High", buttonStyle)) QualitySettings.SetQualityLevel(2);
            GUILayout.EndHorizontal();

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("SAVE & RETURN", buttonStyle, GUILayout.Height(36f)))
            {
                PlayerPrefs.SetFloat("Settings_MasterVol", masterVolume);
                PlayerPrefs.SetFloat("Settings_MusicVol", musicVolume);
                PlayerPrefs.SetFloat("Settings_SfxVol", sfxVolume);
                PlayerPrefs.SetFloat("Settings_MouseSens", mouseSensitivity);
                PlayerPrefs.SetInt("Settings_Subtitles", subtitlesEnabled ? 1 : 0);
                PlayerPrefs.Save();
                currentState = MenuState.Main;
            }

            GUILayout.EndArea();
        }

        private void DrawControls()
        {
            float width = 560f;
            float height = 500f;
            float x = (Screen.width - width) * 0.5f;
            float y = (Screen.height - height) * 0.5f;

            GUI.Box(new Rect(x, y, width, height), GUIContent.none, panelStyle);
            GUILayout.BeginArea(new Rect(x + 30f, y + 20f, width - 60f, height - 40f));

            GUILayout.Label("CONTROLS & SHORTCUTS", headerStyle);
            GUILayout.Space(12f);

            DrawControlRow("W / A / S / D", "Move Investigator", "Left Stick");
            DrawControlRow("Mouse Move", "Orbit Camera / Look Around", "Right Stick");
            DrawControlRow("Left Shift", "Sprint / Run", "Right Trigger / L3");
            DrawControlRow("Space", "Jump", "Button South (A)");
            DrawControlRow("Left Ctrl / C", "Crouch / Sneak Stance", "Button East (B)");
            DrawControlRow("E", "Interact / Talk to NPCs", "Button West (X)");
            DrawControlRow("F", "Investigate Clue (Inspect Mode)", "Left Shoulder (L1)");
            DrawControlRow("G", "Toggle Brass Lantern / Flashlight", "D-Pad Up");
            DrawControlRow("I", "Open Relic Inventory", "Select Button");
            DrawControlRow("J", "Open Investigation Journal", "D-Pad Right");
            DrawControlRow("M", "Open Temple Area Map", "D-Pad Down");
            DrawControlRow("H", "Help / Progressive Objective Hints", "D-Pad Left");
            DrawControlRow("Escape", "Pause Menu & Settings", "Start Button");
            DrawControlRow("1 - 5", "Dev Quick-Warp (Courtyard/Sanctum/...)", "N/A");

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("BACK TO MAIN MENU", buttonStyle, GUILayout.Height(36f)))
            {
                currentState = MenuState.Main;
            }

            GUILayout.EndArea();
        }

        private void DrawControlRow(string key, string action, string gamepad)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label($"<b>{key}</b>", labelStyle, GUILayout.Width(130f));
            GUILayout.Label(action, labelStyle, GUILayout.Width(220f));
            GUILayout.Label($"<i>{gamepad}</i>", subtitleStyle);
            GUILayout.EndHorizontal();
        }

        private void DrawHelp()
        {
            float width = 540f;
            float height = 460f;
            float x = (Screen.width - width) * 0.5f;
            float y = (Screen.height - height) * 0.5f;

            GUI.Box(new Rect(x, y, width, height), GUIContent.none, panelStyle);
            GUILayout.BeginArea(new Rect(x + 30f, y + 20f, width - 60f, height - 40f));

            GUILayout.Label("HOW TO PLAY & INVESTIGATE", headerStyle);
            GUILayout.Space(15f);

            GUILayout.Label("✦ <b>Investigation Mechanics</b>", labelStyle);
            GUILayout.Label("  • Approach suspects, witnesses, and objects to see interactive overhead prompts.", labelStyle);
            GUILayout.Label("  • Press <b>[E]</b> to speak with Priest Ananda and villagers to gather incident testimony.", labelStyle);
            GUILayout.Label("  • Press <b>[F]</b> to inspect crime clues in close-up detail (broken lock, footprints, torn cloth).", labelStyle);
            GUILayout.Space(10f);

            GUILayout.Label("✦ <b>The Journal & Map</b>", labelStyle);
            GUILayout.Label("  • Press <b>[J]</b> to check your active objectives, suspect deductions, and evidence notes.", labelStyle);
            GUILayout.Label("  • Press <b>[M]</b> to view the temple layout and locate unexplored areas.", labelStyle);
            GUILayout.Label("  • Press <b>[H]</b> if you ever feel stuck for a 3-tier progressive hint.", labelStyle);
            GUILayout.Space(10f);

            GUILayout.Label("✦ <b>Cultural Reverence</b>", labelStyle);
            GUILayout.Label("  • The temple sanctum is a sacred space. Walk respectfully and protect our living history.", labelStyle);

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("BACK TO MAIN MENU", buttonStyle, GUILayout.Height(36f)))
            {
                currentState = MenuState.Main;
            }

            GUILayout.EndArea();
        }

        private void DrawCredits()
        {
            float width = 560f;
            float height = 460f;
            float x = (Screen.width - width) * 0.5f;
            float y = (Screen.height - height) * 0.5f;

            GUI.Box(new Rect(x, y, width, height), GUIContent.none, panelStyle);
            GUILayout.BeginArea(new Rect(x + 30f, y + 20f, width - 60f, height - 40f));

            GUILayout.Label("CREDITS", headerStyle);
            GUILayout.Space(15f);

            GUILayout.Label("<b>LEGACY OF THE TEMPLE</b>", labelStyle);
            GUILayout.Label("Developed with Google Antigravity & Unity 6 URP", subtitleStyle);
            GUILayout.Space(12f);

            GUILayout.Label("• <b>Director, Architecture & Visual Art</b>: Hemanth (S20240010236)", labelStyle);
            GUILayout.Label("• <b>Temple Model</b>: Authentic Jagannath Temple architecture from assets.blend", labelStyle);
            GUILayout.Label("• <b>Characters</b>: Investigator Aryan, Pujari Ananda, Meera the Villager, Bhaskar & Kishore", labelStyle);
            GUILayout.Space(12f);

            GUILayout.Label("<b>Fiction & Heritage Note:</b>", headerStyle);
            GUILayout.Label("The story, mystery, and the three stolen relics (Pradip Vessel, Carved Rosette, Palm-Leaf Manuscript) are respectful works of fiction. The sacred Lord Jagannath temple architecture is celebrated with deep cultural reverence and historical integrity.", labelStyle);

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("BACK TO MAIN MENU", buttonStyle, GUILayout.Height(36f)))
            {
                currentState = MenuState.Main;
            }

            GUILayout.EndArea();
        }
    }
}

