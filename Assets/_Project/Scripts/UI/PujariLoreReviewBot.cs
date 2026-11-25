using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;
using TMPro;
using LegacyOfTemples.Core;
using LegacyOfTemples.Gameplay;
using LegacyOfTemples.Gameplay.Puzzles;
using LegacyOfTemples.Characters;

namespace LegacyOfTemples.UI
{
    /// <summary>Opt-in real runtime QA for the six deliveries, also used by Editor PlayMode tests.</summary>
    public sealed class PujariLoreReviewBot : MonoBehaviour
    {
        public static bool Finished;
        static string directory;
        static void Check(bool ok, string what)
        {
            File.AppendAllText(Path.Combine(directory, "checks.txt"), (ok ? "PASS " : "FAIL ") + what + "\n");
            if (!ok) throw new InvalidOperationException(what);
        }
        static Keyboard reviewKeyboard;
        public static IEnumerator Run(string output)
        {
            var background = InputSystem.settings.backgroundBehavior;
            var editor = InputSystem.settings.editorInputBehaviorInPlayMode;
            try { yield return RunCore(output); }
            finally
            {
                if (reviewKeyboard != null) { InputSystem.RemoveDevice(reviewKeyboard); reviewKeyboard = null; }
                InputSystem.settings.backgroundBehavior = background;
                InputSystem.settings.editorInputBehaviorInPlayMode = editor;
            }
        }
        static IEnumerator RunCore(string output)
        {
            directory = output; Directory.CreateDirectory(directory); Finished = false;
            File.WriteAllText(Path.Combine(directory, "checks.txt"), "Pujari lore runtime review\n");
            StoryDialogueSystem.ResetForNewGame();
            SceneManager.LoadScene(Constants.TempleRing);
            yield return new WaitForSeconds(4f);
            foreach (var c in UnityEngine.Object.FindObjectsByType<OpeningCinematic>()) c.EndCinematic();
            UIManager.Instance.DismissHowToPlay(); TutorialManager.Instance?.SkipTutorial();
            UIManager.Instance.CancelDialogue();
            Time.timeScale = 1f;
            yield return new WaitForSeconds(1f);
            var lore = PujariLoreDirector.Instance; var world = NewWorldDirector.Instance; var player = ZoneKit.Player;
            Check(lore != null && lore.Priest != null && world.AllZonesReady, "Pujari and all six zones spawned");
            Check(Physics.Raycast(lore.Priest.position + Vector3.up * 0.1f, Vector3.down, out var floor, 3f, ~0, QueryTriggerInteraction.Ignore), "Stone platform under Pujari");
            foreach (var renderer in lore.Priest.GetComponentsInChildren<Renderer>())
                Check(renderer.bounds.min.y >= floor.point.y - 0.15f, "Pujari rendered model above platform: " + renderer.name);
            foreach (var line in VoiceLibrary.Db.lines) if (line.id.StartsWith("lore_"))
                Check(line.clip != null && line.clip.length > 0.5f && line.clip.frequency == 44100 && line.clip.channels == 1, "Valid mono 44.1kHz voice: " + line.id);
            var keyboard = InputSystem.AddDevice<Keyboard>(); reviewKeyboard = keyboard;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            InputSystem.EnableDevice(keyboard); keyboard.MakeCurrent();
            for (int zone = 1; zone <= 6; zone++)
            {
                player.Teleport(V3Places.Get(zone - 1).at, Quaternion.identity);
                yield return new WaitForSeconds(0.3f);
                if (zone == 1)
                {
                    File.AppendAllText(Path.Combine(directory, "checks.txt"), $"STATE before K: blocked={InputGate.GameplayBlocked}, time={Time.timeScale}, currentKeyboard={Keyboard.current?.name}, world={world.enabled}, player={player.transform.position}, pending={lore.PendingZone}\n");
                    InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.K)); yield return null; yield return null;
                    File.AppendAllText(Path.Combine(directory, "checks.txt"), $"STATE after K: blocked={InputGate.GameplayBlocked}, key={keyboard.kKey.isPressed}, active={Keyboard.current == keyboard}, stage={world.Bazaar.Now}, taken={world.Bazaar.Bell.Taken}\n");
                    InputSystem.QueueStateEvent(keyboard, new KeyboardState()); yield return null;
                }
                else if (zone == 2)
                {
                    UIManager.Instance.TogglePause(); yield return null;
                    Button skip = null;
                    foreach (var button in UnityEngine.Object.FindObjectsByType<Button>())
                    {
                        var label = button.GetComponentInChildren<TMP_Text>();
                        if (label != null && label.text == "SKIP CURRENT TASK") { skip = button; break; }
                    }
                    Check(skip != null, "Pause menu exposes skip task button");
                    skip.onClick.Invoke();
                    yield return new WaitForSeconds(0.2f);
                    Check(!UIManager.Instance.IsPauseOpen, "Pause skip resumes gameplay");
                }
                else
                {
                    if (zone == 3) Brawl.Begin(NewVaults.Zone, player.transform.position, 6f,
                        new[] { Brawl.Robber("Lore QA guard", 1, BrawlType.Brawler, 1f, Color.white) }, _ => { });
                    world.SkipCurrentTask();
                    if (zone == 3) Check(Brawl.Active == null, "Skip resolves active combat cleanly");
                }
                yield return new WaitForSeconds(0.3f);
                Check(lore.Pickup(zone).Taken && PuzzleSave.Get($"nz{zone}.complete") == 1, "Zone " + zone + " skip collects stable-ID relic and completes zone");
                Check(lore.PendingZone == zone && PlayerController.ObjectiveFocus == lore.Priest, "Zone " + zone + " objective returns to Pujari");
                Check(lore.QuickReturn(), "Safe quick return zone " + zone);
                yield return new WaitForSeconds(0.4f);
                if (zone == 1)
                {
                    File.AppendAllText(Path.Combine(directory, "checks.txt"), $"STATE E stand={player.transform.position} priest={lore.Priest.position} nearest={player.CurrentInteractable?.Id} blocked={InputGate.GameplayBlocked}\n");
                    var framing = UnityEngine.Object.FindAnyObjectByType<OrbitCamera>();
                    Vector3 at = lore.Priest.position + new Vector3(-5f, 3f, -7f);
                    if (framing != null) framing.SetCutsceneOverride(at, Quaternion.LookRotation(lore.Priest.position + Vector3.up - at));
                    yield return new WaitForSeconds(0.2f);
                    yield return Shot("01-pujari-platform.png");
                    InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.E)); yield return null; yield return null;
                    InputSystem.QueueStateEvent(keyboard, new KeyboardState()); yield return null;
                }
                else lore.Priest.GetComponent<PujariLoreHandover>().Interact();
                yield return new WaitForSeconds(0.2f);
                if (zone == 3)
                {
                    UIManager.Instance.CancelDialogue(); yield return null;
                    Check(PuzzleSave.Get("lore.z3.handed") == 0 && !lore.Talking, "Cancelled conversation preserves pending delivery");
                    lore.HandOver();
                }
                Check(UIManager.Instance.IsDialogueActive && lore.Talking, "Zone " + zone + " E dialogue opens");
                Check(VoiceManager.Instance.Current != null && VoiceManager.Instance.Current.id == $"lore_ary_z{zone}", "Aryan delivery clip playing zone " + zone);
                UIManager.Instance.AdvanceDialogue(); yield return new WaitForSeconds(0.1f);
                Check(VoiceManager.Instance.Current != null && VoiceManager.Instance.Current.id == $"lore_puj_z{zone}_1", "Pujari clip replaces Aryan without overlap zone " + zone);
                if (zone == 1) { yield return new WaitForSeconds(3.5f); yield return Shot("02-pujari-dialogue.png"); }
                UIManager.Instance.AdvanceDialogue(); UIManager.Instance.AdvanceDialogue(); UIManager.Instance.AdvanceDialogue();
                UIManager.Instance.SelectDialogueChoice(zone % 2);
                yield return new WaitForSeconds(0.1f);
                Check(VoiceManager.Instance.Current != null && VoiceManager.Instance.Current.id == $"lore_puj_z{zone}_" + (zone % 2 == 0 ? "extra" : "care"), "Follow-up choice voiced zone " + zone);
                UIManager.Instance.AdvanceDialogue();
                yield return new WaitForSeconds(0.3f);
                Check(PuzzleSave.Get($"lore.z{zone}.handed") == 1, "Delivery persisted zone " + zone);
                Check(ScoreSystem.Zone(lore.Pickup(zone).zoneId).heritageBonus == 500, "Exactly 500 lore points zone " + zone);
                Check(HeritageNotes.Known(Array.Find(HeritageNotes.All, n => n.id == $"lore_z{zone}")), "Historical journal unlocked zone " + zone);
                if (zone == 1)
                {
                    bool objectiveUpdated = false;
                    foreach (var label in UnityEngine.Object.FindObjectsByType<TMP_Text>())
                        if (label.text == "Continue the investigation") objectiveUpdated = true;
                    Check(objectiveUpdated, "Delivered relic clears return objective");
                    yield return Shot("03-heritage-card.png");
                }
            }
            Check(PuzzleSave.Get("story.completed") == 1, "Finale waits for all six handovers");
            yield return Shot("04-six-deliveries-complete.png");

            // Reload with a recovered but unhanded relic: no repeat pickup or lost pending delivery.
            PuzzleSave.Set("lore.z2.handed", 0);
            SceneManager.LoadScene(Constants.TempleRing); yield return new WaitForSeconds(3f);
            lore = PujariLoreDirector.Instance;
            Check(lore.PendingZone == 2 && lore.Pickup(2).Taken, "Reload restores pending handover from stable save ID");
            Check(PuzzleSave.Get("lore.z1.handed") == 1, "Reload preserves delivered relics");
            Finished = true;
            File.AppendAllText(Path.Combine(directory, "checks.txt"), "ALL CHECKS PASSED\n");
        }
        static IEnumerator Shot(string name)
        {
            string path = Path.Combine(directory, name);
            if (Application.isBatchMode) WindowlessShot.Save(path);
            else { ScreenCapture.CaptureScreenshot(path); yield return new WaitForSeconds(0.25f); }
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] static void Boot()
        {
            string[] args = Environment.GetCommandLineArgs(); int i = Array.IndexOf(args, "-pujariLoreQA");
            if (i < 0 || i + 1 >= args.Length) return;
            var go = new GameObject("Pujari Lore QA"); DontDestroyOnLoad(go);
            go.AddComponent<PujariLoreReviewBot>().StartCoroutine(Standalone(args[i + 1]));
        }
        static IEnumerator Standalone(string output)
        {
            yield return Run(output);
            Application.Quit(Finished ? 0 : 1);
        }
    }
}
