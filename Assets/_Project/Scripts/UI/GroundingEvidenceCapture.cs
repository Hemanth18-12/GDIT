using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using LegacyOfTemples.Core;
using LegacyOfTemples.Characters;
using LegacyOfTemples.Gameplay;

namespace LegacyOfTemples.UI
{
    public sealed class GroundingEvidenceCapture : MonoBehaviour
    {
        private string output;
        private static bool requested;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        public static void Bootstrap()
        {
            if (requested) return;
            string[] args = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(args, "-captureGrounding");
            if (index < 0 || index + 1 >= args.Length) return;
            requested = true;
            Application.runInBackground = true;
            Directory.CreateDirectory(args[index + 1]);
            Debug.Log("[GroundingEvidence] Explicit capture requested: " + args[index + 1]);
            var go = new GameObject("GroundingEvidenceCapture");
            DontDestroyOnLoad(go);
            go.AddComponent<GroundingEvidenceCapture>().output = args[index + 1];
        }

        private IEnumerator Start()
        {
            Directory.CreateDirectory(output);
            yield return new WaitForSecondsRealtime(2f);

            // 1. Main Menu
            yield return Capture("standalone-menu.png");

            // 2. Load Courtyard
            yield return SceneManager.LoadSceneAsync(Constants.Courtyard);
            yield return null;
            foreach (var c in FindObjectsByType<OpeningCinematic>()) c.EndCinematic();
            yield return new WaitForSecondsRealtime(1.5f);

            // 3. How To Play Mission Brief
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowHowToPlay(null);
                yield return new WaitForSecondsRealtime(1f);
                yield return Capture("standalone-howtoplay.png");
                UIManager.Instance.DismissHowToPlay();
                yield return new WaitForSecondsRealtime(0.5f);
            }

            // 4. Tutorial Panel
            if (TutorialManager.Instance != null)
            {
                TutorialManager.Instance.StartTutorial();
                yield return new WaitForSecondsRealtime(1f);
                yield return Capture("standalone-tutorial.png");
                TutorialManager.Instance.SkipTutorial();
                if (UIManager.Instance != null)
                {
                    UIManager.Instance.HideTutorial();
                    UIManager.Instance.DismissHowToPlay();
                }
                yield return new WaitForSecondsRealtime(0.5f);
            }

            // 5. Courtyard Gameplay with HUD & Bottom Hint Bar
            var orbit = Camera.main != null ? Camera.main.GetComponent<OrbitCamera>() : null;
            var player = FindAnyObjectByType<PlayerController>();
            if (orbit != null && player != null)
            {
                orbit.SetCutsceneOverride(player.transform.position - player.transform.forward * 4.2f + Vector3.up * 2.0f, Quaternion.Euler(12f, player.transform.eulerAngles.y, 0f));
            }
            yield return new WaitForSecondsRealtime(0.5f);
            yield return Capture("standalone-courtyard-gameplay.png");
            if (orbit != null) orbit.ClearCutsceneOverride();

            // 6. Dialogue with Pujari Ananda & Choices
            if (UIManager.Instance != null)
            {
                GameObject pObj = GameObject.Find("Priest") ?? GameObject.Find("Priest(Clone)");
                if (orbit != null && pObj != null)
                {
                    Vector3 head = pObj.transform.position + Vector3.up * 1.45f;
                    Vector3 camPos = head + pObj.transform.forward * 2.2f + Vector3.up * 0.1f + pObj.transform.right * 0.5f;
                    orbit.SetCutsceneOverride(camPos, Quaternion.LookRotation(head - camPos));
                }

                string[] lines = new string[]
                {
                    "Namaste. Three pieces from our storage hall are gone: a brass ceremonial vessel, a carved sandstone rosette, and a palm-leaf manuscript. Please find them before the festival.",
                    "Stay calm and touch nothing. Every small detail matters. Did anyone see something?",
                    "Before sunrise I saw two men near the storage door. One wore a red scarf, the other a black hood."
                };
                string[] choices = new string[]
                {
                    "Ask Meera which way they ran",
                    "Ask the Pujari why the pieces matter"
                };
                UIManager.Instance.StartDialogue("Pujari Ananda", lines, choices, null, null);
                yield return new WaitForSecondsRealtime(1.5f);
                yield return Capture("standalone-dialogue.png");
                UIManager.Instance.EndDialogue();
                if (orbit != null) orbit.ClearCutsceneOverride();
                yield return new WaitForSecondsRealtime(0.5f);
            }

            // 7. Investigation Journal [J]
            if (InvestigationJournal.Instance != null)
            {
                InvestigationJournal.Instance.ToggleJournal();
                yield return new WaitForSecondsRealtime(1f);
                yield return Capture("standalone-journal.png");
                InvestigationJournal.Instance.ToggleJournal();
                yield return new WaitForSecondsRealtime(0.5f);
            }

            // 8. Temple Architectural Map [M]
            if (TempleMapOverlay.Instance != null)
            {
                TempleMapOverlay.Instance.ToggleMap();
                yield return new WaitForSecondsRealtime(1f);
                yield return Capture("standalone-map.png");
                TempleMapOverlay.Instance.ToggleMap();
                yield return new WaitForSecondsRealtime(0.5f);
            }

            // 9. High Contrast Mode
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ToggleHighContrast();
                yield return new WaitForSecondsRealtime(0.8f);
                yield return Capture("standalone-highcontrast.png");
                UIManager.Instance.ToggleHighContrast();
                yield return new WaitForSecondsRealtime(0.5f);
            }

