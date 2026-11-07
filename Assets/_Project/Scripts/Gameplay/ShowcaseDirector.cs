using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using LegacyOfTemples.Core;
using LegacyOfTemples.Characters;

namespace LegacyOfTemples.Gameplay
{
    public class ShowcaseDirector : MonoBehaviour
    {
        public static ShowcaseDirector Instance { get; private set; }

        [System.Serializable]
        public struct ShowcaseShot
        {
            public string sceneName;
            public Vector3 camStartPos;
            public Vector3 camEndPos;
            public Vector3 camStartEuler;
            public Vector3 camEndEuler;
            public float duration;
            public string title;
            public string subtitle;
        }

        private bool isTourActive = false;
        private int currentShotIndex = 0;
        private float shotTimer = 0f;
        private OrbitCamera orbitCam;
        private List<ShowcaseShot> activeShots = new List<ShowcaseShot>();

        // Letterbox & Text Styles

        public bool IsTourActive => isTourActive;

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

        private void Start()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            CheckAndStartTour();
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            orbitCam = UnityEngine.Object.FindAnyObjectByType<OrbitCamera>();
            if (isTourActive)
            {
                SetupShotsForScene(scene.name);
            }
        }

        public void CheckAndStartTour()
        {
            if (PlayerPrefs.GetInt("ShowcaseTourActive", 0) == 1)
            {
                StartTour();
            }
        }

        public void StartTour()
        {
            isTourActive = true;
            PlayerPrefs.SetInt("ShowcaseTourActive", 1);
            PlayerPrefs.Save();

            string currentScene = SceneManager.GetActiveScene().name;
            SetupShotsForScene(currentScene);

            // Hide normal player control during showcase
            PlayerController pc = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
            if (pc != null) pc.enabled = false;

            Debug.Log($"[ShowcaseDirector] Started Showcase Tour in scene '{currentScene}'.");
            EventBus.IsShowcaseActive = true;
            EventBus.RaiseShowcaseTourToggled(true);
        }

        public void StopTour(bool loadMenu = true)
        {
            isTourActive = false;
            EventBus.IsShowcaseActive = false;
            PlayerPrefs.SetInt("ShowcaseTourActive", 0);
            PlayerPrefs.Save();
            EventBus.RaiseShowcaseTourToggled(false);

            if (orbitCam != null)
            {
                orbitCam.ClearCutsceneOverride();
            }

            PlayerController pc = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
            if (pc != null) pc.enabled = true;

            if (loadMenu)
            {
                SceneManager.LoadScene("MainMenu");
            }
        }

