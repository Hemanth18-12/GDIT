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
        public IEnumerator EInteraction_KeepsVisibleCharacterAboveTheFloor()
        {
            yield return LoadOriginal();
            var pc = Object.FindAnyObjectByType<PlayerController>();
            var mirror = SunWallPuzzle.Instance.Mirrors[0];
            pc.Teleport(mirror.transform.position + Vector3.back * 1.6f, Quaternion.identity);
            PlayerController.ObjectiveFocus = mirror.transform;
            var animator = pc.GetComponentInChildren<Animator>();
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            var orbit = Object.FindAnyObjectByType<OrbitCamera>();
            Vector3 eye = pc.transform.position + new Vector3(-3f, 2f, -4f);
            orbit.SetCutsceneOverride(eye, Quaternion.LookRotation(pc.transform.position + Vector3.up - eye));
            yield return new WaitForSeconds(1f);
            Assert.IsFalse(InputGate.GameplayBlocked, "The test must press E through normal gameplay input");
            Assert.AreSame(mirror, pc.CurrentInteractable);
            Assert.IsTrue(CharacterMeshBounds.TryMeasure(pc.transform, out var before));
            float beforeFeet = pc.transform.TransformPoint(new Vector3(0f, before.min.y, 0f)).y;
            float beforeHead = animator.GetBoneTransform(HumanBodyBones.Head).position.y;
            float lowestFeet = beforeFeet;
            float lowestHead = beforeHead;
            float lowestRoot = pc.transform.position.y;
            var keyboard = InputSystem.AddDevice<Keyboard>("EInteractionReviewKeyboard");
            var previousBackground = InputSystem.settings.backgroundBehavior;
            var previousEditor = InputSystem.settings.editorInputBehaviorInPlayMode;
            string projectRoot = System.IO.Directory.GetParent(Application.dataPath).FullName;
            if (new System.IO.DirectoryInfo(projectRoot).Name == "My project") projectRoot = System.IO.Directory.GetParent(projectRoot).FullName;
            string output = System.IO.Path.Combine(projectRoot, "Docs/QA/2026-11-25-e-interaction");
            System.IO.Directory.CreateDirectory(output);
            InputSystem.EnableDevice(keyboard); keyboard.MakeCurrent();
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            try
            {
                int state = mirror.State;
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.E)); yield return null; yield return null;
                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                var trace = new System.Text.StringBuilder();
                int i = 0;
                for (float elapsed = 0f; elapsed < 2.8f; elapsed += Time.deltaTime, i++)
                {
                    yield return null;
                    Assert.IsTrue(CharacterMeshBounds.TryMeasure(pc.transform, out var posed));
                    lowestFeet = Mathf.Min(lowestFeet, pc.transform.TransformPoint(new Vector3(0f, posed.min.y, 0f)).y);
                    lowestHead = Mathf.Min(lowestHead, animator.GetBoneTransform(HumanBodyBones.Head).position.y);
                    lowestRoot = Mathf.Min(lowestRoot, pc.transform.position.y);
                    trace.AppendLine($"{i}, root={pc.transform.position.y:0.000}, head={animator.GetBoneTransform(HumanBodyBones.Head).position.y:0.000}, meshFeet={pc.transform.TransformPoint(new Vector3(0f, posed.min.y, 0f)).y:0.000}, state={animator.GetCurrentAnimatorStateInfo(0).shortNameHash}");
                    if (i == 12) CaptureCharacterPose(System.IO.Path.Combine(output, "editor-e-interaction.png"));
                }
                System.IO.File.WriteAllText(System.IO.Path.Combine(output,"editor-pose-trace.txt"),trace.ToString());
                Debug.Log($"[EInteraction] before feet={beforeFeet:0.000}; lowest posed feet={lowestFeet:0.000}; head {beforeHead:0.000}->{lowestHead:0.000}; lowest capsule root={lowestRoot:0.000}; mirror {state}->{mirror.State}");
                Assert.AreNotEqual(state, mirror.State, "E must still operate the real mirror");
                Assert.GreaterOrEqual(lowestFeet, beforeFeet - 0.12f, "Pressing E must not drop the visible mesh below the floor while the capsule stays grounded");
                Assert.GreaterOrEqual(lowestHead, beforeHead - 0.25f, "Pressing E must not collapse the character's pose");
            }
            finally
            {
                InputSystem.settings.backgroundBehavior = previousBackground;
                InputSystem.settings.editorInputBehaviorInPlayMode = previousEditor;
                InputSystem.RemoveDevice(keyboard);
            }
        }

        static void CaptureCharacterPose(string path)
        {
            var camera = Camera.main;
            var previousTarget = camera.targetTexture;
            var previousActive = RenderTexture.active;
            var target = RenderTexture.GetTemporary(1280, 720, 24);
            var pixels = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            try
            {
                camera.targetTexture = target; camera.Render(); RenderTexture.active = target;
                pixels.ReadPixels(new Rect(0f, 0f, 1280f, 720f), 0, 0); pixels.Apply();
                System.IO.File.WriteAllBytes(path, pixels.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = previousTarget; RenderTexture.active = previousActive;
                RenderTexture.ReleaseTemporary(target); Object.Destroy(pixels);
            }
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
