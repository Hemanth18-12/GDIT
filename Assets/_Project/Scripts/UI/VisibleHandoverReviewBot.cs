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
using LegacyOfTemples.Characters;
using LegacyOfTemples.Gameplay;
using LegacyOfTemples.Gameplay.Puzzles;

namespace LegacyOfTemples.UI
{
    public sealed class VisibleHandoverReviewBot : MonoBehaviour
    {
        public static bool Finished;
        static string directory;
        static void Check(bool ok, string what)
        {
            File.AppendAllText(Path.Combine(directory, "checks.txt"), (ok ? "PASS " : "FAIL ") + what + "\n");
            if (!ok) throw new InvalidOperationException(what);
        }
        public static IEnumerator Run(string output)
        {
            directory = output; Directory.CreateDirectory(directory); Finished = false;
            File.WriteAllText(Path.Combine(directory, "checks.txt"), "Original Courtyard — visible skip and automatic history\n");
            var background = InputSystem.settings.backgroundBehavior;
            var editor = InputSystem.settings.editorInputBehaviorInPlayMode;
            var keyboard = InputSystem.AddDevice<Keyboard>();
            try
            {
                InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
                InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
                InputSystem.EnableDevice(keyboard); keyboard.MakeCurrent();
                StoryDialogueSystem.ResetForNewGame();
                SceneManager.LoadScene(Constants.Courtyard);
                yield return null;
                float deadline = Time.realtimeSinceStartup + 40f;
                while ((PujariLoreDirector.Instance == null || !PujariLoreDirector.Instance.Ready) && Time.realtimeSinceStartup < deadline) yield return null;
                foreach (var intro in UnityEngine.Object.FindObjectsByType<OpeningCinematic>()) intro.EndCinematic();
                UIManager.Instance.DismissHowToPlay(); TutorialManager.Instance?.SkipTutorial(); UIManager.Instance.CancelDialogue();
                yield return new WaitForSeconds(0.5f);
                var lore = PujariLoreDirector.Instance;
                Check(lore != null && lore.Ready && lore.Original && NewWorldDirector.Instance == null, "Feature runs in actual New Game Courtyard route");
                Check(lore.Priest.GetComponent<NPCDialogueTrigger>() != null, "Original priest and initial conversation retained");
                var player = ZoneKit.Player;
                player.Teleport(V3Places.Get(1).at, Quaternion.identity);
                yield return new WaitForSeconds(0.2f);
                Button skip = null;
                foreach (var button in UnityEngine.Object.FindObjectsByType<Button>()) if (button.name == "SkipLevelButton") skip = button;
                Check(skip != null && skip.isActiveAndEnabled && skip.interactable, "SKIP LEVEL [K] button visible during normal gameplay");
                yield return Shot("01-visible-skip-button.png");
                for (int zone = 1; zone <= 6; zone++)
                {
                    if (zone == 1)
                    {
                        BazaarTail.Instance.SkipForHandover();
                        var item = lore.Pickup(1);
                        player.Teleport(item.transform.position + Vector3.back * 1.2f, Quaternion.identity);
                        player.FaceTowards(item.transform.position); Physics.SyncTransforms();
                        yield return new WaitForSeconds(0.3f);
                        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.E)); yield return null; yield return null;
                        InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                        Check(item.Taken, "Normal first-level relic pickup uses real E input");
                    }
                    else if (zone % 2 == 0) skip.onClick.Invoke();
                    else
                    {
                        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.K)); yield return null; yield return null;
                        InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                    }
                    float started = Time.time;
                    yield return new WaitForSeconds(1f);
                    Check(!lore.Talking && lore.PendingZone == zone, "Countdown before automatic return zone " + zone);
                    if (zone == 2) yield return Shot("02-skip-return-countdown.png");
                    deadline = Time.realtimeSinceStartup + 12f;
                    while (!lore.Talking && Time.realtimeSinceStartup < deadline) yield return null;
                    Check(lore.Talking && UIManager.Instance.IsDialogueActive, "Three-second auto return opens explanation without T or priest E, zone " + zone);
                    Check(Time.time - started >= 2.7f && Time.time - started < 8f, "Return occurs around three seconds, zone " + zone);
                    Check(Vector3.Distance(player.transform.position, lore.Priest.position) < 4f, "Aryan reaches original Pujari, zone " + zone);
                    // Let Aryan's line finish: the history must start itself without manual advance.
                    deadline = Time.realtimeSinceStartup + 12f;
                    while ((VoiceManager.Instance.Current == null || VoiceManager.Instance.Current.id != $"lore_puj_z{zone}_1") && Time.realtimeSinceStartup < deadline) yield return null;
                    Check(VoiceManager.Instance.Current != null && VoiceManager.Instance.Current.id == $"lore_puj_z{zone}_1", "Narration auto advances to Ananda zone " + zone);
                    var animator = lore.Priest.GetComponentInChildren<Animator>();
                    Check(animator != null && animator.GetBool("IsTalking"), "Ananda uses real talking animation zone " + zone);
                    yield return new WaitForSeconds(0.5f);
                    Check(animator.GetCurrentAnimatorClipInfo(0).Length > 0, "Ananda has a playing animation clip zone " + zone);
                    File.AppendAllText(Path.Combine(directory, "checks.txt"), "ANIMATION " + animator.GetCurrentAnimatorClipInfo(0)[0].clip.name + " zone " + zone + "\n");
                    bool historyShown = false;
                    foreach (var label in UnityEngine.Object.FindObjectsByType<TMP_Text>())
                        if (!string.IsNullOrEmpty(label.text) && label.text.Contains(lore.Relics[zone - 1].lines[0])) historyShown = true;
                    Check(historyShown, "Actual Jagannath heritage content readable on screen zone " + zone);
                    bool titleShown = false;
                    foreach (var text in UnityEngine.Object.FindObjectsByType<TMP_Text>()) if (!string.IsNullOrEmpty(text.text) && text.text.Contains(lore.Relics[zone - 1].title)) titleShown = true;
                    Check(titleShown, "Relic history title visible zone " + zone);
                    yield return Shot($"history-zone-{zone}.png");
                    UIManager.Instance.AdvanceDialogue(); UIManager.Instance.AdvanceDialogue(); UIManager.Instance.AdvanceDialogue();
                    UIManager.Instance.SelectDialogueChoice(zone % 2);
                    yield return new WaitForSeconds(0.1f);
                    UIManager.Instance.AdvanceDialogue(); yield return new WaitForSeconds(0.3f);
                    Check(PuzzleSave.Get($"lore.z{zone}.handed") == 1, "Required history handover completes once zone " + zone);
                    Check(ScoreSystem.Zone(lore.Pickup(zone).zoneId).heritageBonus == 500, "Exactly 500 heritage points zone " + zone);
                }
                Check(PuzzleSave.Get("lore.completed") == 1, "All six original-route explanations completed");
                yield return Shot("09-original-route-mission-complete.png");
                Finished = true; File.AppendAllText(Path.Combine(directory, "checks.txt"), "ALL CHECKS PASSED\n");
            }
            finally
            {
                InputSystem.RemoveDevice(keyboard);
                InputSystem.settings.backgroundBehavior = background;
                InputSystem.settings.editorInputBehaviorInPlayMode = editor;
            }
        }
        static IEnumerator Shot(string file)
        {
            if (Application.isBatchMode) WindowlessShot.Save(Path.Combine(directory, file));
            else { ScreenCapture.CaptureScreenshot(Path.Combine(directory, file)); yield return new WaitForSeconds(0.2f); }
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] static void Boot()
        {
            var args = Environment.GetCommandLineArgs(); int i = Array.IndexOf(args, "-visibleHandoverQA");
            if (i < 0 || i + 1 >= args.Length) return;
            var go = new GameObject("Visible Handover Review"); DontDestroyOnLoad(go);
            go.AddComponent<VisibleHandoverReviewBot>().StartCoroutine(Standalone(args[i + 1]));
        }
        static IEnumerator Standalone(string output) { yield return Run(output); Application.Quit(Finished ? 0 : 1); }
    }
}
