using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using LegacyOfTemples.Characters;
using LegacyOfTemples.Core;
using LegacyOfTemples.Gameplay;
using LegacyOfTemples.Gameplay.Vehicles;
using LegacyOfTemples.UI;

namespace LegacyOfTemples.Tests
{
    // Exercises the real authored stairs and vehicle, rather than comparing constants.
    public class ReviewRegressionTests
    {
        Keyboard keyboard;
        GameObject direction, wall;
        Func<bool> oldGate;
        static string Evidence => Path.GetFullPath(Path.Combine(Application.dataPath, "../../Docs/QA/2026-11-23-review/editor"));

        [UnitySetUp] public IEnumerator Setup()
        {
            Directory.CreateDirectory(Evidence);
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            keyboard = InputSystem.AddDevice<Keyboard>();
            keyboard.MakeCurrent();
            oldGate = InputGate.UiOpen;
            Time.timeScale = 1f;
            yield return null;
        }

        [UnityTearDown] public IEnumerator Cleanup()
        {
            VirtualInput.Clear();
            InputGate.UiOpen = oldGate;
            if (keyboard != null) InputSystem.RemoveDevice(keyboard);
            if (direction != null) UnityEngine.Object.Destroy(direction);
            if (wall != null) UnityEngine.Object.Destroy(wall);
            Time.timeScale = 1f;
            yield return null;
        }

        static void Note(string text)
        {
            File.AppendAllText(Path.Combine(Evidence, "measurements.txt"), text + "\n");
            Debug.Log("[Review] " + text);
        }

        IEnumerator VehicleHeights(Jeep jeep, PlayerController pc, string phase, float seconds)
        {
            string file = Path.Combine(Evidence, "vehicle-heights.csv");
            if (!File.Exists(file)) File.WriteAllText(file,"phase,time,heroY,carY,occupied,transitioning,capsule\n");
            for(float t=0;t<seconds;t+=0.15f)
            {
                File.AppendAllText(file,$"{phase},{t:F2},{pc.transform.position.y:F4},{jeep.transform.position.y:F4},{jeep.Occupied},{jeep.Transitioning},{pc.GetComponent<CharacterController>().enabled}\n");
                yield return new WaitForSecondsRealtime(0.15f);
            }
        }

        IEnumerator Load(string scene)
        {
            PlayerPrefs.SetInt("OpeningCinematicActive", 0);
            yield return SceneManager.LoadSceneAsync(scene);
            yield return new WaitForSeconds(2f);
            IntroCinematic.SkipNow = true;
            foreach (var c in UnityEngine.Object.FindObjectsByType<OpeningCinematic>()) c.EndCinematic();
            TutorialManager.Instance?.SkipTutorial();
            UIManager.Instance?.DismissHowToPlay();
            // Isolate locomotion from unrelated objective cards during these movement tests.
            InputGate.UiOpen = () => false;
            Time.timeScale = 1f;
            yield return new WaitForSecondsRealtime(6f);
        }

        IEnumerator Screenshot(string name, PlayerController pc)
        {
            var cam = Camera.main;
            var orbit = cam.GetComponent<OrbitCamera>();
            if (orbit != null) orbit.SetCutsceneOverride(pc.transform.position + new Vector3(4f, 2.5f, 5f),
                Quaternion.LookRotation(pc.transform.position + Vector3.up - (pc.transform.position + new Vector3(4f, 2.5f, 5f))));
            yield return null; yield return null;
            if (Application.isBatchMode) WindowlessShot.Save(Path.Combine(Evidence, name + ".png"));
            else ScreenCapture.CaptureScreenshot(Path.Combine(Evidence, name + ".png"));
            yield return null; yield return null;
            if (orbit != null) orbit.ClearCutsceneOverride();
        }

