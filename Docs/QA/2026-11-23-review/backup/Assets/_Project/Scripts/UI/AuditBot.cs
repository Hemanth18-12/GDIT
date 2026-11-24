using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using LegacyOfTemples.Core;
using LegacyOfTemples.Characters;
using LegacyOfTemples.Gameplay;

namespace LegacyOfTemples.UI
{
    /// <summary>
    /// Read-only runtime audit. Activated only with the command line flag "-auditBot <outputDir>".
    /// Measures animation (states, params, foot plant, hip drift, T-pose), exercises gameplay keys,
    /// and audits every visible TextMeshPro element (size at 1080p, contrast, overlap, overflow).
    /// Writes audit.md + audit.json-like logs and screenshots into the output directory, then quits.
    /// </summary>
    public sealed class AuditBot : MonoBehaviour
    {
        private string output;
        private static bool requested;
        private readonly StringBuilder report = new StringBuilder();
        private readonly HashSet<Key> held = new HashSet<Key>();

        public static bool IsRunning => requested;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        public static void Bootstrap()
        {
            if (requested) return;
            string[] args = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(args, "-auditBot");
            if (index < 0 || index + 1 >= args.Length) return;
            requested = true;
            Application.runInBackground = true;
            if (Array.IndexOf(args, "-walkthrough") >= 0) savedScores = PlayerPrefs.GetString("lot_v3_scores", "");   // given back at the end: the bot's run must not replace the player's best runs
            foreach (var arg in args) if (arg.StartsWith("-v3Zone") || arg == "-v3Story" || arg == "-walkthrough") { LegacyOfTemples.Gameplay.ZoneKit.NewGame(); LegacyOfTemples.Gameplay.ScoreSystem.ResetAll(); break; }   // zone checks start from a clean save
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus; // the bot must work when the window is not in front
            var go = new GameObject("AuditBot");
            DontDestroyOnLoad(go);
            go.AddComponent<AuditBot>().output = args[index + 1];
        }

        private void Line(string s) { report.AppendLine(s); Debug.Log("[AuditBot] " + s); }
        private static string savedScores;
        private static bool Walkthrough => Array.IndexOf(Environment.GetCommandLineArgs(), "-walkthrough") >= 0;

        private IEnumerator Start()
        {
            Directory.CreateDirectory(output);
            Line("# Runtime audit (" + DateTime.Now.ToString("yyyy-MM-dd HH:mm") + ", " + (Application.isEditor ? "Editor" : "Standalone") + ", " + Screen.width + "x" + Screen.height + ")");
            yield return new WaitForSecondsRealtime(1.5f);
            if (StartupIntroVideo.Playing)
            {   // the intro video at start-up: a few frames as evidence, then let it finish
                Line("## Intro video at start-up: playing");
                foreach (float at in new[] { 3f, 10f, 18f, 27f })
                {
                    float w0 = Time.unscaledTime; while (StartupIntroVideo.Playing && Time.unscaledTime - w0 < at - (at > 3f ? 0f : 1.5f)) yield return null;
                    if (!StartupIntroVideo.Playing) break; yield return Shot($"s0-intro-video-{at:00}s.png");
                }
                float wv = 0f; while (StartupIntroVideo.Playing && wv < 50f) { wv += Time.unscaledDeltaTime; yield return null; }
                Line($"  video finished, menu shown: {!StartupIntroVideo.Playing}");
                yield return new WaitForSecondsRealtime(1.5f);
            }

            // ---------- Main menu UI ----------
            Line("\n## UI: MainMenu");
            yield return Shot("menu.png");
            if (Walkthrough)
            {   // the Controls screen of the main menu, then back
                foreach (var b in FindObjectsByType<Button>()) if (b.name == "Btn_CONTROLS") { b.onClick.Invoke(); break; }
                yield return new WaitForSecondsRealtime(0.8f); yield return Shot("w-menu-controls.png");
                foreach (var b in FindObjectsByType<Button>()) if (b.name == "Btn_BACK") { b.onClick.Invoke(); break; }
                yield return new WaitForSecondsRealtime(0.6f);
            }
            else yield return UiAuditAllResolutions("MainMenu");

            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-skipPlaythrough") < 0)
                yield return PlayThrough();

            string[] scenes = { Constants.Courtyard, Constants.Sanctum, Constants.ShrineCrime, Constants.PuzzleWall, Constants.JunglePath };
            foreach (string scene in scenes)
            {
                yield return SceneManager.LoadSceneAsync(scene);
                yield return new WaitForSecondsRealtime(1f);
                foreach (var c in FindObjectsByType<OpeningCinematic>()) c.EndCinematic();
                if (TutorialManager.Instance != null) TutorialManager.Instance.SkipTutorial();
                yield return new WaitForSecondsRealtime(1f);
                if (UIManager.Instance != null) UIManager.Instance.DismissHowToPlay();
                yield return Tap(Key.Enter);
                yield return new WaitForSecondsRealtime(1.5f);
                Line("\n## Scene: " + scene);
                Census();
                yield return Shot(scene + "-overview.png");
                yield return GroundJumpTest(scene);
                yield return AnimationAudit(scene);
                if (scene == Constants.Courtyard) yield return FunctionAudit();
                Line("\n### UI: " + scene + " HUD");
                yield return UiAuditAllResolutions(scene + " HUD");
            }

            File.WriteAllText(Path.Combine(output, "audit.md"), report.ToString());
            Debug.Log("[AuditBot] DONE " + output);
            yield return new WaitForSecondsRealtime(0.5f);
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }

        // ------------------------------------------------------------------ full play-through
        /// <summary>Only what changed in this round: intro voices, jeep, village and temple colours, and the final fight with the real robbers.</summary>
        private IEnumerator FocusCheck()
        {
            Line($"  intro voice (last line played): {IntroCinematic.LastVoice}");
            yield return JeepCheck();
            yield return VillageShots();
            var story = StoryDialogueSystem.Instance;
            int guard = 0; while (story != null && story.Current != null && story.Current.id != "obj_c5_stealth" && guard++ < 40) { story.DebugCompleteCurrent(); yield return null; }
            Line($"\n## Fight check: objective now {story?.Current?.id}");
            SceneLoader.Load(Constants.JunglePath); yield return WaitLoaded(); yield return new WaitForSecondsRealtime(3f); yield return SkipIntro();
            var pc = FindAnyObjectByType<PlayerController>();
            foreach (var r in new[] { "Robber1", "Robber2" })
            {
                var go = GameObject.Find(r); var an = go != null ? go.GetComponentInChildren<Animator>() : null;
                Line($"  {r}: present={go != null}, model={(an != null ? an.gameObject.name : "-")}, humanoid={(an != null && an.isHuman)}, controller={(an != null && an.runtimeAnimatorController != null ? an.runtimeAnimatorController.name : "-")}");
            }
            yield return PlayMinigame("stealth", story, pc); yield return new WaitForSecondsRealtime(1f);
            if (story.Current != null && story.Current.id == "obj_c5_stealth") { story.DebugCompleteCurrent(); yield return new WaitForSecondsRealtime(1f); }
            yield return PlayMinigame("fight", story, pc);
            File.WriteAllText(Path.Combine(output, "audit.md"), report.ToString());
            Debug.Log("[AuditBot] DONE " + output); yield return new WaitForSecondsRealtime(1f); Application.Quit();
        }

        /// <summary>Stage 7 evidence: screenshots through the whole opening (or the skip path with -introSkip).</summary>
        private IEnumerator IntroShots()
        {
            float w = 0f; while (!IntroCinematic.Playing && w < 12f) { w += Time.unscaledDeltaTime; yield return null; }
            Line($"\n## Intro: playing={IntroCinematic.Playing}");
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-introSkip") >= 0)
            {
                yield return new WaitForSecondsRealtime(5f); Hold(Key.Escape); yield return new WaitForSecondsRealtime(0.5f); yield return Shot("s7-hold-esc.png"); yield return new WaitForSecondsRealtime(1.0f); Release();
                w = 0f; while (IntroCinematic.Playing && w < 5f) { w += Time.unscaledDeltaTime; yield return null; }
                yield return new WaitForSecondsRealtime(1.5f);
                var p0 = FindAnyObjectByType<PlayerController>();
                Line($"  intro skipped: finished={IntroCinematic.Finished}, player enabled={(p0 != null && p0.enabled)}, banner='{ObjectiveBanner.Last}', objective={StoryDialogueSystem.Instance?.Current?.id}, clock hold={DawnClock.Hold}");
                yield return Shot("s7-after-skip.png"); yield break;
            }
            float[] at = { 2.5f, 8.8f, 13.5f, 16.5f, 19.8f, 22.5f, 26f, 29.2f, 33.5f, 38f, 41.5f, 46f, 51.5f, 54f, 56.5f, 60.5f, 64f, 68f };
            int i = 0; float startReal = Time.realtimeSinceStartup;
            while (IntroCinematic.Playing && w < 140f)
            {
                var dir = FindAnyObjectByType<UnityEngine.Playables.PlayableDirector>(); float t = dir != null ? (float)dir.time : 0f;
                if (i < at.Length && t >= at[i]) { yield return Shot($"s7-intro-{i + 1:00}-{IntroCinematic.LastShot}.png"); Line($"  intro t={t:0.0}s shot={IntroCinematic.LastShot}"); i++; }
                w += Time.unscaledDeltaTime; yield return null;
            }
            yield return new WaitForSecondsRealtime(1.2f);
            var p = FindAnyObjectByType<PlayerController>();
            Line($"  intro ended after {Time.realtimeSinceStartup - startReal:0.0}s real time: shots captured={i}, player enabled={(p != null && p.enabled)}, banner='{ObjectiveBanner.Last}', objective={StoryDialogueSystem.Instance?.Current?.id}");
            yield return Shot("s7-handover-objective.png");
        }

        /// <summary>Stage 3 evidence: get in, drive, lights, brake, get out, a real crash and the damage states.</summary>
        private IEnumerator V3Use(PlayerController pc, Vector3 stand, Vector3 look, string what)
        {
            Vector3 d = look - stand; d.y = 0f;
            pc.Teleport(stand, Quaternion.LookRotation(d.sqrMagnitude > 0.001f ? d : Vector3.forward)); yield return new WaitForSecondsRealtime(0.5f);
            string prompt = pc.CurrentInteractable != null ? pc.CurrentInteractable.Prompt : "(none)";
            yield return Tap(Key.E); yield return new WaitForSecondsRealtime(0.6f);
            Line($"  {what}: prompt '{prompt}'");
        }

        private IEnumerator V3Zone2Check()
        {
            var z = LegacyOfTemples.Gameplay.BreakInPuzzle.Instance; var pc = FindAnyObjectByType<PlayerController>();
            Line($"\n## V3 zone 2 break-in puzzle: present={(z != null)}, built={(z != null && z.Built)}");
            if (z == null || !z.Built || pc == null) yield break;
            MonoBehaviour orbit = null; foreach (var mb in FindObjectsByType<MonoBehaviour>()) if (mb != null && mb.GetType().Name == "OrbitCamera") orbit = mb;
            Transform r = z.transform; var cam = Camera.main;
            System.Func<Vector3, Vector3, IEnumerator> view = (from, at) => V3View(orbit, cam, r.TransformPoint(from), r.TransformPoint(at));
            pc.Teleport(r.TransformPoint(new Vector3(0f, 0f, 7f)), Quaternion.LookRotation(-r.forward)); yield return new WaitForSecondsRealtime(1.2f);
            yield return Shot("v3z2-01-arrive-gameplay-camera.png");
            yield return view(new Vector3(6f, 3.2f, 9f), new Vector3(0f, 1.2f, 0.5f)); yield return Shot("v3z2-02-storeroom-wide.png");
            yield return view(new Vector3(-1f, 1.2f, 4.2f), new Vector3(-3.2f, 0.9f, 1.3f)); yield return Shot("v3z2-03-plinth-and-shard.png"); V3ViewEnd(orbit);
            Line($"  start: lantern usable={z.Lantern.enabled}, flagstone locked={z.Flagstone.locked}, treasure usable={z.Chronicle.enabled}");
            // a wrong order first: the flagstone must not lift and the lantern must not be usable yet
            z.Flagstone.Interact(); z.Lantern.Interact();
            Line($"  wrong order tried first (flagstone, lantern): flagstone state={z.Flagstone.State} (0 = still down), flagstone prompt='{z.Flagstone.Prompt}', holding={(LegacyOfTemples.Gameplay.Puzzles.Carryable.Held != null)}");
            for (int i = 0; i < 3; i++)
            {
                Vector3 sp = z.Shards[i].transform.position;
                yield return V3Use(pc, sp + r.forward * 0.9f, sp, $"shard {i + 1} pick up");
                yield return V3Use(pc, z.Plinth.position + r.forward * 1.3f, z.Plinth.position, $"shard {i + 1} at the plinth");
                Line($"    socket states: {z.Sockets[0].State} {z.Sockets[1].State} {z.Sockets[2].State}");
            }
            yield return new WaitForSecondsRealtime(0.5f);
            Line($"  pot whole={z.PotWhole}, lantern usable={z.Lantern.enabled}");
            yield return view(new Vector3(-0.8f, 1.5f, 4.4f), new Vector3(-3.2f, 0.9f, 1.6f)); yield return Shot("v3z2-04-pot-whole-lantern-lit.png"); V3ViewEnd(orbit);
            Vector3 lp = z.Lantern.transform.position;
            yield return V3Use(pc, lp + r.forward * 1.0f, lp, "lantern pick up");
            Vector3 stand = z.Stain.position + r.forward * 0.6f - r.right * -0.25f; stand.y = pc.transform.position.y;
            yield return V3Use(pc, stand, z.Stain.position - r.forward * 3f, "lantern put down on the oil stain");
            yield return new WaitForSecondsRealtime(0.5f);
            Line($"  lantern on the stain={z.LampSpot.State == 1} (distance {Vector3.Distance(z.Lantern.transform.position, z.Stain.position):0.00} m), flagstone locked={z.Flagstone.locked}");
            yield return view(new Vector3(4.2f, 2.6f, 5.6f), new Vector3(0.6f, 1.0f, 0.6f)); yield return Shot("v3z2-05-lantern-on-stain-grille-shadows.png"); V3ViewEnd(orbit);
            Vector3 lk = z.LockPlate.transform.position;
            yield return V3Use(pc, lk + r.forward * 1.1f - r.right * 0.5f, lk, "lock plate turn 1"); yield return V3Use(pc, lk + r.forward * 1.1f - r.right * 0.5f, lk, "lock plate turn 2");
            yield return new WaitForSecondsRealtime(0.6f);
            Line($"  lock plate state={z.LockPlate.State} (2 = inside face showing), evidence saved={LegacyOfTemples.Gameplay.Puzzles.PuzzleSave.Get("evidence.inside_lock")}, banner='{ObjectiveBanner.Last}'");
            yield return view(new Vector3(-2.2f, 1.5f, 1.4f), new Vector3(-1.48f, 1.25f, 0.24f)); yield return Shot("v3z2-06-lock-plate-inside-scratches.png"); V3ViewEnd(orbit);
            yield return V3Use(pc, z.Flagstone.transform.position + r.forward * 1.4f, z.Flagstone.transform.position, "lift the flagstone");
            yield return new WaitForSecondsRealtime(0.5f);
            Line($"  flagstone state={z.Flagstone.State}, puzzle solved={z.Group.Solved}, treasure usable={z.Chronicle.enabled}");
            yield return view(new Vector3(3.2f, 2.2f, 5.6f), new Vector3(0.95f, 0.1f, 3.3f)); yield return Shot("v3z2-07-flagstone-lifted-chronicle.png"); V3ViewEnd(orbit);
            yield return V3Fight("v3z2-07b-fight-lookout.png");
            yield return V3Use(pc, z.Chronicle.transform.position + r.forward * 1.2f + r.right * 0.8f, z.Chronicle.transform.position, "take the Chronicle");
            var zs = LegacyOfTemples.Gameplay.ScoreSystem.Zone(LegacyOfTemples.Gameplay.BreakInPuzzle.ZoneId);
            Line($"  treasure taken={z.Chronicle.Taken}; score: puzzle solved={zs.puzzleSolved}, hints={zs.hintsUsed}, items={zs.items}, points={LegacyOfTemples.Gameplay.ScoreSystem.Points(zs)}, grade={LegacyOfTemples.Gameplay.ScoreSystem.Grade(zs)}");
            Line($"  checkpoint: {LegacyOfTemples.Gameplay.Checkpoints.Current.zone}/{LegacyOfTemples.Gameplay.Checkpoints.Current.label}");
            yield return new WaitForSecondsRealtime(0.5f); yield return Shot("v3z2-08-recovered-banner.png");
        }
        /// <summary>Fights the current V3 encounter through the game's own virtual buttons (strike, heavy, dodge, block, finisher). The bot is moved next to its target instead of walking.</summary>
        private IEnumerator V3Fight(string shot)
        {
            float w = 0f; while (LegacyOfTemples.Gameplay.Brawl.Active == null && w < 4f) { w += Time.unscaledDeltaTime; yield return null; }
            var f = LegacyOfTemples.Gameplay.Brawl.Active; var pc = FindAnyObjectByType<PlayerController>();
            if (f == null || pc == null) { Line("  fight: none started"); yield break; }
            yield return Shot(shot.Replace(".png", "-hud.png"));
            Line($"  fight begins: {f.Enemies.Count} enemy(ies): {string.Join(", ", f.Enemies.ConvertAll(e => e.name + " (" + e.type + ", " + e.maxHealth.ToString("0") + " hp)"))}");
            float t0 = Time.time; int maxAttackers = 0, n = 0, maxPhase = 1; bool shotTaken = false, sawPhase2 = false, sawTelegraph = false;
            while (!f.Won && Time.time - t0 < 150f)
            {
                LegacyOfTemples.Gameplay.BrawlEnemy target = null; float bd = 1e9f; bool danger = false;
                foreach (var e in f.Enemies) { if (!e.Alive) continue; float d = Vector3.Distance(e.transform.position, pc.transform.position); if (d < bd) { bd = d; target = e; } if (e.Now == LegacyOfTemples.Gameplay.BrawlEnemy.State.Telegraph) { sawTelegraph = true; if (d < 4f) danger = true; } if (e.Phase > 1) sawPhase2 = true; maxPhase = Mathf.Max(maxPhase, e.Phase); }
                maxAttackers = Mathf.Max(maxAttackers, f.Attacking);
                if (target == null) break;
                if (danger && n % 3 == 0) { VirtualInput.Block = true; yield return new WaitForSecondsRealtime(0.1f); VirtualInput.Block = false; VirtualInput.Press("dodge"); yield return new WaitForSecondsRealtime(0.5f); }
                Vector3 dir = (target.transform.position - pc.transform.position); dir.y = 0f; if (dir.sqrMagnitude < 0.01f) dir = Vector3.forward;
                Vector3 stand = target.transform.position - dir.normalized * 1.5f; stand.y = pc.transform.position.y;
                if (bd > 2.2f) pc.Teleport(stand, Quaternion.LookRotation(dir)); else pc.FaceTowards(target.transform.position);
                yield return new WaitForSecondsRealtime(0.12f);
                if (target.CanBeFinished && n % 2 == 0) VirtualInput.Press("finisher"); else VirtualInput.Press(n % 5 == 4 ? "heavy" : "light");
                n++; yield return new WaitForSecondsRealtime(n % 5 == 0 ? 0.9f : 0.4f);
                if (!shotTaken && Time.time - t0 > 2.5f) { shotTaken = true; yield return Shot(shot); }
            }
            yield return new WaitForSecondsRealtime(1.2f);
            Line($"  fight result: won={f.Won} in {f.Seconds:0}s, health left {f.Health:0}, damage taken {f.DamageTaken:0}, max combo {f.MaxCombo}, parries {f.Parries}, finishers {f.Finishers}, retries {f.Retries}; most attackers at once {maxAttackers} (limit {LegacyOfTemples.Gameplay.Brawl.MaxAttackers}); telegraph seen={sawTelegraph}; boss phase 2 seen={sawPhase2}, highest phase {maxPhase}");
        }

        private string V3Obj() { var st = LegacyOfTemples.Gameplay.StoryDialogueSystem.Instance; return st != null && st.Current != null ? st.Current.id + " (" + st.Current.title + ")" : "(story complete)"; }
        private IEnumerator V3TalkToPriest(PlayerController pc, string what)
        {
            NPCDialogueTrigger priest = null; foreach (var n in FindObjectsByType<NPCDialogueTrigger>()) if (n.CharacterId == Constants.Priest) priest = n;
            if (priest == null) { Line($"  {what}: priest not found"); yield break; }
            Vector3 at = priest.transform.position + priest.transform.forward * 1.6f;
            pc.Teleport(at, Quaternion.LookRotation(priest.transform.position - at)); yield return new WaitForSecondsRealtime(0.8f);
            string prompt = pc.CurrentInteractable != null ? pc.CurrentInteractable.Prompt : "(none)";
            yield return Tap(Key.E); yield return new WaitForSecondsRealtime(1.2f);
            yield return Shot($"v3s-talk-{what}.png");
            for (int i = 0; i < 6; i++) { yield return Tap(Key.Space); yield return new WaitForSecondsRealtime(0.9f); }
            Line($"  {what}: prompt '{prompt}', objective now {V3Obj()}");
        }

