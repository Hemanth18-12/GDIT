using System;
using UnityEngine;
using UnityEngine.InputSystem;
using LegacyOfTemples.Core;
using LegacyOfTemples.World;

namespace LegacyOfTemples.UI
{
    public class GameIntroOverlay : MonoBehaviour
    {
        [Header("State")]
        [SerializeField] private bool showInstructionsOnStart = true;

        private bool isInstructionsOpen = true;
        private string currentSceneName = Constants.Courtyard;
        private string currentObjective = "Speak with Priest Ananda and Villager Meera in the temple courtyard.";
        private int cluesFound = 0;
        private const int TotalClues = 4;

        private Texture2D bgTex;
        private Texture2D cardTex;
        private Texture2D goldBarTex;
        private Texture2D buttonTex;
        private Texture2D buttonHoverTex;

        private GUIStyle titleStyle;
        private GUIStyle subTitleStyle;
        private GUIStyle sectionHeaderStyle;
        private GUIStyle bodyStyle;
        private GUIStyle highlightStyle;
        private GUIStyle keycapStyle;
        private GUIStyle buttonStyle;
        private GUIStyle hudBoxStyle;
        private GUIStyle hudObjectiveStyle;
        private GUIStyle pillStyle;
        private bool stylesInitialized = false;

        private void Awake()
        {
            isInstructionsOpen = showInstructionsOnStart;
            CreateSolidTextures();
        }

        private void OnEnable()
        {
            EventBus.OnSceneChanged += HandleSceneChanged;
            EventBus.OnClueFound += HandleClueFound;
        }

        private void OnDisable()
        {
            EventBus.OnSceneChanged -= HandleSceneChanged;
            EventBus.OnClueFound -= HandleClueFound;
        }

        private void Start()
        {
            if (WorldManager.Instance != null && !string.IsNullOrEmpty(WorldManager.Instance.CurrentSceneName))
            {
                currentSceneName = WorldManager.Instance.CurrentSceneName;
                UpdateObjectiveForScene(currentSceneName);
            }
        }

        private void Update()
        {
            var kb = Keyboard.current;
            var gp = Gamepad.current;

            // Toggle instructions with H or Escape or Gamepad Start
            if (kb != null && (kb.hKey.wasPressedThisFrame || kb.escapeKey.wasPressedThisFrame))
            {
                isInstructionsOpen = !isInstructionsOpen;
            }
            if (gp != null && gp.startButton.wasPressedThisFrame)
            {
                isInstructionsOpen = !isInstructionsOpen;
            }

            // Dismiss with Space or Enter or South Button if instructions are open
            if (isInstructionsOpen)
            {
                if (kb != null && (kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame))
                {
                    isInstructionsOpen = false;
                }
                if (gp != null && gp.buttonSouth.wasPressedThisFrame)
                {
                    isInstructionsOpen = false;
                }
            }
        }

        private void HandleSceneChanged(string newScene)
        {
            currentSceneName = newScene;
            UpdateObjectiveForScene(newScene);
        }

        private void HandleClueFound(string clueId)
        {
            cluesFound++;
            Debug.Log($"[GameIntroOverlay] Clue uncovered: {clueId} (Total: {cluesFound}/{TotalClues})");
        }

        private void UpdateObjectiveForScene(string sceneId)
        {
            switch (sceneId)
            {
                case Constants.Courtyard:
                    currentObjective = "Examine temple grounds and speak with Priest Ananda and Villager Meera.";
                    break;
                case Constants.Sanctum:
                    currentObjective = "Inspect the empty Ratnavedi altar pedestal and discover where the sacred idol was taken.";
                    break;
                case Constants.ShrineCrime:
                    currentObjective = "Examine the crime scene: footprints, overturned lamp, and torn silk cloth.";
                    break;
                case Constants.PuzzleWall:
                    currentObjective = "Align the ancient 8-spoke Chakra wheel with the 4 relief panels to unlock the passage.";
                    break;
                case Constants.JunglePath:
                    currentObjective = "Pursue the shadow thieves through the moonlit jungle to recover Lord Jagannath!";
                    break;
                default:
                    currentObjective = "Explore the sacred Odisha temple and uncover evidence.";
                    break;
            }
        }

