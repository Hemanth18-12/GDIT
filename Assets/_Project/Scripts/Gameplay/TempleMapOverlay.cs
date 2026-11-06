using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace LegacyOfTemples.Gameplay
{
    public class TempleMapOverlay : MonoBehaviour
    {
        public static TempleMapOverlay Instance { get; private set; }

        private bool isOpen = false;
        private Transform playerTransform;

        private GUIStyle panelStyle;
        private GUIStyle mapTitleStyle;
        private GUIStyle zoneLabelStyle;
        private GUIStyle activeZoneStyle;
        private GUIStyle poiStyle;
        private GUIStyle legendStyle;

        public bool IsOpen => isOpen;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Update()
        {
            var kb = Keyboard.current;
            if (kb != null && kb.mKey.wasPressedThisFrame)
            {
                ToggleMap();
            }

            var gp = Gamepad.current;
            if (gp != null && gp.startButton.wasPressedThisFrame)
            {
                ToggleMap();
            }

            if (isOpen && kb != null && kb.escapeKey.wasPressedThisFrame)
            {
                ToggleMap();
            }

            if (isOpen && playerTransform == null)
            {
                GameObject player = GameObject.FindWithTag("Player");
                if (player != null) playerTransform = player.transform;
            }
        }

        public void ToggleMap()
        {
            isOpen = !isOpen;
            if (isOpen)
            {
                GameObject player = GameObject.FindWithTag("Player");
                if (player != null) playerTransform = player.transform;
            }
        }

        private void OnGUI()
        {
            if (!isOpen) return;

            InitStyles();

            float mapWidth = Mathf.Min(780f, Screen.width - 40f);
            float mapHeight = Mathf.Min(560f, Screen.height - 40f);
            float mapX = (Screen.width - mapWidth) * 0.5f;
            float mapY = (Screen.height - mapHeight) * 0.5f;

            // Background Map Parchment Box
            GUI.Box(new Rect(mapX, mapY, mapWidth, mapHeight), GUIContent.none, panelStyle);

            // Title
            GUI.Label(new Rect(mapX + 20f, mapY + 16f, mapWidth - 40f, 26f), "SURYA JAGANNATH TEMPLE ARCHITECTURAL SCHEMATIC", mapTitleStyle);
            string currentScene = SceneManager.GetActiveScene().name;
            GUI.Label(new Rect(mapX + 20f, mapY + 44f, mapWidth - 40f, 20f), $"Active Sector: <color=#f4c430>{currentScene.ToUpperInvariant()}</color>  |  Orientation: True North Facing", legendStyle);

            // Inner Canvas for Map Layout
            Rect canvas = new Rect(mapX + 30f, mapY + 70f, mapWidth - 60f, mapHeight - 120f);
            GUI.Box(canvas, GUIContent.none, GUI.skin.box);

            // Zone 1: Courtyard (Bottom center)
            Rect courtyardRect = new Rect(canvas.x + canvas.width * 0.35f, canvas.y + canvas.height * 0.65f, canvas.width * 0.30f, canvas.height * 0.28f);
            DrawZone(courtyardRect, "COURTYARD PLAZA\n(Start / Entryway)", currentScene == "Courtyard");

            // Zone 2: Sanctum Sanctorum (Center Top)
            Rect sanctumRect = new Rect(canvas.x + canvas.width * 0.35f, canvas.y + canvas.height * 0.15f, canvas.width * 0.30f, canvas.height * 0.38f);
            DrawZone(sanctumRect, "SANCTUM SANCTORUM\n& RELIC VAULT\n(Deity Shrine)", currentScene == "Sanctum");

            // Zone 3: Outer Shrine Crime Scene (East / Right)
            Rect shrineRect = new Rect(canvas.x + canvas.width * 0.72f, canvas.y + canvas.height * 0.40f, canvas.width * 0.24f, canvas.height * 0.35f);
            DrawZone(shrineRect, "OUTER SHRINE\nCRIME SCENE\n(Level 2 Investigation)", currentScene == "ShrineCrime");

            // Zone 4: Ancient Chakra Puzzle Wall (West / Left)
            Rect puzzleRect = new Rect(canvas.x + canvas.width * 0.04f, canvas.y + canvas.height * 0.40f, canvas.width * 0.24f, canvas.height * 0.35f);
            DrawZone(puzzleRect, "CHAKRA PUZZLE WALL\n(Surya Wheel Seal)", currentScene == "PuzzleWall");

            // Zone 5: Overgrown Jungle Path (South / Far Bottom)
            Rect jungleRect = new Rect(canvas.x + canvas.width * 0.15f, canvas.y + canvas.height * 0.95f, canvas.width * 0.70f, 24f);
            // Just a path label
            GUI.Label(new Rect(canvas.x + canvas.width * 0.2f, canvas.y + canvas.height - 28f, canvas.width * 0.6f, 24f), "▼ SOUTHWARD TRAIL TO DEEP JUNGLE RUINS (LEVEL 3) ▼", legendStyle);

            // Draw player indicator
            DrawPlayerIndicator(canvas, currentScene);

            // POI markers
            DrawPOIs(canvas, currentScene);

            // Bottom Navigation Hint
            GUI.Label(new Rect(mapX + 20f, mapY + mapHeight - 34f, mapWidth - 40f, 22f), "Press [M] or [Esc] to Close Map  •  [J] Investigation Dossier  •  [H] Hints", legendStyle);
        }

        private void DrawZone(Rect rect, string name, bool isCurrent)
        {
            Color oldBg = GUI.backgroundColor;
            GUI.backgroundColor = isCurrent ? new Color(0.9f, 0.75f, 0.2f, 0.6f) : new Color(0.2f, 0.22f, 0.25f, 0.5f);
            GUI.Box(rect, GUIContent.none, GUI.skin.box);
            GUI.backgroundColor = oldBg;

            GUIStyle labelStyle = isCurrent ? activeZoneStyle : zoneLabelStyle;
            GUI.Label(rect, name, labelStyle);
        }

        private void DrawPlayerIndicator(Rect canvas, string currentScene)
        {
            Vector2 playerNormPos = new Vector2(0.5f, 0.5f);

            switch (currentScene)
            {
                case "Courtyard":
                    playerNormPos = new Vector2(0.5f, 0.78f);
                    break;
                case "Sanctum":
                    playerNormPos = new Vector2(0.5f, 0.32f);
                    break;
                case "ShrineCrime":
                    playerNormPos = new Vector2(0.84f, 0.55f);
                    break;
                case "PuzzleWall":
                    playerNormPos = new Vector2(0.16f, 0.55f);
                    break;
                case "JunglePath":
                    playerNormPos = new Vector2(0.5f, 0.92f);
                    break;
                default:
                    playerNormPos = new Vector2(0.5f, 0.5f);
                    break;
            }

            // If player transform exists, offset slightly based on world coordinates
            if (playerTransform != null)
            {
                Vector3 p = playerTransform.position;
                playerNormPos.x += Mathf.Clamp(p.x * 0.003f, -0.08f, 0.08f);
                playerNormPos.y += Mathf.Clamp(-p.z * 0.003f, -0.08f, 0.08f);
            }

            float px = canvas.x + canvas.width * playerNormPos.x;
            float py = canvas.y + canvas.height * playerNormPos.y;

            // Draw pulsing marker
            float pulse = Mathf.Sin(Time.time * 6f) * 3f;
            float markerSize = 16f + pulse;
            GUI.color = new Color(0.2f, 1f, 0.3f, 0.95f);
            GUI.Box(new Rect(px - markerSize * 0.5f, py - markerSize * 0.5f, markerSize, markerSize), GUIContent.none);
            GUI.color = Color.white;
            GUI.Label(new Rect(px - 60f, py + markerSize * 0.5f + 2f, 120f, 18f), "YOU ARE HERE", activeZoneStyle);
        }

        private void DrawPOIs(Rect canvas, string currentScene)
        {
            // Legend on right or top
            float lx = canvas.x + 12f;
            float ly = canvas.y + 12f;
            GUI.Label(new Rect(lx, ly, 180f, 18f), "<color=#00d2d3>◆</color> Head Priest Ananda", poiStyle);
            GUI.Label(new Rect(lx, ly + 18f, 180f, 18f), "<color=#54a0ff>◆</color> Witness Meera Devi", poiStyle);
            GUI.Label(new Rect(lx, ly + 36f, 180f, 18f), "<color=#ff9f43>▲</color> Breached Relic Vault", poiStyle);
            GUI.Label(new Rect(lx, ly + 54f, 180f, 18f), "<color=#ee5253>★</color> Suspects Escape Trail", poiStyle);
        }

        private void InitStyles()
        {
            if (panelStyle == null)
            {
                panelStyle = new GUIStyle(GUI.skin.box);
            }

            if (mapTitleStyle == null)
            {
                mapTitleStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 16,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = new Color(0.96f, 0.82f, 0.28f) }
                };
            }

            if (zoneLabelStyle == null)
            {
                zoneLabelStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 11,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = new Color(0.75f, 0.75f, 0.75f) }
                };
            }

            if (activeZoneStyle == null)
            {
                activeZoneStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 12,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                    richText = true,
                    normal = { textColor = new Color(1f, 0.85f, 0.25f) }
                };
            }

            if (poiStyle == null)
            {
                poiStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 11,
                    richText = true,
                    normal = { textColor = Color.white }
                };
            }

            if (legendStyle == null)
            {
                legendStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 11,
                    alignment = TextAnchor.MiddleCenter,
                    richText = true,
                    normal = { textColor = new Color(0.7f, 0.7f, 0.7f) }
                };
            }
        }
    }
}