        private IEnumerator V3StoryCheck(string auditPath)
        {
            var pc = FindAnyObjectByType<PlayerController>();
            Line($"\n## V3 main path: start objective {V3Obj()}");
            if (pc == null) yield break;
            yield return Shot("v3s-01-start-hud.png");
            int doors = 0; foreach (var d in FindObjectsByType<LegacyOfTemples.Gameplay.SceneTransitionTrigger>()) if (d.Radius > 0f) doors++;
            Line($"  old scene doors still usable: {doors}");
            pc.Teleport(LegacyOfTemples.World.Village.GatePos + Vector3.back * 2f, Quaternion.LookRotation(Vector3.back)); yield return new WaitForSecondsRealtime(12f);
            Line($"  through the gate: objective {V3Obj()}");
            yield return V3TalkToPriest(pc, "priest-start");
            yield return new WaitForSecondsRealtime(1.5f); yield return Shot("v3s-02-goal-card-new.png");
            yield return new WaitForSecondsRealtime(9f); yield return Shot("v3s-03-goal-card-faded.png");
            var ui = FindAnyObjectByType<UIManager>(); Line($"  goal card alpha 9 s after the goal changed: {(ui != null ? ui.ObjectiveAlpha : -1f):0.00}");
            yield return V3Zone1Check(); Line($"  after the bazaar: objective {V3Obj()}"); File.WriteAllText(auditPath, report.ToString());
            yield return V3PerfAll();
            yield return V3MadhavShot("storeroom");
            yield return V3Zone2Check(); Line($"  after the storeroom: objective {V3Obj()}"); File.WriteAllText(auditPath, report.ToString());
            yield return V3MadhavShot("vaults");
            yield return V3Zone3Check(); Line($"  after the vaults: objective {V3Obj()}"); File.WriteAllText(auditPath, report.ToString());
            yield return V3MadhavShot("stepwell");
            yield return V3Zone4Check(); Line($"  after the stepwell: objective {V3Obj()}"); File.WriteAllText(auditPath, report.ToString());
            yield return V3MadhavShot("sunwall");
            yield return V3Zone5Check(); Line($"  after the Sun Wall: objective {V3Obj()}"); File.WriteAllText(auditPath, report.ToString());
            Line($"  evidence found {LegacyOfTemples.Gameplay.EvidenceBoard.Found} of 4; naming Madhav: {LegacyOfTemples.Gameplay.EvidenceBoard.Name(LegacyOfTemples.Gameplay.EvidenceBoard.InsideMan)}");
            yield return V3Zone6Check(); Line($"  after the river road: objective {V3Obj()}"); File.WriteAllText(auditPath, report.ToString());
            Line($"  art pass props placed so far: {LegacyOfTemples.Gameplay.ZoneDress.Placed}; Madhav on stage at {(LegacyOfTemples.Gameplay.MadhavCameos.Instance != null ? LegacyOfTemples.Gameplay.MadhavCameos.Instance.Where : "-")}");
            yield return V3TalkToPriest(pc, "priest-return");
            yield return new WaitForSecondsRealtime(4f); yield return Shot("v3s-09-ending.png");
            var st = LegacyOfTemples.Gameplay.StoryDialogueSystem.Instance;
            { var parts = new List<string>(); foreach (var kv in GameSfx.Counts) parts.Add($"{kv.Key} x{kv.Value}"); parts.Sort(); Line($"  3D sound effects played: {GameSfx.Played} ({GameSfx.Counts.Count} different): {string.Join(", ", parts)}"); }
            Line($"  story complete={(st != null && st.IsComplete)}; total {LegacyOfTemples.Gameplay.ScoreSystem.Total} points, grade {LegacyOfTemples.Gameplay.ScoreSystem.OverallGrade(6)}");
        }

        /// <summary>Frame time over 4 seconds of normal play at a place: average, 99th percentile and worst frame (ms).</summary>
        private IEnumerator V3Perf(string where, Vector3 at, Vector3 look)
        {
            var pc = FindAnyObjectByType<PlayerController>(); if (pc == null) yield break;
            Vector3 d = look - at; d.y = 0f; pc.Teleport(at, Quaternion.LookRotation(d.sqrMagnitude > 0.01f ? d : Vector3.forward));
            yield return new WaitForSecondsRealtime(1.5f);
            var ms = new List<float>(); float t0 = Time.realtimeSinceStartup; int vs = QualitySettings.vSyncCount, tf = Application.targetFrameRate;
            QualitySettings.vSyncCount = 0; Application.targetFrameRate = -1;   // measure the real cost, not the vsync step
            double cpuMain = 0, cpuRender = 0, gpu = 0; int timed = 0; var ft = new FrameTiming[1];
            while (Time.realtimeSinceStartup - t0 < 4f)
            {
                yield return null; ms.Add(Time.unscaledDeltaTime * 1000f);
                FrameTimingManager.CaptureFrameTimings(); if (FrameTimingManager.GetLatestTimings(1, ft) > 0) { cpuMain += ft[0].cpuMainThreadFrameTime; cpuRender += ft[0].cpuRenderThreadFrameTime; gpu += ft[0].gpuFrameTime; timed++; }
            }
            QualitySettings.vSyncCount = vs; Application.targetFrameRate = tf;
            string split = timed > 0 ? $", CPU main {cpuMain / timed:0.0} ms, CPU render {cpuRender / timed:0.0} ms, GPU {gpu / timed:0.0} ms" : ", (no frame timing data)";
            ms.Sort(); float avg = 0f; foreach (var m in ms) avg += m; avg /= Mathf.Max(1, ms.Count);
            float p99 = ms[Mathf.Clamp(Mathf.RoundToInt(ms.Count * 0.99f), 0, ms.Count - 1)], worst = ms[ms.Count - 1];
            Line($"  PERF {where}: {ms.Count} frames, average {avg:0.0} ms ({1000f / avg:0} fps), 99% {p99:0.0} ms, worst {worst:0.0} ms{split}, graphics {LegacyOfTemples.Gameplay.GraphicsPresets.Current.name}, draw-visible villagers {LegacyOfTemples.Gameplay.CrowdCulling.Visible}");
        }

        private IEnumerator V3Beauty()
        {
            var orbit = V3Orbit(); var cam = Camera.main; if (orbit != null) orbit.enabled = false;
            var pc = FindAnyObjectByType<PlayerController>(); if (pc != null) pc.Teleport(new Vector3(9000f, 0f, 9000f), Quaternion.identity);   // the hero out of frame
            System.Func<Transform, Vector3, Vector3, string, IEnumerator> shot = (t, from, at, name) => BeautyShot(cam, t, from, at, name);
            var b = LegacyOfTemples.Gameplay.TempleSite.Footprint; var tl = LegacyOfTemples.Gameplay.TempleLife.Instance; var vl = LegacyOfTemples.Gameplay.VillageLife.Instance;
            var ts = LegacyOfTemples.Gameplay.TempleSite.Instance;
            if (ts != null)
            {
                yield return shot(null, new Vector3(b.center.x + 70f, 30f, b.max.z + 110f), new Vector3(b.center.x, 25f, b.center.z), "temple-aerial");
                yield return shot(null, LegacyOfTemples.World.Village.GatePos + new Vector3(3f, 2f, 4f), ts.TowerTop, "temple-from-the-gate");
                yield return shot(null, new Vector3(b.center.x - 40f, 3f, b.max.z + 40f), new Vector3(b.center.x, 18f, b.center.z), "temple-low");
            }
            if (tl != null && tl.Bell != null) yield return shot(null, tl.Bell.position + new Vector3(2.5f, -1.2f, 3.5f), tl.Bell.position + Vector3.down * 0.6f, "temple-bell");
            if (vl != null)
            {
                yield return shot(null, vl.TeaStallStand + new Vector3(4f, 1.8f, -5f), vl.TeaStallStand + new Vector3(-2f, 0.9f, 1f), "bazaar-tea-stall");
                yield return shot(null, vl.TeaStallStand + new Vector3(2f, 3f, 30f), vl.TeaStallStand + new Vector3(0f, 1f, 0f), "bazaar-road");
            }
            var z1 = LegacyOfTemples.Gameplay.BazaarTail.Instance; if (z1 != null && z1.Yard != null) yield return shot(z1.Yard, new Vector3(5f, 4f, -11f), new Vector3(0f, 1f, 2f), "bazaar-yard");
            var z2 = LegacyOfTemples.Gameplay.BreakInPuzzle.Instance;
            if (z2 != null) { yield return shot(z2.transform, new Vector3(6f, 3.2f, 9f), new Vector3(0f, 1.4f, 0.5f), "storeroom-wide"); yield return shot(z2.transform, new Vector3(-2.5f, 1.4f, 4.5f), new Vector3(-3.2f, 0.9f, 1.3f), "storeroom-plinth"); yield return shot(z2.transform, new Vector3(2.5f, 1.6f, 3.5f), new Vector3(0f, 1.6f, 0f), "storeroom-grille"); }
            var z3 = LegacyOfTemples.Gameplay.VaultPuzzle.Instance;
            if (z3 != null) { yield return shot(z3.transform, new Vector3(-10.5f, 3f, -4f), new Vector3(0f, 1f, 1f), "vaults-hall"); yield return shot(z3.transform, new Vector3(1.5f, 3.4f, -3.6f), new Vector3(7f, 0.4f, 0.4f), "vaults-light-pattern"); yield return shot(z3.transform, new Vector3(6f, 2f, 3.8f), new Vector3(11.8f, 1.7f, 0f), "vaults-discs"); }
            var z4 = LegacyOfTemples.Gameplay.StepwellPuzzle.Instance;
            if (z4 != null) { yield return shot(z4.transform, new Vector3(-12f, 12f, -15f), new Vector3(1f, 2f, 0f), "stepwell-aerial"); yield return shot(z4.transform, new Vector3(-9f, 6.5f, -9f), new Vector3(3f, 3f, 3f), "stepwell-rim"); yield return shot(z4.transform, new Vector3(-14f, 1.7f, -6f), new Vector3(0f, 2f, 0f), "stepwell-wet-stone"); }
            var z5 = LegacyOfTemples.Gameplay.SunWallPuzzle.Instance;
            if (z5 != null) { yield return shot(z5.transform, new Vector3(-12f, 8f, -18f), new Vector3(0f, 2f, 6f), "sunwall-plaza"); yield return shot(z5.transform, new Vector3(4f, 2.2f, 4f), new Vector3(0f, 3f, 0f), "sunwall-wheel"); yield return shot(z5.transform, new Vector3(0f, 2f, 8f), new Vector3(0f, 2.4f, 14.5f), "sunwall-carved-wall"); }
            var z6 = LegacyOfTemples.Gameplay.JungleChase.Instance;
            if (z6 != null) { yield return shot(null, z6.Road[2] + new Vector3(-6f, 5f, 8f), z6.Road[8], "river-road"); yield return shot(z6.Court, new Vector3(12f, 10f, 24f), new Vector3(0f, 1f, 0f), "ruins-court"); yield return shot(z6.Court, new Vector3(0f, 1.8f, 22f), new Vector3(0f, 3f, 14f), "ruins-arch"); }
            if (orbit != null) orbit.enabled = true;
        }
        private IEnumerator BeautyShot(Camera cam, Transform t, Vector3 from, Vector3 at, string name)
        {
            if (cam == null) yield break;
            cam.transform.position = t != null ? t.TransformPoint(from) : from; cam.transform.LookAt(t != null ? t.TransformPoint(at) : at);
            yield return new WaitForSecondsRealtime(2.8f); yield return Shot($"beauty-{name}.png"); Line($"  beauty shot {name}");
        }

        private IEnumerator V3PerfAll()
        {
            { var z = LegacyOfTemples.Gameplay.BreakInPuzzle.Instance; if (z != null) yield return V3Perf("storeroom", z.transform.TransformPoint(new Vector3(0f, 0f, 9f)), z.transform.position); }
            { var z = LegacyOfTemples.Gameplay.VaultPuzzle.Instance; if (z != null) yield return V3Perf("vaults", z.transform.TransformPoint(new Vector3(-9f, 0f, 0f)), z.transform.position); }
            { var z = LegacyOfTemples.Gameplay.StepwellPuzzle.Instance; if (z != null) yield return V3Perf("stepwell", z.transform.TransformPoint(new Vector3(-4f, LegacyOfTemples.Gameplay.StepwellPuzzle.Rim, -8.5f)), z.transform.position); }
            { var z = LegacyOfTemples.Gameplay.SunWallPuzzle.Instance; if (z != null) yield return V3Perf("sunwall", z.transform.TransformPoint(new Vector3(0f, 0f, -10f)), z.transform.TransformPoint(new Vector3(0f, 0f, 14f))); }
            { var z = LegacyOfTemples.Gameplay.JungleChase.Instance; if (z != null) yield return V3Perf("river road", z.RoadStart, z.Road[4]); if (z != null) yield return V3Perf("ruin court", z.Court.TransformPoint(new Vector3(0f, 0f, 10f)), z.Court.position); }
            { var vl = LegacyOfTemples.Gameplay.VillageLife.Instance; if (vl != null) yield return V3Perf("bazaar", vl.TeaStallStand + Vector3.forward * 4f, vl.TeaStallStand + Vector3.forward * 40f); }
            { var ts = LegacyOfTemples.Gameplay.TempleSite.Instance; if (ts != null) yield return V3Perf("temple front", LegacyOfTemples.World.Village.GatePos + Vector3.back * 8f, ts.TowerTop); }
        }

        private IEnumerator V3MadhavShot(string where)
        {
            var m = LegacyOfTemples.Gameplay.MadhavCameos.Instance; yield return new WaitForSecondsRealtime(0.5f);
            if (m == null || m.Madhav == null || !m.Madhav.gameObject.activeInHierarchy) { Line($"  Madhav at the {where}: not on stage"); yield break; }
            var orbit = V3Orbit(); var cam = Camera.main;
            yield return V3View(orbit, cam, m.Madhav.position + m.Madhav.forward * 3.2f + Vector3.up * 1.7f + m.Madhav.right * 1.2f, m.Madhav.position + Vector3.up * 1.4f);
            yield return Shot($"v3m-madhav-{where}.png"); V3ViewEnd(orbit);
            Line($"  Madhav at the {where}: on stage ({m.Where})");
        }

        private IEnumerator V3Zone1Check()
        {
            var z = LegacyOfTemples.Gameplay.BazaarTail.Instance; var pc = FindAnyObjectByType<PlayerController>();
            Line($"\n## V3 zone 1 Bazaar tail: present={(z != null)}, built={(z != null && z.Built)}, courier={(z != null && z.Courier != null)}");
            if (z == null || !z.Built || z.Courier == null || pc == null) yield break;
            var cam = Camera.main; var orbit = V3Orbit();
            pc.Teleport(z.Route[0] + Vector3.back * 20f, Quaternion.LookRotation(Vector3.forward)); yield return new WaitForSecondsRealtime(2f);
            Line($"  near the tea stall: courier phase {z.Now}");
            yield return Shot("v3z1-01-courier-sets-off.png");
            bool testedSpot = false; float t0 = Time.time;
            while (z.Now != LegacyOfTemples.Gameplay.BazaarTail.Phase.Arrived && z.Now != LegacyOfTemples.Gameplay.BazaarTail.Phase.Fight && z.Now != LegacyOfTemples.Gameplay.BazaarTail.Phase.Done && Time.time - t0 < 180f)
            {
                Vector3 c = z.Courier.position; Vector3 back = (z.Route[0] - c); back.y = 0f; back = back.sqrMagnitude > 0.01f ? back.normalized : Vector3.back;
                if (!testedSpot && z.Now == LegacyOfTemples.Gameplay.BazaarTail.Phase.LookingBack)
                {   // one deliberate mistake: stand close in the open while he looks back
                    testedSpot = true; pc.Teleport(c + back * 6f, Quaternion.LookRotation(-back)); yield return new WaitForSecondsRealtime(2.4f);
                    Line($"  stood 6 m behind him in the open while he looked back: spotted {z.Spotted}");
                    yield return Shot("v3z1-02-spotted.png");
                }
                else pc.Teleport(c + back * 19f, Quaternion.LookRotation(-back));
                yield return new WaitForSecondsRealtime(0.5f);
            }
            Line($"  tail finished: phase {z.Now}, spotted {z.Spotted} time(s), {Time.time - t0:0} s");
            yield return V3View(orbit, cam, z.Yard.TransformPoint(new Vector3(4f, 5f, -12f)), z.Yard.position + Vector3.up); yield return Shot("v3z1-03-the-yard.png"); V3ViewEnd(orbit);
            pc.Teleport(z.Yard.TransformPoint(new Vector3(0f, 0f, -4f)), z.Yard.rotation); yield return new WaitForSecondsRealtime(1f);
            yield return V3Fight("v3z1-04-street-fight.png");
            yield return new WaitForSecondsRealtime(1f);
            yield return V3Use(pc, z.Bell.transform.position - z.Yard.forward * 1.2f, z.Bell.transform.position, "take the Dawn Bell");
            Line($"  bell taken={z.Bell.Taken}, evidence list={LegacyOfTemples.Gameplay.Puzzles.PuzzleSave.Get("evidence.list")}"); V3Score(LegacyOfTemples.Gameplay.BazaarTail.ZoneId);
        }

        private IEnumerator V3Zone6Check()
        {
            var z = LegacyOfTemples.Gameplay.JungleChase.Instance; var pc = FindAnyObjectByType<PlayerController>();
            Line($"\n## V3 zone 6 Jungle road: present={(z != null)}, built={(z != null && z.Built)}, runner={(z != null && z.Runner != null)}");
            if (z == null || !z.Built || z.Runner == null || pc == null) yield break;
            var cam = Camera.main; var orbit = V3Orbit(); yield return new WaitForSecondsRealtime(1f);
            var jeep = LegacyOfTemples.Gameplay.Vehicles.Jeep.Instance;
            Line($"  jeep at the road start: {(jeep != null ? Vector3.Distance(jeep.transform.position, z.RoadStart).ToString("0.0") + " m" : "no jeep")}");
            yield return V3View(orbit, cam, z.RoadStart + new Vector3(-8f, 6f, 10f), z.Road[5]); yield return Shot("v3z6-01-jungle-road.png"); V3ViewEnd(orbit);
            bool drove = false;
            if (jeep != null)
            {
                pc.Teleport(jeep.DriverExitPoint, Quaternion.LookRotation(jeep.transform.position - jeep.DriverExitPoint)); yield return new WaitForSecondsRealtime(0.8f);
                yield return Tap(Key.E); yield return new WaitForSecondsRealtime(2.5f); drove = jeep.Occupied;
            }
            Line($"  in the jeep: {drove}; chase phase {z.Now}");
            float t0 = Time.time; bool shot = false;
            while (z.Now == LegacyOfTemples.Gameplay.JungleChase.Phase.Waiting || z.Now == LegacyOfTemples.Gameplay.JungleChase.Phase.Chase)
            {
                if (Time.time - t0 > 150f) break;
                if (drove && jeep.Occupied)
                {   // the bot drives along the road: aim at the road point a little ahead
                    int best = 0; float bd = 1e9f; for (int i = 0; i < z.Road.Count; i++) { float d = Vector3.Distance(jeep.transform.position, z.Road[i]); if (d < bd) { bd = d; best = i; } }
                    // drive like a careful person: look three points ahead, keep to the free side of the fallen logs, slow for bends
                    int k = Mathf.Min(best + 3, z.Road.Count - 1);
                    Vector3 target = z.Road[k];
                    foreach (var log in z.GetComponentsInChildren<Transform>())
                    {   // within 20 m of a fallen log, keep to the free side of the road until past it
                        if (log.name != "FallenLog") continue; Vector3 lp = log.position; lp.y = jeep.transform.position.y;
                        if (Vector3.Distance(jeep.transform.position, lp) > 20f) continue;
                        int li = 0; float ld = 1e9f; for (int i = 0; i < z.Road.Count; i++) { float d = Vector3.Distance(lp, z.Road[i]); if (d < ld) { ld = d; li = i; } }
                        if (Mathf.Abs(best - li) > 2) continue;
                        float side = Mathf.Sign(lp.x - z.Road[li].x); target = z.Road[Mathf.Min(best + 1, z.Road.Count - 1)] + Vector3.right * -side * 3.1f;
                    }
                    Vector3 aim = target - jeep.transform.position; aim.y = 0f;
                    float ang = Vector3.SignedAngle(jeep.transform.forward, aim, Vector3.up);
                    float gap = Vector3.Distance(jeep.transform.position, z.Runner.position);
                    float wantSpeed = Mathf.Abs(ang) > 18f ? 6f : gap < 12f ? 7f : 11f;
                    jeep.Car.autopilot = true; jeep.Car.autoSteer = Mathf.Clamp(ang / 22f, -1f, 1f);
                    jeep.Car.autoThrottle = jeep.Car.CurrentSpeed < wantSpeed ? 0.7f : 0f; jeep.Car.autoBrake = jeep.Car.CurrentSpeed > wantSpeed + 2.5f || gap < 6f;
                }
                else { Vector3 back = z.Runner.position - z.Road[0]; pc.Teleport(z.Runner.position - back.normalized * 10f, Quaternion.LookRotation(back)); }
                if (!shot && z.Now == LegacyOfTemples.Gameplay.JungleChase.Phase.Chase && Time.time - t0 > 6f) { shot = true; yield return Shot("v3z6-02-chase.png"); }
                yield return new WaitForSecondsRealtime(0.1f);
            }
            if (jeep != null) { jeep.Car.autoThrottle = 0f; jeep.Car.autoBrake = true; jeep.Car.autoSteer = 0f; yield return new WaitForSecondsRealtime(2.5f); jeep.Car.autopilot = false; }
            Line($"  chase over in {Time.time - t0:0} s: phase {z.Now}, closest gap {z.ClosestGap:0.0} m, caught up={z.Caught}, jeep damage state {(jeep != null ? jeep.StateName : "-")} (total damage {(jeep != null ? jeep.TotalDamage : 0f):0})");
            if (jeep != null && jeep.Occupied) { yield return Tap(Key.E); yield return new WaitForSecondsRealtime(2.5f); }
            pc.Teleport(z.Court.TransformPoint(new Vector3(0f, 0f, 9f)), Quaternion.LookRotation(-z.Court.forward)); yield return new WaitForSecondsRealtime(1.5f);
            yield return V3View(orbit, cam, z.Court.TransformPoint(new Vector3(10f, 9f, 22f)), z.Court.position); yield return Shot("v3z6-03-ruin-court.png"); V3ViewEnd(orbit);
            yield return V3Fight("v3z6-04-wave-1.png");
            yield return new WaitForSecondsRealtime(3f); Line($"  wave 2 (inside man named correctly={LegacyOfTemples.Gameplay.EvidenceBoard.NamedCorrectly}):");
            yield return V3Fight("v3z6-05-wave-2.png");
            yield return new WaitForSecondsRealtime(3f); Line("  Bhaskar:");
            yield return V3Fight("v3z6-06-bhaskar.png");
            yield return new WaitForSecondsRealtime(4f); yield return Shot("v3z6-07-madhav-gives-up.png");
            yield return V3Use(pc, z.Finial.transform.position + Vector3.forward * 1.2f, z.Finial.transform.position, "take the Chariot Finial");
            Line($"  finial taken={z.Finial.Taken}, phase {z.Now}"); V3Score(LegacyOfTemples.Gameplay.JungleChase.ZoneId);
        }

        private MonoBehaviour V3Orbit() { MonoBehaviour o = null; foreach (var mb in FindObjectsByType<MonoBehaviour>()) if (mb != null && mb.GetType().Name == "OrbitCamera") o = mb; return o; }
        private void V3Score(string zone)
        {
            var zs = LegacyOfTemples.Gameplay.ScoreSystem.Zone(zone);
            Line($"  score {zone}: puzzle solved={zs.puzzleSolved}, hints={zs.hintsUsed}, items={zs.items}, points={LegacyOfTemples.Gameplay.ScoreSystem.Points(zs)}, grade={LegacyOfTemples.Gameplay.ScoreSystem.Grade(zs)}");
        }

