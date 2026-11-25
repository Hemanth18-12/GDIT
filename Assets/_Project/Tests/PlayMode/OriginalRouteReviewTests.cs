using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using LegacyOfTemples.Core;
using LegacyOfTemples.Characters;
using LegacyOfTemples.Gameplay;
using LegacyOfTemples.Gameplay.Vehicles;
using LegacyOfTemples.UI;

namespace LegacyOfTemples.Tests
{
    /// <summary>Checks the established locations confirmed by the user's screenshots.</summary>
    public class OriginalRouteReviewTests
    {
        static IEnumerator LoadOriginal()
        {
            StoryDialogueSystem.ResetForNewGame();
            SceneManager.LoadScene(Constants.Courtyard);
            yield return new WaitForSeconds(12f);
            foreach (var intro in Object.FindObjectsByType<OpeningCinematic>()) intro.EndCinematic();
            UIManager.Instance?.DismissHowToPlay(); TutorialManager.Instance?.SkipTutorial();
            Time.timeScale = 1f; yield return new WaitForSeconds(0.3f);
        }

        [UnityTest, Timeout(90000)]
        public IEnumerator EstablishedLocations_ArePreserved_AndSunWallUsesRealReflections()
        {
            yield return LoadOriginal();
            Assert.IsNull(NewWorldDirector.Instance, "The replacement TempleRing must not be the original route");
            Assert.IsNotNull(TempleSite.Instance);
            Assert.Greater(TempleSite.Footprint.size.x, 200f, "Preserve the established temple kit footprint used by the confirmed locations");
            Assert.IsNotNull(TempleSite.Instance.Temple.Find("LOD0"));
            var closedDoor = GameObject.Find("Courtyard_Sanctum_ClosedDoors/Closed wooden doors");
            Assert.IsNotNull(closedDoor, "The original Courtyard doorway must be sealed");
            Assert.IsFalse(closedDoor.GetComponent<Collider>().isTrigger);
            var portal = GameObject.Find("Portal_To_Sanctum");
            Assert.IsFalse(portal.GetComponent<SceneTransitionTrigger>().enabled);
            Assert.IsTrue(BreakInPuzzle.Instance.Built); Assert.IsTrue(VaultPuzzle.Instance.Built);
            Assert.IsTrue(StepwellPuzzle.Instance.Built); Assert.IsTrue(SunWallPuzzle.Instance.Built);
            Assert.IsTrue(JungleChase.Instance.Built);
            var sun = SunWallPuzzle.Instance;
            foreach (var mirror in sun.Mirrors)
            {
                mirror.Interact(); yield return new WaitForSeconds(0.4f);
                mirror.Interact(); yield return new WaitForSeconds(0.4f);
            }
            Assert.IsFalse(sun.Group.Solved, "The wheel spoke must still block one beam");
            sun.Wheel.Interact(); yield return new WaitForSeconds(1.5f);
            Assert.AreEqual(1, sun.Sockets[0].State); Assert.AreEqual(1, sun.Sockets[1].State);
            Assert.IsTrue(sun.Group.Solved, "Both real raycast beams must reach the original wall");
        }

