using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using LegacyOfTemples.Core;
using LegacyOfTemples.Characters;
using LegacyOfTemples.Gameplay;

namespace LegacyOfTemples.UI
{
    /// <summary>
    /// The one owner of every spoken line (story barks, dialogue lines and short event lines).
    /// - One voice at a time: a higher priority line interrupts a lower one (with a short fade); a lower or equal line waits
    ///   in a queue (priority 3 and up) until it expires, or is dropped (chatter, priority 1-2).
    /// - Each trigger has a cooldown, chatter lines keep a gap between them, the same line is not picked twice in a row,
    ///   and once-only lines are remembered in the save. The Voice Frequency setting scales chance, cooldown and gap.
    /// - Aryan, the narrator and story lines play in 2D; NPC event lines play in 3D at the speaker's head.
    /// - Subtitles (setting), lip sync (FaceAnimator), a talking gesture and a head turn toward the player.
    /// - Pauses with the pause menu, stops on scene change, cutscene, conversation and cutscene skip.
    /// Lines come from Docs/VOICE_LINES.csv through the VoiceDatabase asset. F10 shows the debug overlay (development builds / demo mode).
    /// </summary>
    [DefaultExecutionOrder(900)]
    public sealed class VoiceManager : MonoBehaviour
    {
        public static VoiceManager Instance { get; private set; }
        /// <summary>Tests: ignore world conditions and chance (cooldown, once-only and priority still apply).</summary>
        public static bool TestMode;

        public sealed class Item
        {
            public string id, speaker, text, trigger, context; public AudioClip clip; public int priority; public bool story, subtitle, spatial, once;
            public Transform at; public float expires, cooldown, volume = 1f; public string tier = "calm";
        }
        public struct Record { public float time; public string trigger, result, line, reason; }

        public AudioSource Source2D { get; private set; }
        public AudioSource Source3D { get; private set; }
        public Item Current { get; private set; }
        public static bool AnyPlaying => Instance != null && Instance.Current != null;
        public static readonly List<Record> Log = new List<Record>();
        public static int Played { get; private set; }

        readonly Dictionary<string, List<VoiceLine>> byTrigger = new Dictionary<string, List<VoiceLine>>();
        readonly Dictionary<string, float> triggerReady = new Dictionary<string, float>();
        readonly Dictionary<string, string> lastLine = new Dictionary<string, string>();
        /// <summary>When each line last played (unscaled seconds): chatter and reactions are not repeated within RepeatGap.</summary>
        readonly Dictionary<string, float> linePlayedAt = new Dictionary<string, float>();
        /// <summary>A priority 1-2 line is not said again within this many seconds (45 s on Normal), even when it is the trigger's only line.</summary>
        public static float RepeatGap => 45f * CooldownMul;
        static float clock; static int clockFrame = -1;
        /// <summary>The voice timers' clock (cooldowns, gaps, line ends, the log): real time, not slowed by slow motion, but while the game records
        /// a video (Time.captureDeltaTime) it steps by the recording's fixed frame time, so a recording keeps the real pacing of the voices.</summary>
        public static float Now { get { Tick(); return clock; } }
        static float Dt => Time.captureDeltaTime > 0f ? Time.captureDeltaTime : Time.unscaledDeltaTime;
        static void Tick() { if (Time.frameCount != clockFrame) { clockFrame = Time.frameCount; clock += Dt; } }
        readonly HashSet<string> once = new HashSet<string>();
        readonly List<Item> queue = new List<Item>();
        // lowGapUntil: no chatter (priority 1) before this; reactGapUntil: no reaction (priority 2) before this (a third of the gap, set only by reactions)
        float currentEnd, lowGapUntil, reactGapUntil, fade = -1f; Item next; bool paused, overlay; GameObject speakerGo; Animator talkAnim; Transform headBone; float headW;
        const string OnceKey = "lot_voice_once";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (Instance != null) return;
            var go = new GameObject("VoiceManager"); DontDestroyOnLoad(go); Instance = go.AddComponent<VoiceManager>();
        }