        private void CreateSolidTextures()
        {
            bgTex = MakeTex(2, 2, new Color(0.04f, 0.05f, 0.08f, 0.94f));
            cardTex = MakeTex(2, 2, new Color(0.10f, 0.11f, 0.15f, 0.96f));
            goldBarTex = MakeTex(2, 2, new Color(0.92f, 0.76f, 0.32f, 1.0f));
            buttonTex = MakeTex(2, 2, new Color(0.85f, 0.52f, 0.12f, 1.0f));
            buttonHoverTex = MakeTex(2, 2, new Color(1.0f, 0.68f, 0.22f, 1.0f));
        }

        private Texture2D MakeTex(int width, int height, Color col)
        {
            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; ++i) pix[i] = col;
            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }

        private void InitStyles()
        {
            if (stylesInitialized) return;

            titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 24,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            titleStyle.normal.textColor = new Color(1.0f, 0.88f, 0.50f);

            subTitleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Italic,
                alignment = TextAnchor.MiddleCenter
            };
            subTitleStyle.normal.textColor = new Color(0.85f, 0.85f, 0.90f);

            sectionHeaderStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft
            };
            sectionHeaderStyle.normal.textColor = new Color(1.0f, 0.82f, 0.40f);

            bodyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                wordWrap = true,
                alignment = TextAnchor.MiddleLeft
            };
            bodyStyle.normal.textColor = new Color(0.90f, 0.90f, 0.92f);

            highlightStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft
            };
            highlightStyle.normal.textColor = new Color(1.0f, 0.72f, 0.28f);

            keycapStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft
            };
            keycapStyle.normal.textColor = new Color(0.40f, 0.85f, 1.0f);

            buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            buttonStyle.normal.background = buttonTex;
            buttonStyle.normal.textColor = Color.white;
            buttonStyle.hover.background = buttonHoverTex;
            buttonStyle.hover.textColor = Color.black;

            hudBoxStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(12, 12, 8, 8)
            };
            hudBoxStyle.normal.background = cardTex;

            hudObjectiveStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                wordWrap = true
            };
            hudObjectiveStyle.normal.textColor = Color.white;

            pillStyle = new GUIStyle(GUI.skin.box)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            pillStyle.normal.background = cardTex;
            pillStyle.normal.textColor = new Color(0.95f, 0.88f, 0.65f);

            stylesInitialized = true;
        }

        private void OnGUI()
        {
            InitStyles();

            if (isInstructionsOpen)
            {
                DrawInstructionsModal();
            }
            else
            {
                DrawInGameHUD();
            }
        }

        private void DrawInstructionsModal()
        {
            // Dim background
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), bgTex);

            float cardW = Mathf.Min(880f, Screen.width - 40f);
            float cardH = Mathf.Min(640f, Screen.height - 40f);
            float cardX = (Screen.width - cardW) * 0.5f;
            float cardY = (Screen.height - cardH) * 0.5f;

            Rect cardRect = new Rect(cardX, cardY, cardW, cardH);
            GUI.DrawTexture(cardRect, cardTex);

            // Gold header accent bar
            GUI.DrawTexture(new Rect(cardX, cardY, cardW, 4f), goldBarTex);

            GUILayout.BeginArea(new Rect(cardX + 24f, cardY + 16f, cardW - 48f, cardH - 32f));

            // Title & Subtitle
            GUILayout.Label("LEGACY OF TEMPLES", titleStyle);
            GUILayout.Label("The Sacred Theft - Odisha Temple Archaeological Investigation", subTitleStyle);
            GUILayout.Space(10);

            // Story Narrative
            GUILayout.Label("THE MYSTERY & PROLOGUE", sectionHeaderStyle);
            GUILayout.Label(
                "Dawn breaks over the sacred 12th-century Jagannath Temple in Puri, Odisha. " +
                "The inner sanctum has been breached overnight, and the sacred wooden idol of Lord Jagannath has vanished from the Ratnavedi altar! " +
                "As Detective Aryan, you must search the temple grounds, question eyewitness Meera and Priest Ananda, gather forensic clues, decode ancient stone chakra reliefs, and pursue the shadowy thieves through the moonlit jungle.",
                bodyStyle
            );
            GUILayout.Space(8);

            // Story Level Progression (Answering the user's question directly!)
            GUILayout.Label("INVESTIGATION PROGRESSION (5 SCENES IN THE STORY)", sectionHeaderStyle);
            DrawSceneProgressRow("Scene 1 - Courtyard", "Arrive at dawn. Inspect Jagannath Temple exterior, lions, lamps & speak to witnesses.", true);
            DrawSceneProgressRow("Scene 2 - Sanctum", "Enter Garbhagriha. Inspect the empty idol pedestal and ransacked ritual altar.", false);
            DrawSceneProgressRow("Scene 3 - Shrine Crime", "Forensic crime scene. Examine muddy footprints, broken lamp & torn robber silk.", false);
            DrawSceneProgressRow("Scene 4 - Puzzle Wall", "Secret passage. Rotate the Chakra wheel and solve 4 relief panels to unlock vault.", false);
            DrawSceneProgressRow("Scene 5 - Jungle Path", "Chase shadow thieves through moonlit jungle steps to confront them and recover the idol.", false);
            GUILayout.Space(8);

            // Controls
            GUILayout.Label("HOW TO PLAY & CONTROLS", sectionHeaderStyle);
            GUILayout.BeginHorizontal();

            // Column 1: Movement
            GUILayout.BeginVertical(GUILayout.Width((cardW - 60f) * 0.5f));
            DrawControlRow("[W] [A] [S] [D]", "Move & Walk (Full 3D leg locomotion)");
            DrawControlRow("[Left Shift]", "Sprint / High-speed Run");
            DrawControlRow("[C]", "Stealth Crouch");
            DrawControlRow("[Space]", "Jump / Advance Dialogue");
            GUILayout.EndVertical();

            // Column 2: Camera & Actions
            GUILayout.BeginVertical(GUILayout.Width((cardW - 60f) * 0.5f));
            DrawControlRow("[Right Mouse Drag]", "Orbit Camera & Free Look");
            DrawControlRow("[Mouse Scroll]", "Zoom Camera In / Out");
            DrawControlRow("[E] / [Enter]", "Interact with NPCs & Artifacts");
            DrawControlRow("[1] - [5] Keys", "Fast-Travel between 5 Scenes");
            DrawControlRow("[Q] / [H]", "Cycle Graphics / Re-open Instructions");
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();
            GUILayout.FlexibleSpace();

            // Start Button
            if (GUILayout.Button("START INVESTIGATION  [Press Space / Enter]", buttonStyle, GUILayout.Height(40)))
            {
                isInstructionsOpen = false;
            }

            GUILayout.EndArea();
        }

        private void DrawSceneProgressRow(string sceneTitle, string desc, bool isCurrent)
        {
            GUILayout.BeginHorizontal();
            string statusTag = isCurrent ? "[CURRENT]" : "[PENDING]";
            Color tagCol = isCurrent ? new Color(0.4f, 1.0f, 0.4f) : new Color(0.7f, 0.7f, 0.7f);
            
            var prevCol = GUI.color;
            GUI.color = tagCol;
            GUILayout.Label(statusTag, keycapStyle, GUILayout.Width(80));
            GUI.color = prevCol;

            GUILayout.Label(sceneTitle + ":", highlightStyle, GUILayout.Width(170));
            GUILayout.Label(desc, bodyStyle);
            GUILayout.EndHorizontal();
        }

        private void DrawControlRow(string key, string desc)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(key, keycapStyle, GUILayout.Width(150));
            GUILayout.Label(desc, bodyStyle);
            GUILayout.EndHorizontal();
        }

        private void DrawInGameHUD()
        {
            // Top-Left: Investigation Notebook
            Rect nbRect = new Rect(16, 16, 360, 105);
            GUI.Box(nbRect, GUIContent.none, hudBoxStyle);
            GUI.DrawTexture(new Rect(16, 16, 360, 3f), goldBarTex);

            GUILayout.BeginArea(new Rect(26, 24, 340, 90));
            GUILayout.Label($"INVESTIGATION NOTEBOOK - {currentSceneName.ToUpper()}", sectionHeaderStyle);
            GUILayout.Label($"Objective: {currentObjective}", hudObjectiveStyle);
            GUILayout.Label($"Forensic Clues Found: {cluesFound} / {TotalClues}", bodyStyle);
            GUILayout.EndArea();

            // Top-Right: Fast Info Pill
            Rect pillRect = new Rect(Screen.width - 320, 16, 304, 36);
            GUI.Box(pillRect, $"Scene: {currentSceneName} (1/5)  |  [H] Help / Controls", pillStyle);

            // Bottom-Right: Controls reminder
            Rect botRect = new Rect(Screen.width - 440, Screen.height - 38, 424, 28);
            GUI.Box(botRect, "[WASD] Move  |  [Shift] Sprint  |  [E] Interact  |  [1-5] Scenes", pillStyle);
        }
    }
}