        private IEnumerator V3Zone5Check()
        {
            var z = LegacyOfTemples.Gameplay.SunWallPuzzle.Instance; var pc = FindAnyObjectByType<PlayerController>();
            Line($"\n## V3 zone 5 Sun Wall: present={(z != null)}, built={(z != null && z.Built)}");
            if (z == null || !z.Built || pc == null) yield break;
            Transform r = z.transform; var cam = Camera.main; var orbit = V3Orbit();
            pc.Teleport(r.TransformPoint(new Vector3(-4f, 0f, -9f)), Quaternion.LookRotation(r.forward)); yield return new WaitForSecondsRealtime(3.5f);
            yield return Shot("v3z5-01-arrive-gameplay-camera.png");
            yield return V3View(orbit, cam, r.TransformPoint(new Vector3(-11f, 7f, -17f)), r.TransformPoint(new Vector3(1f, 1.5f, 6f))); yield return Shot("v3z5-02-plaza-before.png"); V3ViewEnd(orbit);
            Line($"  start: sockets lit {z.Sockets[0].State} {z.Sockets[1].State}, wheel stop {z.Wheel.State}, door item usable={z.Case.enabled}");
            for (int m = 0; m < 4; m++)
            {
                Vector3 mp = z.Mirrors[m].transform.position; Vector3 stand = mp - r.forward * 1.3f + r.right * (m == 1 || m == 2 ? 1.1f : -1.1f); stand.y = pc.transform.position.y;
                yield return V3Use(pc, stand, mp, $"mirror {m + 1} turn 1"); yield return V3Use(pc, stand, mp, $"mirror {m + 1} turn 2");
            }
            yield return new WaitForSecondsRealtime(0.6f);
            Line($"  four mirrors set, wheel not moved: sockets lit {z.Sockets[0].State} {z.Sockets[1].State} (the right one must still be dark: a spoke blocks its beam)");
            yield return V3View(orbit, cam, r.TransformPoint(new Vector3(-11f, 7f, -17f)), r.TransformPoint(new Vector3(1f, 1.5f, 6f))); yield return Shot("v3z5-03-mirrors-set-spoke-blocks.png"); V3ViewEnd(orbit);
            Vector3 wp = z.Wheel.transform.position; wp.y = pc.transform.position.y;
            yield return V3Use(pc, wp - r.forward * 2.6f + r.right * 1.6f, wp, "push the wheel"); yield return new WaitForSecondsRealtime(2.2f);
            Line($"  wheel stop {z.Wheel.State}: sockets lit {z.Sockets[0].State} {z.Sockets[1].State}, solved={z.Group.Solved}");
            yield return V3View(orbit, cam, r.TransformPoint(new Vector3(-11f, 7f, -17f)), r.TransformPoint(new Vector3(1f, 1.5f, 6f))); yield return Shot("v3z5-04-both-beams-reach-the-wall.png");
            yield return new WaitForSecondsRealtime(2.5f);
            yield return V3View(orbit, cam, r.TransformPoint(new Vector3(-5f, 2.4f, 7f)), r.TransformPoint(new Vector3(0.5f, 1.2f, 15.5f))); yield return Shot("v3z5-05-hidden-door-open.png"); V3ViewEnd(orbit);
            yield return V3Fight("v3z5-06-fight-kishore.png");
            yield return V3Use(pc, r.TransformPoint(new Vector3(0f, 0f, 15.9f)), z.Case.transform.position, "take the case");
            Line($"  case taken={z.Case.Taken}"); V3Score(LegacyOfTemples.Gameplay.SunWallPuzzle.ZoneId);
        }

        private IEnumerator V3Zone4Check()
        {
            var z = LegacyOfTemples.Gameplay.StepwellPuzzle.Instance; var pc = FindAnyObjectByType<PlayerController>();
            Line($"\n## V3 zone 4 Stepwell: present={(z != null)}, built={(z != null && z.Built)}");
            if (z == null || !z.Built || pc == null) yield break;
            Transform r = z.transform; var cam = Camera.main; var orbit = V3Orbit(); float rim = LegacyOfTemples.Gameplay.StepwellPuzzle.Rim;
            pc.Teleport(r.TransformPoint(new Vector3(7f, 0f, -11f)), Quaternion.LookRotation(r.forward - r.right)); yield return new WaitForSecondsRealtime(3.5f);
            yield return Shot("v3z4-01-arrive-gameplay-camera.png");
            yield return V3View(orbit, cam, r.TransformPoint(new Vector3(-9f, 11f, -13f)), r.TransformPoint(new Vector3(2f, 2f, 0f))); yield return Shot("v3z4-02-tank-empty.png"); V3ViewEnd(orbit);
            Line($"  start: water level {z.Basin.State}, panel locked={z.Panel.locked}");
            z.Panel.Interact(); Line($"  panel tried early: state={z.Panel.State} (0 = shut)");
            yield return V3Use(pc, r.TransformPoint(new Vector3(-3.7f, rim + 0.05f, -6.8f)), z.SluiceA.transform.position, "south sluice wheel");
            yield return V3Use(pc, r.TransformPoint(new Vector3(3.7f, rim + 0.05f, 6.8f)), z.SluiceB.transform.position, "north sluice wheel");
            yield return new WaitForSecondsRealtime(6.5f);
            Line($"  both sluices open: water level {z.Basin.State}, raft top at {r.InverseTransformPoint(z.Raft.position).y + 0.125f:0.00} m (rim {rim:0.0}), panel locked={z.Panel.locked}");
            yield return V3View(orbit, cam, r.TransformPoint(new Vector3(-9f, 11f, -13f)), r.TransformPoint(new Vector3(2f, 3f, 0f))); yield return Shot("v3z4-03-tank-full-panel-under-water.png"); V3ViewEnd(orbit);
            pc.Teleport(z.Raft.position + Vector3.up * 0.2f + r.right * 0.3f, Quaternion.LookRotation(-r.right)); yield return new WaitForSecondsRealtime(0.8f);
            yield return V3Use(pc, z.Raft.position + Vector3.up * 0.2f + r.right * 0.3f, z.Drain.transform.position, "drain chain 1");
            yield return new WaitForSecondsRealtime(1.6f);
            yield return V3Use(pc, pc.transform.position, z.Drain.transform.position, "drain chain 2");
            yield return new WaitForSecondsRealtime(2.2f);
            Line($"  drained twice: water level {z.Basin.State}, player height above the tank floor {r.InverseTransformPoint(pc.transform.position).y:0.00} m (rode the raft down), panel locked={z.Panel.locked}");
            yield return V3View(orbit, cam, r.TransformPoint(new Vector3(-4f, 6.5f, -4f)), r.TransformPoint(new Vector3(6f, 3.2f, 0f))); yield return Shot("v3z4-04-water-on-the-long-mark.png"); V3ViewEnd(orbit);
            yield return V3Use(pc, pc.transform.position, z.Panel.transform.position, "push the panel"); yield return new WaitForSecondsRealtime(0.6f);
            Line($"  panel state={z.Panel.State}, solved={z.Group.Solved}, evidence key ring={LegacyOfTemples.Gameplay.Puzzles.PuzzleSave.Get("evidence.key_ring")}");
            yield return V3View(orbit, cam, r.TransformPoint(new Vector3(3.2f, 4.6f, -1.2f)), r.TransformPoint(new Vector3(9.6f, 3f, 0.2f))); yield return Shot("v3z4-05-chamber-vessel.png"); V3ViewEnd(orbit);
            yield return V3Use(pc, r.TransformPoint(new Vector3(8.6f, 2.7f, 0.2f)), z.Vessel.transform.position, "take the vessel");
            Line($"  vessel taken={z.Vessel.Taken}");
            pc.Teleport(r.TransformPoint(new Vector3(6f, 0f, -11.5f)), Quaternion.LookRotation(r.right)); yield return new WaitForSecondsRealtime(1f);
            yield return V3Fight("v3z4-06-fight-knife-man.png");
            V3Score(LegacyOfTemples.Gameplay.StepwellPuzzle.ZoneId);
        }

        private IEnumerator V3Zone3Check()
        {
            var z = LegacyOfTemples.Gameplay.VaultPuzzle.Instance; var pc = FindAnyObjectByType<PlayerController>();
            Line($"\n## V3 zone 3 Vaults: present={(z != null)}, built={(z != null && z.Built)}");
            if (z == null || !z.Built || pc == null) yield break;
            Transform r = z.transform; var cam = Camera.main; var orbit = V3Orbit();
            pc.Teleport(r.TransformPoint(new Vector3(-10.5f, 0f, 0f)), Quaternion.LookRotation(r.right)); yield return new WaitForSecondsRealtime(3.5f);
            yield return Shot("v3z3-01-arrive-gameplay-camera.png");
            Line($"  start: gate open={z.GateOpen}, plates {z.Plates[0].State} {z.Plates[1].State}");
            // a wrong push first: block B west into the wall corner, then the reset lever
            Vector3 b = z.BlockB.transform.position; b.y = pc.transform.position.y;
            for (int i = 0; i < 4; i++) yield return V3Use(pc, z.BlockB.transform.position + r.right * 1.1f - Vector3.up * 0.45f, z.BlockB.transform.position, "block B pushed the wrong way (west)");
            Line($"  block B after wrong pushes at {r.InverseTransformPoint(z.BlockB.transform.position):0.0}");
            yield return V3Use(pc, r.TransformPoint(new Vector3(-10.6f, 0f, -2.2f)), z.ResetLever.transform.position, "reset lever"); yield return new WaitForSecondsRealtime(0.5f);
            Line($"  after the lever: block A at {r.InverseTransformPoint(z.BlockA.transform.position):0.0}, block B at {r.InverseTransformPoint(z.BlockB.transform.position):0.0}");
            for (int i = 0; i < 4; i++) yield return V3Use(pc, z.BlockA.transform.position - r.right * 1.1f - Vector3.up * 0.45f, z.BlockA.transform.position, "block A east");
            for (int i = 0; i < 2; i++) yield return V3Use(pc, z.BlockB.transform.position + r.forward * 1.1f - Vector3.up * 0.45f, z.BlockB.transform.position, "block B south (round the pillar)");
            for (int i = 0; i < 2; i++) yield return V3Use(pc, z.BlockB.transform.position - r.right * 1.1f - Vector3.up * 0.45f, z.BlockB.transform.position, "block B east");
            yield return new WaitForSecondsRealtime(1.8f);
            Line($"  blocks placed: A at {r.InverseTransformPoint(z.BlockA.transform.position):0.0}, B at {r.InverseTransformPoint(z.BlockB.transform.position):0.0}, plates {z.Plates[0].State} {z.Plates[1].State}, gate open={z.GateOpen}");
            yield return V3View(orbit, cam, r.TransformPoint(new Vector3(-10.8f, 3.2f, -3.8f)), r.TransformPoint(new Vector3(-2f, 0.8f, 0.5f))); yield return Shot("v3z3-02-blocks-on-plates-gate-open.png");
            yield return V3View(orbit, cam, r.TransformPoint(new Vector3(1.2f, 3.4f, -3.6f)), r.TransformPoint(new Vector3(8f, 0.6f, 0.6f))); yield return Shot("v3z3-03-light-pattern-and-discs.png"); V3ViewEnd(orbit);
            int[] turns = LegacyOfTemples.Gameplay.VaultPuzzle.Answer;
            for (int d = 0; d < 3; d++) for (int k = 0; k < turns[d]; k++)
            { Vector3 dp = z.Discs[d].transform.position; Vector3 st = dp - r.right * 1.3f; st.y = pc.transform.position.y; if (k == 0) st.y = r.position.y; yield return V3Use(pc, st, dp, $"disc {d + 1} turn {k + 1}"); }
            yield return new WaitForSecondsRealtime(2.2f);
            Line($"  discs at {z.Discs[0].State} {z.Discs[1].State} {z.Discs[2].State} (answer {turns[0]} {turns[1]} {turns[2]}), solved={z.Group.Solved}, evidence ledger={LegacyOfTemples.Gameplay.Puzzles.PuzzleSave.Get("evidence.ledger")}");
            yield return V3View(orbit, cam, r.TransformPoint(new Vector3(5.5f, 2.6f, -3.4f)), r.TransformPoint(new Vector3(10.5f, 1.2f, 0.4f))); yield return Shot("v3z3-04-discs-solved-chest-open.png"); V3ViewEnd(orbit);
            yield return V3Fight("v3z3-05-fight-vault-guards.png");
            yield return V3Use(pc, r.TransformPoint(new Vector3(7.8f, 0f, 0.9f)), z.Rosette.transform.position, "take the rosette");
            Line($"  rosette taken={z.Rosette.Taken}"); V3Score(LegacyOfTemples.Gameplay.VaultPuzzle.ZoneId);
        }

        /// <summary>Walkthrough pictures that the story run does not take: HUD, map, journal, evidence board, guide, pause, characters, jeep.</summary>
        private IEnumerator WalkthroughExtras()
        {
            Line("\n## Walkthrough extras");
            yield return new WaitForSecondsRealtime(7f);   // the temple reveal and the first goal card
            yield return Shot("w-hud-goal.png");
            foreach (var (key, file, what) in new[] { (Key.M, "w-map.png", "map"), (Key.J, "w-journal.png", "journal"), (Key.B, "w-evidence-board.png", "evidence board"), (Key.H, "w-guide.png", "guide") })
            {
                yield return Tap(key); yield return new WaitForSecondsRealtime(0.8f); yield return Shot(file);
                yield return Tap(key); yield return new WaitForSecondsRealtime(0.5f);
                if (Time.timeScale == 0f) { yield return Tap(Key.Escape); yield return new WaitForSecondsRealtime(0.4f); }
                Line($"  {what}: captured {file}");
            }
            yield return Tap(Key.Escape); yield return new WaitForSecondsRealtime(0.8f); yield return Shot("w-pause.png");
            yield return Tap(Key.Escape); yield return new WaitForSecondsRealtime(0.6f); if (Time.timeScale == 0f) { yield return Tap(Key.Escape); yield return new WaitForSecondsRealtime(0.4f); }
            var orbit = V3Orbit(); var cam = Camera.main; var pc = FindAnyObjectByType<PlayerController>();
            if (pc != null) { var t = pc.transform; yield return V3View(orbit, cam, t.position + t.forward * 2.6f + Vector3.up * 1.6f + t.right * 0.6f, t.position + Vector3.up * 1.3f); yield return Shot("w-char-aryan.png"); V3ViewEnd(orbit); }
            foreach (var n in FindObjectsByType<NPCDialogueTrigger>())
            {
                string who = n.Id == "npc_priest" ? "pujari" : n.Id == "npc_villager" ? "meera" : null; if (who == null || !n.gameObject.activeInHierarchy) continue;
                var t = n.transform; yield return V3View(orbit, cam, t.position + t.forward * 2.4f + Vector3.up * 1.6f + t.right * 0.5f, t.position + Vector3.up * 1.35f); yield return Shot($"w-char-{who}.png"); V3ViewEnd(orbit);
                Line($"  character {who}: captured at {t.position:0.0}");
            }
            var j = LegacyOfTemples.Gameplay.Vehicles.Jeep.Instance;
            if (j != null) { var t = j.transform; yield return V3View(orbit, cam, t.position + t.forward * 6f + t.right * 4f + Vector3.up * 2.2f, t.position + Vector3.up * 0.9f); yield return Shot("w-jeep.png"); V3ViewEnd(orbit); Line("  jeep: captured"); }
            yield return new WaitForSecondsRealtime(0.5f);
        }

        private IEnumerator V3View(MonoBehaviour orbit, Camera cam, Vector3 from, Vector3 at)
        {
            if (orbit != null) orbit.enabled = false;
            if (cam != null) { cam.transform.position = from; cam.transform.LookAt(at); }
            yield return null; yield return null;
        }
        private void V3ViewEnd(MonoBehaviour orbit) { if (orbit != null) orbit.enabled = true; }

        private IEnumerator JeepCheck()
        {
            var jeep = LegacyOfTemples.Gameplay.Vehicles.Jeep.Instance; var pc = FindAnyObjectByType<PlayerController>();
            Line($"\n## Jeep: present={(jeep != null)}");
            if (jeep == null || pc == null) yield break;
            pc.Teleport(jeep.DriverExitPoint, Quaternion.LookRotation(jeep.transform.position - jeep.DriverExitPoint));
            yield return new WaitForSecondsRealtime(1.0f);
            Line($"  prompt near the door: '{(pc.CurrentInteractable != null ? pc.CurrentInteractable.Prompt : "(none)")}'");
            yield return Shot("s3-jeep-parked.png");
            yield return Tap(Key.E); yield return new WaitForSecondsRealtime(2.2f);
            Line($"  after E: occupied={jeep.Occupied}, driven={jeep.Car.Driven}, player visible={pc.GetComponentInChildren<Renderer>()?.enabled}, seated pose={pc.GetComponent<LegacyOfTemples.Gameplay.Vehicles.SeatedPose>() != null}, player parent={(pc.transform.parent != null ? pc.transform.parent.name : "none")}");
            {   // close-up through the driver's window: the investigator must be visible in the right-hand seat
                var fc = Camera.main != null ? Camera.main.GetComponent<LegacyOfTemples.Gameplay.Vehicles.CarFollowCamera>() : null; bool was = fc != null && fc.enabled; if (fc != null) fc.enabled = false;
                var cam = Camera.main; Vector3 seat = jeep.transform.TransformPoint(jeep.DriverSeatLocal);
                cam.transform.position = seat + jeep.transform.right * Mathf.Sign(jeep.DriverSeatLocal.x) * 2.6f + jeep.transform.forward * 1.2f + Vector3.up * 0.55f; cam.transform.LookAt(seat + Vector3.up * 0.3f);
                yield return null; yield return Shot("s3-jeep-driver-seated.png");
                cam.transform.position = seat + jeep.transform.forward * 3.2f + Vector3.up * 0.9f; cam.transform.LookAt(seat + Vector3.up * 0.35f);
                yield return null; yield return Shot("s3-jeep-driver-front.png");
                if (fc != null) fc.enabled = was;
            }
            Vector3 start = jeep.transform.position;
            Hold(Key.W); yield return new WaitForSecondsRealtime(3.5f);
            yield return Shot("s3-jeep-driving.png"); Release();
            Line($"  after 3.5 s of W: speed={jeep.Car.CurrentSpeed * 3.6f:0} km/h, moved {Vector3.Distance(start, jeep.transform.position):0.0} m, wheel FL rpm={jeep.Car.frontLeftWheel.rpm:0}, tilt={Vector3.Angle(jeep.transform.up, Vector3.up):0} deg");
            Hold(Key.Space); yield return new WaitForSecondsRealtime(0.6f); yield return Shot("s3-jeep-braking.png"); yield return new WaitForSecondsRealtime(1.4f); Release();
            Line($"  after braking: speed={jeep.Car.CurrentSpeed * 3.6f:0} km/h");
            yield return Tap(Key.L); yield return new WaitForSecondsRealtime(0.4f); Line($"  headlights toggled: on={jeep.Lights.HeadLightsOn}");
            yield return Tap(Key.E);
            { var ys = new System.Collections.Generic.List<string>(); for (int k = 0; k < 16; k++) { yield return new WaitForSecondsRealtime(0.15f); ys.Add($"{pc.transform.position.y:0.00}/{jeep.transform.position.y:0.00}"); }
              Line($"  exit heights player/jeep every 0.15 s: {string.Join(" ", ys)}"); }
            Line($"  after E: occupied={jeep.Occupied}, player enabled={pc.enabled}"); yield return Shot("s3-jeep-got-out.png");
            MonoBehaviour orbitCam = null; foreach (var mb in FindObjectsByType<MonoBehaviour>()) if (mb != null && mb.GetType().Name == "OrbitCamera" && mb.enabled) { mb.enabled = false; orbitCam = mb; }   // the crash and damage shots place the camera themselves
            var wall = GameObject.Find("Wall_South_L");
            if (wall != null)
            {   // a hard crash into a wall must damage it (a bump must not)
                var rbj = jeep.GetComponent<Rigidbody>(); Vector3 wp = wall.GetComponent<Renderer>().bounds.center; wp.y = jeep.transform.position.y + 0.2f;
                rbj.position = wp + Vector3.forward * 16f; rbj.rotation = Quaternion.LookRotation(Vector3.back); jeep.transform.SetPositionAndRotation(rbj.position, rbj.rotation);
                rbj.linearVelocity = Vector3.back * 11f; rbj.angularVelocity = Vector3.zero;
                jeep.Car.autopilot = true; jeep.Car.autoThrottle = 1f; jeep.Car.autoBrake = false; jeep.Car.autoSteer = 0f;   // a driver flooring it into the wall
                float w = 0f; while (w < 4f && jeep.LastImpact < 2f) { w += Time.unscaledDeltaTime; yield return null; }
                jeep.Car.autopilot = false; yield return new WaitForSecondsRealtime(0.8f);
                Line($"  crash at 43 km/h into the courtyard wall: impact dv={jeep.LastImpact:0.0} m/s, state={jeep.StateName}, total damage={jeep.TotalDamage:0}, parts broken={jeep.PartsBroken}");
                var cam = Camera.main; if (cam != null) { cam.transform.position = jeep.transform.position + new Vector3(4f, 2.5f, 5f); cam.transform.LookAt(jeep.transform.position + Vector3.up); }
                yield return null; yield return Shot("s3-jeep-crash.png");
            }
            for (int k = jeep.DamageState + 1; k <= 3; k++)
            {
                float need = (k == 1 ? 20f : k == 2 ? 55f : 100f) - jeep.TotalDamage + 1f;
                jeep.ApplyDamage(new Vector3(k == 2 ? -0.9f : 0f, 0.9f, k == 3 ? -2.3f : 2.3f), need, jeep.transform.position + jeep.transform.forward * 2f, -jeep.transform.forward);
                yield return new WaitForSecondsRealtime(1.2f);
                var cam = Camera.main; if (cam != null) { cam.transform.position = jeep.transform.position + jeep.transform.forward * 5.5f + jeep.transform.right * 3.5f + Vector3.up * 2.4f; cam.transform.LookAt(jeep.transform.position + Vector3.up); }
                yield return null;
                Line($"  damage step: state={jeep.StateName}, total={jeep.TotalDamage:0}, parts broken={jeep.PartsBroken}");
                yield return Shot($"s3-jeep-state-{k}-{jeep.StateName}.png");
            }
            if (orbitCam != null) orbitCam.enabled = true;
            var stand = LegacyOfTemples.Gameplay.VillageLife.Instance != null ? LegacyOfTemples.Gameplay.VillageLife.Instance.TeaStallStand + Vector3.right * 2f : pc.transform.position;
            pc.Teleport(stand, Quaternion.LookRotation(Vector3.back)); yield return new WaitForSecondsRealtime(0.8f);
        }