        [UnityTest, Timeout(90000)]
        public IEnumerator OriginalApproachRoad_RKeyRecoversLocally()
        {
            yield return LoadOriginal();
            var keyboard=InputSystem.AddDevice<Keyboard>("OriginalRoadReviewKeyboard");
            InputSystem.EnableDevice(keyboard); keyboard.MakeCurrent();
            InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            try
            {
                var jeep=Jeep.Instance; var pc=Object.FindAnyObjectByType<PlayerController>();
                Vector3 road=LegacyOfTemples.World.Village.RoadPoint(LegacyOfTemples.World.Village.ZMax+80f);
                Vector3 crash=road+new Vector3(8f,0.3f,0f);
                var body=jeep.GetComponent<Rigidbody>();
                jeep.transform.SetPositionAndRotation(crash,Quaternion.Euler(0f,30f,70f));
                body.position=crash;body.rotation=jeep.transform.rotation;body.linearVelocity=body.angularVelocity=Vector3.zero;
                pc.Teleport(crash+Vector3.right*4f,Quaternion.identity);Physics.SyncTransforms();
                yield return new WaitForSeconds(0.2f);
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.R));yield return null;yield return null;
                InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return new WaitForSeconds(0.3f);
                Assert.Less(Vector2.Distance(new Vector2(jeep.transform.position.x,jeep.transform.position.z),new Vector2(road.x,road.z)),3f);
                Assert.Less(Vector3.Angle(jeep.transform.up,Vector3.up),6f);
                Assert.AreEqual(0,jeep.DamageState);
            }
            finally { InputSystem.RemoveDevice(keyboard); }
        }

        [UnityTest, Timeout(90000)]
        public IEnumerator PauseMenu_Fits720p_AndFiveSlidersReachAudioSystems()
        {
            yield return LoadOriginal();
            var audio = AudioManager.Instance;
            float master=audio.MasterVolume, music=audio.MusicVolume, ambience=audio.AmbienceVolume, effects=audio.SFXVolume, voice=VoicePlayer.Volume;
            UIManager.Instance.TogglePause(); yield return null; yield return null;
            try
            {
                var scroll = Object.FindObjectsByType<ScrollRect>().Single(s => s.name == "PauseMenuViewport");
                Assert.IsTrue(scroll.vertical); Assert.IsFalse(scroll.horizontal);
                var corners = new Vector3[4]; scroll.viewport.GetWorldCorners(corners);
                Assert.GreaterOrEqual(corners[0].y,0); Assert.LessOrEqual(corners[2].y,Screen.height);
                var sliders = scroll.content.GetComponentsInChildren<Slider>(); Assert.AreEqual(5,sliders.Length);
                sliders.Single(s=>s.name=="Slider_Master").value=0.6f;
                sliders.Single(s=>s.name=="Slider_Music").value=0f;
                sliders.Single(s=>s.name=="Slider_Ambience").value=0.8f;
                sliders.Single(s=>s.name=="Slider_Effects").value=0.4f;
                sliders.Single(s=>s.name=="Slider_Voice").value=0.7f;
                Assert.AreEqual(0.6f,audio.MasterVolume,0.01f); Assert.AreEqual(0f,audio.MusicVolume,0.01f);
                Assert.AreEqual(0.8f,audio.AmbienceVolume,0.01f); Assert.AreEqual(0.4f,audio.SFXVolume,0.01f);
                Assert.AreEqual(0.7f,VoicePlayer.Volume,0.01f);
                Assert.Greater(scroll.content.rect.height,scroll.viewport.rect.height,"Long menu must scroll instead of clipping off-screen");
            }
            finally
            {
                audio.SetMasterVolume(master);audio.SetMusicVolume(music);audio.SetAmbienceVolume(ambience);audio.SetSFXVolume(effects);audio.SetVoiceVolume(voice);
                if(UIManager.Instance.IsPauseOpen)UIManager.Instance.TogglePause();
            }
        }

        [UnityTest, Timeout(90000)]
        public IEnumerator Retry_RemainsInOriginalSceneAndUsesLocalClockBudget()
        {
            yield return LoadOriginal();
            var pc=Object.FindAnyObjectByType<PlayerController>();
            var place=V3Places.Get(5);
            Checkpoints.Save("sunwall","Sun Wall plaza",place.at,0f,6f*60f);
            pc.Teleport(place.at+Vector3.right*8f,Quaternion.identity);
            DawnClock.Instance.RetryOnSpot(false);yield return null;
            Assert.AreEqual(Constants.Courtyard,SceneManager.GetActiveScene().name);
            Assert.Less(Vector3.Distance(pc.transform.position,place.at),1f);
            Assert.AreEqual(6f,DawnClock.Instance.Minutes,0.1f);
            Checkpoints.Current.scene=Constants.TempleRing;
            Vector3 before=pc.transform.position;
            Assert.IsFalse(Checkpoints.Restore(pc.transform),"A replacement-world checkpoint cannot move the player in the original scene");
            Assert.AreEqual(before,pc.transform.position);
            Checkpoints.Clear();
        }
    }
}
