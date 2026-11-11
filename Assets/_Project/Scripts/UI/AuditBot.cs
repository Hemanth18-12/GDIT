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
            var go = new GameObject("AuditBot");
            DontDestroyOnLoad(go);
            go.AddComponent<AuditBot>().output = args[index + 1];
        }

        private void Line(string s) { report.AppendLine(s); Debug.Log("[AuditBot] " + s); }

        private IEnumerator Start()
        {
            Directory.CreateDirectory(output);
            Line("# Runtime audit (" + DateTime.Now.ToString("yyyy-MM-dd HH:mm") + ", " + (Application.isEditor ? "Editor" : "Standalone") + ", " + Screen.width + "x" + Screen.height + ")");
            yield return new WaitForSecondsRealtime(2f);

            // ---------- Main menu UI ----------
            Line("\n## UI: MainMenu");
            yield return Shot("menu.png");
            yield return UiAuditAllResolutions("MainMenu");

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
                yield return new WaitForSecondsRealtime(1.6f);
                n++;
                Line($"  loading screen {n}: image={LoadingScreen.LastImage}, bar={LoadingScreen.LastProgress:0.00}");
                yield return Shot("v2-loading-" + n + "-" + LoadingScreen.LastImage + ".png");
                if (n == 1) yield return UiAuditAllResolutions("Loading screen");
                while (SceneLoader.IsLoading) yield return null;
            }
        }

        /// <summary>Plays each new section through its real actions where possible, with screenshots.</summary>
        private IEnumerator PlayMinigame(string id, StoryDialogueSystem story, PlayerController player)
        {
            var cd = ChapterDirector.Instance; var fd = FightDirector.Instance;
            Line("  minigame '" + id + "'");
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
            yield return new WaitForSecondsRealtime(3f);
            yield return Shot("pt-00-cinematic.png");
            yield return SkipIntro();

            // Tutorial: play it for a while with real keys, then skip the rest.
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
                    for (int ck = 0; ck < 20 && pzc != null && !pzc.IsSolved; ck++) { yield return Tap(ck % 2 == 0 ? Key.A : Key.D); if (ck == 6) yield return Shot("v2-sunwall-crank.png"); }
                    yield return new WaitForSecondsRealtime(1.5f);
                    Line($"  puzzle: wrong-order message='{wrongMsg}', solved={ChakraPuzzleController.Active != null && ChakraPuzzleController.Active.IsSolved}");
                    yield return FinishDialogue(null);
                }
                else if (t == "clues")
                {
                    yield return Tap(Key.F); yield return new WaitForSecondsRealtime(0.8f);
                    if (!Flag("ClueInspectable", "isInspecting")) { yield return Tap(Key.E); yield return new WaitForSecondsRealtime(0.8f); }
                    bool insp = Flag("ClueInspectable", "isInspecting");
                    yield return Shot($"pt-clue-{target.name}.png");
                    if (insp && step < 8) yield return UiAudit("Clue inspect", Screen.width, Screen.height);
                    yield return Tap(Key.Escape); yield return new WaitForSecondsRealtime(0.6f);
                    if (Time.timeScale == 0f) { yield return Tap(Key.Escape); yield return new WaitForSecondsRealtime(0.4f); }
                    Line($"  clue {target.name}: inspecting={insp}, found={story.ClueCount}");
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
        private void Hold(params Key[] keys) { held.Clear(); foreach (var k in keys) held.Add(k); Push(); }
        private void Release() { held.Clear(); Push(); }
        private void Push()
        {
            if (Keyboard.current == null) InputSystem.AddDevice<Keyboard>();
            var keys = new Key[held.Count];
            held.CopyTo(keys);
            InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState(keys));
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
            ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(output, file));
            yield return null;
        }
    }
}
