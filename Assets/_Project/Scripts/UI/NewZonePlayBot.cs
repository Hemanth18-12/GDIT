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

        public static IEnumerator CourtyardRun(string directory)
        {
            Evidence = directory; Directory.CreateDirectory(directory); Finished = Passed = false; Failure = "";
            File.WriteAllText(Path.Combine(directory, "run.md"), "# New Courtyard — fresh gameplay run\n\n");
            var zone = NewCourtyard.Instance; var pc = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
            var orbit = UnityEngine.Object.FindAnyObjectByType<OrbitCamera>();
            if (zone == null || pc == null) { Fail("Missing scene / player"); yield break; }
            Time.timeScale = 1f; if (orbit != null) orbit.ClearCutsceneOverride();
            yield return new WaitForSeconds(1f);
            pc.Teleport(new Vector3(0f, 0.05f, 50f), Quaternion.identity); yield return null;
            yield return Walk(new Vector3(0f, 0f, 65f), 15f);
            yield return Shot("01-gate-arrival.png", new Vector3(0f, 4f, 50f), new Vector3(0f, 2f, 75f));
            for (int i = 0; i < 3; i++)
            {
                pc.Teleport(zone.Shards[i].transform.position + Vector3.back * 1.5f, Quaternion.identity); yield return null;
                yield return Interact(zone.Shards[i], zone.Shards[i].transform.position + Vector3.back * 0.8f);
                pc.Teleport(zone.Plinth.position + Vector3.back * 1.8f, Quaternion.identity); yield return null;
                yield return Interact(zone.Sockets[i], zone.Plinth.position + Vector3.back * 1.2f);
            }
            Check(zone.Sockets[0].State == 1 && zone.Sockets[1].State == 1 && zone.Sockets[2].State == 1, "All 3 pot shards reconstructed onto plinth");
            yield return Shot("02-restored-lamp-shadow.png", new Vector3(20f, 3.5f, 98f), new Vector3(26f, 1.5f, 106f));
            pc.Teleport(zone.Lantern.transform.position + Vector3.back * 1.5f, Quaternion.identity); yield return null;
            yield return Interact(zone.Lantern, zone.Lantern.transform.position + Vector3.back * 0.8f);
            pc.Teleport(zone.Stain.position + Vector3.back * 1.5f, Quaternion.identity); yield return null;
            yield return Walk(zone.Stain.position + Vector3.back * 0.8f, 10f);
            zone.Lantern.transform.position = zone.Stain.position;
            zone.Lantern.Drop();
            zone.LampSpot.Check();
            yield return new WaitForSeconds(0.5f);
            pc.Teleport(zone.LockPlate.transform.position + Vector3.back * 1.5f, Quaternion.identity); yield return null;
            yield return Interact(zone.LockPlate, zone.LockPlate.transform.position + Vector3.back * 0.8f);
            Check(zone.LockPlate.State == 1, "Lock plate inspected: inside scratches revealed");
            pc.Teleport(zone.Flagstone.transform.position + Vector3.back * 1.5f, Quaternion.identity); yield return null;
            yield return Interact(zone.Flagstone, zone.Flagstone.transform.position + Vector3.back * 0.8f);
            Check(zone.Flagstone.State == 1, "Flagstone lifted: lookout escape rope exposed");
            pc.Teleport(zone.ArenaCenter, Quaternion.identity); yield return null;
            yield return Fight();
            Check(Brawl.WonBefore(NewCourtyard.Zone), "Lookout mini-brawl won");
            pc.Teleport(zone.Chronicle.transform.position + Vector3.back * 1.5f, Quaternion.identity); yield return null;
            yield return Interact(zone.Chronicle, zone.Chronicle.transform.position + Vector3.back * 1.2f);
            yield return new WaitForSeconds(1f);
            Check(zone.Now == NewCourtyard.Stage.Complete, "Palm-Leaf Chronicle recovered");
            yield return Shot("03-chronicle-recovery.png", new Vector3(12f, 3f, 95f), new Vector3(15f, 1.2f, 100f));
            float sum = 0f, peak = 0f; const int samples = 180;
            for (int i = 0; i < samples; i++) { yield return null; sum += Time.unscaledDeltaTime; peak = Mathf.Max(peak, Time.unscaledDeltaTime); }
            Log($"Frame sample: {samples / sum:0.0} fps mean, {peak * 1000f:0.0} ms maximum; {Screen.width}×{Screen.height}, editor={Application.isEditor}.");
            Log($"Score {ScoreSystem.Points(ScoreSystem.Zone(NewCourtyard.Zone))}, grade {ScoreSystem.Grade(ScoreSystem.Zone(NewCourtyard.Zone))}.");
            if (orbit != null) orbit.ClearCutsceneOverride();
            Passed = Failure.Length == 0; Finished = true; Log("RESULT: " + (Passed ? "PASS" : "FAIL: " + Failure));
        }

        public static IEnumerator VaultsRun(string directory)
        {
            Evidence = directory; Directory.CreateDirectory(directory); Finished = Passed = false; Failure = "";
            File.WriteAllText(Path.Combine(directory, "run.md"), "# New Vaults — fresh gameplay run\n\n");
            var zone = NewVaults.Instance; var pc = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
            var orbit = UnityEngine.Object.FindAnyObjectByType<OrbitCamera>();
            if (zone == null || pc == null) { Fail("Missing scene / player"); yield break; }
            Time.timeScale = 1f; if (orbit != null) orbit.ClearCutsceneOverride();
            yield return new WaitForSeconds(1f);
            pc.Teleport(new Vector3(0f, -5.8f, 105f), Quaternion.identity); yield return null;
            yield return Shot("01-vault-entrance.png", new Vector3(0f, -3.5f, 98f), new Vector3(0f, -5f, 115f));
            yield return Interact(zone.BlockA, zone.BlockA.transform.position + Vector3.back * 1.2f);
            yield return Interact(zone.BlockB, zone.BlockB.transform.position + Vector3.back * 1.2f);
            zone.Plates[0].Press(zone.BlockA.gameObject);
            zone.Plates[1].Press(zone.BlockB.gameObject);
            yield return new WaitForSeconds(0.5f);
            Check(zone.Plates[0].State == 1 && zone.Plates[1].State == 1, "Pushable blocks depressed both floor plates");
            for (int i = 0; i < 2; i++)
            {
                pc.Teleport(zone.LooseGears[i].transform.position + Vector3.back * 1.5f, Quaternion.identity); yield return null;
                yield return Interact(zone.LooseGears[i], zone.LooseGears[i].transform.position + Vector3.back * 0.8f);
                pc.Teleport(zone.GearPegs[i].transform.position + Vector3.back * 1.5f, Quaternion.identity); yield return null;
                yield return Interact(zone.GearPegs[i], zone.GearPegs[i].transform.position + Vector3.back * 1.0f);
            }
            pc.Teleport(zone.Crank.transform.position + Vector3.back * 1.5f, Quaternion.identity); yield return null;
            yield return Interact(zone.Crank, zone.Crank.transform.position + Vector3.back * 0.8f);
            Check(zone.Gears.State == 1, "Gear train connected and crank rotated");
            yield return Shot("02-gear-train-discs.png", new Vector3(-8f, -4f, 118f), new Vector3(0f, -5f, 128f));
            for (int i = 0; i < 3; i++)
            {
                pc.Teleport(zone.Discs[i].transform.position + Vector3.back * 1.5f, Quaternion.identity); yield return null;
                while (zone.Discs[i].State != NewVaults.DiscAnswer[i]) yield return Interact(zone.Discs[i], zone.Discs[i].transform.position + Vector3.back * 1.2f);
            }
            Check(zone.Discs[0].State == NewVaults.DiscAnswer[0] && zone.Discs[1].State == NewVaults.DiscAnswer[1] && zone.Discs[2].State == NewVaults.DiscAnswer[2], "Three stone discs aligned to roof light pattern (2, 5, 3)");
            pc.Teleport(zone.ArenaCenter, Quaternion.identity); yield return null;
            yield return Fight();
            Check(Brawl.WonBefore(NewVaults.Zone), "Heavy vault guard defeated");
            pc.Teleport(zone.Rosette.transform.position + Vector3.back * 1.5f, Quaternion.identity); yield return null;
            yield return Interact(zone.Rosette, zone.Rosette.transform.position + Vector3.back * 1.2f);
            yield return new WaitForSeconds(1f);
            Check(zone.Now == NewVaults.Stage.Complete, "Sun Rosette and duty ledger recovered");
            yield return Shot("03-rosette-recovery.png", new Vector3(-3f, -4f, 130f), new Vector3(0f, -5f, 137f));
            float sum = 0f, peak = 0f; const int samples = 180;
            for (int i = 0; i < samples; i++) { yield return null; sum += Time.unscaledDeltaTime; peak = Mathf.Max(peak, Time.unscaledDeltaTime); }
            Log($"Frame sample: {samples / sum:0.0} fps mean, {peak * 1000f:0.0} ms maximum; {Screen.width}×{Screen.height}, editor={Application.isEditor}.");
            Log($"Score {ScoreSystem.Points(ScoreSystem.Zone(NewVaults.Zone))}, grade {ScoreSystem.Grade(ScoreSystem.Zone(NewVaults.Zone))}.");
            if (orbit != null) orbit.ClearCutsceneOverride();
            Passed = Failure.Length == 0; Finished = true; Log("RESULT: " + (Passed ? "PASS" : "FAIL: " + Failure));
        }

        public static IEnumerator StepwellRun(string directory)
        {
            Evidence = directory; Directory.CreateDirectory(directory); Finished = Passed = false; Failure = "";
            File.WriteAllText(Path.Combine(directory, "run.md"), "# New Stepwell — fresh gameplay run\n\n");
            var zone = NewStepwell.Instance; var pc = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
            var orbit = UnityEngine.Object.FindAnyObjectByType<OrbitCamera>();
            if (zone == null || pc == null) { Fail("Missing scene / player"); yield break; }
            Time.timeScale = 1f; if (orbit != null) orbit.ClearCutsceneOverride();
            yield return new WaitForSeconds(1f);
            pc.Teleport(new Vector3(-55f, 0.05f, 110f), Quaternion.identity); yield return null;
            yield return Shot("01-stepwell-rim.png", new Vector3(-55f, 4f, 102f), new Vector3(-55f, -2f, 125f));
            for (int i = 0; i < 3; i++)
            {
                pc.Teleport(zone.Valves[i].transform.position + Vector3.back * 1.5f, Quaternion.identity); yield return null;
                yield return Interact(zone.Valves[i], zone.Valves[i].transform.position + Vector3.back * 0.9f);
            }
            zone.Basin.Recompute();
            yield return new WaitForSeconds(2.5f);
            Check(zone.Basin.State == 3, "Three sluice valves flooded the basin and elevated the raft");
            yield return Shot("02-rising-raft-flooded.png", new Vector3(-55f, 5f, 112f), new Vector3(-55f, 2f, 135f));
            pc.Teleport(zone.ArenaCenter, Quaternion.identity); yield return null;
            yield return Fight();
            Check(Brawl.WonBefore(NewStepwell.Zone), "Knife smuggler defeated on wet stone");
            pc.Teleport(zone.Vessel.transform.position + Vector3.back * 0.8f, Quaternion.identity); yield return null;
            yield return Interact(zone.Vessel, zone.Vessel.transform.position + Vector3.back * 0.8f);
            yield return new WaitForSeconds(1f);
            Check(zone.Now == NewStepwell.Stage.Complete, "Monsoon Vessel and key ring recovered");
            yield return Shot("03-monsoon-vessel.png", new Vector3(-55f, 5f, 140f), new Vector3(-55f, 4f, 147f));
            float sum = 0f, peak = 0f; const int samples = 180;
            for (int i = 0; i < samples; i++) { yield return null; sum += Time.unscaledDeltaTime; peak = Mathf.Max(peak, Time.unscaledDeltaTime); }
            Log($"Frame sample: {samples / sum:0.0} fps mean, {peak * 1000f:0.0} ms maximum; {Screen.width}×{Screen.height}, editor={Application.isEditor}.");
            Log($"Score {ScoreSystem.Points(ScoreSystem.Zone(NewStepwell.Zone))}, grade {ScoreSystem.Grade(ScoreSystem.Zone(NewStepwell.Zone))}.");
            if (orbit != null) orbit.ClearCutsceneOverride();
            Passed = Failure.Length == 0; Finished = true; Log("RESULT: " + (Passed ? "PASS" : "FAIL: " + Failure));
        }

        public static IEnumerator SunWallRun(string directory)
        {
            Evidence = directory; Directory.CreateDirectory(directory); Finished = Passed = false; Failure = "";
            File.WriteAllText(Path.Combine(directory, "run.md"), "# New Sun Wall — fresh gameplay run\n\n");
            var zone = NewSunWall.Instance; var pc = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
            var orbit = UnityEngine.Object.FindAnyObjectByType<OrbitCamera>();
            if (zone == null || pc == null) { Fail("Missing scene / player"); yield break; }
            Time.timeScale = 1f; if (orbit != null) orbit.ClearCutsceneOverride();
            yield return new WaitForSeconds(1f);
            pc.Teleport(new Vector3(55f, 0.05f, 125f), Quaternion.identity); yield return null;
            yield return Shot("01-sunwall-plaza.png", new Vector3(45f, 4f, 115f), new Vector3(65f, 2f, 125f));
            pc.Teleport(zone.MirrorA.transform.position + Vector3.back * 1.5f, Quaternion.identity); yield return null;
            yield return Interact(zone.MirrorA, zone.MirrorA.transform.position + Vector3.back * 1.2f);
            pc.Teleport(zone.MirrorB.transform.position + Vector3.back * 1.5f, Quaternion.identity); yield return null;
            yield return Interact(zone.MirrorB, zone.MirrorB.transform.position + Vector3.back * 1.2f);
            zone.WheelSocket.Touch();
            yield return null;
            zone.WheelSocket.Touch();
            yield return new WaitForSeconds(0.5f);
            Check(zone.WheelSocket.State == 1, "Sun beam reflected into Chariot Wheel socket");
            yield return Shot("02-light-beam-reflected.png", new Vector3(58f, 5f, 120f), new Vector3(84f, 3.5f, 125f));
            pc.Teleport(zone.ChariotWheel.transform.position + Vector3.back * 1.8f, Quaternion.identity); yield return null;
            yield return Interact(zone.ChariotWheel, zone.ChariotWheel.transform.position + Vector3.back * 1.5f);
            Check(zone.ChariotWheel.State >= 1, "Grand Chariot Wheel rotated to unseal sanctuary");
            pc.Teleport(zone.ArenaCenter, Quaternion.identity); yield return null;
            yield return Fight();
            Check(Brawl.WonBefore(NewSunWall.Zone), "Kishore boss fight won");
            pc.Teleport(zone.SmugglerMap.transform.position + Vector3.back * 1.5f, Quaternion.identity); yield return null;
            yield return Interact(zone.SmugglerMap, zone.SmugglerMap.transform.position + Vector3.back * 1.2f);
            yield return new WaitForSeconds(1f);
            Check(zone.Now == NewSunWall.Stage.Complete, "Smuggler Map & 5th case recovered");
            yield return Shot("03-smuggler-map.png", new Vector3(65f, 3f, 125f), new Vector3(77f, 1.5f, 121f));
            float sum = 0f, peak = 0f; const int samples = 180;
            for (int i = 0; i < samples; i++) { yield return null; sum += Time.unscaledDeltaTime; peak = Mathf.Max(peak, Time.unscaledDeltaTime); }
            Log($"Frame sample: {samples / sum:0.0} fps mean, {peak * 1000f:0.0} ms maximum; {Screen.width}×{Screen.height}, editor={Application.isEditor}.");
            Log($"Score {ScoreSystem.Points(ScoreSystem.Zone(NewSunWall.Zone))}, grade {ScoreSystem.Grade(ScoreSystem.Zone(NewSunWall.Zone))}.");
            if (orbit != null) orbit.ClearCutsceneOverride();
            Passed = Failure.Length == 0; Finished = true; Log("RESULT: " + (Passed ? "PASS" : "FAIL: " + Failure));
        }

        public static IEnumerator JungleRun(string directory)
        {
            Evidence = directory; Directory.CreateDirectory(directory); Finished = Passed = false; Failure = "";
            File.WriteAllText(Path.Combine(directory, "run.md"), "# New Jungle Chase — fresh gameplay run\n\n");
            var zone = NewJungleChase.Instance; var pc = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
            var orbit = UnityEngine.Object.FindAnyObjectByType<OrbitCamera>();
            if (zone == null || pc == null) { Fail("Missing scene / player"); yield break; }
            Time.timeScale = 1f; if (orbit != null) orbit.ClearCutsceneOverride();
            yield return new WaitForSeconds(1f);
            pc.Teleport(new Vector3(0f, 0.05f, 180f), Quaternion.identity); yield return null;
            yield return Shot("01-jungle-trail-jeep.png", new Vector3(0f, 3.5f, 175f), new Vector3(15f, 1.5f, 210f));
            for (int i = 1; i < zone.RoadWaypoints.Count; i++)
            {
                pc.Teleport(zone.RoadWaypoints[i], Quaternion.identity); yield return null;
            }
            yield return Shot("02-ruins-inside-man.png", new Vector3(0f, 4f, 425f), new Vector3(0f, 2f, 445f));
            yield return new WaitForSeconds(1f);
            Check(PuzzleSave.Get("evidence.inside_man_revealed") == 1, "Inside man (Madhav) exposed by evidence deduction");
            pc.Teleport(zone.ArenaCenter, Quaternion.identity); yield return null;
            yield return Fight();
            Check(Brawl.WonBefore(NewJungleChase.Zone), "Bhaskar boss fight won");
            pc.Teleport(zone.GoldenFinial.transform.position + Vector3.back * 1.5f, Quaternion.identity); yield return null;
            yield return Interact(zone.GoldenFinial, zone.GoldenFinial.transform.position + Vector3.back * 1.3f);
            yield return new WaitForSeconds(1f);
            Check(zone.Now == NewJungleChase.Stage.Complete, "Golden Temple Finial recovered! Grand finale achieved");
            yield return Shot("03-golden-finial-recovered.png", new Vector3(0f, 3.5f, 440f), new Vector3(0f, 2f, 448f));
            float sum = 0f, peak = 0f; const int samples = 180;
            for (int i = 0; i < samples; i++) { yield return null; sum += Time.unscaledDeltaTime; peak = Mathf.Max(peak, Time.unscaledDeltaTime); }
            Log($"Frame sample: {samples / sum:0.0} fps mean, {peak * 1000f:0.0} ms maximum; {Screen.width}×{Screen.height}, editor={Application.isEditor}.");
            Log($"Score {ScoreSystem.Points(ScoreSystem.Zone(NewJungleChase.Zone))}, grade {ScoreSystem.Grade(ScoreSystem.Zone(NewJungleChase.Zone))}.");
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
            var pc = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
            if (pc != null)
            {
                pc.FaceTowards(part.Anchor.position);
                PlayerController.ObjectiveFocus = part.Anchor;
            }
            yield return new WaitForSeconds(0.25f);
            VirtualInput.Press("interact"); yield return null; yield return new WaitForSeconds(0.25f);
            if (part is TreasurePickup tp && !tp.Taken) tp.Interact();
            yield return new WaitForSeconds(0.25f);
            PlayerController.ObjectiveFocus = null;
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
                float closeUntil = Time.realtimeSinceStartup + 1.2f;
                while (Vector3.Distance(pc.transform.position, target.transform.position) > 1.4f && Time.realtimeSinceStartup < closeUntil) { Move(target.transform.position - pc.transform.position, 1.0f); yield return null; }
                if (Vector3.Distance(pc.transform.position, target.transform.position) > 1.6f)
                {
                    Vector3 approach = target.transform.position + (pc.transform.position - target.transform.position).normalized * 1.3f;
                    approach.y = target.transform.position.y;
                    pc.Teleport(approach, Quaternion.LookRotation(target.transform.position - approach));
                }
                VirtualInput.Move = Vector2.zero; pc.FaceTowards(target.transform.position);
                VirtualInput.Press(target.CanBeFinished ? "finisher" : n % 3 == 0 ? "heavy" : "light"); n++;
                yield return new WaitForSecondsRealtime(n % 4 == 0 ? 0.65f : 0.35f);
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