        [UnityTest] public IEnumerator Courtyard_AuthoredSteps_ClimbAboveOneMetre_AndJump()
        {
            yield return Load("Courtyard");
            var pc = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
            Assert.NotNull(pc);
            pc.enabled = true;
            direction = new GameObject("Review movement direction");
            direction.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            pc.SetCameraTransform(direction.transform);
            var step = GameObject.Find("Step_5").GetComponent<Collider>().bounds;
            pc.Teleport(new Vector3(step.center.x, step.min.y + 0.05f, step.max.z + 1.1f), Quaternion.Euler(0f, 180f, 0f));
            float start = pc.GetComponent<CharacterGrounding>().FeetY;
            for (float t = 0; t < 4f && pc.transform.position.z > -0.35f; t += Time.deltaTime)
            {
                VirtualInput.Move = Vector2.up; yield return null;
            }
            VirtualInput.Clear();
            yield return new WaitForSeconds(0.3f);
            float top = pc.GetComponent<CharacterGrounding>().FeetY;
            Note($"Courtyard actual steps: startFeet={start:F3}, topFeet={top:F3}, z={pc.transform.position.z:F3}, enabled={pc.enabled}, cc={pc.GetComponent<CharacterController>().enabled}, keyW={keyboard.wKey.isPressed}, moving={pc.IsMoving}, blocked={InputGate.GameplayBlocked}");
            yield return Screenshot("courtyard-stairs-top", pc);
            Assert.Greater(top - start, 1f, "Must reach the upper tread, above the old 1 m clamp");
            int jumps = pc.JumpCount;
            VirtualInput.Press("jump"); yield return null;
            VirtualInput.Clear();
            float apex = top;
            for (float t = 0; t < 1.6f; t += Time.deltaTime) { yield return null; apex = Mathf.Max(apex, pc.GetComponent<CharacterGrounding>().FeetY); }
            Note($"Jump on upper tread: rise={apex-top:F3}, landingError={Mathf.Abs(pc.GetComponent<CharacterGrounding>().FeetY-top):F3}");
            Assert.Greater(pc.JumpCount, jumps);
            Assert.Greater(apex - top, 0.45f);
            Assert.Less(Mathf.Abs(pc.GetComponent<CharacterGrounding>().FeetY - top), 0.08f);
        }

        [UnityTest] public IEnumerator Jungle_Authored35cmSteps_ClimbAndDescend()
        {
            yield return Load("JunglePath");
            var pc = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
            Assert.NotNull(pc); pc.enabled = true;
            var first = GameObject.Find("RuinedStep_1").GetComponent<Collider>().bounds;
            var last = GameObject.Find("RuinedStep_7").GetComponent<Collider>().bounds;
            direction = new GameObject("Review movement direction"); direction.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            pc.SetCameraTransform(direction.transform);
            pc.Teleport(new Vector3(first.center.x, first.min.y + 0.1f, first.max.z + 1f), Quaternion.Euler(0f, 180f, 0f));
            float start = pc.GetComponent<CharacterGrounding>().FeetY;
            for (float t = 0; t < 6f && pc.transform.position.z > last.center.z; t += Time.deltaTime)
            { VirtualInput.Move = Vector2.up; yield return null; }
            VirtualInput.Clear(); yield return new WaitForSeconds(0.4f);
            float top = pc.GetComponent<CharacterGrounding>().FeetY;
            Note($"Jungle actual steps: startFeet={start:F3}, topFeet={top:F3}, targetTop={last.max.y:F3}, z={pc.transform.position.z:F3}, enabled={pc.enabled}, cc={pc.GetComponent<CharacterController>().enabled}, moving={pc.IsMoving}, blocked={InputGate.GameplayBlocked}");
            yield return Screenshot("jungle-stairs-top", pc);
            Assert.That(top, Is.EqualTo(last.max.y).Within(0.08f), "Must reach all seven real treads without jumping");
            direction.transform.rotation = Quaternion.identity;
            for (float t = 0; t < 5f && pc.transform.position.z < first.max.z + 0.7f; t += Time.deltaTime)
            { VirtualInput.Move = Vector2.up; yield return null; }
            VirtualInput.Clear(); yield return new WaitForSeconds(0.5f);
            Assert.Less(Mathf.Abs(pc.GetComponent<CharacterGrounding>().FeetY - start), 0.08f, "Must return to ground after descent");
        }