        void Awake()
        {
            PujariLoreDirector.PlayVoice = (id, speaker, at) => {
                var line = VoiceLibrary.Line(id);
                if (line != null) PlayStory(id, speaker, line.text, line.clip, "calm", true, 1f, at);
            };
            PujariLoreDirector.StopVoices = () => StopAll("heritage dialogue");
            PujariLoreDirector.DialogueIsOpen = () => UIManager.Instance != null && UIManager.Instance.IsDialogueActive;
            Source2D = gameObject.AddComponent<AudioSource>(); Source2D.playOnAwake = false; Source2D.spatialBlend = 0f; Source2D.priority = 0;
            var e = new GameObject("VoiceEmitter3D"); e.transform.SetParent(transform, false);
            Source3D = e.AddComponent<AudioSource>(); Source3D.playOnAwake = false; Source3D.spatialBlend = 0.85f; Source3D.priority = 0;
            Source3D.rolloffMode = AudioRolloffMode.Logarithmic; Source3D.minDistance = 3f; Source3D.maxDistance = 35f; Source3D.dopplerLevel = 0f;
            var db = VoiceLibrary.Db;
            if (db != null) foreach (var l in db.lines) { if (l == null || l.trigger == "story") continue; if (!byTrigger.TryGetValue(l.trigger, out var list)) byTrigger[l.trigger] = list = new List<VoiceLine>(); list.Add(l); }
            foreach (var id in PlayerPrefs.GetString(OnceKey, "").Split(',')) if (id.Length > 0) once.Add(id);
            VoiceEvents.OnFire += (t, at, ctx) => Fire(t, at, ctx);
            StoryDialogueSystem.OnNewGame += () => { once.Clear(); PlayerPrefs.DeleteKey(OnceKey); triggerReady.Clear(); lastLine.Clear(); linePlayedAt.Clear(); };
            SceneManager.activeSceneChanged += (a, b) => StopAll("scene change");
            EventBus.OnGameState += st => { if (st == GameState.Ending) Fire("ending"); };
            gameObject.AddComponent<VoiceWatchers>();
            Debug.Log($"[Voice] ready: {(db != null ? db.lines.Count : 0)} lines, {byTrigger.Count} event triggers, {once.Count} once-only lines already used");
        }

        // ------------------------------------------------------------------ frequency setting
        static int Freq => GameOptions.VoiceFrequency;
        static float ChanceMul(int pri) => pri >= 3 ? 1f : Freq == 0 ? 0.5f : Freq == 2 ? 1.4f : 1f;
        static float CooldownMul => Freq == 0 ? 1.6f : Freq == 2 ? 0.7f : 1f;
        static float ChatterGap => Freq == 0 ? 30f : Freq == 2 ? 6f : 12f;

        // ------------------------------------------------------------------ public API
        /// <summary>An event happened. Picks a fitting line (or none) and plays or queues it. Returns the line id, or null.</summary>
        public static string Fire(string trigger, Transform at = null, string context = null)
        {
            var vm = Instance; if (vm == null) return null;
            return vm.DoFire(trigger, at, context);
        }

        /// <summary>A story bark (priority 4) or dialogue line (priority 5) from the story system: plays now on the 2D source.
        /// Subtitles are drawn by the story UI itself.</summary>
        public static void PlayStory(string id, string speaker, string text, AudioClip clip, string tier, bool dialogue, float volumeScale, Transform at = null)
        {
            var vm = Instance; if (vm == null || clip == null) return;
            var it = new Item { id = id, speaker = speaker, text = text, trigger = dialogue ? "dialogue" : "bark", clip = clip, priority = dialogue ? 5 : 4, story = true, subtitle = false, tier = tier ?? "calm", at = at };
            it.volume = Mathf.Clamp(volumeScale, 0.5f, 1f);
            vm.Record_("story:" + (dialogue ? "dialogue" : "bark"), "played", id, "story line");
            vm.StartNow(it, true);
        }

        public static void StopStory() { var vm = Instance; if (vm != null && vm.Current != null && vm.Current.story) vm.Stop("dialogue ended"); }

        public static void StopAll(string why)
        {
            var vm = Instance; if (vm == null) return;
            if (vm.Current != null) vm.Record_(vm.Current.trigger, "stopped", vm.Current.id, why);
            foreach (var q in vm.queue) vm.Record_(q.trigger, "dropped", q.id, why);
            vm.queue.Clear(); vm.next = null; vm.Stop(why);
        }

