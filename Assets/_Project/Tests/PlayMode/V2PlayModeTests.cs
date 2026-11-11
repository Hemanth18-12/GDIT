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
            yield return new WaitForSeconds(1.5f); // an opening cinematic may take the hero for a while
            while (t < 45f && (SceneLoader.IsLoading || Object.FindAnyObjectByType<PlayerController>() == null || !Object.FindAnyObjectByType<PlayerController>().enabled)) { t += Time.unscaledDeltaTime; yield return null; }
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

        /// <summary>Jump the story to an objective (same scene loads the player would see, without playing).</summary>
        public static IEnumerator GoTo(string objectiveId)
        {
            var story = StoryDialogueSystem.Instance; int guard = 0;
            while (story.Current != null && story.Current.id != objectiveId && guard++ < 60)
            {
                var cur = story.Current;
                if (cur.complete.type == "scene") { yield return SceneManager.LoadSceneAsync(cur.complete.id); yield return Settle(); }
                else story.DebugCompleteCurrent();
                yield return null;
            }
            Assert.AreEqual(objectiveId, story.Current != null ? story.Current.id : "end");
            if (UIManager.Instance != null && UIManager.Instance.IsDialogueActive) UIManager.Instance.CancelDialogue();
            yield return new WaitForSeconds(1.5f);
        }

        private static IEnumerator SkipDialogue(float seconds)
        {
            for (float t = 0; t < seconds; t += 0.1f)
            {
                if (UIManager.Instance != null && UIManager.Instance.IsDialogueActive) UIManager.Instance.AdvanceDialogue();
                yield return new WaitForSecondsRealtime(0.1f);
            }
        }

        [UnityTest]
        public IEnumerator DawnClock_Penalties_Timeout_Retry_And_Flags()
        {
            yield return Boot("Courtyard");
            var clock = DawnClock.Instance; var story = StoryDialogueSystem.Instance;
            Assert.AreEqual("03:10", DawnClock.ClockText(0f)); Assert.AreEqual("05:45", DawnClock.ClockText(155f)); Assert.AreEqual("Dawn in 02:15", DawnClock.DawnInText(20f));
            GameOptions.Difficulty = 1;
            float m0 = clock.Minutes; clock.Penalty(3f, "test");
            Assert.AreEqual(3f, clock.Minutes - m0, 0.05f, "penalty on Normal");
            GameOptions.Difficulty = 2; m0 = clock.Minutes; clock.Penalty(2f, "test"); Assert.AreEqual(3f, clock.Minutes - m0, 0.05f, "penalty x1.5 on Hard");
            GameOptions.Difficulty = 0; m0 = clock.Minutes; clock.Penalty(2f, "test"); Assert.AreEqual(1f, clock.Minutes - m0, 0.05f, "penalty x0.5 on Easy");
            GameOptions.Difficulty = 1;
            // flags and conditions
            StoryFlags.Apply("trust_meera+1;has_message=1"); Assert.AreEqual(1, StoryFlags.Get("trust_meera")); Assert.IsTrue(StoryFlags.Eval("has_message==1 && trust_meera>=1")); Assert.IsFalse(StoryFlags.Eval("trust_meera>=2"));
            StoryFlags.Apply("trust_meera+5"); Assert.AreEqual(3, StoryFlags.Get("trust_meera"), "trust is clamped");
            // run out of the chapter budget
            clock.Penalty(40f, "test");
            yield return null; yield return null;
            Assert.IsTrue(clock.OutOfTime, "chapter 1 budget (25 min) exceeded must fail");
            string before = story.Current.id;
            clock.Retry();
            yield return null; yield return Settle();
            Assert.IsFalse(clock.OutOfTime); Assert.AreEqual(before, story.Current.id); Assert.LessOrEqual(clock.Minutes, 25f - 7.9f, "budget restored on retry");
            Assert.AreEqual("late_dawn", DawnClock.EndingId());
            StoryFlags.Set("kishore_turned", 1); StoryFlags.Set("late", 0); Assert.AreEqual("dawn_light", DawnClock.EndingId());
            StoryFlags.Set("lost_dawn", 1); Assert.AreEqual("lost_dawn", DawnClock.EndingId());
            Debug.Log("[V2Test] clock ok");
        }

        [UnityTest]
        public IEnumerator Chapter2_Lamps_Deduction_And_Chapter3_Traps_Forensics()
        {
            yield return Boot("Courtyard");
            var story = StoryDialogueSystem.Instance; var cd = ChapterDirector.Instance; var clock = DawnClock.Instance;
            yield return GoTo("obj_c2_lamps");
            float m = clock.Minutes;
            cd.LightLamp(0); cd.LightLamp(1); Assert.AreEqual(2, cd.LampsLit);
            cd.LightLamp(4); Assert.AreEqual(0, cd.LampsLit, "wrong lamp blows all out"); Assert.Greater(clock.Minutes - m, 1.9f, "wrong lamp costs 2 minutes");
            for (int i = 0; i < 5; i++) cd.LightLamp(i);
            yield return null;
            Assert.AreEqual("obj_c2_pujari", story.Current.id, "five lamps in sun order complete the objective");
            yield return GoTo("obj_c2_deduce");
            cd.OpenBoard(); Assert.IsTrue(cd.BoardOpen);
            m = clock.Minutes; cd.BoardAnswerPick(0); Assert.AreEqual(0, cd.BoardStep); Assert.Greater(clock.Minutes - m, 0.9f, "wrong deduction costs a minute");
            cd.BoardAnswerPick(1); cd.BoardAnswerPick(0); cd.BoardAnswerPick(1);
            yield return null;
            Assert.AreEqual("obj_c2_exit", story.Current.id);
            yield return GoTo("obj_c3_traps");
            Assert.AreEqual(3, cd.Traps.Count, "three bell traps");
            cd.BeginDisarm(cd.Traps[0], true); m = clock.Minutes;
            cd.DisarmPress(0.05f); Assert.AreEqual(1, cd.BellsRung, "a miss rings the bell"); Assert.Greater(clock.Minutes - m, 2.9f);
            foreach (var t in cd.Traps) { cd.BeginDisarm(t, true); for (int i = 0; i < 3; i++) cd.DisarmPress(0.5f); Assert.IsTrue(t.disarmed); }
            yield return null;
            Assert.AreEqual("obj_c3_clues", story.Current.id);
            yield return GoTo("obj_c3_forensics");
            var kit = ForensicKit.Instance; kit.ResetKit(); VirtualInput.Press("kit");
            yield return null; yield return null;
            Assert.IsTrue(kit.IsOpen, "kit opens");
            kit.Confirm(); Assert.AreEqual(0, kit.Task, "ruler not lined up yet");
            kit.SlideRuler(6.5f); kit.Confirm(); kit.SlideRuler(10f); kit.Confirm();
            kit.Answer(0); Assert.AreEqual(0, kit.Task, "wrong boot"); kit.Answer(1); Assert.AreEqual(1, kit.Task);
            kit.HoldLamp(2.1f); Assert.AreEqual(2, kit.Task);
            kit.Confirm(); Assert.AreEqual(2, kit.Task, "pieces not joined yet");
            for (int pc = 0; pc < 4; pc++) { kit.Answer(pc); for (int r = 0; r < 4 && kit.PieceRotation(pc) != 0; r++) kit.RotatePiece(); }
            kit.Confirm();
            yield return null;
            Assert.AreEqual(3, kit.Task); Assert.AreEqual("obj_c3_pujari", story.Current.id);
            Debug.Log("[V2Test] chapters 2-3 ok");
        }

        [UnityTest]
        public IEnumerator Chapter1_Chase_And_Chapter4_TimedWall()
        {
            yield return Boot("Courtyard");
            var story = StoryDialogueSystem.Instance; var cd = ChapterDirector.Instance; var clock = DawnClock.Instance;
            yield return GoTo("obj_c1_chase");
            yield return new WaitForSeconds(1.5f);
            Assert.IsTrue(cd.ChaseRunning, "chase starts"); Assert.IsNotNull(GameObject.Find("Marker_Runner"), "runner exists");
            Vector3 a = GameObject.Find("Marker_Runner").transform.position;
            yield return new WaitForSeconds(2f);
            Assert.Greater((GameObject.Find("Marker_Runner").transform.position - a).magnitude, 3f, "runner runs");
            cd.DebugEndChase(); yield return null;
            Assert.AreEqual("obj_c1_token", story.Current.id); Assert.IsNotNull(GameObject.Find("Clue_Token17"), "token dropped");
            yield return GoTo("obj_c4_wall");
            var wall = ChakraPuzzleController.Active; Assert.IsNotNull(wall, "wall in scene");
            float m = clock.Minutes;
            wall.PressSymbol(2); Assert.Greater(clock.Minutes - m, 3.9f, "wrong symbol costs 4 minutes"); Assert.AreEqual(1, wall.Resets);
            wall.PressSymbol(0); Assert.Greater(wall.ResetLeft, 60f, "reset timer started");
            wall.PressSymbol(1); wall.PressSymbol(2); wall.PressSymbol(3); wall.TurnCentre();
            Assert.IsTrue(wall.Cranking, "hand-wheel freed"); Assert.IsFalse(wall.IsSolved);
            wall.CrankPush(1); wall.CrankPush(1); Assert.Less(wall.Crank, 0.1f, "same key twice does not crank");
            for (int i = 0; i < 16; i++) wall.CrankPush(i % 2 == 0 ? 2 : 1);
            Assert.IsTrue(wall.IsSolved, "alternating A/D opens the wall");
            Debug.Log("[V2Test] chase and wall ok");
        }

        [UnityTest]
        public IEnumerator Weather_On_Off_And_Audio_Mixer()
        {
            yield return Boot("Courtyard");
            var w = WeatherManager.Instance; Assert.IsNotNull(w);
            GameOptions.WeatherQuality = 2; w.SetTarget(0.8f, true, true);
            yield return new WaitForSeconds(2.5f);
            Assert.IsTrue(w.RainActive, "rain emits at High"); Assert.Greater(w.ParticleCount, 200, "rain particles alive"); Assert.LessOrEqual(w.ParticleCount, 3900, "particle cap");
            Assert.Less(w.FlameFactor, 0.9f, "flames gutter in rain");
            GameOptions.WeatherQuality = 0;
            yield return new WaitForSeconds(1.5f);
            Assert.IsFalse(w.RainActive, "weather Off stops the rain");
            GameOptions.WeatherQuality = 2;
            var ad = AudioDirector.Instance; var am = AudioManager.Instance;
            Assert.IsTrue(ad.MixerLoaded, "mixer asset loads"); Assert.AreEqual(5, ad.StemsPlaying, "five music stems playing in sync");
            yield return new WaitForSeconds(0.6f);
            Assert.AreEqual("Calm", ad.Snapshot);
            int n = am.SfxPlayed; am.PlaySFX("sfx_hit_heavy"); am.PlaySFX("sfx_thunder_near"); Assert.AreEqual(2, am.SfxPlayed - n, "effects play");
            foreach (string clip in new[] { "fs_stone_1", "fs_wet_3", "fs_wood_2", "fs_gravel_1", "fs_grass_2", "fs_mud_1", "amb_rain_light", "amb_rain_heavy", "amb_rain_interior", "sfx_bell_trip", "sfx_blade_block", "sfx_brazier_crash", "sfx_swish_wide", "sfx_grunt_aryan", "mus_combat", "mus_resolve", "sfx_stone_grind", "amb_jungle_night" })
                Assert.IsNotNull(Resources.Load<AudioClip>("Audio/" + clip), "missing clip " + clip);
            foreach (var l in new[] { "c1_puj_01", "c5_ary_04", "bk_ary_01", "c0_kis_01" })
            { var line = StoryData.Line(l); Assert.IsNotNull(line, l); Assert.IsNotNull(Resources.Load<AudioClip>("Voice/" + line.s + "/" + l + "_tts"), "voice file for " + l); }
            StoryDialogueSystem.Instance.Say("phase1");
            yield return new WaitForSeconds(0.5f);
            Assert.AreEqual("shout", VoicePlayer.LastTier); Assert.AreEqual("shout", ad.Tier, "Aryan's shout tier reaches the mixer"); Assert.That(VoicePlayer.Instance.LastPlayed.Contains("c5_ary_04"), VoicePlayer.Instance.LastPlayed);
            Assert.IsTrue(VoicePlayer.Instance.Source.isPlaying, "voice audio source playing");
            yield return new WaitForSeconds(0.4f);
            Assert.Less(ad.DuckDb, -3f, "music ducks under the voice");
            Debug.Log("[V2Test] weather and audio ok, particles at 0.8 = " + w.ParticleCount);
        }

        [UnityTest]
        public IEnumerator Combat_Flow_And_OnScreen_Controls()
        {
            yield return Boot("Courtyard");
            var story = StoryDialogueSystem.Instance; var fd = FightDirector.Instance; var touch = TouchControls.Instance;
            // on-screen controls respond outside a fight when set to Always
            GameOptions.TouchMode = 2;
            yield return new WaitForSeconds(0.5f);
            Assert.IsTrue(touch.Visible, "on-screen controls visible (Always)");
            var pc = Object.FindAnyObjectByType<PlayerController>(); int j = pc.JumpCount;
            Debug.Log($"[V2Test] before JUMP click: blocked={InputGate.GameplayBlocked}, enabled={pc.enabled}, crouch={pc.IsCrouching}, vy={pc.VerticalVelocity:0.00}, timeScale={Time.timeScale}");
            Assert.IsTrue(touch.Click("jump")); yield return new WaitForSeconds(0.3f);
            Assert.AreEqual(1, pc.JumpCount - j, "JUMP button makes one hop");
            GameOptions.TouchMode = 1;
            yield return new WaitForSeconds(0.3f);
            Assert.IsFalse(touch.Visible, "hidden outside fights in the default mode");

            StoryFlags.Set("trust_meera", 2);
            yield return GoTo("obj_c5_fight");
            float w = 0f; while (w < 20f && fd.Phase != 1) { w += 0.1f; yield return SkipDialogue(0.1f); }
            Assert.AreEqual(1, fd.Phase, "fight reaches phase 1"); Assert.IsTrue(CombatState.InCombat); Assert.IsNotNull(fd.Bhaskar);
            yield return new WaitForSeconds(0.5f);
            Assert.IsTrue(touch.Visible, "on-screen controls appear in the fight"); Assert.IsTrue(FightHud.Instance.Visible, "fight HUD visible");
            pc = Object.FindAnyObjectByType<PlayerController>();
            var b = fd.Bhaskar;
            pc.Teleport(b.transform.position + b.transform.forward * 1.5f, Quaternion.LookRotation(-b.transform.forward));
            yield return new WaitForSeconds(0.2f);
            float g = b.Guard; touch.Click("attack");
            yield return new WaitForSeconds(0.6f);
            Assert.Less(b.Guard, g, "ATTACK button hit drains his Guard"); Assert.GreaterOrEqual(fd.HitsLanded, 1);
            float st = fd.Stamina; g = b.Guard; fd.Attack(true); yield return new WaitForSeconds(0.9f);
            Assert.Less(fd.Stamina, st + 0.1f); Assert.Less(b.Guard, g - 10f, "heavy attack does more");
            Assert.IsTrue(Flashlight.Flash(), "flash available"); yield return null;
            Assert.AreEqual(EnemyState.Stunned, b.State, "flash stuns him"); Assert.IsFalse(Flashlight.Flash(), "flash recharging");
            float h = fd.Health; fd.Hurt(18f, b.transform.position); Assert.Less(fd.Health, h, "damage lowers health");
            st = fd.Stamina; fd.Dodge(); Assert.IsTrue(fd.Invulnerable, "dodge gives a short invulnerable step"); Assert.Less(fd.Stamina, st);
            // enemy attacks are telegraphed
            int started = b.AttacksStarted; w = 0f; bool sawTelegraph = false;
            while (w < 9f && !sawTelegraph) { w += Time.deltaTime; if (b.State == EnemyState.Telegraph) sawTelegraph = true; yield return null; }
            Assert.IsTrue(sawTelegraph && b.AttacksStarted > started, "Bhaskar attacks with a telegraph");
            // phase 1 -> Kishore turns (trust 2) -> phase 2
            b.TakeGuard(9999f); Assert.AreEqual(EnemyState.Staggered, b.State); fd.Finisher(b);
            w = 0f; while (w < 15f && fd.Phase != 2) { w += Time.deltaTime; yield return null; }
            Assert.AreEqual(2, fd.Phase); Assert.AreEqual(1, StoryFlags.Get("kishore_turned"), "Kishore turned on Bhaskar"); Assert.IsTrue(story.HasClue("relic_manuscript"), "manuscript caught");
            g = b.Guard; fd.KnockBrazier(0); 
            pc.Teleport(b.transform.position + b.transform.forward * 1.5f, Quaternion.LookRotation(-b.transform.forward));
            b.TakeGuard(9999f); fd.Finisher(b);
            w = 0f; while (w < 15f && fd.Phase != 3) { w += Time.deltaTime; yield return null; }
            Assert.AreEqual(3, fd.Phase); Assert.IsFalse(b.hasBlade, "blade disarmed after phase 2");
            yield return new WaitForSeconds(0.5f);
            Assert.AreEqual(EnemyState.Flee, b.State, "he runs for the ford");
            fd.DebugGrapple(); fd.GrappleTwist(0.5f);
            w = 0f; while (w < 15f && story.Current != null && story.Current.id == "obj_c5_fight") { w += 0.1f; yield return SkipDialogue(0.1f); }
            Assert.AreEqual("obj_c5_recover", story.Current.id, "fight won: recover the pieces"); Assert.IsFalse(CombatState.InCombat);
            Assert.AreEqual(EnemyState.Down, b.State, "Bhaskar is subdued, not killed");
            Debug.Log("[V2Test] combat and on-screen controls ok");
        }
    }
}
