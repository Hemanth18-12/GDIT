using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using LegacyOfTemples.Characters;
using LegacyOfTemples.Core;
using LegacyOfTemples.Gameplay;
using LegacyOfTemples.UI;
using LegacyOfTemples.World;

namespace LegacyOfTemples.Tests
{
    /// <summary>v2 thriller systems: objective chain, dawn clock, loading screen, chapter mechanics, weather.</summary>
    public class V2PlayModeTests
    {
        public static string QaDir => Path.GetFullPath(Path.Combine(Application.dataPath, "../../Docs/QA/v2"));

        public static IEnumerator Boot(string scene, bool newGame = true)
        {
            LogAssert.ignoreFailingMessages = true; // scene content logs warnings we do not own
            if (newGame) { StoryDialogueSystem.ResetForNewGame(); PlayerPrefs.SetInt("OpeningCinematicActive", 0); PlayerPrefs.SetInt("Force_Tutorial_On_Start", 0); PlayerPrefs.SetInt("ShowcaseTourActive", 0); }
            yield return SceneManager.LoadSceneAsync(scene);
            yield return Settle();
        }

        public static IEnumerator Settle()
        {
            float t = 0f;
            while (t < 30f && (SceneLoader.IsLoading || Object.FindAnyObjectByType<PlayerController>() == null || !Object.FindAnyObjectByType<PlayerController>().enabled)) { t += Time.unscaledDeltaTime; yield return null; }
            TutorialManager.Instance?.SkipTutorial();
            UIManager.Instance?.DismissHowToPlay();
            yield return new WaitForSeconds(1.2f);
        }

        public static IEnumerator Shot(string name)
        {
            Directory.CreateDirectory(QaDir);
            yield return null; // WaitForEndOfFrame never fires in batch mode
            ScreenCapture.CaptureScreenshot(Path.Combine(QaDir, name));
            yield return null; yield return null;
        }

        [UnityTest]
        public IEnumerator ObjectiveChain_MenuToEnding_WithLoadingScreens()
        {
            yield return Boot("Courtyard");
            var story = StoryDialogueSystem.Instance;
            Assert.AreEqual(22, story.ObjectiveCount);
            int loads0 = LoadingScreen.LoadsShown; int guard = 0; string trail = "";
            yield return Shot("chain-courtyard.png");
            while (!story.IsComplete && guard++ < 80)
            {
                var cur = story.Current; trail += cur.id + " ";
                if (cur.complete.type == "scene")
                {
                    SceneLoader.Load(cur.complete.id);
                    yield return null;
                    Assert.IsTrue(SceneLoader.IsLoading, "loading screen did not start for " + cur.complete.id);
                    yield return new WaitForSecondsRealtime(1.2f);
                    Assert.Greater(LoadingScreen.LastProgress, 0f, "bar did not move");
                    Assert.AreNotEqual("(gradient fallback)", LoadingScreen.LastImage, "missing loading image for " + cur.complete.id);
                    yield return Shot("loading-" + cur.complete.id + ".png");
                    yield return Settle();
                    Assert.AreEqual(cur.complete.id, SceneManager.GetActiveScene().name);
                    yield return Shot("chain-" + cur.complete.id + ".png");
                }
                else if (cur.id == "obj_return")
                {
                    float w = 0f; // the story walks us back to the courtyard by itself
                    while (w < 20f && (SceneManager.GetActiveScene().name != "Courtyard" || SceneLoader.IsLoading)) { w += Time.unscaledDeltaTime; yield return null; }
                    yield return Settle();
                    Assert.AreEqual("Courtyard", SceneManager.GetActiveScene().name, "did not return at sunrise");
                    story.DebugCompleteCurrent();
                }
                else story.DebugCompleteCurrent();
                yield return null;
            }
            Assert.IsTrue(story.IsComplete, "chain stopped at: " + (story.Current != null ? story.Current.id : "?") + " trail: " + trail);
            Assert.GreaterOrEqual(LoadingScreen.LoadsShown - loads0, 5, "expected a loading screen per map change");
            yield return new WaitForSecondsRealtime(1.5f);
            yield return Shot("ending.png");
            string end = DawnClock.EndingId();
            Assert.That(end == "dawn_light" || end == "late_dawn" || end == "lost_dawn");
            Debug.Log("[V2Test] chain ok, ending=" + end + ", loads=" + (LoadingScreen.LoadsShown - loads0));
        }
    }
}