        /// <summary>Tests: forget cooldowns, once-only flags, the chatter gap and the queue.</summary>
        public static void ResetForTests()
        {
            var vm = Instance; if (vm == null) return;
            StopAll("test reset"); vm.triggerReady.Clear(); vm.once.Clear(); vm.lowGapUntil = vm.reactGapUntil = 0f; vm.lastLine.Clear(); vm.linePlayedAt.Clear(); PlayerPrefs.DeleteKey(OnceKey);
        }
        /// <summary>Tests: just the cooldowns and the chatter gap (keeps once-only and last-line memory).</summary>
        public static void ResetCooldownsForTests() { var vm = Instance; if (vm == null) return; vm.triggerReady.Clear(); vm.lowGapUntil = vm.reactGapUntil = 0f; vm.linePlayedAt.Clear(); }
        /// <summary>Ends the trigger cooldowns and the chatter gap, but remembers which lines were just said (tests the repeat gap).</summary>
        public static void EndCooldownsForTests() { var vm = Instance; if (vm == null) return; vm.triggerReady.Clear(); vm.lowGapUntil = vm.reactGapUntil = 0f; }
        /// <summary>The chance a line really has with the current Voice Frequency setting.</summary>
        public static float EffectiveChance(VoiceLine l) => Mathf.Clamp01(l.chance * ChanceMul(l.priority));
        /// <summary>The trigger cooldown in seconds with the current Voice Frequency setting.</summary>
        public static float EffectiveCooldown(VoiceLine l) => l.cooldown * CooldownMul;
        public static float EffectiveChatterGap => ChatterGap;
        public int QueueCount => queue.Count;

        /// <summary>Plain text of the decision log (QA).</summary>
        public static string LogText()
        {
            var sb = new StringBuilder("time_s\ttrigger\tresult\tline\treason\n");
            foreach (var r in Log) sb.Append($"{r.time:0.0}\t{r.trigger}\t{r.result}\t{r.line}\t{r.reason}\n");
            return sb.ToString();
        }

        // ------------------------------------------------------------------ choosing a line
        string DoFire(string trigger, Transform at, string context)
        {
            if (!byTrigger.TryGetValue(trigger, out var lines)) { Record_(trigger, "no lines", "", "trigger not in the database"); return null; }
            float now = Now;
            string block = Blocked(trigger);
            if (block != null) { Record_(trigger, "blocked", "", block); return null; }
            if (triggerReady.TryGetValue(trigger, out var ready) && now < ready) { Record_(trigger, "cooldown", "", $"{ready - now:0}s left"); return null; }
            var ctx = Parse(context);
            var pool = new List<VoiceLine>(); string why = "no line fits";
            foreach (var l in lines)
            {
                if (l.clip == null) { why = "clip missing"; continue; }
                if (l.once && once.Contains(l.id)) { why = "once-only used"; continue; }
                if (!TestMode && l.priority <= 2 && linePlayedAt.TryGetValue(l.id, out float said) && now - said < RepeatGap) { why = $"said {now - said:0}s ago"; continue; }
                if (!TestMode && !ConditionsMet(l, ctx, out string cw)) { why = cw; continue; }
                pool.Add(l);
            }
            if (pool.Count == 0) { Record_(trigger, "no candidate", "", why); return null; }
            if (pool.Count > 1 && lastLine.TryGetValue(trigger, out var last)) pool.RemoveAll(l => l.id == last && pool.Count > 1);   // never the same line twice in a row
            var line = pool[UnityEngine.Random.Range(0, pool.Count)];
            if (!TestMode && UnityEngine.Random.value > Mathf.Clamp01(line.chance * ChanceMul(line.priority))) { Record_(trigger, "chance", line.id, $"p={line.chance * ChanceMul(line.priority):0.00}"); return null; }
            float gapEnd = line.priority == 1 ? lowGapUntil : line.priority == 2 ? reactGapUntil : 0f;   // chatter waits the full gap after any small line; a reaction only a third of it after another reaction
            if (now < gapEnd) { Record_(trigger, "chatter gap", line.id, $"{gapEnd - now:0}s"); return null; }
            var it = new Item
            {
                id = line.id, speaker = line.speaker, text = line.text, trigger = trigger, context = context, clip = line.clip, priority = line.priority,
                subtitle = line.subtitle, once = line.once, cooldown = line.cooldown, at = at, spatial = line.speaker != "aryan" && line.speaker != "narrator" && at != null
            };
            if (it.speaker != "aryan" && it.speaker != "narrator" && it.at == null) { var go = FindSpeaker(it.speaker); if (go != null) { it.at = go.transform; it.spatial = true; } }
            if (Current == null && next == null) { StartNow(it, false); return it.id; }
            int busyPri = Current != null ? Current.priority : next.priority;
            if (it.priority > busyPri) { Record_(trigger, "interrupts", it.id, $"over {(Current ?? next).id} (p{busyPri})"); StartNow(it, false); return it.id; }
            if (it.priority >= 3) { it.expires = now + (it.priority >= 4 ? 12f : 6f); queue.Add(it); Record_(trigger, "queued", it.id, $"behind p{busyPri}"); return it.id; }
            Record_(trigger, "busy", it.id, $"p{it.priority} behind p{busyPri}"); return null;
        }

