using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using LegacyOfTemples.Core;

namespace LegacyOfTemples.Gameplay
{
    /// <summary>
    /// The Dawn Clock: story time from 03:10 to 05:45 (155 story minutes). Runs only during play, each chapter
    /// has a deadline, mistakes cost minutes, and running out in chapters 1-4 means a retry from the checkpoint.
    /// Also owns checkpoints (start of each objective) and decides the ending.
    /// </summary>
    public sealed class DawnClock : MonoBehaviour
    {
        public static DawnClock Instance { get; private set; }
        public const float DawnMinute = 155f;
        private static readonly float[] Deadline = { 35f, 70f, 100f, 125f, 155f };   // V3 chapters: bazaar, storeroom+vaults, stepwell, Sun Wall, river road

        public float Minutes { get; private set; }
        public bool OutOfTime { get; private set; }
        public string FailReason { get; private set; } = "";
        public int Retries { get; private set; }
        public float LastPenalty { get; private set; }
        public static event Action<float, string> OnPenalty;
        public static event Action OnRetry;

        private string cpSnapshot, cpScene; private float cpMinutes; private string cpObjective = "";
        private float toastUntil; private string toast = "";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (Instance != null) return;
            var go = new GameObject("DawnClock"); DontDestroyOnLoad(go);
            Instance = go.AddComponent<DawnClock>();
        }

        private void Awake()
        {
            Minutes = PlayerPrefs.GetFloat("Story_Clock", 0f);
            StoryFlags.ClockPenalty = n => Penalty(n, "a wrong guess");
            StoryDialogueSystem.OnObjectiveChanged += OnObjective;
            StoryDialogueSystem.OnChapterChanged += OnChapter;
            StoryDialogueSystem.OnNewGame += () => { Minutes = 0f; OutOfTime = false; cpObjective = ""; Retries = 0; PlayerPrefs.SetFloat("Story_Clock", 0f); };
        }

        public static string ClockText(float minutes)
        {
            int t = 3 * 60 + 10 + Mathf.FloorToInt(minutes);
            return $"{t / 60:00}:{t % 60:00}";
        }
        public static string DawnInText(float minutes)
        {
            int left = Mathf.Max(0, Mathf.CeilToInt(DawnMinute - minutes));
            return $"Dawn in {left / 60:00}:{left % 60:00}";
        }
        public string DawnIn => DawnInText(Minutes);

        public static int ChapterIndex(StoryChapter c) => c != null ? Mathf.Clamp(c.n - 1, 0, 5) : 5;
        public float ChapterDeadline { get { int i = ChapterIndex(StoryDialogueSystem.Instance != null ? StoryDialogueSystem.Instance.Chapter : null); return i < 5 ? Deadline[i] : DawnMinute; } }
        public float ChapterLeft => ChapterDeadline - Minutes;

        /// <summary>Lose story minutes (scaled by difficulty).</summary>
        public void Penalty(float minutes, string why)
        {
            float m = minutes * GameOptions.PenaltyScale;
            if (m <= 0f) return;
            Minutes += m; LastPenalty = m;
            toast = $"−{m:0.#} min: {why}"; toastUntil = Time.unscaledTime + 3.5f;
            OnPenalty?.Invoke(m, why);
        }

        /// <summary>A section failed (lost the runner, health zero...): show the retry card.</summary>
        public void Fail(string reason) { if (!OutOfTime) { OutOfTime = true; FailReason = reason; } }

        private void OnObjective(StoryObjective o)
        {
            if (o == null || o.id == cpObjective || SceneLoader.IsLoading) return;
            var story = StoryDialogueSystem.Instance; if (story == null) return;
            cpObjective = o.id; cpSnapshot = story.Snapshot(); cpMinutes = Minutes;
            cpScene = SceneManager.GetActiveScene().name;
            var player = FindAnyObjectByType<LegacyOfTemples.Characters.PlayerController>();
            if (player != null && !Constants.IsNewWorld(cpScene))
                Checkpoints.Save(o.id, o.title, player.transform.position, player.transform.eulerAngles.y, Minutes * 60f);
            PlayerPrefs.SetFloat("Story_Clock", Minutes);
        }

        private void OnChapter(string prev, string next)
        {
            var p = StoryData.Chapter(prev); if (p == null) return;
            int i = ChapterIndex(p);
            if (i < 4) Minutes = Mathf.Max(Minutes, Deadline[i] - 5f); // at most 5 saved minutes carry forward
            if (next == "ch6" || next == "end") StoryFlags.Set("late", Minutes > DawnMinute ? 1 : 0);
        }

        /// <summary>dawn_light / late_dawn / lost_dawn</summary>
        public static string EndingId()
        {
            if (StoryFlags.Get("lost_dawn") == 1) return "lost_dawn";
            // V3: on time = before dawn. (The old V2 rule also needed "kishore_turned", a flag only the old jungle fight sets,
            // so every V3 run ended as LATE DAWN even when the player finished early.)
            return StoryFlags.Get("late") == 0 ? "dawn_light" : "late_dawn";
        }

        private static bool IsGameplay(string s) => Constants.IsNewWorld(s) || s == Constants.Courtyard || s == Constants.Sanctum || s == Constants.ShrineCrime || s == Constants.PuzzleWall || s == Constants.JunglePath;