        /// <summary>Stage 1 evidence: the five hints appear one at a time and each goes away when its action is done.</summary>
        private IEnumerator HintsCheck()
        {
            yield return new WaitForSecondsRealtime(1.5f);
            Line($"- Hints: step at start={HintTutorial.Step} of {HintTutorial.Count}, old tutorial active={Flag("TutorialManager", "isTutorialActive")}");
            var pc = FindAnyObjectByType<PlayerController>();
            for (int i = 0; i < HintTutorial.Count && HintTutorial.Step < HintTutorial.Count; i++)
            {
                int was = HintTutorial.Step; float w = 0f;
                while (!HintTutorial.Showing && w < 4f) { w += Time.unscaledDeltaTime; yield return null; }
                yield return Shot($"s1-hint-{was + 1}.png");
                if (was == 0) { Hold(Key.W); yield return new WaitForSecondsRealtime(1.3f); Release(); }
                else if (was == 1) { for (int k = 0; k < 40; k++) { if (Mouse.current != null) InputSystem.QueueDeltaStateEvent(Mouse.current.delta, new Vector2(14f, 0f)); VirtualInput.Look = new Vector2(10f, 0f); yield return null; } VirtualInput.Look = Vector2.zero; }
                else if (was == 2)
                {
                    var npc = FindAnyObjectByType<NPCDialogueTrigger>();
                    if (pc != null && npc != null) { pc.Teleport(npc.transform.position + npc.transform.forward * 1.3f, Quaternion.LookRotation(-npc.transform.forward)); yield return new WaitForSecondsRealtime(1.2f); }
                    yield return Tap(Key.E); yield return new WaitForSecondsRealtime(0.8f);
                    if (DialogueActive()) yield return FinishDialogue(null);
                }
                else if (was == 3) { yield return Tap(Key.J); yield return new WaitForSecondsRealtime(0.5f); yield return Tap(Key.J); }
                else { yield return Tap(Key.H); yield return new WaitForSecondsRealtime(0.5f); yield return Tap(Key.H); }
                yield return new WaitForSecondsRealtime(1.6f);
                Line($"  hint {was + 1}: step after action={HintTutorial.Step} => {(HintTutorial.Step > was ? "CLEARED" : "STILL SHOWING")}");
            }
            if (HintTutorial.Step < HintTutorial.Count) Line("  hints did not all clear");
        }

        private IEnumerator SkipIntro()
        {
            for (int i = 0; i < 3; i++)
            {
                yield return new WaitForSecondsRealtime(1.2f);
                foreach (var c in FindObjectsByType<OpeningCinematic>()) c.EndCinematic();
                if (UIManager.Instance != null) UIManager.Instance.DismissHowToPlay();
            }
        }

        private static bool DialogueActive() => Flag("UIManager", "isDialogueActive");

        private IEnumerator FinishDialogue(string shotName)
        {
            int guard = 0; bool shotTaken = false, choiceShot = false;
            while (DialogueActive() && guard++ < 60)
            {
                yield return new WaitForSecondsRealtime(0.9f);
                if (!shotTaken && shotName != null) { shotTaken = true; yield return Shot(shotName); }
                var choice = GameObject.Find("ChoiceBtn_1");
                if (choice != null && choice.activeInHierarchy)
                {
                    if (!choiceShot) { choiceShot = true; yield return Shot("choices-" + shotName); yield return UiAudit("Dialogue choices", Screen.width, Screen.height); }
                    yield return Tap(Key.Digit1);
                }
                else { yield return Tap(Key.Space); yield return new WaitForSecondsRealtime(0.15f); yield return Tap(Key.Space); }
            }
        }

        private readonly HashSet<string> rainShots = new HashSet<string>();
        private IEnumerator WaitLoaded() { float w = 0f; yield return null; while (SceneLoader.IsLoading && w < 30f) { w += Time.unscaledDeltaTime; yield return null; } }

        /// <summary>Captures every loading screen while it is up.</summary>
        private IEnumerator LoadingWatcher()
        {
            int n = 0;
            while (true)
            {
                while (!SceneLoader.IsLoading) yield return null;
                n++;
                if (n <= 3)
                {   // prove the bar moves: photograph it at about 30%, 60% and 100%
                    float w0 = 0f;
                    foreach (float mark in new[] { 0.3f, 0.6f, 0.995f })
                    {
                        while (SceneLoader.IsLoading && LoadingScreen.LastProgress < mark && w0 < 12f) { w0 += Time.unscaledDeltaTime; yield return null; }
                        Line($"  loading bar: {LoadingScreen.LastProgress * 100f:0}% at {w0:0.00} s (image {LoadingScreen.LastImage})");
                        yield return Shot($"s2-loading-{n}-{Mathf.RoundToInt(mark * 100f)}pct.png");
                    }
                }
                else yield return new WaitForSecondsRealtime(1.2f);
                Line($"  loading screen {n}: image={LoadingScreen.LastImage}, bar={LoadingScreen.LastProgress:0.00}");
                yield return Shot("v2-loading-" + n + "-" + LoadingScreen.LastImage + ".png");

                while (SceneLoader.IsLoading) yield return null;
            }
        }

        /// <summary>Plays each new section through its real actions where possible, with screenshots.</summary>
        private IEnumerator PlayMinigame(string id, StoryDialogueSystem story, PlayerController player)
        {
            var cd = ChapterDirector.Instance; var fd = FightDirector.Instance;
            Line("  minigame '" + id + "'");
            if (id == "reach_temple")
            {   // walk in through the courtyard gateway: the objective completes and the temple is "discovered"
                var gate = GameObject.Find("Gateway_TEMPLE_COURTYARD");
                if (gate != null) { player.Teleport(gate.transform.position + Vector3.back * 2.5f + Vector3.up * 0.1f, Quaternion.LookRotation(Vector3.back)); }
                float w = 0f; while (w < 2.5f && !ObjectiveBanner.Last.StartsWith("NEW")) { w += Time.unscaledDeltaTime; yield return null; }
                yield return new WaitForSecondsRealtime(1.0f); yield return Shot("s8-new-location-discovered.png");
                w = 0f; while (w < 8f && story.Current != null && story.Current.id == "obj_c0_reach") { w += 0.25f; yield return new WaitForSecondsRealtime(0.25f); }
                Line($"  reach temple: banner='{ObjectiveBanner.Last}', objective now {(story.Current != null ? story.Current.id : "end")}");
                yield break;
            }
            if (id == "chase_c1")
            {
                yield return new WaitForSecondsRealtime(1.5f);
                Hold(Key.W); float t = 0f;
                while (t < 3.5f) { t += Time.unscaledDeltaTime; yield return null; }
                Release();
                Line($"  chase: running={cd.ChaseRunning}, distance={cd.ChaseDistance:0.0} m");
                yield return Shot("v2-chase.png");
                cd.DebugEndChase();
            }
            else if (id == "timeline")
            {   // real keys: open with T, one wrong pick, then the right order (keys map to the shuffled list 2,4,0,3,1)
                yield return Tap(Key.T); yield return new WaitForSecondsRealtime(0.4f);
                Line($"  timeline: board open={ChapterDirector.TimelineOpen}");
                yield return Tap(Key.Digit1); yield return new WaitForSecondsRealtime(0.3f);
                yield return Shot("s4-timeline-wrong.png"); Line($"  timeline: wrong first pick -> step={ChapterDirector.TimelineStep}");
                foreach (var k in new[] { Key.Digit3, Key.Digit5, Key.Digit1, Key.Digit4 }) { yield return Tap(k); yield return new WaitForSecondsRealtime(0.25f); }
                yield return Shot("s4-timeline-four.png");
                yield return Tap(Key.Digit2); yield return new WaitForSecondsRealtime(1.0f);
                Line($"  timeline: after the right order -> step={ChapterDirector.TimelineStep}, banner='{ObjectiveBanner.Last}', objective now {story.Current?.id}");
            }
            else if (id == "lock")
            {   // walk to the case, press E, enter a wrong code, then the right one, all with real keys
                var box = GameObject.Find("StorageCase_Locked");
                if (box != null) { player.Teleport(box.transform.position + box.transform.forward * 1.6f, Quaternion.LookRotation(-box.transform.forward)); yield return new WaitForSecondsRealtime(0.8f); }
                Line($"  lock: case present={box != null}, prompt='{(player.CurrentInteractable != null ? player.CurrentInteractable.Prompt : "(none)")}'");
                yield return Tap(Key.E); yield return new WaitForSecondsRealtime(0.5f);
                Line($"  lock: panel open={ChapterDirector.LockOpen}");
                yield return Tap(Key.W); yield return Tap(Key.Enter); yield return new WaitForSecondsRealtime(0.4f);
                yield return Shot("s4-lock-wrong.png"); Line($"  lock: wrong code 1-0-0 -> still open={ChapterDirector.LockOpen}, objective={story.Current?.id}");
                yield return Tap(Key.D); for (int k = 0; k < 7; k++) yield return Tap(Key.W);
                yield return Tap(Key.D); for (int k = 0; k < 5; k++) yield return Tap(Key.S);
                yield return Shot("s4-lock-175.png"); yield return Tap(Key.Enter); yield return new WaitForSecondsRealtime(1.0f);
                yield return Shot("s4-lock-opened.png");
                Line($"  lock: code 1-7-5 -> panel open={ChapterDirector.LockOpen}, banner='{ObjectiveBanner.Last}', objective now {story.Current?.id}");
            }
            else if (id == "lamps")
            {
                cd.LightLamp(3); yield return new WaitForSecondsRealtime(0.4f);
                Line($"  lamps: wrong lamp first -> lit={cd.LampsLit}, penalty={(DawnClock.Instance != null ? DawnClock.Instance.LastPenalty : 0f):0.0} min");
                for (int i = 0; i < 5; i++) { cd.LightLamp(i); yield return new WaitForSecondsRealtime(0.35f); if (i == 2) yield return Shot("v2-lamps.png"); }
                Line($"  lamps: lit={cd.LampsLit}");
            }
            else if (id == "deduction")
            {
                cd.OpenBoard(); yield return new WaitForSecondsRealtime(0.6f);
                yield return Shot("v2-deduction-board.png");
                yield return UiAuditAllResolutions("Deduction board");
                cd.BoardAnswerPick(2); yield return new WaitForSecondsRealtime(0.4f);
                foreach (int a in new[] { 1, 0, 1 }) { cd.BoardAnswerPick(a); yield return new WaitForSecondsRealtime(0.4f); }
                Line($"  deduction: step={cd.BoardStep}");
            }
            else if (id == "traps")
            {
                int k = 0;
                foreach (var t in cd.Traps)
                {
                    cd.BeginDisarm(t, true); yield return new WaitForSecondsRealtime(0.5f);
                    if (k++ == 0) { yield return Shot("v2-trap-disarm.png"); yield return UiAudit("Trap ring", Screen.width, Screen.height); }
                    for (int i = 0; i < 3; i++) { cd.DisarmPress(0.5f); yield return new WaitForSecondsRealtime(0.2f); }
                }
                Line($"  traps: {cd.Traps.Count} disarmed, bells rung={cd.BellsRung}");
            }
            else if (id == "forensics")
            {
                var kit = ForensicKit.Instance;
                VirtualInput.Press("kit"); yield return new WaitForSecondsRealtime(0.6f);
                yield return Shot("v2-forensics-ruler.png");
                yield return UiAudit("Forensic kit", Screen.width, Screen.height);
                kit.SlideRuler(6.5f); kit.Confirm(); yield return new WaitForSecondsRealtime(0.3f);
                kit.SlideRuler(10f); kit.Confirm(); yield return new WaitForSecondsRealtime(0.3f);
                kit.Answer(1); yield return new WaitForSecondsRealtime(0.3f);
                kit.HoldLamp(2.1f); yield return new WaitForSecondsRealtime(0.3f);
                yield return Shot("v2-forensics-note.png");
                for (int pc = 0; pc < 4; pc++) { kit.Answer(pc); for (int r = 0; r < 4 && kit.PieceRotation(pc) != 0; r++) kit.RotatePiece(); }
                kit.Confirm();
                Line($"  forensics: task={kit.Task}/3");
            }
            else if (id == "stealth")
            {
                yield return new WaitForSecondsRealtime(2.5f);
                Hold(Key.C); yield return new WaitForSecondsRealtime(1.5f);
                fd.ThrowPebble(); yield return new WaitForSecondsRealtime(0.5f);
                Line($"  stealth: running={fd.StealthRunning}, hidden={fd.Hidden}, detection={fd.Detection:0.00}, spotted={fd.Spotted}");
                yield return Shot("v2-stealth.png");
                yield return UiAudit("Stealth HUD", Screen.width, Screen.height);
                Release();
            }
            else if (id == "fight")
            {
                float w = 0f;
                while (w < 25f && fd.Phase != 1) { w += 0.5f; if (DialogueActive()) yield return FinishDialogue("v2-fight-confront.png"); yield return new WaitForSecondsRealtime(0.5f); }
                yield return new WaitForSecondsRealtime(1.5f);
                Line($"  fight: phase={fd.Phase}, kishore turns={fd.KishoreTurns}, on-screen controls visible={(TouchControls.Instance != null && TouchControls.Instance.Visible)}, storm intensity={(LegacyOfTemples.World.WeatherManager.Instance != null ? LegacyOfTemples.World.WeatherManager.Instance.Intensity : 0f):0.00}");
                yield return Shot("v2-fight-phase1-hud.png");
                yield return UiAuditAllResolutions("Fight HUD with on-screen controls");
                // real actions through the on-screen buttons
                if (fd.Bhaskar != null) player.Teleport(fd.Bhaskar.transform.position - fd.Bhaskar.transform.forward * -1.6f, Quaternion.LookRotation(-fd.Bhaskar.transform.forward));
                yield return new WaitForSecondsRealtime(0.3f);
                float g0 = fd.Bhaskar != null ? fd.Bhaskar.Guard : 0f; int hits0 = fd.HitsLanded;
                for (int i = 0; i < 4; i++) { TouchControls.Instance.Click("attack"); yield return new WaitForSecondsRealtime(0.45f); }
                TouchControls.Instance.Click("heavy"); yield return new WaitForSecondsRealtime(0.9f);
                Line($"  fight: 4 ATTACK + 1 HEAVY button clicks -> hits landed={fd.HitsLanded - hits0}, Bhaskar guard {g0:0} -> {(fd.Bhaskar != null ? fd.Bhaskar.Guard : 0f):0}");
                yield return Shot("v2-fight-attack.png");
                TouchControls.Instance.Click("flash"); yield return new WaitForSecondsRealtime(0.3f);
                Line($"  fight: FLASH -> Bhaskar state={(fd.Bhaskar != null ? fd.Bhaskar.State.ToString() : "?")}");
                float h0 = fd.Health; yield return new WaitForSecondsRealtime(4f);
                Line($"  fight: after 4 s of his attacks health {h0:0} -> {fd.Health:0}, attacks started={(fd.Bhaskar != null ? fd.Bhaskar.AttacksStarted : 0)}");
                yield return Shot("v2-fight-telegraph.png");
                for (int ph = 1; ph <= 2 && fd.Bhaskar != null; ph++)
                {
                    fd.Bhaskar.TakeGuard(9999f); yield return new WaitForSecondsRealtime(0.4f);
                    yield return Shot("v2-fight-phase" + ph + "-staggered.png");
                    fd.Finisher(fd.Bhaskar);
                    w = 0f; while (w < 15f && fd.Phase != ph + 1) { w += 0.25f; yield return new WaitForSecondsRealtime(0.25f); }
                    yield return new WaitForSecondsRealtime(1.2f);
                    Line($"  fight: phase now {fd.Phase}, kishore_turned={StoryFlags.Get("kishore_turned")}");
                    Line($"  fight: Bhaskar height above player = {(fd.Bhaskar != null ? fd.Bhaskar.transform.position.y - player.transform.position.y : 0f):0.00} m");
                    yield return Shot("v2-fight-phase" + (ph + 1) + ".png");
                }
                yield return new WaitForSecondsRealtime(1.5f);
                fd.DebugGrapple(); yield return new WaitForSecondsRealtime(0.5f);
                yield return Shot("v2-fight-phase3-grapple.png");
                fd.GrappleTwist(0.5f);
                w = 0f; while (w < 15f && story.Current != null && story.Current.complete.id == "fight") { w += 0.5f; if (DialogueActive()) yield return FinishDialogue("v2-fight-after.png"); yield return new WaitForSecondsRealtime(0.5f); }
                if (DialogueActive()) yield return FinishDialogue("v2-fight-after.png");
                Line($"  fight: finished, objective now {(story.Current != null ? story.Current.id : "end")}, health={fd.Health:0}");
            }
        }