        private void SetupShotsForScene(string sceneName)
        {
            activeShots.Clear();
            currentShotIndex = 0;
            shotTimer = 0f;

            switch (sceneName)
            {
                case Constants.Courtyard:
                    activeShots.Add(new ShowcaseShot
                    {
                        sceneName = Constants.Courtyard,
                        camStartPos = new Vector3(0f, 15f, 30f),
                        camEndPos = new Vector3(0f, 6.5f, 22f),
                        camStartEuler = new Vector3(18f, 180f, 0f),
                        camEndEuler = new Vector3(10f, 180f, 0f),
                        duration = 6.0f,
                        title = "JAGANNATH TEMPLE COURTYARD",
                        subtitle = "Carved Odisha Sandstone Deula · Sunrise Dawn"
                    });
                    activeShots.Add(new ShowcaseShot
                    {
                        sceneName = Constants.Courtyard,
                        camStartPos = new Vector3(0f, 2.5f, 18f),
                        camEndPos = new Vector3(0f, 1.8f, 13.5f),
                        camStartEuler = new Vector3(6f, 180f, 0f),
                        camEndEuler = new Vector3(4f, 180f, 0f),
                        duration = 5.5f,
                        title = "INVESTIGATOR ARYAN",
                        subtitle = "Lead Heritage Investigator · Examining Crime Scene"
                    });
                    activeShots.Add(new ShowcaseShot
                    {
                        sceneName = Constants.Courtyard,
                        camStartPos = new Vector3(3.5f, 2.2f, 13f),
                        camEndPos = new Vector3(2.2f, 1.6f, 10.5f),
                        camStartEuler = new Vector3(6f, -35f, 0f),
                        camEndEuler = new Vector3(4f, -25f, 0f),
                        duration = 5.5f,
                        title = "PUJARI ANANDA",
                        subtitle = "Elderly Head Priest · Custodian of Sacred Living Heritage"
                    });
                    activeShots.Add(new ShowcaseShot
                    {
                        sceneName = Constants.Courtyard,
                        camStartPos = new Vector3(-3.5f, 2.2f, 13f),
                        camEndPos = new Vector3(-2.2f, 1.6f, 10.5f),
                        camStartEuler = new Vector3(6f, 35f, 0f),
                        camEndEuler = new Vector3(4f, 25f, 0f),
                        duration = 5.5f,
                        title = "MEERA",
                        subtitle = "Village Witness · Saw Red-Scarfed & Black-Hooded Robbers"
                    });
                    break;

                case Constants.Sanctum:
                    activeShots.Add(new ShowcaseShot
                    {
                        sceneName = Constants.Sanctum,
                        camStartPos = new Vector3(0f, 4.5f, 16f),
                        camEndPos = new Vector3(0f, 2.6f, 10f),
                        camStartEuler = new Vector3(12f, 180f, 0f),
                        camEndEuler = new Vector3(6f, 180f, 0f),
                        duration = 6.0f,
                        title = "INNER SANCTUM SANCTORUM",
                        subtitle = "Carved Pillars · Brass Oil Lamps · Sacred Background Shrine"
                    });
                    activeShots.Add(new ShowcaseShot
                    {
                        sceneName = Constants.Sanctum,
                        camStartPos = new Vector3(-2f, 2.2f, 7f),
                        camEndPos = new Vector3(0f, 1.4f, 4f),
                        camStartEuler = new Vector3(8f, 160f, 0f),
                        camEndEuler = new Vector3(4f, 180f, 0f),
                        duration = 5.5f,
                        title = "THE CRIME: THREE EMPTY STANDS",
                        subtitle = "Brass Vessel · Sandstone Rosette · Palm-Leaf Manuscript Stolen"
                    });
                    break;

                case Constants.ShrineCrime:
                    activeShots.Add(new ShowcaseShot
                    {
                        sceneName = Constants.ShrineCrime,
                        camStartPos = new Vector3(0f, 4.0f, 14f),
                        camEndPos = new Vector3(0f, 2.4f, 7f),
                        camStartEuler = new Vector3(10f, 180f, 0f),
                        camEndEuler = new Vector3(5f, 180f, 0f),
                        duration = 6.0f,
                        title = "RANSACKED CRIME SCENE",
                        subtitle = "Broken Lamp · Bootprints · Red Silk Caught on Door"
                    });
                    break;

                case Constants.PuzzleWall:
                    activeShots.Add(new ShowcaseShot
                    {
                        sceneName = Constants.PuzzleWall,
                        camStartPos = new Vector3(0f, 4.5f, 9.5f),
                        camEndPos = new Vector3(0f, 2.2f, 4.8f),
                        camStartEuler = new Vector3(8f, 180f, 0f),
                        camEndEuler = new Vector3(4f, 180f, 0f),
                        duration = 6.0f,
                        title = "CARVED SURYA CHAKRA PUZZLE WALL",
                        subtitle = "Lotus · Conch · Wheel · Kalash Solar Mechanism"
                    });
                    break;

                case Constants.JunglePath:
                    activeShots.Add(new ShowcaseShot
                    {
                        sceneName = Constants.JunglePath,
                        camStartPos = new Vector3(0f, 6.0f, 25f),
                        camEndPos = new Vector3(0f, 3.2f, 16f),
                        camStartEuler = new Vector3(12f, 180f, 0f),
                        camEndEuler = new Vector3(6f, 180f, 0f),
                        duration = 6.0f,
                        title = "MOONLIT JUNGLE RUINS",
                        subtitle = "Ancient Stone Steps · Dense Forest Canopy · Thieves Cornered"
                    });
                    activeShots.Add(new ShowcaseShot
                    {
                        sceneName = Constants.JunglePath,
                        camStartPos = new Vector3(2.5f, 2.8f, -10f),
                        camEndPos = new Vector3(1.2f, 2.2f, -12.5f),
                        camStartEuler = new Vector3(6f, 195f, 0f),
                        camEndEuler = new Vector3(4f, 185f, 0f),
                        duration = 5.5f,
                        title = "BHASKAR & KISHORE",
                        subtitle = "Smugglers Cornered · Stolen Relics Recovered Without Violence"
                    });
                    break;

                default:
                    StopTour(false);
                    break;
            }
        }

        private void Update()
        {
            if (!isTourActive) return;

            // Esc stops the tour and returns to menu
            var kb = Keyboard.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame)
            {
                StopTour(true);
                return;
            }

            if (activeShots.Count == 0) return;

            ShowcaseShot currentShot = activeShots[currentShotIndex];
            shotTimer += Time.deltaTime;
            float t = Mathf.Clamp01(shotTimer / currentShot.duration);
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            Vector3 pos = Vector3.Lerp(currentShot.camStartPos, currentShot.camEndPos, smoothT);
            Quaternion rot = Quaternion.Euler(Vector3.Lerp(currentShot.camStartEuler, currentShot.camEndEuler, smoothT));

            if (orbitCam != null)
            {
                orbitCam.SetCutsceneOverride(pos, rot);
            }

            if (shotTimer >= currentShot.duration)
            {
                currentShotIndex++;
                shotTimer = 0f;

                if (currentShotIndex >= activeShots.Count)
                {
                    AdvanceToNextScene();
                }
            }
        }

        private void AdvanceToNextScene()
        {
            string currentScene = SceneManager.GetActiveScene().name;
            switch (currentScene)
            {
                case Constants.Courtyard:
                    SceneManager.LoadScene(Constants.Sanctum);
                    break;
                case Constants.Sanctum:
                    SceneManager.LoadScene(Constants.ShrineCrime);
                    break;
                case Constants.ShrineCrime:
                    SceneManager.LoadScene(Constants.PuzzleWall);
                    break;
                case Constants.PuzzleWall:
                    SceneManager.LoadScene(Constants.JunglePath);
                    break;
                case Constants.JunglePath:
                default:
                    // Tour complete! Return to menu
                    StopTour(true);
                    break;
            }
        }

        private void LateUpdate()
        {
            if (!isTourActive || activeShots.Count == 0 || currentShotIndex >= activeShots.Count) return;
            ShowcaseShot s = activeShots[currentShotIndex];
            ScreenText.Panel("showcase_caption", s.title, s.subtitle, PanelAnchor.BottomCenter, 1400f, null, false, 10);
            ScreenText.Panel("showcase_exit", null, ScreenText.Key("Esc") + " Exit showcase", PanelAnchor.TopRight, 420f, null, false, 10);
        }
    }
}

