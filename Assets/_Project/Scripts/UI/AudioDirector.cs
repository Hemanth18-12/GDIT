using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using LegacyOfTemples.Characters;
using LegacyOfTemples.Core;
using LegacyOfTemples.Gameplay;
using LegacyOfTemples.World;

namespace LegacyOfTemples.UI
{
    /// <summary>
    /// Runs the Audio Mixer (Resources/Audio/LotMixer): routes every source to its group, plays the five
    /// loop-synchronised music stems and switches them with mixer snapshots (calm, tension, stealth, combat, resolve),
    /// ducks music and ambience under voices, sets the reverb "zone" per map, drives Aryan's voice chain by
    /// vocal effort (calm / firm / shout), picks the footstep surface and plays the ambience beds.
    /// </summary>
    public sealed class AudioDirector : MonoBehaviour
    {
        public static AudioDirector Instance { get; private set; }
        public bool MixerLoaded => mixer != null;
        public string Snapshot { get; private set; } = "";
        public int StemsPlaying { get { int n = 0; foreach (var s in stems) if (s != null && s.isPlaying) n++; return n; } }
        public string Tier { get; private set; } = "calm";
        public float DuckDb { get; private set; }

        private AudioMixer mixer; private readonly AudioSource[] stems = new AudioSource[5];
        private static readonly string[] StemNames = { "Calm", "Tension", "Stealth", "Combat", "Resolve" };
        private AudioSource wind, jungle, flame, heart; private PlayerController player; private float surfaceT, stateT, tierUntil; private bool weatherRouted;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (Instance != null) return;
            var go = new GameObject("AudioDirector"); DontDestroyOnLoad(go);
            Instance = go.AddComponent<AudioDirector>();
        }

        private AudioMixerGroup Group(string name) { var g = mixer != null ? mixer.FindMatchingGroups(name) : null; return g != null && g.Length > 0 ? g[0] : null; }

        private AudioSource Loop(string clip, string group)
        {
            var a = gameObject.AddComponent<AudioSource>(); a.clip = Resources.Load<AudioClip>("Audio/" + clip); a.loop = true; a.playOnAwake = false; a.volume = 0f; a.spatialBlend = 0f;
            a.outputAudioMixerGroup = Group(group); return a;
        }

        private void Awake()
        {
            mixer = Resources.Load<AudioMixer>("Audio/LotMixer");
            if (mixer == null) { Debug.LogWarning("[AudioDirector] LotMixer not found; audio plays unmixed."); return; }
            AudioManager.Router = (src, name) => { var g = Group(name); if (g != null) src.outputAudioMixerGroup = g; };
            for (int i = 0; i < 5; i++) stems[i] = Loop("mus_" + StemNames[i].ToLowerInvariant(), "Stem" + StemNames[i]);
            double t0 = AudioSettings.dspTime + 0.3;
            foreach (var s in stems) if (s.clip != null) s.PlayScheduled(t0); // same start sample: the stems stay in sync
            wind = Loop("amb_wind", "Ambience"); jungle = Loop("amb_jungle_night", "Ambience"); flame = Loop("amb_flame_loop", "Ambience"); heart = Loop("amb_heartbeat", "UI");
            VoicePlayer.OnVoice += OnVoice;
            DawnClock.OnPenalty += (m, why) => AudioManager.Instance?.PlaySFX("sfx_penalty");
            StoryDialogueSystem.OnObjectiveChanged += OnObjective;
        }

        private string lastObjective = "";
        private void OnObjective(StoryObjective o) { if (o != null && o.id != lastObjective) { lastObjective = o.id; AudioManager.Instance?.PlaySFX("sfx_objective", 0.7f); } }

        private void OnVoice(string speaker, string tier)
        {
            Tier = tier; tierUntil = Time.unscaledTime + 8f;
            if (VoicePlayer.Instance != null) VoicePlayer.Instance.Source.outputAudioMixerGroup = Group(speaker == "aryan" ? "VoiceAryan" : "VoiceNPC");
        }

        private static float Db(float linear) => linear <= 0.0001f ? -80f : Mathf.Log10(linear) * 20f;
        private static void Fade(AudioSource a, float target)
        {
            if (a == null || a.clip == null) return;
            a.volume = Mathf.MoveTowards(a.volume, target, Time.unscaledDeltaTime * 0.6f);
            if (a.volume > 0.004f && !a.isPlaying) a.Play(); else if (a.volume <= 0.004f && a.isPlaying) a.Stop();
        }
        private static bool Gameplay(string s) => s == Constants.Courtyard || s == Constants.Sanctum || s == Constants.ShrineCrime || s == Constants.PuzzleWall || s == Constants.JunglePath;

        private string PickState(string scene)
        {
            var story = StoryDialogueSystem.Instance; var fight = FightDirector.Instance; var ch = ChapterDirector.Instance; var clock = DawnClock.Instance;
            if (story != null && (story.IsComplete || (story.Chapter != null && story.Chapter.id == "ch6"))) return "Resolve";
            if (CombatState.InCombat || (ch != null && ch.ChaseRunning)) return "Combat";
            if (fight != null && fight.StealthRunning) return "Stealth";
            var wall = ChakraPuzzleController.Active;
            bool tense = (clock != null && clock.ChapterLeft < 5f) || (wall != null && wall.ResetLeft > 0f && wall.ResetLeft < 30f) || (ch != null && ch.ActiveTrap != null) || (clock != null && clock.OutOfTime);
            return tense ? "Tension" : "Calm";
        }