        private IEnumerator PlayThrough()
        {
            Line("\n## Play-through from New Game (bot uses the Guide target for every objective)");
            var newGame = GameObject.Find("Btn_NEW GAME");
            if (newGame == null) { Line("- New Game button NOT FOUND => BROKEN"); yield break; }
            StartCoroutine(LoadingWatcher());
            newGame.GetComponent<Button>().onClick.Invoke();
            yield return WaitLoaded();
            yield return IntroShots();
            yield return SkipIntro();
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-focusCheck") >= 0) { yield return FocusCheck(); yield break; }
            if (Walkthrough)
            {   // walkthrough capture: HUD panels and characters, then the whole V3 story with real key presses (windowed: the HUD is in the pictures)
                string auditPath = Path.Combine(output, "audit.md");
                yield return WalkthroughExtras();
                File.WriteAllText(auditPath, report.ToString());
                yield return V3StoryCheck(auditPath);
                yield return new WaitForSecondsRealtime(2f); yield return Shot("w-ending-final.png");
                File.WriteAllText(auditPath, report.ToString());
                if (string.IsNullOrEmpty(savedScores)) PlayerPrefs.DeleteKey("lot_v3_scores"); else PlayerPrefs.SetString("lot_v3_scores", savedScores); PlayerPrefs.Save();
                Debug.Log("[AuditBot] DONE " + output); yield return new WaitForSecondsRealtime(1f); Application.Quit(); yield break;
            }
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-voiceFocus") >= 0)
            {   // voice-over proof: movement, pause and the jeep, then the whole story; writes the trigger log
                string savedTable = PlayerPrefs.GetString("lot_v3_scores", "");
                StartCoroutine(VoiceOverlapWatch());
                yield return VoiceFocus();
                string auditPath = Path.Combine(output, "audit.md"); File.WriteAllText(auditPath, report.ToString());
                if (Array.IndexOf(Environment.GetCommandLineArgs(), "-voiceQuick") < 0)   // -voiceQuick: only the movement, pause and jeep part
                {
                    yield return V3StoryCheck(auditPath);
                    yield return new WaitForSecondsRealtime(9f);   // the ending narration
                }
                WriteVoiceLog(Path.Combine(output, "voice_trigger_log.txt"));
                File.WriteAllText(auditPath, report.ToString());
                if (savedTable == "") PlayerPrefs.DeleteKey("lot_v3_scores"); else PlayerPrefs.SetString("lot_v3_scores", savedTable); PlayerPrefs.Save();
                Debug.Log("[AuditBot] DONE " + output); yield return new WaitForSecondsRealtime(1f); Application.Quit(); yield break;
            }
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-voiceDemo") >= 0)
            {   // the scripted demo for Docs/QA/voice_demo.mov (frames and audio by FrameRecorder): walking, the jeep and the pot puzzle
                yield return VoiceDemo();
                Debug.Log("[AuditBot] DONE " + output); yield return new WaitForSecondsRealtime(1f); Application.Quit(); yield break;
            }
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-endingFocus") >= 0)
            {   // Task 4: finish the story quickly (one step per two frames), then check the ending id and the best-runs table
                string savedTable = PlayerPrefs.GetString("lot_v3_scores", "");   // test runs must not leave a row in the player's best-runs table
                var st = StoryDialogueSystem.Instance; int before = LegacyOfTemples.Gameplay.ScoreSystem.HighScores.Count; int guard = 0;
                yield return new WaitForSecondsRealtime(4f);
                while (st != null && !st.IsComplete && guard++ < 30) { st.DebugCompleteCurrent(); yield return null; yield return null; if (DialogueActive()) yield return FinishDialogue("t4-talk.png"); }
                float w = 0f; while (w < 20f && !st.IsComplete) { w += 0.5f; if (DialogueActive()) yield return FinishDialogue("t4-talk.png"); yield return new WaitForSecondsRealtime(0.5f); }
                yield return new WaitForSecondsRealtime(3f); yield return Shot("t4-ending.png");
                Line($"\n## Ending check: story complete={st.IsComplete}, clock {DawnClock.ClockText(DawnClock.Instance != null ? DawnClock.Instance.Minutes : 0f)}, late flag={StoryFlags.Get("late")}, ending id={DawnClock.EndingId()}, best-runs table {before} -> {LegacyOfTemples.Gameplay.ScoreSystem.HighScores.Count} rows");
                File.WriteAllText(Path.Combine(output, "audit.md"), report.ToString());
                if (savedTable == "") PlayerPrefs.DeleteKey("lot_v3_scores"); else PlayerPrefs.SetString("lot_v3_scores", savedTable); PlayerPrefs.Save();
                Debug.Log("[AuditBot] DONE " + output); yield return new WaitForSecondsRealtime(1f); Application.Quit(); yield break;
            }
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-speedFocus") >= 0)
            {   // Task 1 evidence: real measured speeds, auto-run, travel times, fast travel, F9 demo jump to the fight
                var pc = FindAnyObjectByType<PlayerController>(); var st = StoryDialogueSystem.Instance;
                yield return new WaitForSecondsRealtime(4.5f);   // let the reveal finish
                Line($"\n## Movement (speeds in the build: walk {pc.WalkSpeed} m/s, run {pc.RunSpeed} m/s)");
                foreach (var mode in new[] { "walk", "run" })
                {
                    var cam = Camera.main.transform; cam.rotation = Quaternion.LookRotation(Vector3.back);
                    pc.Teleport(LegacyOfTemples.World.Village.RoadPoint(LegacyOfTemples.World.Village.ZMax + 100f) + Vector3.up * 0.1f, Quaternion.LookRotation(Vector3.back));
                    yield return new WaitForSecondsRealtime(0.8f);
                    Vector3 a0 = pc.transform.position; if (mode == "run") Hold(Key.LeftShift, Key.W); else Hold(Key.W);
                    float t0 = Time.time; yield return new WaitForSecondsRealtime(1.0f); Vector3 a1 = pc.transform.position; float t1 = Time.time;
                    float travelled = 0f; Vector3 lastP = a1; float until = Time.realtimeSinceStartup + 3f;
                    while (Time.realtimeSinceStartup < until) { yield return null; travelled += Vector3.Distance(Flat(lastP), Flat(pc.transform.position)); lastP = pc.transform.position; }
                    Vector3 a2 = pc.transform.position; float t2 = Time.time;
                    if (mode == "run") yield return Shot("t1-running.png");
                    Release();
                    float ss = travelled / (t2 - t1);
                    Line($"  {mode}: steady speed {ss:0.00} m/s (measured over {t2 - t1:0.0} s), first second {Vector3.Distance(Flat(a0), Flat(a1)):0.0} m, anim speed x{pc.GetComponentInChildren<Animator>().speed:0.00}");
                }
                pc.Teleport(LegacyOfTemples.World.Village.RoadPoint(LegacyOfTemples.World.Village.ZMax + 100f) + Vector3.up * 0.1f, Quaternion.LookRotation(Vector3.back)); yield return new WaitForSecondsRealtime(0.6f);
                Vector3 c0 = pc.transform.position; yield return Tap(Key.CapsLock); yield return new WaitForSecondsRealtime(3f);
                Line($"  auto-run (Caps Lock, no keys held): on={pc.AutoRun}, moved {Vector3.Distance(Flat(c0), Flat(pc.transform.position)):0.0} m in 3 s");
                yield return Tap(Key.CapsLock); yield return new WaitForSecondsRealtime(0.3f);
                // straight-line distance from the arrival point (tea stall) to every place, and the time at old and new speeds
                var vlf = LegacyOfTemples.Gameplay.VillageLife.Instance; Vector3 home = vlf != null ? vlf.TeaStallStand : pc.transform.position;
                Line("\n| Place | Straight-line distance from the arrival (m) | Old walk 1.6 m/s | Old run 4.2 m/s | New walk 4.0 m/s | New run 7.5 m/s |\n|---|---|---|---|---|---|");
                for (int i = 0; i < V3Places.Count; i++) { var pl = V3Places.Get(i); if (!pl.ok) { Line($"| {pl.name} | not built | | | | |"); continue; } float dd = Vector3.Distance(Flat(home), Flat(pl.at)); Line($"| {pl.name} | {dd:0} | {dd / 1.6f:0} s | {dd / 4.2f:0} s | {dd / 4f:0} s | {dd / 7.5f:0} s |"); }
                // map fast travel: reach the storeroom step, open the map, press 3
                int guard = 0; while (st != null && st.Current != null && st.Current.id != "obj_v3_storeroom" && guard++ < 10) { st.DebugCompleteCurrent(); yield return null; yield return null; }
                yield return new WaitForSecondsRealtime(1f);
                yield return Tap(Key.M); yield return new WaitForSecondsRealtime(0.5f); yield return Shot("t1-map-fast-travel.png");
                float clock0 = DawnClock.Instance != null ? DawnClock.Instance.Minutes : 0f;
                yield return Tap(Key.Digit3); yield return new WaitForSecondsRealtime(1.0f);
                var sp = V3Places.Get(2);
                Line($"\n  map fast travel (M, 3): now {Vector3.Distance(Flat(pc.transform.position), Flat(sp.at)):0.0} m from the storeroom point, map open={TempleMapOverlay.Instance != null && TempleMapOverlay.Instance.IsOpen}, clock moved {(DawnClock.Instance != null ? DawnClock.Instance.Minutes - clock0 : 0f):0.0} min");
                yield return Shot("t1-after-travel.png");
                yield return Tap(Key.M); yield return new WaitForSecondsRealtime(0.3f); yield return Tap(Key.Digit8); yield return new WaitForSecondsRealtime(0.5f);
                Line($"  map: try the ruin court before the story reaches it -> still open={TempleMapOverlay.Instance.IsOpen}"); yield return Tap(Key.M); yield return new WaitForSecondsRealtime(0.3f);
                // F9 demo menu: straight to the final fight
                yield return Tap(Key.F9); yield return new WaitForSecondsRealtime(0.5f); yield return Shot("t1-demo-menu.png");
                Line($"  F9 demo menu open={DemoMenu.Open}");
                yield return Tap(Key.Digit8); float w = 0f;
                while (w < 30f && LegacyOfTemples.Gameplay.Brawl.Active == null) { w += 0.5f; yield return new WaitForSecondsRealtime(0.5f); }
                yield return new WaitForSecondsRealtime(1.5f); yield return Shot("t1-demo-fight.png");
                var jc = LegacyOfTemples.Gameplay.JungleChase.Instance;
                Line($"  F9 > 8 'ruin court (final fight)': objective {st?.Current?.id}, chase phase {(jc != null ? jc.Now.ToString() : "-")}, fight running={LegacyOfTemples.Gameplay.Brawl.Active != null} with {(LegacyOfTemples.Gameplay.Brawl.Active != null ? LegacyOfTemples.Gameplay.Brawl.Active.Enemies.Count : 0)} robbers, after {w:0.0} s");
                File.WriteAllText(Path.Combine(output, "audit.md"), report.ToString());
                Debug.Log("[AuditBot] DONE " + output); yield return new WaitForSecondsRealtime(1f); Application.Quit(); yield break;
            }
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-chaseFocus") >= 0)
            {   // Level 1 runner chase in the jeep: real E to get in; the bot steers through the car's autopilot (it cannot see the road)
                var st = StoryDialogueSystem.Instance; int guard = 0;
                var pc0 = FindAnyObjectByType<PlayerController>();
                if (pc0 != null && LegacyOfTemples.World.Village.GatePos != Vector3.zero) pc0.Teleport(LegacyOfTemples.World.Village.GatePos - Vector3.forward * 10f, Quaternion.LookRotation(Vector3.forward));   // start inside the courtyard, as in the story
                yield return new WaitForSecondsRealtime(1f);
                while (st != null && st.Current != null && st.Current.id != "obj_c1_chase" && guard++ < 10) { st.DebugCompleteCurrent(); yield return null; }
                yield return new WaitForSecondsRealtime(2.5f);
                var cd = ChapterDirector.Instance; var jp = LegacyOfTemples.Gameplay.Vehicles.Jeep.Instance; var pc = FindAnyObjectByType<PlayerController>();
                Line($"\n## Runner chase: objective {st?.Current?.id}, running={(cd != null && cd.ChaseRunning)}, jeep={jp != null}");
                yield return Shot("s5-chase-start.png");
                // run to the jeep on foot (about the time a player needs), then get in
                if (jp != null) { float run = Vector3.Distance(pc.transform.position, jp.DriverExitPoint) / 6.5f; Line($"  run to the jeep: {Vector3.Distance(pc.transform.position, jp.DriverExitPoint):0} m, waiting {run:0.0} s"); yield return new WaitForSecondsRealtime(run); pc.Teleport(jp.DriverExitPoint, Quaternion.LookRotation(jp.transform.position - jp.DriverExitPoint)); yield return new WaitForSecondsRealtime(0.8f); yield return Tap(Key.E); yield return new WaitForSecondsRealtime(2.4f); }
                Line($"  in the jeep={jp != null && jp.Occupied}, runner {(cd != null ? cd.ChaseDistance : -1f):0} m away");
                float w = 0f; bool shot = false;
                while (w < 40f && st.Current != null && st.Current.id == "obj_c1_chase")
                {
                    var r = GameObject.Find("Marker_Runner");
                    if (jp != null && r != null) { Vector3 to = r.transform.position - jp.transform.position; to.y = 0f; jp.Car.autopilot = true; jp.Car.autoSteer = Mathf.Clamp(Vector3.SignedAngle(Vector3.ProjectOnPlane(jp.transform.forward, Vector3.up), to, Vector3.up) / 25f, -1f, 1f); jp.Car.autoThrottle = to.magnitude > 14f ? 1f : 0.4f; jp.Car.autoBrake = false; }
                    if (!shot && jp != null && jp.Car.CurrentSpeed > 6f) { shot = true; yield return Shot("s5-chase-driving.png"); }
                    w += Time.unscaledDeltaTime; yield return null;
                }
                yield return new WaitForSecondsRealtime(2.5f); yield return Shot("s5-chase-caught.png");
                Line($"  after {w:0.0} s: objective now {st.Current?.id}, token on the ground={(GameObject.Find("Clue_Token17") != null)}, jeep occupied={jp != null && jp.Occupied}, failed={(DawnClock.Instance != null && DawnClock.Instance.OutOfTime)}");
                File.WriteAllText(Path.Combine(output, "audit.md"), report.ToString());
                Debug.Log("[AuditBot] DONE " + output); yield return new WaitForSecondsRealtime(1f); Application.Quit(); yield break;
            }
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-lockFocus") >= 0)
            {   // Level 2 combination lock only
                var st = StoryDialogueSystem.Instance; int guard = 0;
                while (st != null && st.Current != null && st.Current.id != "obj_c2_lock" && guard++ < 40) { st.DebugCompleteCurrent(); yield return null; }
                SceneLoader.Load(Constants.Sanctum); yield return WaitLoaded(); yield return new WaitForSecondsRealtime(3f); yield return SkipIntro();
                Line($"\n## Lock check: objective {st?.Current?.id}");
                yield return PlayMinigame("lock", st, FindAnyObjectByType<PlayerController>());
                guard = 0; while (st != null && st.Current != null && st.Current.id != "obj_c3_timeline" && guard++ < 40) { st.DebugCompleteCurrent(); yield return null; }
                SceneLoader.Load(Constants.ShrineCrime); yield return WaitLoaded(); yield return new WaitForSecondsRealtime(3f); yield return SkipIntro();
                Line($"\n## Timeline board: objective {st?.Current?.id}");
                yield return PlayMinigame("timeline", st, FindAnyObjectByType<PlayerController>());
                guard = 0; while (st != null && st.Current != null && st.Current.id != "obj_c4_pujari" && guard++ < 40) { st.DebugCompleteCurrent(); yield return null; }
                st.DebugCompleteCurrent(); yield return null;
                Line($"\n## PuzzleWall: after the scroll talk the objective is {st?.Current?.id} (Sun Wall step removed)");
                SceneLoader.Load(Constants.PuzzleWall); yield return WaitLoaded(); yield return new WaitForSecondsRealtime(4f); yield return SkipIntro();
                var wm2 = LegacyOfTemples.World.WeatherManager.Instance; var cpz = ChakraPuzzleController.Active;
                Line($"  rain intensity={(wm2 != null ? wm2.Intensity : -1f):0.00}, wall solved={(cpz != null && cpz.IsSolved)}, wall prompt radius={(cpz != null ? cpz.Radius : -1f):0.0}, sun={(RenderSettings.sun != null ? RenderSettings.sun.intensity : -1f):0.0}");
                yield return Shot("s4-puzzlewall-open.png");
                File.WriteAllText(Path.Combine(output, "audit.md"), report.ToString());
                Debug.Log("[AuditBot] DONE " + output); yield return new WaitForSecondsRealtime(1f); Application.Quit(); yield break;
            }
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-v3Zone2") >= 0)
            {   // V3 zone 2 only: play the break-in puzzle with real key presses and take a picture of every step
                yield return new WaitForSecondsRealtime(6f);
                yield return V3Zone2Check();
                File.WriteAllText(Path.Combine(output, "audit.md"), report.ToString());
                Debug.Log("[AuditBot] DONE " + output); yield return new WaitForSecondsRealtime(1f); Application.Quit(); yield break;
            }
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-v3Beauty") >= 0)
            {   // three framed pictures of every place, the temple, the bell and the tea stall (camera only, no HUD in windowless mode)
                yield return new WaitForSecondsRealtime(6f);
                var pcb = FindAnyObjectByType<PlayerController>(); pcb.Teleport(LegacyOfTemples.World.Village.GatePos + Vector3.back * 2f, Quaternion.LookRotation(Vector3.back)); yield return new WaitForSecondsRealtime(12f);
                yield return V3Beauty();
                File.WriteAllText(Path.Combine(output, "audit.md"), report.ToString());
                Debug.Log("[AuditBot] DONE " + output); yield return new WaitForSecondsRealtime(1f); Application.Quit(); yield break;
            }
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-v3Perf") >= 0)
            {   // frame time at every place, once per graphics preset
                yield return new WaitForSecondsRealtime(6f);
                var pc0 = FindAnyObjectByType<PlayerController>(); pc0.Teleport(LegacyOfTemples.World.Village.GatePos + Vector3.back * 2f, Quaternion.LookRotation(Vector3.back)); yield return new WaitForSecondsRealtime(10f);
                foreach (int preset in new[] { 2, 0 })
                {
                    GameOptions.Graphics = preset; Line($"\n## Preset {LegacyOfTemples.Gameplay.GraphicsPresets.Current.name}"); yield return new WaitForSecondsRealtime(1f);
                    yield return V3PerfAll();
                }
                GameOptions.Graphics = 2;
                File.WriteAllText(Path.Combine(output, "audit.md"), report.ToString());
                Debug.Log("[AuditBot] DONE " + output); yield return new WaitForSecondsRealtime(1f); Application.Quit(); yield break;
            }
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-v3Story") >= 0)
            {   // V3 main path start to finish: gate, priest, storeroom, vaults, stepwell, Sun Wall, return
                string auditPath = Path.Combine(output, "audit.md");
                yield return new WaitForSecondsRealtime(4f);
                yield return V3StoryCheck(auditPath);
                File.WriteAllText(auditPath, report.ToString());
                Debug.Log("[AuditBot] DONE " + output); yield return new WaitForSecondsRealtime(1f); Application.Quit(); yield break;
            }
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-v3Zones") >= 0)
            {   // V3 zones 2, 5, 4 and 3: play each puzzle with real key presses, a picture of every stage
                yield return new WaitForSecondsRealtime(6f);
                string auditPath = Path.Combine(output, "audit.md");
                yield return V3Zone2Check(); File.WriteAllText(auditPath, report.ToString());
                yield return V3Zone5Check(); File.WriteAllText(auditPath, report.ToString());
                yield return V3Zone4Check(); File.WriteAllText(auditPath, report.ToString());
                yield return V3Zone3Check(); File.WriteAllText(auditPath, report.ToString());
                Line($"\n## V3 run total: {LegacyOfTemples.Gameplay.ScoreSystem.Total} points, overall grade for 4 zones {LegacyOfTemples.Gameplay.ScoreSystem.OverallGrade(4)}");
                File.WriteAllText(Path.Combine(output, "audit.md"), report.ToString());
                Debug.Log("[AuditBot] DONE " + output); yield return new WaitForSecondsRealtime(1f); Application.Quit(); yield break;
            }
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-jeepFocus") >= 0)
            {   // Stage 2 check only: no hop at spawn (height log), seated visible driver, driving, damage
                yield return new WaitForSecondsRealtime(1.0f); yield return Shot("s3-temple-reveal.png"); Line($"\n## Reveal banner: '{ObjectiveBanner.Last}'");
                yield return new WaitForSecondsRealtime(4.5f); Line($"  after reveal: banner='{ObjectiveBanner.Last}', player enabled={FindAnyObjectByType<PlayerController>()?.enabled}");
                var hs = LegacyOfTemples.Gameplay.Vehicles.Jeep.SpawnHeights;
                Line($"\n## Jeep spawn height every 0.5 s (10 s): {string.Join(", ", hs.ConvertAll(h => h.ToString("0.000")))}  max-min={(hs.Count > 0 ? (Mathf.Max(hs.ToArray()) - Mathf.Min(hs.ToArray())) : -1f):0.000} m");
                yield return Shot("s3-jeep-after-intro.png");
                yield return JeepCheck();
                foreach (var lv in new[] { "Sanctum", "ShrineCrime", "PuzzleWall", "JunglePath" })
                {   // Stage 4: each level has its own look (and rain at the shrine)
                    UnityEngine.SceneManagement.SceneManager.LoadScene(lv); yield return new WaitForSecondsRealtime(5f);
                    var wm = FindAnyObjectByType<LegacyOfTemples.World.WeatherManager>();
                    Line($"- Level look {lv}: weather managers={FindObjectsByType<LegacyOfTemples.World.WeatherManager>().Length}, intensity={(wm != null ? wm.Intensity : -1f):0.00}, rain active={(wm != null && wm.RainActive)}, sun={(RenderSettings.sun != null ? RenderSettings.sun.intensity.ToString("0.0") + " at " + RenderSettings.sun.transform.eulerAngles.x.ToString("0") + " deg" : "none")}, fog={RenderSettings.fogDensity:0.0000}");
                    yield return Shot($"s4-look-{lv}.png");
                }
                File.WriteAllText(Path.Combine(output, "audit.md"), report.ToString());
                Debug.Log("[AuditBot] DONE " + output); yield return new WaitForSecondsRealtime(1f); Application.Quit(); yield break;
            }
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-stopAfterIntro") >= 0)
            {   // short run: the first objective, a few seconds of walking up the road, then stop
                yield return new WaitForSecondsRealtime(2.5f); yield return Shot("s7-first-objective.png");
                Hold(Key.W); yield return new WaitForSecondsRealtime(5f); Release(); yield return new WaitForSecondsRealtime(1f);
                yield return Shot("s7-walking-to-temple.png");
                yield return Tap(Key.Escape); yield return new WaitForSecondsRealtime(0.6f);
                foreach (var b in FindObjectsByType<Button>()) if (b.name == "Btn_CONTROLS") { b.onClick.Invoke(); break; }
                yield return new WaitForSecondsRealtime(0.6f); yield return Shot("s1-pause-controls.png"); yield return UiAudit("Pause controls", Screen.width, Screen.height);
                yield return Tap(Key.Escape); yield return new WaitForSecondsRealtime(0.4f); if (Time.timeScale == 0f) { yield return Tap(Key.Escape); yield return new WaitForSecondsRealtime(0.4f); }
                Line($"  after intro: objective={StoryDialogueSystem.Instance?.Current?.id}, hint step={HintTutorial.Step}");
                File.WriteAllText(Path.Combine(output, "audit.md"), report.ToString());
                Debug.Log("[AuditBot] DONE " + output); yield return new WaitForSecondsRealtime(1f); Application.Quit(); yield break;
            }
            yield return JeepCheck();

            // Tutorial: play it for a while with real keys, then skip the rest.
            yield return HintsCheck();
            if (TutorialManager.Instance != null)
            {
                bool active = Flag("TutorialManager", "isTutorialActive");
                yield return Shot("pt-01-tutorial.png");
                yield return UiAudit("Tutorial", Screen.width, Screen.height);
                foreach (var k in new[] { Key.Enter, Key.W, Key.LeftShift, Key.Space, Key.C, Key.E, Key.F, Key.J, Key.J, Key.M, Key.M, Key.H, Key.H })
                {
                    if (k == Key.W) { Hold(Key.W); yield return new WaitForSecondsRealtime(1.2f); Release(); }
                    else yield return Tap(k);
                    yield return new WaitForSecondsRealtime(0.8f);
                    var pcT = FindAnyObjectByType<PlayerController>();
                    if (pcT != null)
                    {
                        var cols = Physics.OverlapSphere(pcT.transform.position, 0.6f, ~0, QueryTriggerInteraction.Ignore);
                        var names = new StringBuilder();
                        foreach (var c in cols) if (!c.transform.IsChildOf(pcT.transform)) names.Append(c.name).Append(c.isTrigger ? "(T)" : "").Append(',');
                        Line($"  tutorial key {k}: player {pcT.transform.position.ToString("0.00")} near [{names}] timeScale={Time.timeScale}");
                    }
                }
                Line($"- Tutorial: active at start={active}, still active after 13 key steps={Flag("TutorialManager", "isTutorialActive")}");
                if (Flag("TutorialManager", "isTutorialActive")) TutorialManager.Instance.SkipTutorial();
                yield return new WaitForSecondsRealtime(0.5f);
            }

            var story = StoryDialogueSystem.Instance;
            if (story == null) { Line("- Story director missing => BROKEN"); yield break; }
            var guideShotScenes = new HashSet<string>();
            int fails = 0, step = 0;
            while (!story.IsComplete && fails < 6 && step < 60)
            {
                step++;
                yield return WaitLoaded();
                if (DialogueActive()) yield return FinishDialogue(null);
                var o = story.Current;
                if (o == null) break;
                string scene = SceneManager.GetActiveScene().name;
                var player = FindAnyObjectByType<PlayerController>();
                if (player == null) { yield return new WaitForSecondsRealtime(1f); fails++; continue; }

                if (rainShots.Add(scene))
                {
                    yield return new WaitForSecondsRealtime(4f);
                    var wm = LegacyOfTemples.World.WeatherManager.Instance;
                    Line($"  weather in {scene}: intensity={(wm != null ? wm.Intensity : -1f):0.00}, rain active={(wm != null && wm.RainActive)}, particles={(wm != null ? wm.ParticleCount : 0)}, interior={(wm != null && wm.Interior)}, clock={(DawnClock.Instance != null ? DawnClock.Instance.DawnIn : "?")}");
                    yield return Shot("v2-rain-" + scene + ".png");
                    if (scene == Constants.Courtyard) { yield return Shot("v2-dawn-clock.png"); yield return UiAuditAllResolutions("HUD with dawn clock"); }
                }
                if (guideShotScenes.Add(scene) && GuideSystem.Instance != null)
                {
                    if (GuideSystem.Instance.IsOpen) GuideSystem.Instance.Toggle();
                    yield return Tap(Key.H); yield return new WaitForSecondsRealtime(0.6f);
                    yield return Tap(Key.Digit1); yield return Tap(Key.Digit2); yield return Tap(Key.Digit3);
                    yield return new WaitForSecondsRealtime(0.3f);
                    yield return Shot($"pt-guide-{scene}.png");
                    if (scene == Constants.Courtyard) yield return UiAuditAllResolutions("Guide panel");
                    else yield return UiAudit("Guide panel " + scene, Screen.width, Screen.height);
                    yield return Tap(Key.Enter); yield return new WaitForSecondsRealtime(0.3f);
                    yield return Tap(Key.H); yield return new WaitForSecondsRealtime(1.0f);
                    bool way = GuideSystem.Instance.WayShown;
                    yield return Shot($"pt-guide-marker-{scene}.png");
                    Line($"- Guide in {scene}: panel opened with H, hints 1-3 revealed (level {GuideSystem.Instance.HintLevel}), Show me the way={way}");
                    if (scene == Constants.Courtyard)
                    {
                        var pc0 = FindAnyObjectByType<PlayerController>();
                        var ys = new StringBuilder();
                        Hold(Key.W);
                        for (int k = 0; k < 8; k++) { yield return new WaitForSecondsRealtime(0.2f); if (pc0 != null) ys.Append(pc0.transform.position.ToString("0.0")).Append(' '); }
                        Release();
                        string under = "nothing";
                        if (pc0 != null && Physics.Raycast(pc0.transform.position + Vector3.up * 0.5f, Vector3.down, out RaycastHit uh, 3f, ~0, QueryTriggerInteraction.Ignore)) under = PathOf(uh.collider.transform);
                        Line("  walk samples: " + ys + " standing on: " + under);
                        yield return Shot("pt-player-walking-Courtyard.png");
                        yield return KeyToggle("Journal", Key.J, "InvestigationJournal", "isOpen", "pt-journal.png");
                        yield return KeyToggle("Map", Key.M, "TempleMapOverlay", "isOpen", "pt-map.png");
                    }
                }

                if (UIManager.Instance != null) UIManager.Instance.DismissHowToPlay();
                if (Time.timeScale == 0f) { yield return Tap(Key.Escape); yield return new WaitForSecondsRealtime(0.3f); }
                player = FindAnyObjectByType<PlayerController>();
                if (player == null) { fails++; continue; }
                if (step == 1)
                {
                    var cam = Camera.main;
                    string hitName = "nothing";
                    if (cam != null && Physics.Raycast(cam.transform.position, cam.transform.forward, out RaycastHit ch, 6f, ~0, QueryTriggerInteraction.Ignore)) hitName = ch.collider.name + " at " + ch.distance.ToString("0.0") + "m";
                    Line($"  start: player={player.transform.position} yaw={player.transform.eulerAngles.y:0}, camera={(cam ? cam.transform.position.ToString() : "none")} yaw={(cam ? cam.transform.eulerAngles.y : 0):0}, camera looks at {hitName}");
                    yield return Shot("pt-start-view.png");
                    yield return Tap(Key.Escape); yield return new WaitForSecondsRealtime(0.6f);
                    foreach (var b in FindObjectsByType<Button>()) if (b.name == "Btn_CONTROLS") { b.onClick.Invoke(); break; }
                    yield return new WaitForSecondsRealtime(0.5f); yield return Shot("s1-pause-controls.png"); yield return UiAudit("Pause controls", Screen.width, Screen.height);
                    yield return Tap(Key.Escape); yield return new WaitForSecondsRealtime(0.5f);
                    if (Time.timeScale == 0f) { yield return Tap(Key.Escape); yield return new WaitForSecondsRealtime(0.4f); }
                    yield return VillageShots();
                }
                Transform target = story.ResolveTarget(player.transform.position);
                Line($"- [{story.ObjectiveIndex + 1}/{story.ObjectiveCount}] {o.id} \"{o.title}\" in {scene}: target={(target ? target.name : "NONE")}");
                if (o.complete.type == "minigame")
                {
                    int was = story.ObjectiveIndex;
                    yield return PlayMinigame(o.complete.id, story, player);
                    yield return new WaitForSecondsRealtime(1f);
                    if (story.ObjectiveIndex == was) { Line("  minigame '" + o.complete.id + "' did not complete by play; completed by the bot's debug hook"); story.DebugCompleteCurrent(); yield return new WaitForSecondsRealtime(0.5f); }
                    fails = 0; continue;
                }
                if (target == null) { fails++; yield return new WaitForSecondsRealtime(1f); continue; }

                Vector3 fwd = target.forward; fwd.y = 0; if (fwd.sqrMagnitude < 0.01f) fwd = Vector3.forward;
                if ((o.target ?? "") == "clues") { fwd = player.transform.position - target.position; fwd.y = 0; if (fwd.sqrMagnitude < 0.01f) fwd = Vector3.back; }
                Vector3 p = target.position + fwd.normalized * ((o.target ?? "") == "clues" ? 1.0f : 1.3f);
                if (Physics.Raycast(p + Vector3.up * 2f, Vector3.down, out RaycastHit hit, 5f, ~0, QueryTriggerInteraction.Ignore)) p.y = hit.point.y + 0.05f;
                player.Teleport(p, Quaternion.LookRotation(Vector3.ProjectOnPlane(target.position - p, Vector3.up).normalized + Vector3.forward * 0.0001f));
                yield return new WaitForSecondsRealtime(0.8f);
                int before = story.ObjectiveIndex;
                string t = o.target ?? "";
                if (o.id == "obj_c2_search") Line($"  flashlight before: on={Flashlight.On}, battery={Flashlight.Battery:0}");
                if (o.id == "obj_c2_search" && !Flashlight.On) { yield return Tap(Key.G); yield return new WaitForSecondsRealtime(0.5f); yield return Shot("pt-flashlight-Sanctum.png"); }
                if (t == "puzzle")
                {
                    yield return Tap(Key.E); yield return new WaitForSecondsRealtime(0.6f);
                    yield return Shot("pt-puzzle.png");
                    yield return UiAudit("Puzzle", Screen.width, Screen.height);
                    yield return Tap(Key.Digit2); yield return new WaitForSecondsRealtime(0.4f); // wrong order on purpose
                    string wrongMsg = ChakraPuzzleController.Active != null ? ChakraPuzzleController.Active.Message : "";
                    foreach (var k in new[] { Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4 }) { yield return Tap(k); yield return new WaitForSecondsRealtime(0.4f); }
                    yield return Shot("v2-sunwall-timer.png");
                    yield return Tap(Key.Enter);
                    yield return new WaitForSecondsRealtime(0.5f);
                    var pzc = ChakraPuzzleController.Active;
                    Line($"  puzzle: resets={(pzc != null ? pzc.Resets : -1)}, timer left={(pzc != null ? pzc.ResetLeft : -1f):0}s, cranking={(pzc != null && pzc.Cranking)}");
                    for (int ck = 0; ck < 90 && pzc != null && pzc.Cranking && !pzc.IsSolved; ck++) { yield return Tap(ck % 2 == 0 ? Key.A : Key.D); if (ck == 6) yield return Shot("v2-sunwall-crank.png"); }
                    yield return new WaitForSecondsRealtime(1.5f);
                    Line($"  puzzle: wrong-order message='{wrongMsg}', solved={ChakraPuzzleController.Active != null && ChakraPuzzleController.Active.IsSolved}");
                    yield return FinishDialogue(null);
                }
                else if (t == "clues")
                {
                    if (scene == Constants.Sanctum && !Flashlight.Usable) { Line("  flashlight was off before the clue: forcing it on (G did not hold)"); Flashlight.Set(true); Flashlight.AddBattery(100f); yield return null; }
                    yield return Tap(Key.F); yield return new WaitForSecondsRealtime(0.8f);
                    if (!Flag("ClueInspectable", "isInspecting")) { yield return Tap(Key.E); yield return new WaitForSecondsRealtime(0.8f); }
                    bool insp = Flag("ClueInspectable", "isInspecting");
                    yield return Shot($"pt-clue-{target.name}.png");
                    if (insp && step < 8) yield return UiAudit("Clue inspect", Screen.width, Screen.height);
                    yield return Tap(Key.Escape); yield return new WaitForSecondsRealtime(0.6f);
                    if (Time.timeScale == 0f) { yield return Tap(Key.Escape); yield return new WaitForSecondsRealtime(0.4f); }
                    Line($"  clue {target.name}: inspecting={insp}, found={story.ClueCount}");
                    if (!insp) Line($"    target at {target.position}, active={target.gameObject.activeInHierarchy}, player at {player.transform.position}, prompt='{(player.CurrentInteractable != null ? player.CurrentInteractable.Prompt : "(none)")}', combat={CombatState.InCombat}, gate={InputGate.GameplayBlocked}");
                }
                else
                {
                    if (o.id == "obj_enter_hall") yield return Shot("pt-temple-entrance.png");
                    var npcTrig = target.GetComponent<NPCDialogueTrigger>();
                    Line($"  before E: prompt='{(player.CurrentInteractable != null ? player.CurrentInteractable.Prompt : "(none)")}', distance={Vector3.Distance(player.transform.position, target.position):0.0}m, npcBusy={(npcTrig != null && npcTrig.IsDialogueActive)}, playerEnabled={player.enabled}");
                    yield return Tap(Key.E); yield return new WaitForSecondsRealtime(1.2f);
                    if (DialogueActive())
                    {
                        if (o.id == "obj_c1_pujari") yield return UiAuditAllResolutions("Dialogue");
                        yield return new WaitForSecondsRealtime(0.4f);
                        Line($"  voice playing: {(VoicePlayer.Instance != null ? VoicePlayer.Instance.LastPlayed : "none")}");
                        yield return FinishDialogue($"pt-dialogue-{o.id}.png");
                    }
                    if (t.StartsWith("exit:"))
                    {
                        if (o.id == "obj_enter_hall") Line("  (temple entrance screenshot taken before entering)");
                        yield return new WaitForSecondsRealtime(0.5f);
                        yield return WaitLoaded();
                        yield return new WaitForSecondsRealtime(1.5f);
                        yield return SkipIntro();
                        Line($"  exit: now in {SceneManager.GetActiveScene().name}");
                        yield return Shot($"pt-scene-{SceneManager.GetActiveScene().name}.png");
                    }
                }
                yield return new WaitForSecondsRealtime(0.6f);
                if (story.ObjectiveIndex == before) { fails++; Line("  (objective did not advance)"); }
                else fails = 0;
                if (story.Current != null && story.Current.id == "obj_return" && SceneManager.GetActiveScene().name != Constants.Courtyard)
                {
                    float wr = 0f;
                    while (wr < 25f && (SceneManager.GetActiveScene().name != Constants.Courtyard || SceneLoader.IsLoading)) { wr += Time.unscaledDeltaTime; if (DialogueActive()) yield return FinishDialogue(null); yield return null; }
                    yield return new WaitForSecondsRealtime(2f);
                    yield return SkipIntro();
                    yield return Shot("v2-first-light-courtyard.png");
                    Line($"  returned at sunrise: scene={SceneManager.GetActiveScene().name}");
                }
            }
            yield return new WaitForSecondsRealtime(2.5f);
            bool endingShown = GameLevelManager.Instance != null && GameLevelManager.Instance.IsEndingActive;
            yield return Shot("pt-ending.png");
            yield return Shot("v2-ending.png");
            Line($"- Ending id={DawnClock.EndingId()}, clock={DawnClock.ClockText(DawnClock.Instance != null ? DawnClock.Instance.Minutes : 0f)}, trust_meera={StoryFlags.Get("trust_meera")}, kishore_turned={StoryFlags.Get("kishore_turned")}, loading screens shown={LoadingScreen.LoadsShown}");
            yield return UiAuditAllResolutions("Ending");
            Line($"- Story complete={story.IsComplete}, ending shown={endingShown}, score={story.Score}, time={story.Elapsed:0}s, hints={story.HintsUsed} => {(story.IsComplete && endingShown ? "WORKS" : "BROKEN")}");
            yield return Tap(Key.Enter);
            yield return new WaitForSecondsRealtime(3f);
            Line("- After ending, scene=" + SceneManager.GetActiveScene().name);
            File.WriteAllText(Path.Combine(output, "audit.md"), report.ToString());
        }

