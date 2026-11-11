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
        private readonly HashSet<string> codexUnlocked = new HashSet<string>();
        private readonly HashSet<string> minigames = new HashSet<string>();
        private readonly Queue<StoryLine> barkQueue = new Queue<StoryLine>();
        private StoryLine barkNow; private float barkUntil;
        private bool codexOpen; private float codexToast; private string codexToastTitle;

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
        public static event Action OnNewGame;
        /// <summary>A one-off spoken line outside a conversation (subtitle + voice).</summary>
        public static event Action<StoryLine> OnBark;
        public StoryChapter Chapter => StoryData.Chapter(Current != null ? Current.chapter : "ch6");
        public bool IsCodexOpen => codexOpen;
        public bool IsCodexUnlocked(string id) => codexUnlocked.Contains(id);
        public bool ConversationDone(string id) => conversationsDone.Contains(id);
        /// <summary>A gameplay section announces that it can complete this minigame objective (otherwise a skip prompt is shown, so the story can never dead-end).</summary>
        public void RegisterMinigame(string id) { minigames.Add(id); }

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
            UpdateBarks();
            UpdateCodex(IsGameplayScene(scene));
            if (!IsGameplayScene(scene) || IsComplete) return;
            MinigameFallback(scene);
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
            foreach (string k in new[] { "Story_Objective", "Story_Clues", "Story_Convs", "Story_Time", "Story_Hints", "Story_Choices", "Story_Flags", "Story_Codex",
                "LevelUnlocked_2", "LevelUnlocked_3", "Case_Completed", "ChakraPuzzle_Solved", "LastPlayedScene" })
                PlayerPrefs.DeleteKey(k);
            PlayerPrefs.Save();
            StoryFlags.Clear();
            if (Instance != null) Instance.Load();
            OnNewGame?.Invoke();
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
            StoryFlags.Deserialize(PlayerPrefs.GetString("Story_Flags", ""));
            codexUnlocked.Clear();
            foreach (var c in PlayerPrefs.GetString("Story_Codex", "").Split(',')) if (c.Length > 0) codexUnlocked.Add(c);
            barkQueue.Clear(); barkNow = null;
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
            PlayerPrefs.SetString("Story_Flags", StoryFlags.Serialize());
            PlayerPrefs.SetString("Story_Codex", string.Join(",", codexUnlocked));
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
            activeConversation = null; activeTrigger = null; talking = null; // a conversation never survives a scene change
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

        private void OnClue(string id) { bool isNew = clues.Add(id); if (isNew) Say("clue:" + id); CheckAutoComplete(); Publish(); Save(); }
        private void OnPuzzle(string id)
        {
            var cur = Current;
            if (cur != null && cur.complete != null && cur.complete.type == "puzzle" && cur.complete.id == id) Advance(true);
            PlayConversation(StoryData.Conversation("conv_c4_open"), null);
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
            var prev = Current;
            objectiveIndex++;
            while (Current != null && !StoryFlags.Eval(Current.when)) objectiveIndex++; // skip objectives whose condition is false
            idleTime = 0f;
            Save();
            var cur = Current;
            if (prev != null) Say("done:" + prev.id);
            if (cur != null) Say("start:" + cur.id);
            if (prev != null && (cur == null || cur.chapter != prev.chapter)) OnChapterChanged?.Invoke(prev.chapter, cur != null ? cur.chapter : "end");
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
            SceneLoader.Load(Constants.Courtyard);
            returning = false;
        }

        /// <summary>(finished chapter id, next chapter id or "end")</summary>
        public static event Action<string, string> OnChapterChanged;

        /// <summary>Test / bot helper: complete the current objective without playing it.</summary>
        public void DebugCompleteCurrent()
        {
            var cur = Current; if (cur == null) return;
            if (cur.complete.type == "clues" && cur.complete.ids != null) foreach (var id in cur.complete.ids) clues.Add(id);
            if (cur.complete.type == "dialogue") conversationsDone.Add(cur.complete.id);
            if (cur.complete.type == "minigame") conversationsDone.Add("mg:" + cur.complete.id);
            if (cur.complete.type == "puzzle") { PlayerPrefs.SetInt("ChakraPuzzle_Solved", 1); }
            if (cur.complete.type == "scene") { Advance(true); return; }
            CheckAutoComplete(); Publish(); Save();
        }

        /// <summary>Restore progress to a checkpoint snapshot (see CheckpointSystem).</summary>
        public string Snapshot() => objectiveIndex + "|" + string.Join(",", clues) + "|" + string.Join(",", conversationsDone) + "|" + StoryFlags.Serialize();
        public void Restore(string snap)
        {
            var p = snap.Split('|'); if (p.Length < 4) return;
            objectiveIndex = int.Parse(p[0]);
            clues.Clear(); foreach (var c in p[1].Split(',')) if (c.Length > 0) clues.Add(c);
            conversationsDone.Clear(); foreach (var c in p[2].Split(',')) if (c.Length > 0) conversationsDone.Add(c);
            StoryFlags.Deserialize(p[3]);
            Save(); Publish();
        }

        // ------------------------------------------------------------ barks (one-off lines)
        public void Say(string trigger)
        {
            var l = StoryData.Bark(trigger);
            if (l == null || !StoryFlags.Eval(l.when)) return;
            if (barkQueue.Count < 4) barkQueue.Enqueue(l);
        }

        private void UpdateBarks()
        {
            bool talking = activeConversation != null;
            if (barkNow == null && barkQueue.Count > 0 && !talking)
            {
                barkNow = barkQueue.Dequeue();
                barkUntil = Time.unscaledTime + Mathf.Clamp(barkNow.t.Length * 0.065f, 1.8f, 6f);
                OnBark?.Invoke(barkNow);
            }
            if (barkNow == null) return;
            if (Time.unscaledTime > barkUntil || talking) { barkNow = null; return; }
            var sp = StoryData.Speaker(barkNow.s);
            ScreenText.Panel("bark", null, $"<color={(sp != null ? sp.color : ScreenText.Gold)}><b>{(sp != null ? sp.name : barkNow.s)}</b></color>  {barkNow.t}", PanelAnchor.BottomCenter, 1100f, null, false, 3);
        }

        // ------------------------------------------------------------ codex (B)
        private void UnlockCodex(string trigger)
        {
            foreach (var c in StoryData.File.codex ?? new StoryCodex[0])
                if (c.unlock == trigger && codexUnlocked.Add(c.id)) { codexToast = Time.unscaledTime + 5f; codexToastTitle = c.title; }
        }

        private void UpdateCodex(bool gameplay)
        {
            if (!gameplay) { codexOpen = false; return; }
            var kb = UnityEngine.InputSystem.Keyboard.current;
            bool busy = activeConversation != null;
            if (kb != null && kb.bKey.wasPressedThisFrame && !busy && !(Current != null && Current.complete.id == "deduction" && Current.scene == SceneManager.GetActiveScene().name)) codexOpen = !codexOpen;
            if (codexOpen && kb != null && kb.escapeKey.wasPressedThisFrame) { EventBus.EscapeConsumedFrame = Time.frameCount; codexOpen = false; }
            if (busy) codexOpen = false;
            if (codexOpen)
            {
                var sb = new System.Text.StringBuilder();
                foreach (var c in StoryData.File.codex ?? new StoryCodex[0])
                    sb.Append(codexUnlocked.Contains(c.id) ? $"<color={ScreenText.Gold}><b>{c.title}</b></color>\n{c.text}\n\n" : $"<color={ScreenText.Grey}>??? (not yet discovered)</color>\n\n");
                sb.Append($"<color={ScreenText.Cyan}>Trust (Meera): {StoryFlags.Get("trust_meera")}</color>");
                ScreenText.Panel("codex", "CODEX", sb.ToString(), PanelAnchor.Center, 1300f, ScreenText.Key("B") + " / " + ScreenText.Key("Esc") + " Close", true, 55);
            }
            else if (Time.unscaledTime < codexToast && !busy)
                ScreenText.Panel("codex_toast", null, $"<color={ScreenText.Gold}>Codex updated:</color> {codexToastTitle}   {ScreenText.Key("B")}", PanelAnchor.TopRight, 620f, null, false, 6);
        }

        // ------------------------------------------------------------ minigame safety net
        private void MinigameFallback(string scene)
        {
            var cur = Current;
            if (cur == null || cur.complete.type != "minigame" || cur.scene != scene || minigames.Contains(cur.complete.id) || activeConversation != null) return;
            if (Time.timeSinceLevelLoad < 3f) return;
            ScreenText.Panel("mg_fallback", null, $"This section ({cur.title}) is not available in this build. Press {ScreenText.Key("Enter")} to continue the story.", PanelAnchor.TopRight, 640f, null, false, 4);
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null && kb.enterKey.wasPressedThisFrame) CompleteMinigame(cur.complete.id);
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
            else if (t.StartsWith("obj:"))
            {
                if (markerCache == null || markerCache.name != t.Substring(4)) markerCache = GameObject.Find(t.Substring(4));
                return markerCache != null ? markerCache.transform : null;
            }
            return null;
        }

        private GameObject markerCache;

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
                var c = StoryData.Conversation(string.IsNullOrEmpty(cur.start) ? cur.complete.id : cur.start);
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
            UnlockCodex(conv.id);
            Debug.Log($"[Story] Conversation start: '{conv.id}' with {(trigger != null ? trigger.CharacterId : "none")}");
            var lineList = new List<string>(FormatLines(conv.lines));
            var choices = new List<StoryChoice>();
            foreach (var c in conv.choices ?? new StoryChoice[0]) if (StoryFlags.Eval(c.when)) choices.Add(c);
            string[] choiceTexts = null;
            if (choices.Count > 0)
            {
                if (!string.IsNullOrEmpty(conv.prompt)) lineList.Add("Aryan: (" + conv.prompt + ")");
                choiceTexts = new string[choices.Count];
                for (int i = 0; i < choiceTexts.Length; i++) choiceTexts[i] = choices[i].text;
            }
            string[] lines = lineList.ToArray();
            EventBus.RaiseDialogueStart(trigger != null ? trigger.CharacterId : "story");
            string firstSpeaker = conv.lines.Length > 0 ? (StoryData.Speaker(conv.lines[0].s)?.name ?? "") : "";
            EventBus.RaiseRequestDialogue(firstSpeaker, lines, choiceTexts, choiceTexts == null ? null : (Action<int>)(i =>
            {
                choicesMade++;
                var ch = choices[i];
                StoryFlags.Apply(ch.effects);
                LastChoiceId = ch.id;
                Debug.Log($"[Story] Choice {ch.id} -> trust_meera={StoryFlags.Get("trust_meera")}");
                var follow = ch.lines;
                if (follow != null && follow.Length > 0)
                    EventBus.RaiseRequestDialogue(StoryData.Speaker(follow[0].s)?.name ?? "", FormatLines(follow), null, null, () => FinishConversation(conv, ch.next));
                else FinishConversation(conv, ch.next);
            }), () => { if (choiceTexts == null) FinishConversation(conv, null); });
        }

        public string LastChoiceId { get; private set; } = "";

        private static string[] FormatLines(StoryLine[] src)
        {
            var list = new List<string>();
            foreach (var l in src ?? new StoryLine[0]) if (StoryFlags.Eval(l.when)) list.Add(StoryData.Format(l));
            return list.ToArray();
        }

        private void FinishConversation(StoryConversation conv, string next)
        {
            if (activeConversation != conv.id && conv.id != "") return; // only finish once
            Debug.Log($"[Story] Conversation finished: '{conv.id}'");
            if (!string.IsNullOrEmpty(conv.id)) conversationsDone.Add(conv.id);
            var nextConv = StoryData.Conversation(next);
            if (nextConv != null) { Save(); PlayConversation(nextConv, activeTrigger); return; } // chained: keep camera and trigger state
            activeConversation = null;
            if (orbitCam != null) orbitCam.ClearCutsceneOverride();
            SetTalking(null);
            var trig = activeTrigger; activeTrigger = null;
            if (trig != null) trig.EndDialogue(); else EventBus.RaiseDialogueEnd("story");
            if (!string.IsNullOrEmpty(conv.id))
            {
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