        private void Update()
        {
            if (mixer == null) return;
            var am = AudioManager.Instance; if (am == null) return;
            if (!weatherRouted) { weatherRouted = true; am.RouteOwnSources(); if (WeatherManager.Instance != null) foreach (var a in WeatherManager.Instance.GetComponents<AudioSource>()) a.outputAudioMixerGroup = Group("Ambience"); }
            string scene = SceneManager.GetActiveScene().name; bool game = Gameplay(scene);

            // music: stems always run together; snapshots choose which are heard
            float mv = game ? am.MasterVolume * am.MusicVolume * 0.8f : 0f;
            foreach (var s in stems) if (s != null) s.volume = Mathf.MoveTowards(s.volume, mv, Time.unscaledDeltaTime);
            if (game && am.IsBGMPlaying) am.StopBGM();
            stateT -= Time.unscaledDeltaTime;
            if (game && stateT <= 0f)
            {
                stateT = 0.25f;
                string st = PickState(scene);
                if (st != Snapshot)
                {
                    var snap = mixer.FindSnapshot(st);
                    if (snap != null) snap.TransitionTo(st == "Combat" ? 0.5f : st == "Stealth" ? 1f : st == "Tension" ? 1.5f : 3f);
                    Snapshot = st;
                }
            }

            // ducking: music and ambience step back while a voice line plays
            bool speaking = VoicePlayer.Instance != null && VoicePlayer.Instance.Source != null && VoicePlayer.Instance.Source.isPlaying;
            DuckDb = Mathf.MoveTowards(DuckDb, speaking ? -6f : 0f, Time.unscaledDeltaTime * (speaking ? 40f : 10f));
            mixer.SetFloat("MusicVol", DuckDb);
            mixer.SetFloat("AmbienceVol", Db(am.MasterVolume * am.SFXVolume) + DuckDb * 0.66f);
            mixer.SetFloat("VoiceVol", Db(am.MasterVolume));

            // reverb "zones": one setting per map (stone hall, open court, shrine forecourt, jungle, ruin courtyard)
            float room, decay;
            switch (scene)
            {
                case "Sanctum": case "PuzzleWall": room = -900f; decay = 2.4f; break;
                case "ShrineCrime": room = -1600f; decay = 1.4f; break;
                case "JunglePath": room = CombatState.InCombat ? -1800f : -3500f; decay = CombatState.InCombat ? 1.2f : 0.5f; break;
                case "Courtyard": room = -2600f; decay = 0.9f; break;
                default: room = -4000f; decay = 0.6f; break;
            }
            mixer.SetFloat("SfxRoom", room); mixer.SetFloat("SfxDecay", decay);

            // Aryan's chain by vocal effort: calm baritone, firm, projected shout (more presence, harder compression, more hall)
            if (Time.unscaledTime > tierUntil) Tier = "calm";
            float low = Tier == "shout" ? 1.41f : Tier == "firm" ? 1.33f : 1.26f, pres = Tier == "shout" ? 1.58f : Tier == "firm" ? 1.41f : 1.26f;
            mixer.SetFloat("AryanLowGain", low); mixer.SetFloat("AryanPresenceGain", pres);
            mixer.SetFloat("AryanCompThreshold", Tier == "shout" ? -16f : Tier == "firm" ? -18f : -20f);
            mixer.SetFloat("AryanMakeUp", Tier == "shout" ? 4f : 2f);
            mixer.SetFloat("VoiceRoom", room + (Tier == "shout" ? 700f : 0f)); mixer.SetFloat("VoiceDecay", decay);

            // ambience beds
            var w = WeatherManager.Instance; float sv = am.MasterVolume * am.SFXVolume;
            Fade(wind, game && scene != "Sanctum" && scene != "PuzzleWall" ? (0.12f + (w != null ? w.Wind * 0.3f : 0f)) : 0f);
            Fade(jungle, scene == "JunglePath" ? 0.4f * (1f - (w != null ? w.Intensity * 0.6f : 0f)) : 0f);
            Fade(flame, scene == "Sanctum" || scene == "PuzzleWall" || CombatState.InCombat ? 0.3f : 0f);
            var f = FightDirector.Instance;
            bool danger = f != null && ((CombatState.InCombat && f.Health < 30f) || (f.StealthRunning && f.Detection > 0.5f));
            Fade(heart, danger ? 0.7f * sv : 0f);

            // footstep surface under the player
            surfaceT -= Time.unscaledDeltaTime;
            if (game && surfaceT <= 0f)
            {
                surfaceT = 0.25f;
                if (player == null) player = FindAnyObjectByType<PlayerController>();
                if (player != null && Physics.Raycast(player.transform.position + Vector3.up * 0.5f, Vector3.down, out RaycastHit h, 2f, ~0, QueryTriggerInteraction.Ignore))
                {
                    string n = (h.collider.name + " " + (h.collider.sharedMaterial != null ? h.collider.sharedMaterial.name : "")).ToLowerInvariant();
                    bool raining = w != null && !w.Interior && w.Intensity > 0.3f;
                    string surf = n.Contains("wood") || n.Contains("plank") || n.Contains("bridge") ? "wood" : n.Contains("gravel") || n.Contains("pebble") ? "gravel" :
                                  n.Contains("grass") || n.Contains("leaf") ? "grass" : n.Contains("mud") || n.Contains("dirt") || n.Contains("terrain") || (scene == "JunglePath" && n.Contains("ground")) ? "mud" : "stone";
                    if (raining && surf == "stone") surf = "wet";
                    if (raining && surf == "grass") surf = "mud";
                    AudioManager.Surface = surf;
                }
            }
        }
    }
}