        // ------------------------------------------------------------------ ground / jump test
        private IEnumerator GroundJumpTest(string scene)
        {
            var pc = FindAnyObjectByType<PlayerController>();
            if (pc == null) { Line("- GROUND TEST " + scene + ": no player => BROKEN"); yield break; }
            float ground = pc.transform.position.y;
            float minRel = 99f, maxRel = -99f; int jumps = 0, landed = 0; bool airborne = false; float t = 0f, nextJump = 1f;
            Key[] dirs = { Key.W, Key.A, Key.S, Key.D };
            int di = 0; float nextTurn = 0f;
            bool jumpShot = false;
            while (t < 30f)
            {
                if (t >= nextTurn) { di = (di + 1) % 4; Hold(dirs[di]); nextTurn = t + 2.5f; }
                if (t >= nextJump && jumps < 20) { StartCoroutine(Tap(Key.Space)); jumps++; nextJump = t + 1.4f; }
                yield return null;
                t += Time.unscaledDeltaTime;
                pc = FindAnyObjectByType<PlayerController>();
                if (pc == null) break;
                float g = ground;
                if (Physics.Raycast(pc.transform.position + Vector3.up * 0.5f, Vector3.down, out RaycastHit h, 3f, ~0, QueryTriggerInteraction.Ignore) && !h.collider.transform.IsChildOf(pc.transform)) g = h.point.y;
                float rel = pc.transform.position.y - g;
                minRel = Mathf.Min(minRel, pc.transform.position.y - ground); maxRel = Mathf.Max(maxRel, pc.transform.position.y - ground);
                if (rel > 0.15f) airborne = true;
                else if (airborne) { airborne = false; landed++; }
                if (!jumpShot && rel > 0.3f && scene == Constants.Courtyard) { jumpShot = true; yield return Shot("ground-jump-Courtyard.png"); }
            }
            Release();
            bool ok = minRel > -0.1f && maxRel < 1.0f && landed >= Mathf.Min(10, jumps / 2);
            Line($"- GROUND TEST {scene}: 30 s walk + {jumps} jumps, feet height vs spawn floor min {minRel:0.00} max {maxRel:0.00} m, landings {landed} => {(ok ? "PASS" : "FAIL")}");
            yield return Shot($"ground-{scene}.png");
        }

        // ------------------------------------------------------------------ census
        private void Census()
        {
            var counts = new Dictionary<string, int>();
            foreach (var a in FindObjectsByType<Animator>())
            {
                string root = a.transform.root.name;
                counts[root] = counts.TryGetValue(root, out int n) ? n + 1 : 1;
            }
            var sb = new StringBuilder("Animated roots: ");
            foreach (var kv in counts) sb.Append(kv.Key).Append(" x").Append(kv.Value).Append("; ");
            Line(sb.ToString());

            int priests = 0;
            foreach (var r in FindObjectsByType<SkinnedMeshRenderer>())
                if (r.transform.root.name.IndexOf("Priest", StringComparison.OrdinalIgnoreCase) >= 0 && r.enabled && r.gameObject.activeInHierarchy) priests++;
            var priestRoots = new HashSet<Transform>();
            foreach (var r in FindObjectsByType<SkinnedMeshRenderer>())
                if (r.transform.root.name.IndexOf("Priest", StringComparison.OrdinalIgnoreCase) >= 0 && r.gameObject.activeInHierarchy) priestRoots.Add(r.transform.root);
            Line("Visible priest roots: " + priestRoots.Count + " (skinned renderers " + priests + ")");
            foreach (var t in FindObjectsByType<Transform>())
            {
                string n = t.name.ToLowerInvariant();
                if (t.gameObject.activeInHierarchy && (n.Contains("placeholder") || n.Contains("capsule") || n == "cube" || n == "sphere"))
                    Line("Placeholder-like object: " + PathOf(t));
            }
        }

        private static string PathOf(Transform t)
        {
            string p = t.name;
            while (t.parent != null) { t = t.parent; p = t.name + "/" + p; }
            return p;
        }

        // ------------------------------------------------------------------ input injection
        private static Vector3 Flat(Vector3 v) { v.y = 0f; return v; }

        // ------------------------------------------------------------------ voice-over proof
        int voiceFrames, overlapFrames;
        private IEnumerator VoiceOverlapWatch()
        {
            while (true)
            {
                var vm = VoiceManager.Instance;
                if (vm != null) { bool a = vm.Source2D.isPlaying, b = vm.Source3D.isPlaying; if (a || b || VoiceManager.AnyPlaying) voiceFrames++; if (a && b) overlapFrames++; }
                yield return null;
            }
        }

        private IEnumerator VoiceFocus()
        {
            var pc = FindAnyObjectByType<PlayerController>(); var vm = VoiceManager.Instance;
            Line($"\n## Voice focus (frequency {GameOptions.VoiceFrequencyNames[GameOptions.VoiceFrequency]}, subtitles {(GameOptions.Subtitles ? "on" : "off")})");
            if (pc == null || vm == null) { Line("  no player or no voice manager"); yield break; }
            yield return new WaitForSecondsRealtime(6f);
            // 1. walk and run down the approach road (W moves where the camera looks, so the camera is put behind the player and the bot steers along the road)
            var orbit = FindAnyObjectByType<OrbitCamera>(); float z0 = LegacyOfTemples.World.Village.ZMax + 100f;
            pc.Teleport(LegacyOfTemples.World.Village.RoadPoint(z0) + Vector3.up * 0.1f, Quaternion.LookRotation(Vector3.back)); if (orbit != null) orbit.SetTarget(pc.transform);
            yield return new WaitForSecondsRealtime(1f);
            yield return WalkRoad(pc, orbit, 2f, false);
            yield return Tap(Key.Space); yield return WalkRoad(pc, orbit, 1f, false);   // a jump while walking
            yield return WalkRoad(pc, orbit, 10.6f, true); Release(); yield return new WaitForSecondsRealtime(1.5f);
            Line($"  walked 3 s with a jump, ran 10.6 s down the road ({z0 - pc.transform.position.z:0} m): voice lines played so far {VoiceManager.Played}");
            // pause while a line is playing: the voice must hold and then carry on
            VoiceManager.ResetCooldownsForTests(); string id = VoiceManager.Fire("fight_start"); yield return new WaitForSecondsRealtime(0.3f);
            yield return Tap(Key.Escape); yield return new WaitForSecondsRealtime(0.6f);
            bool heldWhilePaused = vm.Current != null && !vm.Source2D.isPlaying; float before = vm.Source2D.time;
            yield return new WaitForSecondsRealtime(0.8f); bool frozen = Mathf.Approximately(before, vm.Source2D.time);
            yield return Tap(Key.Escape); yield return new WaitForSecondsRealtime(0.3f); if (Time.timeScale == 0f) { yield return Tap(Key.Escape); yield return new WaitForSecondsRealtime(0.3f); }
            Line($"  pause test: line {id}, held in the pause menu={heldWhilePaused} (position frozen={frozen}), still the current line after resume={(vm.Current != null && vm.Current.id == id)}");
            yield return new WaitForSecondsRealtime(3f);
            // 2. the first goal (reach the temple) before the jeep: near the gate that moment takes the player out of the jeep
            pc.Teleport(LegacyOfTemples.World.Village.GatePos + Vector3.back * 2f, Quaternion.LookRotation(Vector3.back)); yield return new WaitForSecondsRealtime(8f);
            Line($"  arrived at the temple: objective {V3Obj()}");
            // a real fall (a teleport snaps to the ground, so it cannot fake one): stand on the courtyard wall and walk off its inner side
            var wr = GameObject.Find("Wall_South_R");
            if (wr != null)
            {
                Bounds b = wr.GetComponent<Renderer>().bounds;
                pc.Teleport(new Vector3(b.center.x, b.max.y + 0.4f, b.center.z), Quaternion.LookRotation(Vector3.back)); if (orbit != null) orbit.SetTarget(pc.transform);
                yield return new WaitForSecondsRealtime(1.2f); float stood = pc.transform.position.y;
                Hold(Key.W); yield return new WaitForSecondsRealtime(2.5f); Release(); yield return new WaitForSecondsRealtime(2f);
                Line($"  walked off the courtyard wall (wall {b.size.y:0.0} m high, {b.size.z:0.0} m thick): stood at y {stood:0.0}, landed at y {pc.transform.position.y:0.0}");
            }
            // 3. the jeep (bot staging: parked on the open road north of the village, facing the village): approach, get in, drive off fast, handbrake
            var j = LegacyOfTemples.Gameplay.Vehicles.Jeep.Instance;
            if (j == null) { Line("  no jeep"); yield break; }
            yield return ParkJeepOnRoad(j, LegacyOfTemples.World.Village.ZMax + 125f);
            Vector3 door = j.DriverExitPoint, away = (door - j.transform.position); away.y = 0f;
            pc.Teleport(door + away.normalized * 6f, Quaternion.LookRotation(-away)); yield return new WaitForSecondsRealtime(0.8f);
            pc.Teleport(door, Quaternion.LookRotation(j.transform.position - door)); yield return new WaitForSecondsRealtime(1.5f);
            yield return Tap(Key.E); yield return new WaitForSecondsRealtime(3f);
            Line($"  in the jeep: {j.Occupied}, driven {j.Car.Driven}");
            float w = 0f; while (w < 11f && j.Car.CurrentSpeed < j.Car.maxForwardSpeed * 0.97f && j.transform.position.z > LegacyOfTemples.World.Village.ZMax + 32f) { SteerRoad(j, true); w += Time.unscaledDeltaTime; yield return null; }
            float top = j.Car.CurrentSpeed; for (float k = 0f; k < 0.6f; k += Time.unscaledDeltaTime) { SteerRoad(j, true); yield return null; }
            Line($"  drove {w:0.0} s down the road: {top * 3.6f:0} km/h");
            held.Clear(); held.Add(Key.Space); Push(); yield return new WaitForSecondsRealtime(3f); Release();   // handbrake at speed, then held to a stop
            Line($"  after the handbrake: {j.Car.CurrentSpeed * 3.6f:0} km/h");
            // 4. crashes (the jeep test's spot outside the courtyard wall; the oak there takes the hit): a light one, then a hard one (Wrecked, still drivable for the story's chase);
            //    then the jeep nose-to-wall inside the courtyard, pressing on (stuck) without a crash
            var wall = GameObject.Find("Wall_South_L");
            if (wall != null)
            {
                foreach (var (speed, throttle) in new[] { (6.5f, 0.3f), (11f, 1f) })
                {
                    var rb = j.GetComponent<Rigidbody>(); Vector3 wp = wall.GetComponent<Renderer>().bounds.center; wp.y = j.transform.position.y + 0.2f;
                    rb.position = wp + Vector3.forward * 16f; rb.rotation = Quaternion.LookRotation(Vector3.back); j.transform.SetPositionAndRotation(rb.position, rb.rotation);
                    rb.linearVelocity = Vector3.back * speed; rb.angularVelocity = Vector3.zero; float dmg0 = j.TotalDamage;
                    j.Car.autopilot = true; j.Car.autoThrottle = throttle; j.Car.autoBrake = false; j.Car.autoSteer = 0f;
                    float c = 0f; while (c < 4f && j.TotalDamage <= dmg0) { c += Time.unscaledDeltaTime; yield return null; }
                    j.Car.autopilot = false; yield return new WaitForSecondsRealtime(2.5f);
                    Line($"  crash at {speed * 3.6f:0} km/h: impact {j.LastImpact:0.0} m/s, state {j.StateName}, damage {j.TotalDamage:0}");
                }
                {
                    var rb = j.GetComponent<Rigidbody>(); Bounds wb = wall.GetComponent<Renderer>().bounds;
                    Vector3 p = new Vector3(wb.center.x, j.transform.position.y + 0.2f, wb.min.z - 2.7f);   // the inner face; the jeep is about 4.2 m long
                    rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero; rb.position = p; rb.rotation = Quaternion.LookRotation(Vector3.forward); j.transform.SetPositionAndRotation(p, rb.rotation);
                    yield return new WaitForSecondsRealtime(1.2f); float dmg0 = j.TotalDamage;
                    Hold(Key.W); yield return new WaitForSecondsRealtime(4.5f); Release();
                    Line($"  pressing against the inside of the wall for 4.5 s: {j.Car.CurrentSpeed * 3.6f:0} km/h, new damage {j.TotalDamage - dmg0:0}, state {j.StateName}");
                }
            }
            float t0 = 0f; while (t0 < 4f && j.Car.CurrentSpeed > 2.5f) { t0 += Time.unscaledDeltaTime; yield return null; }
            yield return Tap(Key.E); yield return new WaitForSecondsRealtime(3f);
            Line($"  out of the jeep: {!j.Occupied}; voice lines played so far {VoiceManager.Played}");
        }