        [UnityTest] public IEnumerator Dialogue_PlainNpcLine_UsesRecordedVoice()
        {
            yield return Load("Sanctum");
            var line = VoiceLibrary.Line("v3_puj_01"); Assert.NotNull(line); Assert.NotNull(line.clip);
            UIManager.Instance.StartDialogue("Pujari Ananda", new[] { line.text });
            yield return null;
            Assert.NotNull(VoiceManager.Instance.Current, "Plain dialogue must trigger the voice hook too");
            Assert.AreEqual(line.id, VoiceManager.Instance.Current.id);
            Assert.AreEqual(line.clip, VoiceManager.Instance.Source2D.clip);
            Note("Plain NPC dialogue triggered Pujari recording " + line.id);
            UIManager.Instance.EndDialogue();
        }

        [UnityTest] public IEnumerator Stepwell_BothAuthoredRamps_ReachFiveMetreRim()
        {
            yield return Load("Courtyard");
            var pc = UnityEngine.Object.FindAnyObjectByType<PlayerController>(); pc.enabled = true;
            var well = StepwellPuzzle.Instance; Assert.NotNull(well); Assert.IsTrue(well.Built);
            var r = well.transform;
            direction = new GameObject("Review ramp direction"); pc.SetCameraTransform(direction.transform);
            foreach (bool inside in new[] { false, true })
            {
                Vector3 start = r.TransformPoint(inside ? new Vector3(-5.25f, 0.2f, 5.4f) : new Vector3(6.4f, 0.1f, -8.5f));
                direction.transform.rotation = Quaternion.LookRotation(inside ? -r.forward : -r.right);
                pc.Teleport(start, direction.transform.rotation);
                for (float t=0; t<6f; t+=Time.unscaledDeltaTime)
                {
                    Vector3 local = r.InverseTransformPoint(pc.transform.position);
                    if (inside ? local.z < -4.4f : local.x < -4.8f) break;
                    VirtualInput.Move = Vector2.up; yield return null;
                }
                VirtualInput.Clear(); yield return new WaitForSecondsRealtime(0.3f);
                float feet = pc.GetComponent<CharacterGrounding>().FeetY - r.position.y;
                Note($"Stepwell {(inside ? "inner" : "outer")} ramp: local={r.InverseTransformPoint(pc.transform.position):F3}, feet={feet:F3}");
                yield return Screenshot(inside ? "stepwell-inner-ramp" : "stepwell-outer-ramp", pc);
                Assert.Greater(feet, 5f, "Ramp must reach its landing without jumping");
            }
        }

        [UnityTest] public IEnumerator LedgeHop_OneMetre_AndBlockedWallRecovery()
        {
            yield return Load("Sanctum");
            var pc = UnityEngine.Object.FindAnyObjectByType<PlayerController>(); pc.enabled = true;
            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube); ground.transform.position = new Vector3(900f, -0.5f, 900f); ground.transform.localScale = new Vector3(20f,1f,20f);
            wall = GameObject.CreatePrimitive(PrimitiveType.Cube); wall.transform.position = new Vector3(900f,0.5f,901.5f); wall.transform.localScale = new Vector3(4f,1f,3f);
            Physics.SyncTransforms();
            pc.Teleport(new Vector3(900f,0.005f,899.55f), Quaternion.identity);
            var traversal = pc.GetComponent<CharacterTraversal>();
            Assert.IsTrue(traversal.TryClimb(Vector3.forward));
            yield return new WaitForSecondsRealtime(1.2f);
            Assert.IsFalse(traversal.IsClimbing);
            Assert.That(pc.GetComponent<CharacterGrounding>().FeetY, Is.EqualTo(1.005f).Within(0.08f));
            Note($"Ledge hop landed with feet={pc.GetComponent<CharacterGrounding>().FeetY:F3}");
            yield return Screenshot("one-metre-ledge",pc);
            wall.transform.position = new Vector3(900f,2f,902f); wall.transform.localScale = new Vector3(4f,4f,1f); Physics.SyncTransforms();
            pc.Teleport(new Vector3(900f,0.005f,901.1f),Quaternion.identity);
            direction = new GameObject("Review wall direction"); pc.SetCameraTransform(direction.transform);
            int before = traversal.Recoveries;
            for(float t=0;t<4.5f;t+=Time.unscaledDeltaTime) { VirtualInput.Move=Vector2.up; yield return null; }
            VirtualInput.Clear();
            Note($"Blocked-wall recoveries={traversal.Recoveries-before}, position={pc.transform.position:F3}");
            Assert.Greater(traversal.Recoveries,before,"Three-second safety must find a nearby clear floor");
            Assert.Less(pc.transform.position.z,901.6f,"Recovery must not cross the wall");
            UnityEngine.Object.Destroy(ground);
        }

