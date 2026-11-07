using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using LegacyOfTemples.Core;
using LegacyOfTemples.Characters;

namespace LegacyOfTemples.Gameplay
{
    /// <summary>
    /// Data-driven story director. Reads Resources/Story/story.json (StoryData), runs conversations
    /// (speaker per line, camera framing on the speaker, NPC talk animation, choices), tracks objectives
    /// (id, instruction, target, 3 hints), saves progress, and keeps score and time for the ending.
    /// </summary>
    public class StoryDialogueSystem : MonoBehaviour
    {
        public static StoryDialogueSystem Instance { get; private set; }

        // ---- progress (saved) ----
        private int objectiveIndex;
        private readonly HashSet<string> clues = new HashSet<string>();
        private readonly HashSet<string> conversationsDone = new HashSet<string>();
        private float elapsed;
        private int hintsUsed;
        private int choicesMade;

        // ---- runtime ----
        private OrbitCamera orbitCam;
        private NPCDialogueTrigger activeTrigger;
        private string activeConversation;
        private float idleTime;
        private bool returning;
        private float focusTimer;

        public StoryObjective Current => objectiveIndex >= 0 && objectiveIndex < StoryData.Objectives.Length ? StoryData.Objectives[objectiveIndex] : null;
        public int ObjectiveIndex => objectiveIndex;
        public int ObjectiveCount => StoryData.Objectives.Length;
        public float Elapsed => elapsed;
        public float IdleTime => idleTime;
        public int HintsUsed => hintsUsed;
        public bool IsComplete => objectiveIndex >= StoryData.Objectives.Length;
        public bool HasClue(string id) => clues.Contains(id);
        public int ClueCount => clues.Count;
        public int Score => Mathf.Max(0, clues.Count * 100 + objectiveIndex * 50 + choicesMade * 20 + Mathf.Max(0, 600 - (int)(elapsed / 60f) * 20) - hintsUsed * 25);

        public static event Action<StoryObjective> OnObjectiveChanged;
        public static event Action OnStoryCompleted;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (Instance != null) return;
            var go = new GameObject("StoryDirector");
            go.AddComponent<StoryDialogueSystem>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
            if (transform.parent != null) transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
            Load();
            NPCDialogueTrigger.StoryHandler = HandleTrigger;
            NPCDialogueTrigger.StoryCancel = CancelConversation;
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            EventBus.OnClueFound += OnClue;
            EventBus.OnPuzzleSolved += OnPuzzle;
            EventBus.OnDialogueLine += OnLineShown;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            EventBus.OnClueFound -= OnClue;
            EventBus.OnPuzzleSolved -= OnPuzzle;
            EventBus.OnDialogueLine -= OnLineShown;
        }

