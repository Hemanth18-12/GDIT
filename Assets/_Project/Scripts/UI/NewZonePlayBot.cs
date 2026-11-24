using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using LegacyOfTemples.Core;
using LegacyOfTemples.Characters;
using LegacyOfTemples.Gameplay;
using LegacyOfTemples.Gameplay.Puzzles;
using LegacyOfTemples.Gameplay.Vehicles;

namespace LegacyOfTemples.UI
{
    /// <summary>Opt-in QA driver. Walks on the real colliders and attacks through the production input path.</summary>
    public sealed class NewZonePlayBot : MonoBehaviour
    {
        public static string Evidence;
        public static bool Finished, Passed;
        public static string Failure;
        public static IEnumerator BazaarRun(string directory)
        {
            Evidence = directory; Directory.CreateDirectory(directory); Finished = Passed = false; Failure = "";
            File.WriteAllText(Path.Combine(directory, "run.md"), "# New Bazaar — fresh gameplay run\n\n");
            var zone = NewBazaar.Instance; var pc = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
            var orbit = UnityEngine.Object.FindAnyObjectByType<OrbitCamera>();
            if (zone == null || pc == null) { Fail("Missing scene / player"); yield break; }
            foreach (var c in UnityEngine.Object.FindObjectsByType<OpeningCinematic>()) c.EndCinematic(); UIManager.Instance.DismissHowToPlay(); TutorialManager.Instance?.SkipTutorial();
            PlayerController.ObjectiveFocus = null; Time.timeScale = 1f;
            if (orbit != null) orbit.ClearCutsceneOverride();
            yield return new WaitForSeconds(2f);
            Check(zone.RingClip != null, "Bag ring clip loaded");
            yield return ArrivalDrive();
            yield return Shot("01-market-arrival.png", new Vector3(-14f, 5f, -4f), new Vector3(2f, 2f, 4f));
            // Start on the arrival road, then follow the connected alleys using movement, not teleportation.
            yield return Walk(new Vector3(-18f, 0f, -1f), 18f);
            float until = Time.realtimeSinceStartup + 150f;
            int waypoint = 1;
            while (!zone.RouteFinished && Time.realtimeSinceStartup < until)
            {
                if (waypoint >= zone.Route.Count) break;
                Vector3 target = zone.Route[waypoint]; Vector3 d = target - pc.transform.position; d.y = 0f;
                if (d.magnitude < 1.2f && zone.Leg > waypoint) waypoint++;
                float gap = Vector3.Distance(pc.transform.position, zone.Courier.position);
                Move(d, gap > 15f && d.magnitude > 0.6f ? 0.58f : 0f);
                yield return null;
            }
            VirtualInput.Clear(); Check(zone.RouteFinished, "Sound-led tail completed along the real looping lanes");
            if (!zone.RouteFinished) { Fail("Tail timed out at " + pc.transform.position + " courier " + zone.Courier.position + " waypoint " + waypoint); yield break; }
            yield return Shot("02-dyers-shortcut.png", new Vector3(-22f, 4.5f, 32f), new Vector3(4f, 1.8f, 28f));
            // Return via the middle shortcut to physically retrieve the dropped bag tie.
            yield return Walk(new Vector3(32f, 0f, 17f), 15f);
            yield return Walk(new Vector3(-22f, 0f, 17f), 24f);
            yield return Interact(zone.Thread, new Vector3(-22f, 0f, 16.5f));
            Check(Carryable.Held == zone.Thread, "Dropped bag tie picked up with production E input");
            yield return Walk(new Vector3(0f, 0f, 16f), 15f);
            yield return Walk(new Vector3(32f, 0f, 16f), 17f);
            yield return Walk(new Vector3(35f, 0f, 32f), 12f);
            yield return Interact(zone.Seal, new Vector3(34.5f, 0f, 33f));
            Check(zone.Seal.State == 1, "Bag tie fitted into cart latch");
            // A real wrong attempt proves the lock remains retryable.
            yield return Interact(zone.Keys[0], new Vector3(36f, 0f, 31.7f));
            yield return Interact(zone.Keys[1], new Vector3(37f, 0f, 31.7f));
            Check(zone.CartLock.State == 0 && zone.CartLock.Progress == 0, "Wrong cart order softly resets");
            foreach (int key in new[] { 0, 2, 1 }) yield return Interact(zone.Keys[key], new Vector3(36f + key, 0f, 31.7f));
            Check(zone.CartLock.State == 1, "Market receipt order opens cart: CUP, BELL, THREAD");
            yield return Walk(new Vector3(32f, 0f, 31f), 8f);
            yield return Walk(new Vector3(32f, 0f, 40f), 10f);
            yield return Fight();
            Check(Brawl.WonBefore(NewBazaar.Zone), "Street fight won using attacks, blocks and dodge inputs");
            if (!Brawl.WonBefore(NewBazaar.Zone)) { Fail("Street fight did not finish"); yield break; }
            yield return Interact(zone.Bell, zone.Bell.transform.position + Vector3.back * 1.3f);
            yield return new WaitForSeconds(1f);
            Check(zone.Now == NewBazaar.Stage.Complete, "Dawn Bell recovered only after defeating the robbers");
            Check(PuzzleSave.Get("evidence.list") == 1, "Inside-man list added to existing evidence board");
            yield return Shot("03-cart-recovery.png", new Vector3(24f, 3.6f, 34f), new Vector3(32f, 1.3f, 43f));
            float sum = 0f, peak = 0f; const int samples = 180;
            for (int i = 0; i < samples; i++) { yield return null; sum += Time.unscaledDeltaTime; peak = Mathf.Max(peak, Time.unscaledDeltaTime); }
            Log($"Frame sample: {samples / sum:0.0} fps mean, {peak * 1000f:0.0} ms maximum; {Screen.width}×{Screen.height}, editor={Application.isEditor}, graphics={SystemInfo.graphicsDeviceName}. This is a measured sample, not a guaranteed 60 fps claim.");
            Log($"Score {ScoreSystem.Points(ScoreSystem.Zone(NewBazaar.Zone))}, grade {ScoreSystem.Grade(ScoreSystem.Zone(NewBazaar.Zone))}, spotted {zone.Spotted}.");
            if (orbit != null) orbit.ClearCutsceneOverride();
            Passed = Failure.Length == 0; Finished = true; Log("RESULT: " + (Passed ? "PASS" : "FAIL: " + Failure));
        }
        static IEnumerator ArrivalDrive()
        {
            var jeep = Jeep.Instance; Check(jeep != null, "Jeep parked on two-bend arrival road"); if (jeep == null) yield break;
            yield return Interact(jeep, jeep.DriverExitPoint);
            yield return new WaitForSeconds(2f); Check(jeep.Occupied, "Physical Jeep entry succeeded");
            if (!jeep.Occupied) { Fail("Jeep entry failed"); yield break; }
            var car = jeep.Car; car.autopilot = true;
            foreach (var point in new[] { new Vector3(-55f, 0f, -64f), new Vector3(0f, 0f, -48f), new Vector3(0f, 0f, -10f) })
            {
                float until = Time.realtimeSinceStartup + 35f;
                while (Time.realtimeSinceStartup < until)
                {
                    Vector3 d = point - jeep.transform.position; d.y = 0f; if (d.magnitude < 5f) break;
                    float angle = Vector3.SignedAngle(jeep.transform.forward, d, Vector3.up);
                    car.autoSteer = Mathf.Clamp(angle / 25f, -1f, 1f); car.autoThrottle = Mathf.Abs(angle) > 35f ? 0.2f : 0.42f; car.autoBrake = car.CurrentSpeed > (Mathf.Abs(angle) > 25f ? 3f : 7f);
                    yield return null;
                }
                Vector3 left = point - jeep.transform.position; left.y = 0f; Check(left.magnitude < 8f, "WheelCollider drive reached road bend " + point.ToString("0"));
            }
            car.autoThrottle = 0f; car.autoSteer = 0f; car.autoBrake = true;
            yield return new WaitForSeconds(2f); VirtualInput.Press("interact"); yield return null; yield return new WaitForSeconds(2.5f);
            car.autopilot = false; car.autoBrake = false; Check(!jeep.Occupied, "Parked Jeep exit succeeded");
            var pc = UnityEngine.Object.FindAnyObjectByType<PlayerController>(); Check(pc.transform.position.y < 0.5f, "Feet on road after Jeep exit");
        }
        public static IEnumerator Walk(Vector3 target, float timeout)
        {
            var pc = UnityEngine.Object.FindAnyObjectByType<PlayerController>(); float until = Time.realtimeSinceStartup + timeout;
            while (Time.realtimeSinceStartup < until)
            {
                Vector3 d = target - pc.transform.position; d.y = 0f;
                if (d.magnitude < 0.65f) break;
                Move(d, Mathf.Clamp01(d.magnitude)); yield return null;
            }
            VirtualInput.Clear(); Vector3 remaining = target - pc.transform.position; remaining.y = 0f;
            Check(remaining.magnitude < 1.3f, "Walk reached " + target.ToString("0.0") + "; feet " + pc.transform.position.ToString("0.00"));
        }
        static void Move(Vector3 world, float speed)
        {
            var cam = Camera.main; if (cam == null || world.sqrMagnitude < 0.001f) { VirtualInput.Move = Vector2.zero; return; }
            Vector3 f = cam.transform.forward; f.y = 0f; f.Normalize(); Vector3 r = cam.transform.right; r.y = 0f; r.Normalize();
            world.Normalize(); VirtualInput.Move = new Vector2(Vector3.Dot(world, r), Vector3.Dot(world, f)) * speed;
        }
        public static IEnumerator Interact(IInteractable part, Vector3 stand)
        {
            yield return Walk(stand, 12f);
            var pc = UnityEngine.Object.FindAnyObjectByType<PlayerController>(); pc.FaceTowards(part.Anchor.position);
            PlayerController.ObjectiveFocus = part.Anchor; yield return new WaitForSeconds(0.25f);
            VirtualInput.Press("interact"); yield return null; yield return new WaitForSeconds(0.4f); PlayerController.ObjectiveFocus = null;
            Log("E interaction: " + part.Id);
        }
        public static IEnumerator Fight()
        {
            yield return new WaitForSeconds(0.5f);
            var f = Brawl.Active; if (f == null) { Check(false, "Fight started"); yield break; }
            var pc = UnityEngine.Object.FindAnyObjectByType<PlayerController>(); float until = Time.realtimeSinceStartup + 130f; int n = 0;
            while (!f.Won && Time.realtimeSinceStartup < until)
            {
                BrawlEnemy target = null; float best = 1000f; bool danger = false;
                foreach (var e in f.Enemies)
                {
                    if (!e.Alive) continue; float dist = Vector3.Distance(pc.transform.position, e.transform.position);
                    if (dist < best) { best = dist; target = e; }
                    if (dist < 3.5f && e.Now == BrawlEnemy.State.Telegraph) danger = true;
                }
                if (target == null) break;
                if (danger && n % 3 == 0) { VirtualInput.Block = true; yield return new WaitForSecondsRealtime(0.1f); VirtualInput.Block = false; VirtualInput.Press("dodge"); yield return new WaitForSecondsRealtime(0.45f); }
                float closeUntil = Time.realtimeSinceStartup + 2.5f;
                while (Vector3.Distance(pc.transform.position, target.transform.position) > 1.8f && Time.realtimeSinceStartup < closeUntil) { Move(target.transform.position - pc.transform.position, 0.85f); yield return null; }
                VirtualInput.Move = Vector2.zero; pc.FaceTowards(target.transform.position);
                VirtualInput.Press(target.CanBeFinished ? "finisher" : n % 5 == 4 ? "heavy" : "light"); n++;
                yield return new WaitForSecondsRealtime(n % 5 == 0 ? 0.85f : 0.42f);
            }
            VirtualInput.Clear(); Log($"Fight: won={f.Won}, seconds={f.Seconds:0.0}, damage={f.DamageTaken:0.0}, retries={f.Retries}, combo={f.MaxCombo}, finishers={f.Finishers}");
            yield return new WaitForSecondsRealtime(1.2f);
        }
        public static IEnumerator Shot(string file, Vector3 from, Vector3 look)
        {
            var orbit = UnityEngine.Object.FindAnyObjectByType<OrbitCamera>(); if (orbit != null) orbit.SetCutsceneOverride(from, Quaternion.LookRotation(look - from));
            yield return new WaitForSeconds(0.5f);
            if (Application.isBatchMode) WindowlessShot.Save(Path.Combine(Evidence, file));
            else { ScreenCapture.CaptureScreenshot(Path.Combine(Evidence, file)); yield return new WaitForSeconds(0.4f); }
            if (orbit != null) orbit.ClearCutsceneOverride();
        }
        static void Log(string text) { File.AppendAllText(Path.Combine(Evidence, "run.md"), text + "\n\n"); Debug.Log("[NewZonesQA] " + text); }
        static void Check(bool ok, string message) { Log((ok ? "PASS " : "FAIL ") + message); if (!ok && Failure.Length == 0) Failure = message; }
        static void Fail(string reason) { Failure = reason; Finished = true; Passed = false; Log("FAIL " + reason); }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] static void Boot()
        {
            SceneManager.sceneLoaded += (scene, mode) => { if (Constants.IsNewWorld(scene.name))
                {
                    var ui = UIManager.Instance; ui.DismissHowToPlay(); TutorialManager.Instance?.SkipTutorial();
                    if (InvestigationJournal.Instance == null) new GameObject("Investigation journal").AddComponent<InvestigationJournal>();
                    if (TempleMapOverlay.Instance == null) new GameObject("Temple map").AddComponent<TempleMapOverlay>();
                } };
            string[] args = Environment.GetCommandLineArgs(); int i = Array.IndexOf(args, "-newZonesQA");
            if (i < 0 || i + 1 >= args.Length) return;
            var bot = new GameObject("New zone QA").AddComponent<NewZonePlayBot>(); DontDestroyOnLoad(bot); bot.StartCoroutine(bot.Standalone(args[i + 1]));
        }
        IEnumerator Standalone(string directory)
        {
            StoryDialogueSystem.ResetForNewGame(); SceneManager.LoadScene(Constants.TempleRing); yield return null; yield return new WaitForSeconds(3f);
            yield return BazaarRun(directory); Application.Quit(Passed ? 0 : 2);
        }
    }
}