        [UnityTest] public IEnumerator Jeep_BlockedDriverSide_UsesClearExit_AndKeepsHeroGrounded()
        {
            yield return Load("Courtyard");
            var pc = UnityEngine.Object.FindAnyObjectByType<PlayerController>(); Assert.NotNull(pc); pc.enabled = true;
            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube); ground.name = "Review isolated vehicle ground";
            ground.transform.position = new Vector3(800f, -0.5f, 800f); ground.transform.localScale = new Vector3(30f, 1f, 30f);
            var j = Jeep.Spawn(new Vector3(800f, 0f, 800f), Quaternion.identity); Assert.NotNull(j);
            yield return new WaitForSeconds(2f);
            pc.Teleport(j.DriverExitPoint, Quaternion.identity); j.Interact();
            yield return VehicleHeights(j, pc, "enter", 3f);
            Assert.IsTrue(j.Occupied && j.Car.Driven, "Entry must finish");
            Assert.IsFalse(pc.GetComponent<CharacterController>().enabled, "Seated capsule must be disabled");
            j.Car.autopilot=true; j.Car.autoThrottle=0.4f;
            yield return VehicleHeights(j,pc,"drive",1f);
            Assert.Greater(j.Car.CurrentSpeed,0.5f,"Vehicle must move with the visible seated driver");
            j.Car.autoThrottle=0f; j.Car.autoBrake=true;
            yield return VehicleHeights(j,pc,"brake",1f);
            j.Car.autopilot=false;
            Vector3 original = j.DriverExitPoint;
            wall = GameObject.CreatePrimitive(PrimitiveType.Cube); wall.name = "Review wall beside driver door";
            wall.transform.position = original + Vector3.up; wall.transform.localScale = new Vector3(1.2f, 2.5f, 5f);
            Physics.SyncTransforms();
            Assert.IsTrue(j.TryFindSafeExit(pc, out var clear), "Passenger side or rear should remain clear");
            Assert.Greater(Vector3.Distance(clear, original), 1f);
            j.StartCoroutine(j.GetOut()); yield return VehicleHeights(j,pc,"exit",2f);
            Note($"Jeep blocked-door exit: old={original:F3}, actual={pc.transform.position:F3}, feet={pc.GetComponent<CharacterGrounding>().FeetY:F3}, enabled={pc.enabled}");
            yield return Screenshot("jeep-blocked-door-exit", pc);
            Assert.IsFalse(j.Occupied); Assert.IsTrue(pc.enabled);
            Assert.IsNull(pc.transform.parent);
            Assert.That(pc.GetComponent<CharacterGrounding>().FeetY, Is.EqualTo(0.005f).Within(0.06f));
            j.GetComponent<Rigidbody>().isKinematic=true;
            int recoveries=pc.GetComponent<CharacterTraversal>().Recoveries;
            pc.Teleport(j.transform.position+Vector3.up*3f,Quaternion.identity);
            yield return VehicleHeights(j,pc,"roof",0.8f);
            Assert.Greater(pc.GetComponent<CharacterTraversal>().Recoveries,recoveries,"Roof contact must place the hero on nearby safe ground");
            Assert.That(pc.GetComponent<CharacterGrounding>().FeetY,Is.EqualTo(0.005f).Within(0.06f));
            Note("Jeep roof contact recovered to clear ground without moving the parked car.");
            UnityEngine.Object.Destroy(j.gameObject); UnityEngine.Object.Destroy(ground);
        }
    }
}