        string Blocked(string trigger)
        {
            if (trigger == "ending") return null;
            if (IntroCinematic.Playing || InputGate.CinematicActive) return "cutscene";
            if (UIManager.Instance != null && UIManager.Instance.IsDialogueActive) return "conversation";
            if (UIManager.Instance != null && UIManager.Instance.IsPauseOpen) return "paused";
            return null;
        }

        static Dictionary<string, string> Parse(string context)
        {
            var d = new Dictionary<string, string>();
            if (string.IsNullOrEmpty(context)) return d;
            foreach (var kv in context.Split(';')) { int i = kv.IndexOf('='); if (i > 0) d[kv.Substring(0, i).Trim()] = kv.Substring(i + 1).Trim(); }
            return d;
        }

        public static string CurrentZone => ZoneAtmosphere.Instance != null && ZoneAtmosphere.Instance.Current != null ? ZoneAtmosphere.Instance.Current.zone.ToString() : "";
        public static bool InJeep => Gameplay.Vehicles.Jeep.Instance != null && Gameplay.Vehicles.Jeep.Instance.Occupied;
        /// <summary>World rain (off everywhere since the player asked) or the stepwell's own rain over the tank.</summary>
        public static bool Raining => (World.WeatherManager.Instance != null && World.WeatherManager.Instance.Intensity > 0.3f) || CurrentZone == "Stepwell";

        static bool ConditionsMet(VoiceLine l, Dictionary<string, string> ctx, out string why)
        {
            why = "";
            if (string.IsNullOrEmpty(l.conditions)) return true;
            var story = StoryDialogueSystem.Instance; string obj = story != null && story.Current != null ? story.Current.id : "";
            foreach (var raw in l.conditions.Split(';'))
            {
                string c = raw.Trim(); if (c.Length == 0) continue;
                bool ok; int eq = c.IndexOf('=');
                if (eq < 0)
                {
                    switch (c)
                    {
                        case "jeep": ok = InJeep; break;
                        case "nojeep": ok = !InJeep; break;
                        case "fight": ok = Brawl.Active != null; break;
                        case "nofight": ok = Brawl.Active == null; break;
                        case "rain": ok = Raining; break;
                        default: ok = true; break;
                    }
                }
                else
                {
                    string k = c.Substring(0, eq), v = c.Substring(eq + 1);
                    switch (k)
                    {
                        case "zone": ok = string.Equals(CurrentZone, v, StringComparison.OrdinalIgnoreCase); break;
                        case "objective": ok = obj == v; break;
                        case "notobjective": ok = obj != v; break;
                        default: ok = ctx.TryGetValue(k, out var have) && string.Equals(have, v, StringComparison.OrdinalIgnoreCase); break;
                    }
                }
                if (!ok) { why = "condition " + c; return false; }
            }
            return true;
        }

        // ------------------------------------------------------------------ playing
        void StartNow(Item it, bool hardCut)
        {
            if (Current != null)
            {
                if (hardCut || Current.story) { Record_(Current.trigger, "cut", Current.id, "replaced by " + it.id); Stop("replaced"); }
                else { next = it; fade = 0.07f; return; }   // short fade-out, then the new line
            }
            Begin(it);
        }

        void Begin(Item it)
        {
            next = null; fade = -1f;
            var src = it.spatial && it.at != null ? Source3D : Source2D;
            if (src == Source3D) Source3D.transform.position = HeadPos(it.at);
            AudioManager.Route(src, it.speaker == "aryan" ? "VoiceAryan" : "VoiceNPC");
            src.clip = it.clip; src.volume = VoicePlayer.Volume * it.volume; src.pitch = 1f; src.Play();
            Current = it; currentEnd = Now + it.clip.length + 0.05f; Played++;
            if (!it.story)
            {
                triggerReady[it.trigger] = Now + it.cooldown * CooldownMul;
                lastLine[it.trigger] = it.id; linePlayedAt[it.id] = Now;
                if (it.priority <= 2) lowGapUntil = Now + it.clip.length + ChatterGap;
                if (it.priority == 2) reactGapUntil = Now + it.clip.length + ChatterGap / 3f;
                if (it.once && once.Add(it.id)) { PlayerPrefs.SetString(OnceKey, string.Join(",", once)); PlayerPrefs.Save(); }
                Record_(it.trigger, "played", it.id, $"p{it.priority} {(src == Source3D ? "3D" : "2D")} {it.context}");
            }
            // face, gesture and head
            speakerGo = it.at != null ? it.at.gameObject : FindSpeaker(it.speaker);
            if (speakerGo != null)
            {
                var face = FaceAnimator.For(it.speaker, speakerGo); if (face != null) face.Speak(src, it.clip.length + 0.3f);
                talkAnim = speakerGo.GetComponentInChildren<Animator>(); headBone = null; headW = 0f;
                if (talkAnim != null) { foreach (var p in talkAnim.parameters) if (p.name == "IsTalking" && p.type == AnimatorControllerParameterType.Bool) talkAnim.SetBool("IsTalking", true); if (talkAnim.isHuman && it.speaker != "aryan") headBone = talkAnim.GetBoneTransform(HumanBodyBones.Head); }
            }
        }

