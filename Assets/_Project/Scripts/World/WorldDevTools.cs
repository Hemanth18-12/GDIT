using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.InputSystem;
using LegacyOfTemples.Core;

namespace LegacyOfTemples.World
{
    [DefaultExecutionOrder(-90)]
    public class WorldDevTools : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private bool showHUD = true;

        private float fadeAlpha = 0f;
        private Texture2D fadeTexture;
        private GUIStyle hudStyle;
        private GUIStyle titleStyle;
        private string qualityToast = string.Empty;
        private float toastTimer = 0f;

        private void Awake()
        {
            fadeTexture = new Texture2D(1, 1);
            fadeTexture.SetPixel(0, 0, Color.black);
            fadeTexture.Apply();

            // Set default 60 FPS target frame rate
            Application.targetFrameRate = 60;
        }

        private void OnEnable()
        {
            if (WorldManager.Instance != null)
            {
                WorldManager.Instance.OnFadeProgress += HandleFade;
            }
        }

        private void OnDisable()
        {
            if (WorldManager.Instance != null)
            {
                WorldManager.Instance.OnFadeProgress -= HandleFade;
            }
        }

        private void Start()
        {
            if (WorldManager.Instance != null)
            {
                WorldManager.Instance.OnFadeProgress += HandleFade;
            }
        }

        private void HandleFade(float alpha)
        {
            fadeAlpha = alpha;
        }

        private void Update()
        {
            var kb = Keyboard.current;
            if (kb != null)
            {
                // Dev Keys 1-5 to switch scenes
                if (kb.digit1Key.wasPressedThisFrame || kb.numpad1Key.wasPressedThisFrame)
                {
                    SwitchScene(Constants.Courtyard);
                }
                else if (kb.digit2Key.wasPressedThisFrame || kb.numpad2Key.wasPressedThisFrame)
                {
                    SwitchScene(Constants.Sanctum);
                }
                else if (kb.digit3Key.wasPressedThisFrame || kb.numpad3Key.wasPressedThisFrame)
                {
                    SwitchScene(Constants.PuzzleWall);
                }
                else if (kb.digit4Key.wasPressedThisFrame || kb.numpad4Key.wasPressedThisFrame)
                {
                    SwitchScene(Constants.ShrineCrime);
                }
                else if (kb.digit5Key.wasPressedThisFrame || kb.numpad5Key.wasPressedThisFrame)
                {
                    SwitchScene(Constants.JunglePath);
                }

                // Key Q to cycle graphics quality
                if (kb.qKey.wasPressedThisFrame)
                {
                    CycleQuality();
                }
            }

            if (toastTimer > 0f)
            {
                toastTimer -= Time.unscaledDeltaTime;
            }
        }

        private void SwitchScene(string sceneName)
        {
            if (WorldManager.Instance != null)
            {
                Debug.Log($"[WorldDevTools] Dev switch scene -> {sceneName}");
                WorldManager.Instance.LoadScene(sceneName);
            }
        }

        private void CycleQuality()
        {
            int current = QualitySettings.GetQualityLevel();
            int total = QualitySettings.names.Length;
            int next = (current + 1) % Mathf.Max(1, total);

            QualitySettings.SetQualityLevel(next, true);
            string qName = QualitySettings.names[next];
            qualityToast = $"Quality: {qName.ToUpperInvariant()} (Target 60 FPS)";
            toastTimer = 2.5f;

            Debug.Log($"[WorldDevTools] {qualityToast}");
        }

        private void OnGUI()
        {
            InitStyles();

            // Screen Fade Loading Overlay
            if (fadeAlpha > 0.001f)
            {
                Color oldColor = GUI.color;
                GUI.color = new Color(0.04f, 0.05f, 0.07f, fadeAlpha);
                GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), fadeTexture);

                if (fadeAlpha > 0.5f)
                {
                    GUI.color = new Color(0.96f, 0.8f, 0.3f, (fadeAlpha - 0.5f) * 2f);
                    string targetScene = WorldManager.Instance != null ? WorldManager.Instance.CurrentSceneName : "Temple Sanctum";
                    GUI.Label(new Rect(0, Screen.height * 0.45f, Screen.width, 50), "LEGACY OF TEMPLES", titleStyle);
                    GUI.Label(new Rect(0, Screen.height * 0.52f, Screen.width, 30), $"Entering {targetScene}...", hudStyle);
                }

                GUI.color = oldColor;
            }

            // Dev HUD (Editor only - stripped from release builds)
            if (showHUD && (Application.isEditor || Debug.isDebugBuild) &&
                !LegacyOfTemples.Core.EventBus.IsPhotoModeActive &&
                !LegacyOfTemples.Core.EventBus.IsShowcaseActive)
            {
                string activeScene = WorldManager.Instance != null ? WorldManager.Instance.CurrentSceneName : "None";
                int qLevel = QualitySettings.GetQualityLevel();
                string qName = qLevel < QualitySettings.names.Length ? QualitySettings.names[qLevel] : "Default";

                float hudX = Screen.width - 276;
                float hudY = 16;
                GUI.Box(new Rect(hudX, hudY, 260, 96), GUIContent.none);
                GUI.Label(new Rect(hudX + 10, hudY + 6, 240, 20), "<b>LEGACY OF TEMPLES - DEV TOOLS</b>", hudStyle);
                GUI.Label(new Rect(hudX + 10, hudY + 26, 240, 20), $"Scene [1-5]: <color=#f4c430>{activeScene}</color>", hudStyle);
                GUI.Label(new Rect(hudX + 10, hudY + 46, 240, 20), $"Quality [Q]: <color=#89c4f4>{qName}</color>", hudStyle);
                GUI.Label(new Rect(hudX + 10, hudY + 66, 240, 20), $"FPS: {1.0f / Mathf.Max(Time.smoothDeltaTime, 0.0001f):F0} (Target 60)", hudStyle);

                // Toast
                if (toastTimer > 0f)
                {
                    GUI.color = new Color(1f, 0.85f, 0.2f, Mathf.Clamp01(toastTimer));
                    GUI.Label(new Rect(hudX, hudY + 100, 240, 30), qualityToast, hudStyle);
                    GUI.color = Color.white;
                }
            }
        }

        private void InitStyles()
        {
            if (hudStyle == null)
            {
                hudStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 12,
                    richText = true,
                    alignment = TextAnchor.UpperLeft
                };
            }

            if (titleStyle == null)
            {
                titleStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 20,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter
                };
            }
        }
    }
}