        private void Start() { OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single); }

        private static bool IsGameplayScene(string s) =>
            s == Constants.Courtyard || s == Constants.Sanctum || s == Constants.ShrineCrime || s == Constants.PuzzleWall || s == Constants.JunglePath;

        private void Update()
        {
            string scene = SceneManager.GetActiveScene().name;
            if (!IsGameplayScene(scene) || IsComplete) return;
            if (Time.timeScale > 0f) { elapsed += Time.unscaledDeltaTime; idleTime += Time.unscaledDeltaTime; }
            focusTimer -= Time.unscaledDeltaTime;
            if (focusTimer <= 0f)
            {
                focusTimer = 0.4f;
                var pc = FindAnyObjectByType<PlayerController>();
                PlayerController.ObjectiveFocus = pc != null ? ResolveTarget(pc.transform.position) : null;
            }
        }

        // ------------------------------------------------------------ save / load
        public static void ResetForNewGame()
        {
            foreach (string k in new[] { "Story_Objective", "Story_Clues", "Story_Convs", "Story_Time", "Story_Hints", "Story_Choices",
                "LevelUnlocked_2", "LevelUnlocked_3", "Case_Completed", "ChakraPuzzle_Solved", "LastPlayedScene" })
                PlayerPrefs.DeleteKey(k);
            PlayerPrefs.Save();
            if (Instance != null) Instance.Load();
        }

        private void Load()
        {
            objectiveIndex = PlayerPrefs.GetInt("Story_Objective", 0);
            clues.Clear(); conversationsDone.Clear();
            foreach (var c in PlayerPrefs.GetString("Story_Clues", "").Split(',')) if (c.Length > 0) clues.Add(c);
            foreach (var c in PlayerPrefs.GetString("Story_Convs", "").Split(',')) if (c.Length > 0) conversationsDone.Add(c);
            elapsed = PlayerPrefs.GetFloat("Story_Time", 0f);
            hintsUsed = PlayerPrefs.GetInt("Story_Hints", 0);
            choicesMade = PlayerPrefs.GetInt("Story_Choices", 0);
            returning = false;
        }

        private void Save()
        {
            PlayerPrefs.SetInt("Story_Objective", objectiveIndex);
            PlayerPrefs.SetString("Story_Clues", string.Join(",", clues));
            PlayerPrefs.SetString("Story_Convs", string.Join(",", conversationsDone));
            PlayerPrefs.SetFloat("Story_Time", elapsed);
            PlayerPrefs.SetInt("Story_Hints", hintsUsed);
            PlayerPrefs.SetInt("Story_Choices", choicesMade);
            string scene = SceneManager.GetActiveScene().name;
            if (IsGameplayScene(scene)) PlayerPrefs.SetString("LastPlayedScene", scene);
            PlayerPrefs.Save();
        }

        /// <summary>Called by the forensic kit / stealth section when the player succeeds.</summary>
        public void CompleteMinigame(string id)
        {
            conversationsDone.Add("mg:" + id);
            CheckAutoComplete();
            Publish();
            Save();
        }

        public void RegisterHintUsed() { hintsUsed++; Save(); }

        // ------------------------------------------------------------ objectives
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            orbitCam = FindAnyObjectByType<OrbitCamera>();
            if (!IsGameplayScene(scene.name)) return;
            var cur = Current;
            while (cur != null && cur.complete != null && cur.complete.type == "scene" && cur.complete.id == scene.name) { Advance(false); cur = Current; }
            CheckAutoComplete();
            Publish();
            Save();
            StartCoroutine(RepublishSoon());
        }

        private IEnumerator RepublishSoon()
        {
            // HUD objects in the new scene may subscribe after we published; publish again once they exist.
            yield return null;
            yield return new WaitForSecondsRealtime(0.5f);
            Publish();
            yield return new WaitForSecondsRealtime(1.5f);
            Publish();
        }

        private void OnClue(string id) { clues.Add(id); CheckAutoComplete(); Publish(); Save(); }
        private void OnPuzzle(string id)
        {
            var cur = Current;
            if (cur != null && cur.complete != null && cur.complete.type == "puzzle" && cur.complete.id == id) Advance(true);
            PlayConversation(StoryData.Conversation("conv_wall_open"), null);
        }

        private void CheckAutoComplete()
        {
            var cur = Current;
            while (cur != null && cur.complete != null)
            {
                bool done = false;
                if (cur.complete.type == "clues" && cur.complete.ids != null)
                {
                    done = true;
                    foreach (var id in cur.complete.ids) if (!clues.Contains(id)) { done = false; break; }
                }
                else if (cur.complete.type == "dialogue") done = conversationsDone.Contains(cur.complete.id);
                else if (cur.complete.type == "minigame") done = conversationsDone.Contains("mg:" + cur.complete.id);
                else if (cur.complete.type == "puzzle") done = PlayerPrefs.GetInt("ChakraPuzzle_Solved", 0) == 1 && cur.complete.id == "chakra_wall";
                if (!done) break;
                Advance(false);
                cur = Current;
            }
        }

        private void Advance(bool publish)
        {
            objectiveIndex++;
            idleTime = 0f;
            Save();
            var cur = Current;
            if (cur?.complete != null && cur.complete.type == "scene")
            {
                if (cur.complete.id == Constants.ShrineCrime) PlayerPrefs.SetInt("LevelUnlocked_2", 1);
                if (cur.complete.id == Constants.JunglePath && PlayerPrefs.GetInt("ChakraPuzzle_Solved", 0) == 1) PlayerPrefs.SetInt("LevelUnlocked_3", 1);
                PlayerPrefs.Save();
            }
            if (cur != null && cur.id == "obj_return" && SceneManager.GetActiveScene().name != Constants.Courtyard && !returning)
                StartCoroutine(ReturnAtSunrise());
            if (publish) Publish();
            if (IsComplete) { OnStoryCompleted?.Invoke(); if (GameLevelManager.Instance != null) GameLevelManager.Instance.CompleteLevel3(); }
        }

        private IEnumerator ReturnAtSunrise()
        {
            returning = true;
            yield return new WaitForSecondsRealtime(4f);
            while (activeTrigger != null) yield return null;
            PlayerPrefs.SetInt("OpeningCinematicActive", 0);
            SceneManager.LoadScene(Constants.Courtyard);
            returning = false;
        }

        public void Publish()
        {
            var cur = Current;
            if (cur == null) { EventBus.RaiseUpdateObjective("Case closed", "The three pieces are home.", null); OnObjectiveChanged?.Invoke(null); return; }
            EventBus.RaiseUpdateObjective(cur.title, cur.instruction, Counter(cur));
            OnObjectiveChanged?.Invoke(cur);
        }

        public string Counter(StoryObjective o)
        {
            if (o?.complete?.ids == null || o.complete.type != "clues") return $"Step {objectiveIndex + 1} of {ObjectiveCount}";
            int n = 0; foreach (var id in o.complete.ids) if (clues.Contains(id)) n++;
            return $"Found {n} of {o.complete.ids.Length}  •  Step {objectiveIndex + 1} of {ObjectiveCount}";
        }

        public List<string> MissingClues()
        {
            var list = new List<string>();
            var cur = Current;
            if (cur?.complete?.ids == null) return list;
            var titles = new Dictionary<string, string>();
            foreach (var c in FindObjectsByType<ClueInspectable>()) titles[c.Id] = c.ClueTitle;
            foreach (var id in cur.complete.ids) if (!clues.Contains(id)) list.Add(titles.TryGetValue(id, out var t) ? t : id);
            return list;
        }

        /// <summary>World position of the current objective's target in this scene (null if not here).</summary>
        public Transform ResolveTarget(Vector3 from)
        {
            var cur = Current;
            if (cur == null || string.IsNullOrEmpty(cur.target)) return null;
            if (cur.scene != SceneManager.GetActiveScene().name && cur.complete.type != "scene") return null;
            string t = cur.target;
            if (t.StartsWith("npc:"))
            {
                string id = "npc_" + t.Substring(4);
                foreach (var n in FindObjectsByType<NPCDialogueTrigger>()) if (n.Id == id) return n.transform;
            }
            else if (t.StartsWith("exit:"))
            {
                string id = "portal_to_" + t.Substring(5);
                foreach (var p in FindObjectsByType<SceneTransitionTrigger>()) if (p.Id == id) return p.transform;
            }
            else if (t == "clues" && cur.complete.ids != null)
            {
                Transform best = null; float bd = float.MaxValue;
                var wanted = new HashSet<string>(cur.complete.ids);
                foreach (var c in FindObjectsByType<ClueInspectable>())
                {
                    if (!wanted.Contains(c.Id) || clues.Contains(c.Id)) continue;
                    float d = (c.transform.position - from).sqrMagnitude;
                    if (d < bd) { bd = d; best = c.transform; }
                }
                return best;
            }
            else if (t == "puzzle" && ChakraPuzzleController.Active != null) return ChakraPuzzleController.Active.transform;
            return null;
        }

        // ------------------------------------------------------------ conversations
        private bool HandleTrigger(NPCDialogueTrigger trigger)
        {
            string scene = SceneManager.GetActiveScene().name;
            string npc = trigger.CharacterId;
            if (npc == Constants.Robber2) npc = Constants.Robber1;
            var cur = Current;

            StoryConversation conv = null;
            if (cur != null && cur.complete != null && cur.complete.type == "dialogue")
            {
                var c = StoryData.Conversation(cur.complete.id);
                if (c != null && c.scene == scene && c.npc == npc) conv = c;
            }
            if (conv == null)
            {
                // Not this NPC's turn: give a short, useful reminder instead of repeating old lines.
                var sp = SpeakerForNpc(npc);
                string text = cur != null ? (cur.hints != null && cur.hints.Length > 0 ? cur.hints[0] + " " : "") + "(" + cur.title + ")" : "Thank you for everything, Aryan.";
                conv = new StoryConversation { id = "", scene = scene, npc = npc, lines = new[] { new StoryLine { s = sp, t = text } } };
            }
            PlayConversation(conv, trigger);
            return true;
        }

        /// <summary>Player walked away mid-conversation: close the dialogue box and reset state (no progress).</summary>
        private void CancelConversation(NPCDialogueTrigger trig)
        {
            if (activeTrigger != trig) { trig.EndDialogue(); return; }
            Debug.Log("[Story] Conversation cancelled (walked away): " + activeConversation);
            activeConversation = null; activeTrigger = null;
            if (orbitCam != null) orbitCam.ClearCutsceneOverride();
            SetTalking(null);
            EventBus.RaiseRequestDialogueEnd();
            trig.EndDialogue();
        }

        private static string SpeakerForNpc(string npc)
        {
            foreach (var s in StoryData.File.speakers) if (s.npc == npc) return s.id;
            return "aryan";
        }

        public void PlayConversation(StoryConversation conv, NPCDialogueTrigger trigger)
        {
            if (conv == null) return;
            activeTrigger = trigger;
            activeConversation = conv.id;
            Debug.Log($"[Story] Conversation start: '{conv.id}' with {(trigger != null ? trigger.CharacterId : "none")}");
            string[] lines = FormatLines(conv.lines);
            string[] choiceTexts = null;
            if (conv.choices != null && conv.choices.Length > 0)
            {
                choiceTexts = new string[conv.choices.Length];
                for (int i = 0; i < choiceTexts.Length; i++) choiceTexts[i] = conv.choices[i].text;
            }
            EventBus.RaiseDialogueStart(trigger != null ? trigger.CharacterId : "story");
            string firstSpeaker = conv.lines.Length > 0 ? (StoryData.Speaker(conv.lines[0].s)?.name ?? "") : "";
            EventBus.RaiseRequestDialogue(firstSpeaker, lines, choiceTexts, choiceTexts == null ? null : (Action<int>)(i =>
            {
                choicesMade++;
                var follow = conv.choices[i].lines;
                if (follow != null && follow.Length > 0)
                    EventBus.RaiseRequestDialogue(StoryData.Speaker(follow[0].s)?.name ?? "", FormatLines(follow), null, null, () => FinishConversation(conv));
                else FinishConversation(conv);
            }), () => FinishConversation(conv));
        }

        private static string[] FormatLines(StoryLine[] src)
        {
            var arr = new string[src.Length];
            for (int i = 0; i < src.Length; i++) arr[i] = StoryData.Format(src[i]);
            return arr;
        }

        private void FinishConversation(StoryConversation conv)
        {
            // Choices: the first callback (onComplete) can fire before the choice callback; only finish once.
            if (activeConversation != conv.id && conv.id != "") return;
            activeConversation = null;
            Debug.Log($"[Story] Conversation finished: '{conv.id}'");
            if (orbitCam != null) orbitCam.ClearCutsceneOverride();
            SetTalking(null);
            var trig = activeTrigger; activeTrigger = null;
            if (trig != null) trig.EndDialogue(); else EventBus.RaiseDialogueEnd("story");
            if (!string.IsNullOrEmpty(conv.id))
            {
                conversationsDone.Add(conv.id);
                var cur = Current;
                if (cur != null && cur.complete != null && cur.complete.type == "dialogue" && cur.complete.id == conv.id) Advance(true);
                Save();
            }
        }

        /// <summary>Raised by UIManager for each displayed line: frame the camera on the speaker and animate them.</summary>
        private void OnLineShown(string speakerName)
        {
            Transform who = null;
            foreach (var n in FindObjectsByType<NPCDialogueTrigger>())
                if (!string.IsNullOrEmpty(speakerName) && (n.SpeakerName.StartsWith(speakerName) || speakerName.StartsWith(n.SpeakerName.Split(' ')[0]) || NameMatches(n, speakerName))) { who = n.transform; break; }
            if (who == null && speakerName == "Aryan")
            {
                var p = FindAnyObjectByType<PlayerController>();
                if (p != null) who = p.transform;
            }
            SetTalking(who);
            FrameSpeaker(who);
        }

        private static bool NameMatches(NPCDialogueTrigger n, string speakerName)
        {
            foreach (var s in StoryData.File.speakers)
                if (s.name == speakerName && ("npc_" + s.npc) == n.Id) return true;
            return false;
        }

        private Transform talking;
        private void SetTalking(Transform who)
        {
            if (talking != null) { var a = talking.GetComponentInChildren<Animator>(); if (a != null && HasParam(a, "IsTalking")) a.SetBool("IsTalking", false); }
            talking = who;
            if (who != null) { var a = who.GetComponentInChildren<Animator>(); if (a != null && HasParam(a, "IsTalking")) a.SetBool("IsTalking", true); }
        }

        private static bool HasParam(Animator a, string p)
        {
            foreach (var x in a.parameters) if (x.name == p) return true;
            return false;
        }

        private void FrameSpeaker(Transform who)
        {
            if (orbitCam == null) orbitCam = FindAnyObjectByType<OrbitCamera>();
            if (orbitCam == null || who == null) return;
            // Over-the-shoulder two-shot: camera beside the listener, looking at the speaker's face.
            Vector3 head = who.position + Vector3.up * 1.6f;
            Vector3 fwd = who.forward; fwd.y = 0; fwd.Normalize();
            Vector3 right = Vector3.Cross(Vector3.up, fwd);
            Vector3 camPos = head + fwd * 2.4f + right * 0.7f + Vector3.up * 0.1f;
            orbitCam.SetCutsceneOverride(camPos, Quaternion.LookRotation(head - camPos));
        }
    }
}