        void Stop(string why)
        {
            if (Current == null) return;
            var src = Current.spatial ? Source3D : Source2D; src.Stop();
            if (talkAnim != null) { foreach (var p in talkAnim.parameters) if (p.name == "IsTalking" && p.type == AnimatorControllerParameterType.Bool) talkAnim.SetBool("IsTalking", false); }
            if (speakerGo != null) { var f = speakerGo.GetComponent<FaceAnimator>(); if (f != null) f.StopSpeaking(); }
            Current = null; speakerGo = null; talkAnim = null; headBone = null; fade = -1f;
        }

        void Update()
        {
            Tick(); float dt = Dt;
            // pause menu: hold the voice where it is
            bool pause = UIManager.Instance != null && UIManager.Instance.IsPauseOpen;
            if (pause != paused)
            {
                paused = pause;
                if (Current != null) { var src = Current.spatial ? Source3D : Source2D; if (paused) src.Pause(); else src.UnPause(); }
            }
            if (paused) { currentEnd += dt; return; }
            // a conversation or cutscene takes over: event lines stop
            if (Current != null && !Current.story && Current.trigger != "ending" && Blocked(Current.trigger) != null) { Record_(Current.trigger, "stopped", Current.id, Blocked(Current.trigger)); Stop("blocked"); }
            if (fade >= 0f && Current != null)
            {
                var src = Current.spatial ? Source3D : Source2D; fade -= dt; src.volume = Mathf.Max(0f, src.volume - dt / 0.07f);
                if (fade <= 0f) { var n = next; Record_(Current.trigger, "cut", Current.id, "faded for " + (n != null ? n.id : "")); Stop("faded"); if (n != null) Begin(n); }
            }
            if (Current != null && Current.spatial && Current.at != null) Source3D.transform.position = HeadPos(Current.at);
            if (Current != null && Now >= currentEnd && !(Current.spatial ? Source3D : Source2D).isPlaying) Stop("finished");
            // queue: drop what expired, then play the most important waiting line
            if (queue.Count > 0)
            {
                for (int i = queue.Count - 1; i >= 0; i--) if (Now > queue[i].expires) { Record_(queue[i].trigger, "expired", queue[i].id, "waited too long"); queue.RemoveAt(i); }
                bool barkShowing = StoryDialogueSystem.Instance != null && StoryDialogueSystem.Instance.BarkShowing;
                if (Current == null && next == null && queue.Count > 0 && !barkShowing && Blocked(queue[0].trigger) == null)
                {
                    queue.Sort((a, b) => b.priority != a.priority ? b.priority.CompareTo(a.priority) : a.expires.CompareTo(b.expires));
                    var it = queue[0]; queue.RemoveAt(0); Record_(it.trigger, "dequeued", it.id, "its turn"); Begin(it);
                }
            }
            // subtitles (story lines draw their own)
            if (Current != null && !Current.story && Current.subtitle && GameOptions.Subtitles)
            {
                string name = SpeakerName(Current.speaker, out string col);
                ScreenText.Panel("voice", null, name.Length > 0 ? $"<color={col}><b>{name}</b></color>  {Current.text}" : $"<i>{Current.text}</i>", PanelAnchor.BottomCenter, 1000f, null, false, 3);
            }
            // debug overlay
            var kb = Keyboard.current;
            if (kb != null && kb.f10Key.wasPressedThisFrame && (Debug.isDebugBuild || GameOptions.DemoMode)) overlay = !overlay;
            if (overlay) DrawOverlay();
        }