        private float retryToastUntil = -10f;
        private string retryToastMsg = "";

        public void Retry()
        {
            RetryOnSpot(true);
        }

        /// <summary>Retry from the local section checkpoint without warping back to the temple start.</summary>
        public void RetryOnSpot(bool allowSceneFallback = false)
        {
            var story = StoryDialogueSystem.Instance;
            OutOfTime = false; FailReason = ""; Retries++;
            if (story != null && !string.IsNullOrEmpty(cpSnapshot)) story.Restore(cpSnapshot);
            int i = ChapterIndex(story != null ? story.Chapter : null);
            float deadline = i < 5 ? Deadline[i] : DawnMinute;
            bool localCheckpoint = Checkpoints.Has && (string.IsNullOrEmpty(Checkpoints.Current.scene) || Checkpoints.Current.scene == SceneManager.GetActiveScene().name);
            float checkpointMinutes = localCheckpoint ? Checkpoints.Current.clockSeconds / 60f : cpMinutes;
            Minutes = Mathf.Clamp(checkpointMinutes, 0f, Mathf.Max(0f, deadline - 8f));
            Time.timeScale = 1f;
            OnRetry?.Invoke();

            string activeScene = SceneManager.GetActiveScene().name;
            string targetScene = string.IsNullOrEmpty(cpScene) ? activeScene : cpScene;
            if (!allowSceneFallback || targetScene == activeScene || Constants.IsNewWorld(activeScene))
            {
                var pc = FindAnyObjectByType<LegacyOfTemples.Characters.PlayerController>();
                if (pc != null && Checkpoints.Has) Checkpoints.Restore(pc.transform);
                var jp = FindAnyObjectByType<Vehicles.Jeep>();
                if (jp != null && jp.Occupied) jp.RepairAndResetToRoad();
                string label = Checkpoints.Has ? Checkpoints.Current.label : (story?.Current?.title ?? "Current Section");
                retryToastMsg = $"Section reset on the spot: <color={ScreenText.Gold}>{label}</color>";
                retryToastUntil = Time.unscaledTime + 2.8f;
                return;
            }
            SceneLoader.Load(targetScene);
        }

        private void LateUpdate()
        {
            if (Time.timeScale > 0f && Time.unscaledTime < retryToastUntil && !string.IsNullOrEmpty(retryToastMsg))
                ScreenText.Panel("retry_notice", null, retryToastMsg, PanelAnchor.TopCenter, 520f, null, false, 3);
        }

        /// <summary>True during the opening cinematic: the clock neither runs nor shows.</summary>
        public static bool Hold;

        private void Update()
        {
            string scene = SceneManager.GetActiveScene().name;
            if (scene == Constants.TempleRing && Puzzles.PuzzleSave.Get("lore.completed") == 1) return;
            var story = StoryDialogueSystem.Instance;
            if (!IsGameplay(scene) || story == null || (story.IsComplete && !Constants.IsNewWorld(scene)) || SceneLoader.IsLoading || Hold) return;
            var ch = story.Chapter; int ci = ChapterIndex(ch);

            if (OutOfTime)
            {
                string why = string.IsNullOrEmpty(FailReason) ? "The chapter's time ran out." : FailReason;
                ScreenText.Panel("clock_fail", string.IsNullOrEmpty(FailReason) ? "OUT OF TIME" : "TRY AGAIN",
                    $"{why}\n\nYou return to the last checkpoint: <color={ScreenText.Gold}>{story.Current?.title}</color>.",
                    PanelAnchor.Center, 1000f, ScreenText.Key("Enter") + " Retry from checkpoint", true, 90);
                var kb = Keyboard.current; var gp = Gamepad.current;
                if ((kb != null && kb.enterKey.wasPressedThisFrame) || (gp != null && gp.buttonSouth.wasPressedThisFrame) || VirtualInput.Down("confirm")) { FailReason = ""; Retry(); }
                return;
            }

            bool ui = InputGate.UiOpen != null && InputGate.UiOpen();
            if (ci < 5 && Time.timeScale > 0f && !ui)
                Minutes += Time.deltaTime / GameOptions.SecondsPerStoryMinute;

            if (!Constants.IsNewWorld(scene) && ci < 4 && Minutes > Deadline[ci]) { OutOfTime = true; FailReason = ""; return; }

            if (ci < 5 && Time.timeScale > 0f)   // hidden while the game is paused, so it never draws over the pause screen
            {
                float left = Constants.IsNewWorld(scene) ? DawnMinute - Minutes : ChapterLeft;
                string col = left < 5f ? ScreenText.Coral : ScreenText.Ivory;
                string body = $"<color={ScreenText.Gold}><b>{DawnIn}</b></color>   <color={col}>{ClockText(Minutes)}  •  {(left >= 0f ? Mathf.CeilToInt(left) + " min left" : "past first light")}</color>";
                if (Time.unscaledTime < toastUntil) body += $"\n<color={ScreenText.Coral}>{toast}</color>";
                ScreenText.Panel("clock", null, body, PanelAnchor.TopCenter, 480f, null, false, 1);
            }
        }
    }
}