            // 10. Photo Mode (Hidden UI)
            if (UIManager.Instance != null)
            {
                UIManager.Instance.TogglePhotoMode();
                yield return new WaitForSecondsRealtime(0.8f);
                yield return Capture("standalone-photomode.png");
                UIManager.Instance.TogglePhotoMode();
                yield return new WaitForSecondsRealtime(0.5f);
            }

            // 11. Character Close-Ups in Courtyard
            orbit = Camera.main != null ? Camera.main.GetComponent<OrbitCamera>() : null;
            player = FindAnyObjectByType<PlayerController>();

            // Investigator Aryan
            if (orbit != null && player != null)
            {
                Vector3 head = player.transform.position + Vector3.up * 1.35f;
                Vector3 camPos = head + player.transform.forward * 2.2f + Vector3.up * 0.1f + player.transform.right * 0.45f;
                orbit.SetCutsceneOverride(camPos, Quaternion.LookRotation(head - camPos));
                yield return new WaitForSecondsRealtime(1f);
                yield return Capture("standalone-char-investigator.png");
                orbit.ClearCutsceneOverride();
            }

            // Pujari Ananda
            GameObject priestObj = GameObject.Find("Priest") ?? GameObject.Find("Priest(Clone)");
            if (orbit != null && priestObj != null)
            {
                Vector3 head = priestObj.transform.position + Vector3.up * 1.45f;
                Vector3 camPos = head + priestObj.transform.forward * 1.8f + Vector3.up * 0.1f;
                orbit.SetCutsceneOverride(camPos, Quaternion.LookRotation(head - camPos));
                yield return new WaitForSecondsRealtime(1f);
                yield return Capture("standalone-char-priest.png");
                orbit.ClearCutsceneOverride();
            }

            // Meera (Villager in saree)
            GameObject villagerObj = GameObject.Find("Villager") ?? GameObject.Find("Villager(Clone)");
            if (orbit != null && villagerObj != null)
            {
                Vector3 head = villagerObj.transform.position + Vector3.up * 1.45f;
                Vector3 camPos = head + villagerObj.transform.forward * 1.8f + Vector3.up * 0.1f;
                orbit.SetCutsceneOverride(camPos, Quaternion.LookRotation(head - camPos));
                yield return new WaitForSecondsRealtime(1f);
                yield return Capture("standalone-char-meera.png");
                orbit.ClearCutsceneOverride();
            }

            // 12. Tour Remaining Scenes
            string[] remainingScenes = new[] { "Sanctum", "PuzzleWall", "ShrineCrime", "JunglePath" };
            foreach (string name in remainingScenes)
            {
                yield return SceneManager.LoadSceneAsync(name);
                yield return null;
                foreach (var c in FindObjectsByType<OpeningCinematic>()) c.EndCinematic();
                yield return new WaitForSecondsRealtime(1.8f);
                yield return Capture($"standalone-{name.ToLowerInvariant()}-gameplay.png");

                // Character close-ups in JunglePath
                if (name == "JunglePath")
                {
                    var sceneOrbit = UnityEngine.Object.FindAnyObjectByType<OrbitCamera>();
                    GameObject r1Obj = GameObject.Find("Robber1") ?? GameObject.Find("Robber1(Clone)");
                    if (sceneOrbit != null && r1Obj != null)
                    {
                        Vector3 head = r1Obj.transform.position + Vector3.up * 1.55f;
                        Vector3 camPos = head + r1Obj.transform.forward * 2.0f + Vector3.up * 0.1f;
                        sceneOrbit.SetCutsceneOverride(camPos, Quaternion.LookRotation(head - camPos));
                        yield return new WaitForSecondsRealtime(1f);
                        yield return Capture("standalone-char-robber1.png");
                        sceneOrbit.ClearCutsceneOverride();
                    }

                    GameObject r2Obj = GameObject.Find("Robber2") ?? GameObject.Find("Robber2(Clone)");
                    if (sceneOrbit != null && r2Obj != null)
                    {
                        Vector3 head = r2Obj.transform.position + Vector3.up * 1.55f;
                        Vector3 camPos = head + r2Obj.transform.forward * 2.0f + Vector3.up * 0.1f;
                        sceneOrbit.SetCutsceneOverride(camPos, Quaternion.LookRotation(head - camPos));
                        yield return new WaitForSecondsRealtime(1f);
                        yield return Capture("standalone-char-robber2.png");
                        sceneOrbit.ClearCutsceneOverride();
                    }
                }
            }

            // 13. Cinematic Showcase Mode
            yield return SceneManager.LoadSceneAsync(Constants.Courtyard);
            yield return null;
            foreach (var c in FindObjectsByType<OpeningCinematic>()) c.EndCinematic();
            if (ShowcaseDirector.Instance != null)
            {
                ShowcaseDirector.Instance.StartTour();
                yield return new WaitForSecondsRealtime(2f);
                yield return Capture("standalone-showcase.png");
                ShowcaseDirector.Instance.StopTour(false);
            }

            File.WriteAllText(Path.Combine(output, "capture-finished.txt"), "Comprehensive standalone QA capture tour completed successfully.\n");
            Debug.Log("[GroundingEvidenceCapture] Comprehensive QA capture tour completed. Exiting.");
            Application.Quit();
        }

        private IEnumerator Capture(string filename)
        {
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(output, filename));
            yield return new WaitForSecondsRealtime(0.4f);
        }
    }
}