        void LateUpdate()
        {
            // the speaking NPC turns its head toward the player (yaw only, up to 45 degrees), after the animation has posed it
            if (Current == null || headBone == null || speakerGo == null) { headW = 0f; return; }
            var pc = Player != null ? Player.transform : null; if (pc == null) return;
            Vector3 to = pc.position - speakerGo.transform.position; to.y = 0f; if (to.sqrMagnitude < 0.01f || to.magnitude > 15f) return;
            float yaw = Mathf.Clamp(Vector3.SignedAngle(Vector3.ProjectOnPlane(speakerGo.transform.forward, Vector3.up), to, Vector3.up), -45f, 45f);
            headW = Mathf.MoveTowards(headW, 0.8f, Time.deltaTime * 3f);
            headBone.rotation = Quaternion.AngleAxis(yaw * headW, Vector3.up) * headBone.rotation;
        }

        static PlayerController player;
        /// <summary>The player (cached; found again after a scene change).</summary>
        public static PlayerController Player { get { if (player == null) player = FindAnyObjectByType<PlayerController>(); return player; } }

        static Vector3 HeadPos(Transform t)
        {
            if (t == null) return Vector3.zero;
            var a = t.GetComponentInChildren<Animator>(); var h = a != null && a.isHuman ? a.GetBoneTransform(HumanBodyBones.Head) : null;
            return h != null ? h.position : t.position + Vector3.up * 1.6f;
        }

        /// <summary>The character a speaker id belongs to (for lip sync, gesture and 3D position).</summary>
        public static GameObject FindSpeaker(string s)
        {
            if (string.IsNullOrEmpty(s)) return null;
            if (s == "aryan") return Player != null ? Player.gameObject : null;
            string npc = s == "pujari" ? "npc_priest" : s == "meera" ? "npc_villager" : null;
            if (npc != null) { foreach (var n in FindObjectsByType<NPCDialogueTrigger>()) if (n.Id == npc) return n.gameObject; return null; }
            if (s == "madhav") { var m = MadhavCameos.Instance != null ? MadhavCameos.Instance.Madhav : null; if (m != null && m.gameObject.activeInHierarchy) return m.gameObject; var g = GameObject.Find("Madhav"); return g; }
            if (s == "courier" && BazaarTail.Instance != null && BazaarTail.Instance.Courier != null) return BazaarTail.Instance.Courier.gameObject;
            if (Brawl.Active != null)
                foreach (var e in Brawl.Active.Enemies) if (e != null && e.Alive && e.name.IndexOf(s, StringComparison.OrdinalIgnoreCase) >= 0) return e.gameObject;
            if (s == "bhaskar" && JungleChase.Instance != null && JungleChase.Instance.Runner != null && JungleChase.Instance.Runner.gameObject.activeInHierarchy) return JungleChase.Instance.Runner.gameObject;
            return null;
        }

        static string SpeakerName(string s, out string color)
        {
            var sp = StoryData.Speaker(s); color = sp != null ? sp.color : ScreenText.Gold;
            if (sp != null) return sp.name;
            switch (s)
            {
                case "robber": color = "#FFB4A8"; return "Robber";
                case "villager_m": case "villager_f": color = "#C8D2DC"; return "Villager";
                case "narrator": return "";
                default: return s;
            }
        }

        void Record_(string trigger, string result, string line, string reason)
        {
            Log.Add(new Record { time = Now, trigger = trigger, result = result, line = line, reason = reason });
            if (Log.Count > 4000) Log.RemoveRange(0, 1000);
            if (result == "played" || result == "interrupts" || result == "queued" || result == "no lines" || result == "stopped" || result == "expired")
                Debug.Log($"[Voice] {trigger}: {result} {line} ({reason})");
        }

        void DrawOverlay()
        {
            var sb = new StringBuilder();
            sb.Append($"Now: {(Current != null ? $"{Current.id} (p{Current.priority}, {Current.trigger})" : "-")}   queue {queue.Count}   freq {GameOptions.VoiceFrequencyNames[Freq]}   zone {CurrentZone}\n");
            for (int i = Mathf.Max(0, Log.Count - 10); i < Log.Count; i++) { var r = Log[i]; sb.Append($"{r.time:0.0}  {r.trigger}  <b>{r.result}</b>  {r.line}  <color=#9AA4AE>{r.reason}</color>\n"); }
            ScreenText.Panel("voice_debug", "VOICE DEBUG (F10)", sb.ToString(), PanelAnchor.TopRight, 760f, null, false, 80);
        }
    }
}
