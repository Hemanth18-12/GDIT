using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using LegacyOfTemples.Core;
using LegacyOfTemples.Characters;

namespace LegacyOfTemples.Gameplay
{
    public class OpeningCinematic : MonoBehaviour
    {
        [System.Serializable]
        public struct Shot
        {
            public string shotName;
            public Vector3 cameraStartPos;
            public Vector3 cameraEndPos;
            public Vector3 cameraStartEuler;
            public Vector3 cameraEndEuler;
            public float duration;
            [TextArea(2, 4)]
            public string subtitle;
        }

        [Header("Cinematic Sequence")]
        [SerializeField] private Shot[] shots;
        [SerializeField] private bool playOnStart = true;

        private OrbitCamera orbitCam;
        private PlayerController player;
        private bool isPlaying = false;
        private int currentShotIndex = 0;
        private float shotTimer = 0f;

        // Subtitles GUI
        private GUIStyle subtitleStyle;
        private GUIStyle skipStyle;
        private Texture2D barTex;
        private bool stylesInit = false;
        private string currentSubtitle = "";

        private void Start()
        {
            orbitCam = UnityEngine.Object.FindAnyObjectByType<OrbitCamera>();
            player = UnityEngine.Object.FindAnyObjectByType<PlayerController>();

            // Setup default shots if not assigned
            if (shots == null || shots.Length == 0)
            {
                SetupDefaultShots();
            }

            // Check if opening cinematic is requested
            bool shouldPlay = playOnStart || PlayerPrefs.GetInt("OpeningCinematicActive", 0) == 1;
            if (shouldPlay)
            {
                PlayerPrefs.SetInt("OpeningCinematicActive", 0);
                PlayerPrefs.Save();
                StartCinematic();
            }
        }

        private void SetupDefaultShots()
        {
            shots = new Shot[]
            {
                new Shot
                {
                    shotName = "WideEstablishing",
                    cameraStartPos = new Vector3(0f, 16.0f, 32.0f),
                    cameraEndPos = new Vector3(0f, 10.0f, 24.0f),
                    cameraStartEuler = new Vector3(18f, 180f, 0f),
                    cameraEndEuler = new Vector3(14f, 180f, 0f),
                    duration = 5.5f,
                    subtitle = "Dawn breaks over the sacred Jagannath Temple. A sanctuary of centuries-old devotion and living heritage."
                },
                new Shot
                {
                    shotName = "ArchitecturePush",
                    cameraStartPos = new Vector3(8.0f, 6.0f, 18.0f),
                    cameraEndPos = new Vector3(2.0f, 4.0f, 10.0f),
                    cameraStartEuler = new Vector3(12f, 195f, 0f),
                    cameraEndEuler = new Vector3(8f, 185f, 0f),
                    duration = 5.0f,
                    subtitle = "Carved stone vimanas and towering mandapas stand silent, having watched over generations."
                },
                new Shot
                {
                    shotName = "PriestDuty",
                    cameraStartPos = new Vector3(4.5f, 2.0f, 9.5f),
                    cameraEndPos = new Vector3(3.2f, 1.6f, 10.5f),
                    cameraStartEuler = new Vector3(5f, -60f, 0f),
                    cameraEndEuler = new Vector3(4f, -45f, 0f),
                    duration = 4.5f,
                    subtitle = "Priest Ananda begins his morning prayers, unaware of the sacrilege that unfolded in the dead of night."
                },
                new Shot
                {
                    shotName = "ThievesShadows",
                    cameraStartPos = new Vector3(-8.0f, 2.5f, 1.0f),
                    cameraEndPos = new Vector3(-4.5f, 1.8f, -2.0f),
                    cameraStartEuler = new Vector3(10f, 75f, 0f),
                    cameraEndEuler = new Vector3(8f, 60f, 0f),
                    duration = 5.0f,
                    subtitle = "Hours earlier... two shadows breached the restricted storage chamber, stealing three ancient relics."
                },
                new Shot
                {
                    shotName = "InvestigatorArrival",
                    cameraStartPos = new Vector3(0f, 2.8f, 18.5f),
                    cameraEndPos = new Vector3(0f, 2.2f, 17.0f),
                    cameraStartEuler = new Vector3(6f, 180f, 0f),
                    cameraEndEuler = new Vector3(5f, 180f, 0f),
                    duration = 5.0f,
                    subtitle = "Detective Aryan arrives on the scene. The investigation begins now."
                }
            };
        }

