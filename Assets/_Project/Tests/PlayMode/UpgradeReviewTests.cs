using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using LegacyOfTemples.Core;
using LegacyOfTemples.Characters;
using LegacyOfTemples.Gameplay;
using LegacyOfTemples.Gameplay.Vehicles;
using LegacyOfTemples.UI;

namespace LegacyOfTemples.Tests
{
    public class UpgradeReviewTests
    {
        Keyboard reviewKeyboard;
        [TearDown]
        public void RemoveReviewKeyboard()
        {
            if (reviewKeyboard != null && reviewKeyboard.added) InputSystem.RemoveDevice(reviewKeyboard);
            reviewKeyboard = null;
        }
        static IEnumerator World()
        {
            StoryDialogueSystem.ResetForNewGame();
            SceneManager.LoadScene(Constants.TempleRing);
            yield return new WaitForSeconds(3f);
            foreach (var intro in Object.FindObjectsByType<OpeningCinematic>()) intro.EndCinematic();
            UIManager.Instance?.DismissHowToPlay(); TutorialManager.Instance?.SkipTutorial();
            Time.timeScale = 1f;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            // The normal input guard intentionally blocks the first three frames after a card closes.
            yield return new WaitForSeconds(0.2f);
        }

        [UnityTest]
        public IEnumerator AmbienceRemainsAudibleWithMusicMuted_AndSettingsSurviveManagerReload()
        {
            var am = AudioManager.Instance;
            float master = am.MasterVolume, music = am.MusicVolume, ambience = am.AmbienceVolume, effects = am.SFXVolume, voice = am.DialogueVolume;
            try
            {
                am.SetMasterVolume(0.8f); am.SetMusicVolume(0f); am.SetAmbienceVolume(0.6f);
                am.SetSFXVolume(0.4f); am.SetDialogueVolume(0.3f);
                am.PlayAmbience("ambience_temple");
                var source = am.GetComponents<AudioSource>().Single(a => a.clip != null && a.clip.name == "ambience_temple");
                Assert.Greater(source.volume, 0.1f, "Muting music must not mute the ambience source");
                am.SetAmbienceVolume(0f); am.SetMusicVolume(0.7f);
                Assert.AreEqual(0f, source.volume, "Muting ambience must survive a music change");
                Object.Destroy(am.gameObject); yield return null;
                am = AudioManager.Instance;
                Assert.AreEqual(0.8f, am.MasterVolume, 0.001f);
                Assert.AreEqual(0.7f, am.MusicVolume, 0.001f);
                Assert.AreEqual(0f, am.AmbienceVolume, 0.001f);
                Assert.AreEqual(0.4f, am.SFXVolume, 0.001f);
                Assert.AreEqual(0.3f, VoicePlayer.Volume, 0.001f);
            }
            finally
            {
                am = AudioManager.Instance;
                am.SetMasterVolume(master); am.SetMusicVolume(music); am.SetAmbienceVolume(ambience);
                am.SetSFXVolume(effects); am.SetVoiceVolume(voice);
            }
        }

        [UnityTest]
        public IEnumerator JeepRKey_RecoversToBuiltWindingRoad_WithoutLandingOnOwnRoof()
        {
            yield return World();
            // Headless Editor may disable the physical keyboard. Use a dedicated input device.
            reviewKeyboard = InputSystem.AddDevice<Keyboard>("UpgradeReviewKeyboard");
            InputSystem.EnableDevice(reviewKeyboard);
            var jeep = Jeep.Instance; var pc = Object.FindAnyObjectByType<PlayerController>();
            var roads = Object.FindObjectsByType<DriveableRoad>();
            Assert.IsNotEmpty(roads);
            foreach (Vector3 crash in new[] { new Vector3(33f, 0.4f, 250f), new Vector3(-28f, 0.4f, 385f) })
            {
                jeep.transform.SetPositionAndRotation(crash, Quaternion.Euler(0f, 40f, 75f));
                var body = jeep.GetComponent<Rigidbody>(); body.position = crash; body.rotation = jeep.transform.rotation;
                body.linearVelocity = Vector3.zero; body.angularVelocity = Vector3.zero;
                pc.Teleport(crash + Vector3.right * 4f, Quaternion.identity);
                Physics.SyncTransforms(); yield return null;
                Assert.IsFalse(InputGate.GameplayBlocked, "Close tutorial cards before testing keyboard recovery");
                Debug.Log($"[UpgradeReview] Before R: occupied={jeep.Occupied}, transitioning={jeep.Transitioning}, brawl={Brawl.Active != null}, combat={CombatState.InCombat}, playerGap={Vector3.Distance(pc.transform.position, jeep.transform.position)}, keyboard={Keyboard.current.enabled}");
                reviewKeyboard.MakeCurrent();
                InputSystem.QueueStateEvent(reviewKeyboard, new KeyboardState(Key.R));
                yield return null; yield return null;
                Debug.Log($"[UpgradeReview] After R: pressed={Keyboard.current.rKey.isPressed}, down={Keyboard.current.rKey.wasPressedThisFrame}, position={jeep.transform.position}");
                InputSystem.QueueStateEvent(reviewKeyboard, new KeyboardState());
                yield return new WaitForSeconds(0.25f);
                Vector3 actual = jeep.transform.position;
                float gap = roads.Min(r => Vector2.Distance(new Vector2(actual.x, actual.z), new Vector2(r.ClosestPoint(actual).x, r.ClosestPoint(actual).z)));
                Assert.Less(gap, 1.5f, "R must recover onto an actual built road segment");
                Assert.Less(Vector2.Distance(new Vector2(actual.x, actual.z), new Vector2(crash.x, crash.z)), 35f, "Recovery must stay near the crash");
                Assert.Less(actual.y, 0.8f, "The reset ray must not land on the Jeep's own roof");
                Assert.Less(Vector3.Angle(jeep.transform.up, Vector3.up), 8f);
            }
        }

        [UnityTest]
        public IEnumerator SunWall_RequiresRealMirrorReflections_AndBreaksWhenMisaligned()
        {
            yield return World();
            var zone = NewSunWall.Instance;
            Assert.AreEqual(0, zone.WheelSocket.State);
            zone.MirrorA.Interact(); yield return new WaitForSeconds(0.6f);
            zone.MirrorB.Interact(); yield return new WaitForSeconds(0.6f);
            Assert.AreEqual(0, zone.WheelSocket.State, "One wrong mirror angle must not solve the puzzle");
            zone.MirrorB.Interact(); yield return new WaitForSeconds(1f);
            Assert.AreSame(zone.WheelSocket, zone.Emitter.LastSocket, "The ray must reach the receiver through physical mirror faces");
            Assert.AreEqual(1, zone.WheelSocket.State);
            Assert.GreaterOrEqual(zone.Emitter.Points.Count, 4);
            zone.MirrorA.Interact(); yield return new WaitForSeconds(0.8f);
            Assert.AreEqual(0, zone.WheelSocket.State, "Moving a mirror away must interrupt the light");
        }
    }
}
