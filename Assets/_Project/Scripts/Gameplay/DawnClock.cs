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
        private static readonly float[] Deadline = { 25f, 55f, 80f, 115f, 155f };

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
            return StoryFlags.Get("late") == 0 && StoryFlags.Get("kishore_turned") == 1 ? "dawn_light" : "late_dawn";
        }

        private static bool IsGameplay(string s) => s == Constants.Courtyard || s == Constants.Sanctum || s == Constants.ShrineCrime || s == Constants.PuzzleWall || s == Constants.JunglePath;

        public void Retry()
        {
            var story = StoryDialogueSystem.Instance;
            OutOfTime = false; Retries++;
            if (story != null && cpSnapshot != null) story.Restore(cpSnapshot);
            int i = ChapterIndex(story != null ? story.Chapter : null);
            float deadline = i < 5 ? Deadline[i] : DawnMinute;
            Minutes = Mathf.Min(cpMinutes, deadline - 8f); // the budget is restored: at least 8 story minutes to work with
            Time.timeScale = 1f;
            OnRetry?.Invoke();
            SceneLoader.Load(string.IsNullOrEmpty(cpScene) ? SceneManager.GetActiveScene().name : cpScene);
        }

        private void Update()
        {
            string scene = SceneManager.GetActiveScene().name;
            var story = StoryDialogueSystem.Instance;
            if (!IsGameplay(scene) || story == null || story.IsComplete || SceneLoader.IsLoading) return;
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

            if (ci < 4 && Minutes > Deadline[ci]) { OutOfTime = true; FailReason = ""; return; }

            if (ci < 5)
            {
                float left = ChapterLeft;
                string col = left < 5f ? ScreenText.Coral : ScreenText.Ivory;
                string body = $"<color={ScreenText.Gold}><b>{DawnIn}</b></color>   <color={col}>{ClockText(Minutes)}  •  {(left >= 0f ? Mathf.CeilToInt(left) + " min left" : "past first light")}</color>";
                if (Time.unscaledTime < toastUntil) body += $"\n<color={ScreenText.Coral}>{toast}</color>";
                ScreenText.Panel("clock", null, body, PanelAnchor.TopCenter, 680f, null, false, 1);
            }
        }
    }
}