        /// <summary>Holds W (and Shift to run) for the given time, turning the player and the camera toward the road a few metres ahead.</summary>
        private IEnumerator WalkRoad(PlayerController pc, OrbitCamera orbit, float seconds, bool run)
        {
            if (run) Hold(Key.LeftShift, Key.W); else Hold(Key.W);
            float lane = Mathf.Clamp(pc.transform.position.x - LegacyOfTemples.World.Village.RoadPoint(pc.transform.position.z).x, -2.6f, 2.6f);   // keep to his side of the road
            for (float t = 0f, next = 0f; t < seconds; t += Time.deltaTime)
            {
                if (t >= next)
                {
                    next = t + 0.4f; Vector3 aim = LegacyOfTemples.World.Village.RoadPoint(pc.transform.position.z - 12f) + Vector3.right * lane - pc.transform.position; aim.y = 0f;
                    if (aim.sqrMagnitude > 0.01f) { pc.transform.rotation = Quaternion.LookRotation(aim); if (orbit != null) orbit.SetTarget(pc.transform); }
                }
                yield return null;
            }
        }
        /// <summary>Bot staging: puts the jeep on the approach road at this z, facing the village (south), and lets it settle on its wheels.</summary>
        private IEnumerator ParkJeepOnRoad(LegacyOfTemples.Gameplay.Vehicles.Jeep j, float z)
        {
            var rb = j.GetComponent<Rigidbody>();
            Vector3 p = LegacyOfTemples.World.Village.RoadPoint(z) + Vector3.up * 0.6f, d = LegacyOfTemples.World.Village.RoadPoint(z - 12f) - LegacyOfTemples.World.Village.RoadPoint(z); d.y = 0f;
            rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero; rb.position = p; rb.rotation = Quaternion.LookRotation(d); j.transform.SetPositionAndRotation(p, rb.rotation);
            yield return new WaitForSecondsRealtime(1.5f);
        }
        /// <summary>Holds W (when asked) and A or D to keep the jeep on the approach road, driving south.</summary>
        private void SteerRoad(LegacyOfTemples.Gameplay.Vehicles.Jeep j, bool gas)
        {
            Vector3 aim = LegacyOfTemples.World.Village.RoadPoint(j.transform.position.z - 16f) - j.transform.position; aim.y = 0f;
            float ang = Vector3.SignedAngle(Vector3.ProjectOnPlane(j.transform.forward, Vector3.up), aim, Vector3.up);
            held.Clear(); if (gas) held.Add(Key.W); if (ang > 3f) held.Add(Key.D); else if (ang < -3f) held.Add(Key.A); Push();
        }

        private void WriteVoiceLog(string path)
        {
            var log = VoiceManager.Log; var sb = new StringBuilder();
            var fired = new SortedDictionary<string, int>(); var played = new SortedDictionary<string, List<string>>(); var reasons = new SortedDictionary<string, SortedDictionary<string, int>>();
            var lastPlay = new Dictionary<string, float>(); float minGap = 1e9f; string minGapLine = ""; int missing = 0, storyPlayed = 0;
            foreach (var r in log)
            {
                if (r.trigger.StartsWith("story:")) { if (r.result == "played") storyPlayed++; continue; }
                if (r.result == "dequeued" || r.result == "cut" || r.result == "stopped" || r.result == "expired" || r.result == "dropped") { }
                else fired[r.trigger] = (fired.TryGetValue(r.trigger, out var n) ? n : 0) + 1;
                if (r.result == "played") { if (!played.TryGetValue(r.trigger, out var l)) played[r.trigger] = l = new List<string>(); l.Add(r.line); if (lastPlay.TryGetValue(r.line, out var lt) && r.time - lt < minGap) { minGap = r.time - lt; minGapLine = r.line; } lastPlay[r.line] = r.time; }
                else { if (!reasons.TryGetValue(r.trigger, out var d)) reasons[r.trigger] = d = new SortedDictionary<string, int>(); d[r.result] = (d.TryGetValue(r.result, out var c) ? c : 0) + 1; }
                if (r.reason == "clip missing") missing++;
            }
            var db = VoiceLibrary.Db; var all = new SortedSet<string>(); if (db != null) foreach (var l in db.lines) if (l.trigger != "story") all.Add(l.trigger);
            int playedLines = 0; foreach (var kv in played) playedLines += kv.Value.Count;
            sb.AppendLine($"Voice trigger log - {DateTime.Now:yyyy-MM-dd HH:mm}, built game, frequency {GameOptions.VoiceFrequencyNames[GameOptions.VoiceFrequency]}, subtitles {(GameOptions.Subtitles ? "on" : "off")}");
            sb.AppendLine($"Event lines played: {playedLines} ({played.Count} of {all.Count} triggers spoke at least once). Story lines played through the same queue: {storyPlayed}.");
            sb.AppendLine($"Frames with a voice: {voiceFrames}; frames with two voices at once: {overlapFrames}. Missing clips: {missing}.");
            sb.AppendLine($"Shortest gap between two plays of the same line: {(minGap < 1e8f ? $"{minGap:0} s ({minGapLine})" : "no line played twice")}.");
            sb.AppendLine(); sb.AppendLine("Per trigger: fired / lines played / other results");
            foreach (var t in all)
            {
                fired.TryGetValue(t, out int f); played.TryGetValue(t, out var pl); reasons.TryGetValue(t, out var rs);
                string other = rs == null ? "" : string.Join(", ", System.Linq.Enumerable.Select(rs, x => $"{x.Key} {x.Value}"));
                sb.AppendLine($"  {t,-18} fired {f,3}  played {(pl != null ? pl.Count : 0),2}  {(pl != null ? string.Join(" ", pl) : "-")}  {(other.Length > 0 ? "[" + other + "]" : "")}");
            }
            sb.AppendLine(); sb.AppendLine("Every decision (time is seconds since the game started):"); sb.Append(VoiceManager.LogText());
            File.WriteAllText(path, sb.ToString());
            Line($"\n## Voice log: {playedLines} event lines, {storyPlayed} story lines, overlap frames {overlapFrames}, missing clips {missing} -> {path}");
        }

        private IEnumerator VoiceDemo()
        {
            // Waits here are in game time (WaitForSeconds): with -recordFrames the game runs at exactly 24 frames per game second, so the video keeps the real pace of the voices.
            var pc = FindAnyObjectByType<PlayerController>(); var j = LegacyOfTemples.Gameplay.Vehicles.Jeep.Instance; var orbit = FindAnyObjectByType<OrbitCamera>();
            int freqBefore = GameOptions.VoiceFrequency; GameOptions.VoiceFrequency = 2;   // High, so the short demo hears more of the event lines (stated in the video notes)
            // the player's saved Master volume is 0 %: raise it for the recording only, put it back at the end
            float masterBefore = PlayerPrefs.GetFloat("Volume_Master", 1f), settingBefore = PlayerPrefs.GetFloat("Settings_MasterVol", 1f);
            AudioManager.Instance?.SetMasterVolume(1f);
            yield return new WaitForSecondsRealtime(5.5f);
            var notes = new StringBuilder(); System.Action<string> note = s => { notes.AppendLine($"{VoiceManager.Now - startT:0.0}\t{s}"); };
            // staging before the recording starts (not in the video): the story moved on to the storeroom step (its spoken lines are left to finish),
            // the jeep parked at the north end of the approach road, the investigator in the storeroom
            var st = StoryDialogueSystem.Instance; int guard = 0;
            while (st != null && st.Current != null && st.Current.id != "obj_v3_storeroom" && guard++ < 10) { st.DebugCompleteCurrent(); yield return null; yield return null; }
            for (float t = 0f; t < 25f && (st.BarkShowing || VoiceManager.AnyPlaying || UIManager.Instance.IsDialogueActive); t += 0.25f) { if (DialogueActive()) yield return FinishDialogue(null); yield return new WaitForSecondsRealtime(0.25f); }
            if (j != null) yield return ParkJeepOnRoad(j, LegacyOfTemples.World.Village.ZMax + 125f);
            V3Places.TeleportTo(2); yield return new WaitForSecondsRealtime(3f);
            var z = BreakInPuzzle.Instance;
            FrameRecorder.BotReady = true; yield return null; startT = VoiceManager.Now;
            note($"recording starts; objective {V3Obj()}; voice lines High");
            yield return new WaitForSeconds(1.5f);
            // 1. the storeroom: the three pot pieces onto the plinth, then the lantern on the stain
            if (z != null && pc != null)
            {
                var r = z.transform;
                for (int i = 0; i < z.Shards.Length; i++)
                {
                    var sh = z.Shards[i]; if (sh == null) continue; Vector3 sp = sh.transform.position;   // the same standing spots as the storeroom test
                    note($"pick shard {i + 1}"); yield return DemoUse(pc, orbit, sp + r.forward * 0.9f, sp); yield return new WaitForSeconds(0.8f);
                    note($"place shard {i + 1}"); yield return DemoUse(pc, orbit, z.Plinth.position + r.forward * 1.3f, z.Plinth.position); yield return new WaitForSeconds(2.2f);
                }
                yield return new WaitForSeconds(2.5f);
                Vector3 lp = z.Lantern.transform.position;
                note("pick lantern"); yield return DemoUse(pc, orbit, lp + r.forward * 1.0f, lp); yield return new WaitForSeconds(0.8f);
                Vector3 stand = z.Stain.position + r.forward * 0.6f - r.right * -0.25f; stand.y = pc.transform.position.y;
                note("place lantern on the stain"); yield return DemoUse(pc, orbit, stand, z.Stain.position - r.forward * 3f); yield return new WaitForSeconds(5f);
            }
            // 2. the jeep (cut to the approach road): in, engine, down the road fast, handbrake, stop, out
            if (j != null && pc != null)
            {
                Vector3 door = j.DriverExitPoint; pc.Teleport(door, Quaternion.LookRotation(j.transform.position - door)); if (orbit != null) orbit.SetTarget(pc.transform);
                note("at the jeep"); yield return new WaitForSeconds(1.5f);
                note("get in (E)"); yield return Tap(Key.E); yield return new WaitForSeconds(3.2f);
                note("drive (W, steering along the road)"); float w = 0f; while (w < 11f && j.Car.CurrentSpeed < j.Car.maxForwardSpeed * 0.97f && j.transform.position.z > LegacyOfTemples.World.Village.ZMax + 34f) { SteerRoad(j, true); w += Time.deltaTime; yield return null; }
                for (float k = 0f; k < 0.5f; k += Time.deltaTime) { SteerRoad(j, true); yield return null; }
                note($"handbrake (Space) at {j.Car.CurrentSpeed * 3.6f:0} km/h"); held.Clear(); held.Add(Key.Space); Push();
                for (float k = 0f; k < 4f && j.Car.CurrentSpeed > 0.5f; k += Time.deltaTime) yield return null;
                Release(); yield return new WaitForSeconds(1f);
                note("get out (E)"); yield return Tap(Key.E); yield return new WaitForSeconds(3.5f);
            }
            // 3. on foot down the road towards the village: walk, then run (in his own lane, beside the jeep)
            if (orbit != null) orbit.SetTarget(pc.transform);
            note("walk (W)"); yield return WalkRoad(pc, orbit, 3f, false);
            note("run (Shift+W)"); yield return WalkRoad(pc, orbit, 11f, true); Release();
            yield return new WaitForSeconds(4f);
            note("end"); FrameRecorder.StopRequested = true;
            float wait = 0f; while (FrameRecorder.Recording && !FrameRecorder.Finished && wait < 30f) { wait += Time.unscaledDeltaTime; yield return null; }   // the audio file is written when the recording ends
            yield return new WaitForSecondsRealtime(2f);   // the last frames are saved on worker threads
            GameOptions.VoiceFrequency = freqBefore;
            AudioManager.Instance?.SetMasterVolume(masterBefore); PlayerPrefs.SetFloat("Settings_MasterVol", settingBefore); PlayerPrefs.Save();
            note($"master volume {masterBefore:0.##} raised to 1 for the recording and restored");
            File.WriteAllText(Path.Combine(output, "voice_demo_log.txt"), "Bot steps (seconds into the video):\n" + notes + "\nVoice decisions (voice clock; the video starts at " + $"{startT:0.0}" + "):\n" + VoiceManager.LogText());
        }
        float startT;
        /// <summary>Demo version of V3Use: stand there, camera behind the player, press E (game-time waits).</summary>
        private IEnumerator DemoUse(PlayerController pc, OrbitCamera orbit, Vector3 stand, Vector3 look)
        {
            Vector3 d = look - stand; d.y = 0f;
            pc.Teleport(stand, Quaternion.LookRotation(d.sqrMagnitude > 0.001f ? d : Vector3.forward)); if (orbit != null) orbit.SetTarget(pc.transform);
            yield return new WaitForSeconds(0.6f);
            yield return Tap(Key.E); yield return new WaitForSeconds(0.5f);
        }
        private void Hold(params Key[] keys) { held.Clear(); foreach (var k in keys) held.Add(k); Push(); }
        private void Release() { held.Clear(); Push(); }
        private void Push()
        {
            if (Keyboard.current == null) InputSystem.AddDevice<Keyboard>();
            var keys = new Key[held.Count];
            held.CopyTo(keys);
            InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState(keys));
        }
        /// <summary>Lifts the camera above the compound wall to photograph the village around the temple.</summary>
        private IEnumerator VillageShots()
        {
            var cam = Camera.main; if (cam == null) yield break;
            var orbits = new List<MonoBehaviour>();
            foreach (var mb in FindObjectsByType<MonoBehaviour>()) if (mb != null && mb.GetType().Name == "OrbitCamera" && mb.enabled) { mb.enabled = false; orbits.Add(mb); }
            Vector3 keepP = cam.transform.position; Quaternion keepR = cam.transform.rotation;
            var views = new[] { (new Vector3(0f, 7f, 0f), new Vector3(1f, -0.12f, 0.25f), "east"), (new Vector3(0f, 7f, 0f), new Vector3(-0.3f, -0.12f, 1f), "south"), (new Vector3(34f, 2.2f, 4f), new Vector3(0.25f, 0.02f, 1f), "lane"), (new Vector3(0f, 2.4f, 20f), new Vector3(0f, 0.08f, 1f), "gate"), (new Vector3(-6f, 2.6f, 34.5f), new Vector3(-1f, 0.06f, 0f), "hall-gateway"), (new Vector3(0f, 2.6f, 8f), new Vector3(0.12f, 0.02f, -1f), "guardians") };
            foreach (var v in views)
            {
                cam.transform.position = v.Item1; cam.transform.rotation = Quaternion.LookRotation(v.Item2.normalized);
                yield return null; yield return null;
                yield return Shot("v3-village-" + v.Item3 + ".png");
            }
            Line($"  village: houses built={LegacyOfTemples.World.Village.Houses}, villagers={LegacyOfTemples.Gameplay.VillageLife.NpcCount}");
            Vector3 vc = LegacyOfTemples.World.Village.Centre; float vz = LegacyOfTemples.World.Village.ZMax;
            var lifeShots = new[] { (LegacyOfTemples.World.Village.RoadStart + new Vector3(-3f, 2.2f, -4f), Vector3.back + Vector3.down * 0.05f, "road-entrance"),
                (new Vector3(vc.x - 1f, 2.4f, vz + 30f), new Vector3(-0.8f, -0.12f, -0.6f), "tea-stall"),
                (new Vector3(vc.x + 1f, 2.4f, vz + 10f), new Vector3(0.5f, 0.02f, 0.85f), "shops-poles") };
            foreach (var v in lifeShots)
            {
                cam.transform.position = v.Item1; cam.transform.rotation = Quaternion.LookRotation(v.Item2.normalized);
                yield return null; yield return null; yield return Shot("s4-village-" + v.Item3 + ".png");
            }
            var site = LegacyOfTemples.Gameplay.TempleSite.Instance;
            if (site != null && site.Temple != null)
            {
                var fb = LegacyOfTemples.Gameplay.TempleSite.Footprint;
                Line($"  temple: {LegacyOfTemples.Gameplay.TempleSite.Triangles} triangles, footprint {fb.size.x:0} x {fb.size.z:0} m, height {fb.size.y:0} m");
                Vector3 aim = site.TowerTop == Vector3.zero ? fb.center : Vector3.Lerp(fb.center, site.TowerTop, 0.5f);
                foreach (var v in new[] { (new Vector3(fb.center.x + 20f, fb.max.y * 0.6f, fb.max.z + 70f), "approach"), (new Vector3(fb.center.x - 90f, fb.max.y + 25f, fb.max.z + 40f), "aerial"), (new Vector3(fb.center.x + 6f, 1.8f, fb.max.z + 6f), "gate-low") })
                {
                    cam.transform.position = v.Item1; cam.transform.rotation = Quaternion.LookRotation(aim - v.Item1);
                    yield return null; yield return null; yield return Shot("s1-temple-" + v.Item2 + ".png");
                }
                cam.transform.position = LegacyOfTemples.World.Village.GatePos + new Vector3(0f, 3f, 8f); cam.transform.rotation = Quaternion.LookRotation(Vector3.back + Vector3.up * 0.15f);
                float ft = 0f; for (int k = 0; k < 90; k++) { yield return null; ft += Time.unscaledDeltaTime; }
                site.Temple.gameObject.SetActive(false); float fn = 0f; for (int k = 0; k < 90; k++) { yield return null; fn += Time.unscaledDeltaTime; }
                site.Temple.gameObject.SetActive(true);
                Line($"  frame time looking toward the temple: {ft / 90f * 1000f:0.0} ms with it, {fn / 90f * 1000f:0.0} ms without it");
            }

            cam.transform.position = keepP; cam.transform.rotation = keepR;
            foreach (var mb in orbits) mb.enabled = true;
        }

        private IEnumerator Tap(Key k)
        {
            var keep = new HashSet<Key>(held);
            held.Add(k); Push();
            yield return null; yield return null;
            held.Clear(); foreach (var x in keep) held.Add(x); Push();
            yield return null;
        }

        // ------------------------------------------------------------------ animation
        private IEnumerator AnimationAudit(string scene)
        {
            var player = FindAnyObjectByType<PlayerController>();
            foreach (var a in FindObjectsByType<Animator>())
            {
                bool isPlayer = player != null && a.transform.IsChildOf(player.transform);
                string who = a.transform.root.name;
                Line("\n### Animator: " + who + (isPlayer ? " (player)" : ""));
                Line("controller=" + (a.runtimeAnimatorController ? a.runtimeAnimatorController.name : "NONE") +
                     ", avatar=" + (a.avatar ? a.avatar.name : "NONE") + ", isHuman=" + a.isHuman +
                     ", avatarValid=" + (a.avatar && a.avatar.isValid) + ", applyRootMotion=" + a.applyRootMotion);
                if (a.runtimeAnimatorController != null)
                {
                    var clips = new List<string>();
                    foreach (var c in a.runtimeAnimatorController.animationClips) if (c && !clips.Contains(c.name)) clips.Add(c.name);
                    Line("clips: " + string.Join(", ", clips));
                }
                if (scene != Constants.Courtyard && isPlayer) continue; // player measured once in Courtyard

                if (isPlayer)
                {
                    yield return Phase(a, "idle", new Key[0], 1.5f);
                    yield return Shot(scene + "-player-idle.png");
                    yield return Phase(a, "walk", new[] { Key.W }, 2.5f);
                    yield return Shot(scene + "-player-walk.png");
                    yield return Phase(a, "run", new[] { Key.W, Key.LeftShift }, 2.0f);
                    yield return Phase(a, "crouch", new[] { Key.W, Key.C }, 2.0f);
                    yield return Shot(scene + "-player-crouch.png");
                    Hold(); yield return new WaitForSecondsRealtime(0.5f);
                    StartCoroutine(Tap(Key.Space));
                    yield return Phase(a, "jump", new Key[0], 1.2f);
                    Release();
                }
                else
                {
                    yield return Phase(a, "idle", null, 1.5f);
                    if (HasParam(a, "IsTalking"))
                    {
                        a.SetBool("IsTalking", true);
                        yield return Phase(a, "talk(IsTalking=true)", null, 1.5f);
                        a.SetBool("IsTalking", false);
                    }
                    else Line("talk: NO IsTalking parameter");
                    foreach (string p in new[] { "IsMoving", "IsStealth" })
                        if (HasParam(a, p)) { a.SetBool(p, true); yield return Phase(a, "move(" + p + "=true)", null, 1.5f); a.SetBool(p, false); }
                    yield return Shot(scene + "-" + who + ".png");
                }
            }
        }

        private static bool HasParam(Animator a, string name)
        {
            foreach (var p in a.parameters) if (p.name == name) return true;
            return false;
        }

        private static string ClipNow(Animator a)
        {
            if (!a.isActiveAndEnabled || a.runtimeAnimatorController == null) return "(disabled)";
            var info = a.GetCurrentAnimatorClipInfo(0);
            if (info.Length == 0) return "(no clip)";
            float best = -1; string n = "";
            foreach (var i in info) if (i.weight > best) { best = i.weight; n = i.clip.name; }
            return n;
        }

