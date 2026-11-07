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
                    subtitle = "Dawn at the old temple. The festival is only days away."
                },
                new Shot
                {
                    shotName = "ArchitecturePush",
                    cameraStartPos = new Vector3(8.0f, 6.0f, 18.0f),
                    cameraEndPos = new Vector3(2.0f, 4.0f, 10.0f),
                    cameraStartEuler = new Vector3(12f, 195f, 0f),
                    cameraEndEuler = new Vector3(8f, 185f, 0f),
                    duration = 5.0f,
                    subtitle = "Its carved stone towers have watched over the village for generations."
                },
                new Shot
                {
                    shotName = "PriestDuty",
                    cameraStartPos = new Vector3(4.5f, 2.0f, 9.5f),
                    cameraEndPos = new Vector3(3.2f, 1.6f, 10.5f),
                    cameraStartEuler = new Vector3(5f, -60f, 0f),
                    cameraEndEuler = new Vector3(4f, -45f, 0f),
                    duration = 4.5f,
                    subtitle = "Pujari Ananda begins the morning rituals, unaware of what happened in the night."
                },
                new Shot
                {
                    shotName = "ThievesShadows",
                    cameraStartPos = new Vector3(-8.0f, 2.5f, 1.0f),
                    cameraEndPos = new Vector3(-4.5f, 1.8f, -2.0f),
                    cameraStartEuler = new Vector3(10f, 75f, 0f),
                    cameraEndEuler = new Vector3(8f, 60f, 0f),
                    duration = 5.0f,
                    subtitle = "Hours earlier, two figures slipped into the storage hall and took three treasured pieces."
                },
                new Shot
                {
                    shotName = "InvestigatorArrival",
                    cameraStartPos = new Vector3(0f, 2.8f, 18.5f),
                    cameraEndPos = new Vector3(0f, 2.2f, 17.0f),
                    cameraStartEuler = new Vector3(6f, 180f, 0f),
                    cameraEndEuler = new Vector3(5f, 180f, 0f),
                    duration = 5.0f,
                    subtitle = "Investigator Aryan arrives. The search begins."
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

        private void LateUpdate()
        {
            if (!isPlaying || LegacyOfTemples.Core.EventBus.IsShowcaseActive) return;
            bool subtitlesEnabled = PlayerPrefs.GetInt("Settings_Subtitles", 1) == 1;
            if (subtitlesEnabled && !string.IsNullOrEmpty(currentSubtitle))
                ScreenText.Panel("cinematic_sub", null, currentSubtitle, PanelAnchor.BottomCenter, 1500f, null, false, 10);
            ScreenText.Panel("cinematic_skip", null, ScreenText.Key("Space") + " / " + ScreenText.Key("Enter") + " Skip", PanelAnchor.TopRight, 420f, null, false, 10);
        }
    }
}

