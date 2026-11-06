using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using LegacyOfTemples.Core;

namespace LegacyOfTemples.Gameplay
{
    public class GameLevelManager : MonoBehaviour
    {
        public static GameLevelManager Instance { get; private set; }

        [Header("Case Progression")]
        [SerializeField] private bool level1Completed = false;
        [SerializeField] private bool level2Completed = false;
        [SerializeField] private bool level3Completed = false;

        [Header("Ending State")]
        private bool isEndingCinematicActive = false;
        private float endingTimer = 0f;

        private GUIStyle bannerStyle;
        private GUIStyle dialogBoxStyle;
        private GUIStyle bodyStyle;

        public bool Level1Completed => level1Completed;
        public bool Level2Completed => level2Completed;
        public bool Level3Completed => level3Completed;
        public bool IsEndingActive => isEndingCinematicActive;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            level1Completed = PlayerPrefs.GetInt("LevelUnlocked_2", 0) == 1;
            level2Completed = PlayerPrefs.GetInt("LevelUnlocked_3", 0) == 1;
            level3Completed = PlayerPrefs.GetInt("Case_Completed", 0) == 1;
        }

        private void Start()
        {
            EventBus.OnClueFound += HandleClueFound;
            EventBus.OnPuzzleSolved += HandlePuzzleSolved;
            EventBus.OnDialogueEnd += HandleDialogueEnd;
        }

        private void OnDestroy()
        {
            EventBus.OnClueFound -= HandleClueFound;
            EventBus.OnPuzzleSolved -= HandlePuzzleSolved;
            EventBus.OnDialogueEnd -= HandleDialogueEnd;
        }

        private void Update()
        {
            if (isEndingCinematicActive)
            {
                endingTimer += Time.deltaTime;
                if (endingTimer > 35f)
                {
                    // Return to Main Menu
                    isEndingCinematicActive = false;
                    SceneManager.LoadScene("MainMenu");
                }
            }
        }

        private void HandleClueFound(string clueId)
        {
            Debug.Log($"[GameLevelManager] Clue registered: {clueId}");
        }

        private void HandlePuzzleSolved(string puzzleId)
        {
            if (puzzleId == "chakra_wall")
            {
                CompleteLevel2();
            }
        }

        private void HandleDialogueEnd(string speakerId)
        {
            // If talking to Priest Ananda in Sanctum after finding clues, complete Level 1
            if (speakerId == Constants.Priest && SceneManager.GetActiveScene().name == "Sanctum")
            {
                if (InvestigationJournal.Instance != null && InvestigationJournal.Instance.ClueCount >= 2)
                {
                    CompleteLevel1();
                }
            }

            // If confronting Kala in JunglePath, trigger ending sequence
            if (speakerId == Constants.Robber1 && SceneManager.GetActiveScene().name == "JunglePath")
            {
                CompleteLevel3();
            }
        }

        public void CompleteLevel1()
        {
            if (!level1Completed)
            {
                level1Completed = true;
                PlayerPrefs.SetInt("LevelUnlocked_2", 1);
                PlayerPrefs.Save();
                if (InvestigationJournal.Instance != null)
                {
                    InvestigationJournal.Instance.CompleteObjective("obj_file_report");
                    InvestigationJournal.Instance.CompleteObjective("obj_track_thieves");
                }
                Debug.Log("[GameLevelManager] LEVEL 1 COMPLETED: Incident Report Filed! Level 2 Unlocked.");
            }
        }

        public void CompleteLevel2()
        {
            if (!level2Completed)
            {
                level2Completed = true;
                PlayerPrefs.SetInt("LevelUnlocked_3", 1);
                PlayerPrefs.Save();
                if (InvestigationJournal.Instance != null)
                {
                    InvestigationJournal.Instance.CompleteObjective("obj_solve_chakra");
                }
                Debug.Log("[GameLevelManager] LEVEL 2 COMPLETED: Surya Chakra Solved! Level 3 Unlocked.");
            }
        }

        public void CompleteLevel3()
        {
            if (!level3Completed)
            {
                level3Completed = true;
                PlayerPrefs.SetInt("Case_Completed", 1);
                PlayerPrefs.Save();

                if (InvestigationJournal.Instance != null)
                {
                    InvestigationJournal.Instance.CompleteObjective("obj_confront_thieves");
                    InvestigationJournal.Instance.RecoverRelic("relic_pradip");
                    InvestigationJournal.Instance.RecoverRelic("relic_rosette");
                    InvestigationJournal.Instance.RecoverRelic("relic_manuscript");
                }

                EventBus.RaiseIdolRecovered("relic_pradip");
                EventBus.RaiseIdolRecovered("relic_rosette");
                EventBus.RaiseIdolRecovered("relic_manuscript");

                TriggerEndingCinematic();
            }
        }

        public void TriggerEndingCinematic()
        {
            isEndingCinematicActive = true;
            endingTimer = 0f;
            EventBus.RaiseGameState(GameState.Ending);
            Debug.Log("[GameLevelManager] ENDING CINEMATIC STARTED: The sacred relics are restored!");
        }

        private void OnGUI()
        {
            if (!isEndingCinematicActive) return;

            InitStyles();

            // Fullscreen dark overlay with gold border
            GUI.color = new Color(0f, 0f, 0f, 0.88f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;

            float boxW = Mathf.Min(780f, Screen.width - 40f);
            float boxH = 340f;
            float boxX = (Screen.width - boxW) * 0.5f;
            float boxY = (Screen.height - boxH) * 0.5f;

            GUI.Box(new Rect(boxX, boxY, boxW, boxH), GUIContent.none, dialogBoxStyle);

            GUI.Label(new Rect(boxX + 20f, boxY + 20f, boxW - 40f, 32f), "★ CASE RESOLVED: LEGACY OF THE TEMPLE RESTORED ★", bannerStyle);

            string endingStory =
                "Through keen observation and relentless pursuit, Detective Aryan has recovered all three sacred heritage treasures:\n\n" +
                "• <b>The Sacred Pradip Brass Vessel</b> — Returned to the temple sanctum altar.\n" +
                "• <b>The Carved Sandstone Rosette</b> — Re-anchored to the vimana relief.\n" +
                "• <b>The Ancient Tala Patra Palm-Leaf Manuscript</b> — Safely preserved in the temple archives.\n\n" +
                "Vikram and Dev have fled empty-handed into the night, their antiquities ring dismantled.\n" +
                "Head Priest Ananda and the pilgrims of Puri offer deep blessings as the evening bells echo across the sanctuary.";

            GUI.Label(new Rect(boxX + 30f, boxY + 65f, boxW - 60f, 190f), endingStory, bodyStyle);

            if (GUI.Button(new Rect(boxX + (boxW - 220f) * 0.5f, boxY + boxH - 55f, 220f, 36f), "Return to Main Menu"))
            {
                isEndingCinematicActive = false;
                SceneManager.LoadScene("MainMenu");
            }
        }

        private void InitStyles()
        {
            if (dialogBoxStyle == null) dialogBoxStyle = new GUIStyle(GUI.skin.box);

            if (bannerStyle == null)
            {
                bannerStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 17,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = new Color(0.96f, 0.82f, 0.28f) }
                };
            }

            if (bodyStyle == null)
            {
                bodyStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 13,
                    wordWrap = true,
                    richText = true,
                    normal = { textColor = Color.white }
                };
            }
        }
    }
}