        private static Transform Bone(Animator a, HumanBodyBones hb, params string[] names)
        {
            if (a.isHuman) { var t = a.GetBoneTransform(hb); if (t) return t; }
            foreach (var t in a.GetComponentsInChildren<Transform>())
                foreach (string n in names)
                    if (t.name.Replace("_", "").Replace(".", "").Replace(" ", "").IndexOf(n, StringComparison.OrdinalIgnoreCase) >= 0) return t;
            return null;
        }

        private IEnumerator Phase(Animator a, string label, Key[] keys, float seconds)
        {
            if (keys != null) Hold(keys);
            yield return new WaitForSecondsRealtime(0.4f); // settle
            Transform root = a.transform;
            Transform mover = a.GetComponentInParent<CharacterController>() ? a.GetComponentInParent<CharacterController>().transform : root;
            Transform lf = Bone(a, HumanBodyBones.LeftFoot, "LeftFoot", "footL", "Lfoot", "FootLeft");
            Transform rf = Bone(a, HumanBodyBones.RightFoot, "RightFoot", "footR", "Rfoot", "FootRight");
            Transform hips = Bone(a, HumanBodyBones.Hips, "Hips", "pelvis", "hip");
            Transform lua = Bone(a, HumanBodyBones.LeftUpperArm, "LeftArm", "upperarmL", "LUpperArm", "shoulderL");
            Transform lla = Bone(a, HumanBodyBones.LeftLowerArm, "LeftForeArm", "forearmL", "lowerarmL");

            var states = new Dictionary<string, int>();
            var lfSpeed = new List<float>(); var rfSpeed = new List<float>();
            float maxHipDrift = 0, minFootAbove = 99, maxFootAbove = -99, moved = 0, tposeFrames = 0, frames = 0;
            Vector3 lfPrev = lf ? lf.position : Vector3.zero, rfPrev = rf ? rf.position : Vector3.zero, mPrev = mover.position;
            float t = 0; string paramsSeen = "";
            while (t < seconds)
            {
                yield return null;
                float dt = Mathf.Max(Time.deltaTime, 1e-4f);
                t += Time.unscaledDeltaTime; frames++;
                string c = ClipNow(a);
                states[c] = states.TryGetValue(c, out int n) ? n + 1 : 1;
                moved += Vector3.ProjectOnPlane(mover.position - mPrev, Vector3.up).magnitude; mPrev = mover.position;
                float ground = mover.position.y;
                if (Physics.Raycast(mover.position + Vector3.up * 1f, Vector3.down, out RaycastHit hit, 3f, ~0, QueryTriggerInteraction.Ignore))
                    if (!hit.collider.transform.IsChildOf(mover)) ground = hit.point.y;
                if (lf && rf)
                {
                    lfSpeed.Add(Vector3.ProjectOnPlane(lf.position - lfPrev, Vector3.up).magnitude / dt);
                    rfSpeed.Add(Vector3.ProjectOnPlane(rf.position - rfPrev, Vector3.up).magnitude / dt);
                    lfPrev = lf.position; rfPrev = rf.position;
                    float low = Mathf.Min(lf.position.y, rf.position.y) - ground;
                    minFootAbove = Mathf.Min(minFootAbove, low); maxFootAbove = Mathf.Max(maxFootAbove, low);
                }
                if (hips) maxHipDrift = Mathf.Max(maxHipDrift, Vector3.ProjectOnPlane(hips.position - mover.position, Vector3.up).magnitude);
                if (lua && lla)
                {
                    float ang = Vector3.Angle(lla.position - lua.position, Vector3.down);
                    if (ang > 70f) tposeFrames++;
                }
                if (frames == 10)
                    paramsSeen = "Speed=" + Get(a, "Speed") + " Grounded=" + Get(a, "Grounded") + " Crouch=" + Get(a, "Crouch") + " IsMoving=" + Get(a, "IsMoving");
            }
            var sb = new StringBuilder();
            foreach (var kv in states) sb.Append(kv.Key).Append(" ").Append((100f * kv.Value / Mathf.Max(1, frames)).ToString("0")).Append("%, ");
            float plantL = Percentile(lfSpeed, 0.1f), plantR = Percentile(rfSpeed, 0.1f);
            float bodySpeed = moved / Mathf.Max(seconds, 0.01f);
            string verdict = "";
            if (states.ContainsKey("(no clip)") || states.ContainsKey("(disabled)")) verdict += " NO-CLIP";
            if (tposeFrames > frames * 0.5f) verdict += " ARMS-RAISED/T-POSE?";
            if (bodySpeed > 0.5f && (plantL > 0.35f || plantR > 0.35f)) verdict += " FOOT-SLIDE";
            if (maxHipDrift > 0.3f) verdict += " HIP-DRIFT";
            if (minFootAbove < -0.06f && minFootAbove > -50) verdict += " SINKING";
            if (minFootAbove > 0.15f && minFootAbove < 50 && label != "jump") verdict += " FLOATING";
            Line($"- {label}: clips[{sb}] {paramsSeen} bodySpeed={bodySpeed:0.00}m/s footPlant10%L/R={plantL:0.00}/{plantR:0.00}m/s hipDrift={maxHipDrift:0.00}m footAboveGround={minFootAbove:0.00}..{maxFootAbove:0.00}m armsRaised={(100f * tposeFrames / Mathf.Max(1, frames)):0}% bones(L/R/hip/arm)={(lf ? 1 : 0)}{(rf ? 1 : 0)}{(hips ? 1 : 0)}{(lua ? 1 : 0)} =>{(verdict == "" ? " OK" : verdict)}");
            if (keys != null) Release();
        }

        private static string Get(Animator a, string p)
        {
            foreach (var x in a.parameters)
            {
                if (x.name != p) continue;
                switch (x.type)
                {
                    case AnimatorControllerParameterType.Float: return a.GetFloat(p).ToString("0.00");
                    case AnimatorControllerParameterType.Bool: return a.GetBool(p) ? "T" : "F";
                    case AnimatorControllerParameterType.Int: return a.GetInteger(p).ToString();
                    default: return "trig";
                }
            }
            return "-";
        }

        private static float Percentile(List<float> v, float q)
        {
            if (v.Count == 0) return -1;
            var s = new List<float>(v); s.Sort();
            return s[Mathf.Clamp((int)(q * s.Count), 0, s.Count - 1)];
        }

        // ------------------------------------------------------------------ functions
        private static bool Flag(string typeName, string field)
        {
            foreach (var mb in FindObjectsByType<MonoBehaviour>())
            {
                if (mb == null || mb.GetType().Name != typeName) continue;
                var f = mb.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                if (f != null && f.FieldType == typeof(bool) && (bool)f.GetValue(mb)) return true;
            }
            return false;
        }
        private static bool Exists(string typeName)
        {
            foreach (var mb in FindObjectsByType<MonoBehaviour>()) if (mb != null && mb.GetType().Name == typeName) return true;
            return false;
        }

        private IEnumerator KeyToggle(string label, Key key, string type, string field, string shot)
        {
            if (!Exists(type)) { Line($"- {label} [{key}]: MISSING ({type} not in scene)"); yield break; }
            bool before = Flag(type, field);
            yield return Tap(key);
            yield return new WaitForSecondsRealtime(0.6f);
            bool after = Flag(type, field);
            if (shot != null) yield return Shot(shot);
            yield return UiAudit(label, Screen.width, Screen.height);
            yield return Tap(key);
            yield return new WaitForSecondsRealtime(0.4f);
            bool closed = !Flag(type, field);
            Line($"- {label} [{key}]: open {before}->{after}, closes on 2nd press={closed} => {(after && !before ? (closed ? "WORKS" : "PARTIAL (does not close)") : "BROKEN (did not open)")}");
            if (!closed) { Release(); yield return Tap(Key.Escape); yield return new WaitForSecondsRealtime(0.3f); }
        }

        private IEnumerator FunctionAudit()
        {
            Line("\n### Function test (Courtyard, injected key presses)");
            yield return KeyToggle("Journal", Key.J, "InvestigationJournal", "isOpen", "journal.png");
            yield return KeyToggle("Map", Key.M, "TempleMapOverlay", "isOpen", "map.png");
            yield return KeyToggle("Guide", Key.H, "GuideSystem", "<IsOpen>k__BackingField", "help.png");
            bool helpIntro = Flag("GameIntroOverlay", "isInstructionsOpen");
            Line("- H also toggles GameIntroOverlay instructions: " + helpIntro);
            yield return KeyToggle("Photo mode", Key.P, "UIManager", "isPhotoMode", "photomode.png");

            float ts = Time.timeScale;
            yield return Tap(Key.Escape); yield return new WaitForSecondsRealtime(0.5f);
            bool paused = Time.timeScale == 0f;
            yield return Shot("pause.png");
            yield return UiAudit("Pause", Screen.width, Screen.height);
            yield return Tap(Key.Escape); yield return new WaitForSecondsRealtime(0.5f);
            Line($"- Pause [Esc]: timeScale {ts}->{(paused ? 0 : Time.timeScale)} closes={(Time.timeScale > 0)} => {(paused ? "WORKS" : "BROKEN")}");

            // Dialogue: teleport player in front of each NPC and press E
            var player = FindAnyObjectByType<PlayerController>();
            foreach (var npc in FindObjectsByType<NPCDialogueTrigger>())
            {
                if (player == null || npc == null) break;
                string npcName = npc.name;
                string sceneBefore = SceneManager.GetActiveScene().name;
                Vector3 p = npc.transform.position + npc.transform.forward * 1.4f;
                player.Teleport(p, Quaternion.LookRotation(npc.transform.position - p));
                yield return new WaitForSecondsRealtime(0.6f);
                string prompt = player.CurrentInteractable != null ? player.CurrentInteractable.Prompt : "(none)";
                yield return Tap(Key.E);
                yield return new WaitForSecondsRealtime(1.5f);
                bool dlg = Flag("UIManager", "isDialogueActive");
                yield return Shot("dialogue-" + npcName + ".png");
                yield return UiAudit("Dialogue " + npcName, Screen.width, Screen.height);
                int advances = 0;
                bool choiceSeen = false;
                while (Flag("UIManager", "isDialogueActive") && advances < 30)
                {
                    yield return Tap(Key.Space); yield return new WaitForSecondsRealtime(0.35f);
                    yield return Tap(Key.Space); yield return new WaitForSecondsRealtime(0.35f);
                    if (!choiceSeen && GameObject.Find("ChoiceButton_0") != null) { choiceSeen = true; yield return Shot("dialogue-choices.png"); }
                    yield return Tap(Key.Digit1); yield return new WaitForSecondsRealtime(0.5f);
                    advances++;
                    if (SceneManager.GetActiveScene().name != sceneBefore) break;
                }
                string sceneAfter = SceneManager.GetActiveScene().name;
                Line($"- Talk [E] to {npcName}: prompt='{prompt}', dialogueOpened={dlg}, finished={(!Flag("UIManager", "isDialogueActive"))} after {advances} advance rounds => {(dlg ? "WORKS" : "BROKEN")}");
                if (sceneAfter != sceneBefore)
                {
                    Line($"  BUG: pressing [1] to pick a dialogue choice also switched scene {sceneBefore} -> {sceneAfter} (WorldDevTools dev hotkey).");
                    yield return SceneManager.LoadSceneAsync(sceneBefore);
                    yield return new WaitForSecondsRealtime(2f);
                    foreach (var c in FindObjectsByType<OpeningCinematic>()) c.EndCinematic();
                    player = FindAnyObjectByType<PlayerController>();
                    break;
                }
            }

            // Clue inspect F
            var clues = FindObjectsByType<ClueInspectable>();
            Line("- Clues in Courtyard: " + clues.Length);
            if (clues.Length > 0 && player != null)
            {
                var c = clues[0];
                Vector3 p = c.transform.position + Vector3.back * 1.2f; p.y = player.transform.position.y;
                player.Teleport(p, Quaternion.LookRotation(Vector3.ProjectOnPlane(c.transform.position - p, Vector3.up)));
                yield return new WaitForSecondsRealtime(0.6f);
                yield return Tap(Key.F); yield return new WaitForSecondsRealtime(0.8f);
                bool insp = Flag("ClueInspectable", "isInspecting");
                if (!insp) { yield return Tap(Key.E); yield return new WaitForSecondsRealtime(0.8f); insp = Flag("ClueInspectable", "isInspecting"); Line("  (F did nothing; tried E)"); }
                yield return Shot("clue-inspect.png");
                Line($"- Clue inspect [F/E] on {c.name}: inspecting={insp} => {(insp ? "WORKS" : "BROKEN")}");
                yield return Tap(Key.Escape); yield return new WaitForSecondsRealtime(0.4f);
            }
            bool saved = PlayerPrefs.HasKey("Story_Objective");
            Line("- Save/Load: Story_Objective saved=" + PlayerPrefs.GetInt("Story_Objective", -1) + ", LastPlayedScene=" + PlayerPrefs.GetString("LastPlayedScene", "none") + " => " + (saved ? "WORKS" : "BROKEN"));
            Line("- Guide button present=" + (GameObject.Find("GuideButton") != null));
        }

        // ------------------------------------------------------------------ UI audit
        private static readonly Vector2Int[] Resolutions = { new Vector2Int(1280, 720), new Vector2Int(1920, 1080), new Vector2Int(2560, 1440) };

        private IEnumerator UiAuditAllResolutions(string label)
        {
            int ow = Screen.width, oh = Screen.height;
            foreach (var r in Resolutions)
            {
                Screen.SetResolution(r.x, r.y, FullScreenMode.Windowed);
                yield return new WaitForSecondsRealtime(0.6f);
                yield return UiAudit(label, r.x, r.y);
            }
            Screen.SetResolution(ow, oh, FullScreenMode.Windowed);
            yield return new WaitForSecondsRealtime(0.4f);
        }

        private struct TextItem { public TMP_Text t; public Rect r; public string name; }

        private IEnumerator UiAudit(string label, int reqW, int reqH)
        {
            yield return new WaitForEndOfFrame();
            Canvas.ForceUpdateCanvases();
            int imgui = 0;
            foreach (var mb in FindObjectsByType<MonoBehaviour>())
                if (mb != null && mb.isActiveAndEnabled && mb.GetType().GetMethod("OnGUI", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public) != null && mb.GetType().Namespace != null && mb.GetType().Namespace.StartsWith("LegacyOfTemples") && mb.GetType() != typeof(AuditBot)) imgui++;
            var items = new List<TextItem>();
            foreach (var t in FindObjectsByType<TMP_Text>())
            {
                if (!t.isActiveAndEnabled || string.IsNullOrWhiteSpace(t.GetParsedText()) || t.canvas == null) continue;
                if (t.color.a < 0.05f || EffectiveAlpha(t.transform) < 0.05f) continue;
                items.Add(new TextItem { t = t, r = ScreenRect(t.rectTransform), name = PathOf(t.transform) });
            }
            Line($"\n#### UI table: {label} @ requested {reqW}x{reqH} (actual {Screen.width}x{Screen.height}); TMP texts={items.Count}; active IMGUI(OnGUI) scripts not measurable={imgui}");
            Line("| text | px@1080p | min | contrast(worst) | issues |");
            Line("|---|---|---|---|---|");
            float scaleTo1080 = 1080f / Mathf.Max(1, Screen.height);
            int problems = 0;
            foreach (var it in items)
            {
                var t = it.t;
                float px = t.fontSize * t.canvas.scaleFactor * t.rectTransform.lossyScale.y / Mathf.Max(0.0001f, t.canvas.transform.lossyScale.y) * scaleTo1080;
                float min = MinFor(it.name, t);
                Color bg; bool panel = Background(t.transform, out bg);
                float contrast = Mathf.Min(Contrast(t.color, Over(bg, Color.white)), Contrast(t.color, Over(bg, Color.black)));
                var issues = new List<string>();
                if (px + 0.5f < min) issues.Add("SMALL");
                if (contrast < 7f) issues.Add("LOW-CONTRAST");
                if (!panel) issues.Add("no-panel");
                if (t.isTextOverflowing && t.GetComponentInParent<RectMask2D>() == null) issues.Add("OVERFLOW(text>rect)");
                Rect tb = TextBounds(t);
                RectTransform pr = PanelRect(t.transform);
                if (pr != null && tb.width > 0)
                {
                    Rect prr = ScreenRect(pr);
                    if (tb.xMin < prr.xMin - 2 || tb.xMax > prr.xMax + 2 || tb.yMin < prr.yMin - 2 || tb.yMax > prr.yMax + 2) issues.Add("OVERFLOW(panel)");
                }
                if (tb.width > 0 && (tb.xMin < -1 || tb.yMin < -1 || tb.xMax > Screen.width + 1 || tb.yMax > Screen.height + 1)) issues.Add("OFFSCREEN");
                foreach (var o in items)
                {
                    if (o.t == t || string.CompareOrdinal(o.name, it.name) <= 0) continue;
                    if (o.t.transform.IsChildOf(t.transform) || t.transform.IsChildOf(o.t.transform)) continue;
                    Rect ob = TextBounds(o.t);
                    if (tb.width > 0 && ob.width > 0 && tb.Overlaps(ob))
                    {
                        float ix = Mathf.Min(tb.xMax, ob.xMax) - Mathf.Max(tb.xMin, ob.xMin);
                        float iy = Mathf.Min(tb.yMax, ob.yMax) - Mathf.Max(tb.yMin, ob.yMin);
                        if (ix > 3 && iy > 3) issues.Add("OVERLAPS '" + Short(o.t) + "'");
                    }
                }
                if (issues.Count > 0 && !(issues.Count == 1 && issues[0] == "no-panel")) problems++;
                Line($"| {Short(t)} | {px:0} | {min:0} | {contrast:0.0} | {string.Join(", ", issues)} |");
            }
            Line($"Problems in this view: {problems}");
        }

        private static string Short(TMP_Text t)
        {
            string s = t.GetParsedText().Replace("\n", " ").Replace("|", "/");
            return t.name + ": " + (s.Length > 40 ? s.Substring(0, 40) + "…" : s);
        }

        private static float MinFor(string path, TMP_Text t)
        {
            string n = t.name.ToLowerInvariant();
            if (n == "labeltext") return 19;
            if (n.Contains("keyhint") || n.Contains("keytext") || n.Contains("hinttext") || n.Contains("keycap") || n.Contains("distance")) return 18;
            if (n.Contains("objective") || n.Contains("prompt") || n.Contains("goal")) return 23;
            if (n.Contains("speaker") || n.Contains("dialogue") || n.Contains("subtitle")) return 26;
            if (n.Contains("title") || n == "portraitinitial") return 48;
            return 22;
        }

        private static float EffectiveAlpha(Transform t)
        {
            float a = 1;
            for (var x = t; x != null; x = x.parent) { var g = x.GetComponent<CanvasGroup>(); if (g) a *= g.alpha; }
            return a;
        }

        private static bool Background(Transform t, out Color bg)
        {
            for (var x = t.parent; x != null; x = x.parent)
            {
                var g = x.GetComponent<Graphic>();
                if (g != null && !(g is TMP_Text) && g.enabled && g.color.a > 0.2f)
                {
                    bg = g.color;
                    if (g is Image im && im.sprite != null && im.sprite.texture != null && im.sprite.texture.isReadable)
                    {
                        var r = im.sprite.textureRect;
                        bg *= im.sprite.texture.GetPixel((int)r.center.x, (int)r.center.y);
                    }
                    if (bg.a > 0.2f) return true;
                }
            }
            bg = new Color(0, 0, 0, 0); return false;
        }
        private static RectTransform PanelRect(Transform t)
        {
            for (var x = t.parent; x != null; x = x.parent)
            {
                var g = x.GetComponent<Graphic>();
                if (g != null && !(g is TMP_Text) && g.enabled && g.color.a > 0.2f) return (RectTransform)x;
            }
            return null;
        }
        private static Color Over(Color top, Color under) => new Color(
            top.r * top.a + under.r * (1 - top.a), top.g * top.a + under.g * (1 - top.a), top.b * top.a + under.b * (1 - top.a), 1);
        private static float Lum(Color c)
        {
            float f(float v) => v <= 0.03928f ? v / 12.92f : Mathf.Pow((v + 0.055f) / 1.055f, 2.4f);
            return 0.2126f * f(c.r) + 0.7152f * f(c.g) + 0.0722f * f(c.b);
        }
        public static float Contrast(Color a, Color b)
        {
            float l1 = Lum(a), l2 = Lum(b);
            return (Mathf.Max(l1, l2) + 0.05f) / (Mathf.Min(l1, l2) + 0.05f);
        }

        private static Rect ScreenRect(RectTransform rt)
        {
            var c = new Vector3[4]; rt.GetWorldCorners(c);
            var cv = rt.GetComponentInParent<Canvas>();
            Camera cam = cv != null && cv.renderMode != RenderMode.ScreenSpaceOverlay ? cv.worldCamera : null;
            Vector2 a = RectTransformUtility.WorldToScreenPoint(cam, c[0]), b = RectTransformUtility.WorldToScreenPoint(cam, c[2]);
            return Rect.MinMaxRect(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));
        }
        private static Rect TextBounds(TMP_Text t)
        {
            Bounds b = t.textBounds;
            if (b.size.x <= 0 || b.size.y <= 0) return new Rect();
            var cv = t.canvas;
            Camera cam = cv != null && cv.renderMode != RenderMode.ScreenSpaceOverlay ? cv.worldCamera : null;
            Vector2 a = RectTransformUtility.WorldToScreenPoint(cam, t.transform.TransformPoint(b.min));
            Vector2 c = RectTransformUtility.WorldToScreenPoint(cam, t.transform.TransformPoint(b.max));
            var r = Rect.MinMaxRect(Mathf.Min(a.x, c.x), Mathf.Min(a.y, c.y), Mathf.Max(a.x, c.x), Mathf.Max(a.y, c.y));
            // Text inside a scroll mask is only visible within the mask: measure the visible part.
            var mask = t.GetComponentInParent<RectMask2D>();
            if (mask != null)
            {
                Rect m = ScreenRect((RectTransform)mask.transform);
                r = Rect.MinMaxRect(Mathf.Max(r.xMin, m.xMin), Mathf.Max(r.yMin, m.yMin), Mathf.Min(r.xMax, m.xMax), Mathf.Min(r.yMax, m.yMax));
                if (r.width <= 0 || r.height <= 0) return new Rect();
            }
            return r;
        }

        private IEnumerator Shot(string file)
        {
            yield return new WaitForEndOfFrame();
            if (Application.isBatchMode) { WindowlessShot.Save(System.IO.Path.Combine(output, file)); yield break; }   // no window: render the camera to an image (no on-screen HUD)
            ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(output, file));
            yield return null;
        }
    }
}