        public void StartCinematic()
        {
            isPlaying = true;
            currentShotIndex = 0;
            shotTimer = 0f;

            if (player != null) player.enabled = false;
        }

        private void Update()
        {
            if (!isPlaying) return;

            // Check for skip
            Keyboard kb = Keyboard.current;
            Gamepad gp = Gamepad.current;
            if ((kb != null && (kb.spaceKey.wasPressedThisFrame || kb.escapeKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame)) ||
                (gp != null && (gp.buttonSouth.wasPressedThisFrame || gp.startButton.wasPressedThisFrame)))
            {
                EndCinematic();
                return;
            }

            if (currentShotIndex >= shots.Length)
            {
                EndCinematic();
                return;
            }

            Shot currentShot = shots[currentShotIndex];
            shotTimer += Time.deltaTime;
            float t = Mathf.Clamp01(shotTimer / currentShot.duration);
            // Smooth step interpolation
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            Vector3 camPos = Vector3.Lerp(currentShot.cameraStartPos, currentShot.cameraEndPos, smoothT);
            Quaternion camRot = Quaternion.Euler(Vector3.Lerp(currentShot.cameraStartEuler, currentShot.cameraEndEuler, smoothT));

            if (orbitCam != null)
            {
                orbitCam.SetCutsceneOverride(camPos, camRot);
            }

            currentSubtitle = currentShot.subtitle;

            if (shotTimer >= currentShot.duration)
            {
                currentShotIndex++;
                shotTimer = 0f;
            }
        }

        public void EndCinematic()
        {
            isPlaying = false;
            currentSubtitle = "";

            if (orbitCam != null)
            {
                orbitCam.ClearCutsceneOverride();
            }

            if (player != null)
            {
                player.enabled = true;
            }
        }

        private void InitStyles()
        {
            if (stylesInit) return;

            barTex = new Texture2D(2, 2);
            Color[] pix = new Color[4];
            for (int i = 0; i < 4; i++) pix[i] = new Color(0.04f, 0.04f, 0.04f, 0.88f);
            barTex.SetPixels(pix);
            barTex.Apply();

            subtitleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 17,
                fontStyle = FontStyle.Normal,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true
            };
            subtitleStyle.normal.textColor = new Color(1.0f, 0.94f, 0.82f);

            skipStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Italic,
                alignment = TextAnchor.MiddleRight
            };
            skipStyle.normal.textColor = new Color(0.75f, 0.70f, 0.60f);

            stylesInit = true;
        }

        private void OnGUI()
        {
            if (!isPlaying) return;

            InitStyles();

            bool subtitlesEnabled = PlayerPrefs.GetInt("Settings_Subtitles", 1) == 1;

            float barHeight = 85f;
            Rect bottomBar = new Rect(0, Screen.height - barHeight, Screen.width, barHeight);
            GUI.DrawTexture(bottomBar, barTex);

            if (subtitlesEnabled && !string.IsNullOrEmpty(currentSubtitle))
            {
                GUI.Label(new Rect(80f, Screen.height - barHeight + 10f, Screen.width - 160f, barHeight - 20f), currentSubtitle, subtitleStyle);
            }

            // Skip Hint
            GUI.Label(new Rect(Screen.width - 250f, 20f, 230f, 24f), "[Space / Enter] Skip Prologue", skipStyle);
        }
    }
}

