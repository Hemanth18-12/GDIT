using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using LegacyOfTemples.Gameplay;
using LegacyOfTemples.Gameplay.Puzzles;

namespace LegacyOfTemples.Tests
{
    /// <summary>V3 core systems: every puzzle part can be completed, cannot lock up, resets, and is saved. Score and checkpoint rules.</summary>
    public class PuzzleFrameworkTests
    {
        readonly List<GameObject> made = new List<GameObject>();
        T Make<T>(string name, Vector3 pos = default) where T : Component { var g = new GameObject(name); g.transform.position = pos; made.Add(g); return g.AddComponent<T>(); }
        GameObject Cube(string name, Vector3 pos, Vector3 scale) { var g = GameObject.CreatePrimitive(PrimitiveType.Cube); g.name = name; g.transform.position = pos; g.transform.localScale = scale; made.Add(g); return g; }
        PuzzleGroup Group(string id, params (PuzzleElement e, int s)[] conds)
        {
            var g = Make<PuzzleGroup>("group_" + id); g.puzzleId = id; g.zoneId = "test_zone"; g.hints = new[] { "look", "how", "do" };
            foreach (var c in conds) g.conditions.Add(new PuzzleGroup.Condition { element = c.e, state = c.s });
            return g;
        }

        [SetUp] public void Clean() { PuzzleSave.Clear(); ScoreSystem.ResetAll(); Checkpoints.Clear(); }
        [TearDown] public void Down() { foreach (var g in made) if (g != null) Object.Destroy(g); made.Clear(); PuzzleSave.Clear(); ScoreSystem.ResetAll(); Checkpoints.Clear(); }

        [Test]
        public void Logic_Rules()
        {
            Assert.AreEqual(0, PuzzleLogic.NextStep(7, 8)); Assert.AreEqual(7, PuzzleLogic.NextStep(0, 8, -1));
            Assert.AreEqual(2, PuzzleLogic.WaterLevel(new[] { 1, 1 }, new[] { 2, 2 }, 2, 4), "two sluices open, drain half open = the middle mark");
            Assert.AreEqual(4, PuzzleLogic.WaterLevel(new[] { 1, 1 }, new[] { 2, 2 }, 0, 4)); Assert.AreEqual(0, PuzzleLogic.WaterLevel(new[] { 0, 0 }, new[] { 2, 2 }, 3, 4));
            Assert.IsTrue(PuzzleLogic.SequenceMatches(new[] { 9, 2, 0, 1 }, new[] { 2, 0, 1 })); Assert.IsFalse(PuzzleLogic.SequenceMatches(new[] { 2, 1, 0 }, new[] { 2, 0, 1 }));
            Vector3 r = PuzzleLogic.Reflect(Vector3.forward, new Vector3(1f, 0f, -1f)); Assert.Less(Vector3.Distance(r, Vector3.right), 1e-4f, "a 45 degree mirror turns the beam a quarter turn");
            Assert.AreEqual(Vector3.right, PuzzleLogic.PushDirection(new Vector3(-2f, 0f, 0.3f), Vector3.zero));
        }

        [UnityTest]
        public IEnumerator Discs_Solve_Lock_Reset_And_Save()
        {
            var a = Make<RotatingDisc>("disc_a"); a.steps = 4; a.turnSeconds = 0.02f; var b = Make<RotatingDisc>("disc_b"); b.steps = 4; b.turnSeconds = 0.02f;
            var g = Group("discs", (a, 2), (b, 1)); yield return null;
            Assert.IsFalse(g.Solved);
            a.Interact(); yield return new WaitForSeconds(0.06f); a.Interact(); yield return new WaitForSeconds(0.06f); Assert.AreEqual(2, a.State);
            Assert.IsFalse(g.Solved, "half done is not solved");
            g.ResetPuzzle(); Assert.AreEqual(0, a.State, "reset puts parts back");
            for (int i = 0; i < 2; i++) { a.Interact(); yield return new WaitForSeconds(0.06f); }
            b.Interact(); yield return new WaitForSeconds(0.06f);
            Assert.IsTrue(g.Solved); Assert.AreEqual("", a.Prompt, "solved parts stop offering a prompt");
            a.Interact(); yield return null; Assert.AreEqual(2, a.State, "solved parts do not move");
            Assert.AreEqual(1, PuzzleSave.Get("solved.discs")); Assert.AreEqual(2, PuzzleSave.Get(a.SaveId));
            Assert.IsTrue(ScoreSystem.Zone("test_zone").puzzleSolved);
            // a new object with the same id comes back in the saved state (load game)
            var a2 = Make<RotatingDisc>("disc_a"); a2.steps = 4; yield return null; Assert.AreEqual(2, a2.State);
        }

        [UnityTest]
        public IEnumerator Hints_Three_Tiers_Cost_Score()
        {
            var lever = Make<PuzzleLever>("lever"); var g = Group("hinted", (lever, 1)); yield return null;
            Assert.AreSame(g, PuzzleGroup.Nearest(Vector3.zero));
            Assert.AreEqual("look", g.NextHint()); Assert.AreEqual("how", g.NextHint()); Assert.AreEqual("do", g.NextHint()); Assert.AreEqual("do", g.NextHint());
            Assert.AreEqual(3, g.HintsUsed);
            lever.Interact(); yield return null; Assert.IsTrue(g.Solved);
            Assert.IsNull(PuzzleGroup.Nearest(Vector3.zero), "a solved puzzle gives no more hints");
            var z = ScoreSystem.Zone("test_zone"); Assert.AreEqual(3, z.hintsUsed);
            Assert.AreEqual(ScoreSystem.HintCost, ScoreSystem.PuzzlePoints - ScoreSystem.HintCost * 3, "three hints leave the minimum puzzle points");
        }

        [UnityTest]
        public IEnumerator Plate_And_Block()
        {
            Cube("floor", new Vector3(100f, -0.5f, 100f), new Vector3(20f, 1f, 20f));
            Cube("wall", new Vector3(100f, 0.5f, 103f), new Vector3(4f, 1f, 1f));
            var blockGo = Cube("block", new Vector3(100f, 0.45f, 100f), Vector3.one * 0.9f); var block = blockGo.AddComponent<PushableBlock>(); block.moveSeconds = 0.02f;
            var plate = Make<PressurePlate>("plate", new Vector3(102f, 0f, 100f));
            var g = Group("plate", (plate, 1)); yield return null; Physics.SyncTransforms();
            Assert.IsTrue(block.Push(Vector3.right)); yield return new WaitForSeconds(0.08f);
            Assert.IsTrue(block.Push(Vector3.forward)); yield return new WaitForSeconds(0.08f);
            Assert.IsTrue(block.Push(Vector3.forward)); yield return new WaitForSeconds(0.08f);
            Assert.IsFalse(block.Push(Vector3.forward), "the wall stops the block");
            Assert.Less(Vector3.Distance(blockGo.transform.position, new Vector3(101f, 0.45f, 102f)), 0.01f);
            block.ResetState(); Assert.Less(Vector3.Distance(blockGo.transform.position, new Vector3(100f, 0.45f, 100f)), 0.01f, "a block pushed into a corner can always be reset");
            plate.Press(blockGo); Assert.AreEqual(1, plate.State); Assert.IsTrue(g.Solved);
        }

        [UnityTest]
        public IEnumerator Water_Level_Puzzle_Has_An_Aha_And_No_Lockup()
        {
            var s1 = Make<WaterValve>("sluice_1"); s1.steps = 2; s1.turnSeconds = 0.01f; var s2 = Make<WaterValve>("sluice_2"); s2.steps = 2; s2.turnSeconds = 0.01f;
            var drain = Make<WaterValve>("drain"); drain.steps = 5; drain.turnSeconds = 0.01f;
            var basin = Make<WaterBasin>("basin"); basin.inflows.AddRange(new[] { s1, s2 }); basin.inflowGains.AddRange(new[] { 2, 2 }); basin.drain = drain; basin.maxLevel = 4; basin.riseSpeed = 100f;
            var g = Group("water", (basin, 2), (s1, 1), (s2, 1)); yield return null;
            s1.Interact(); yield return new WaitForSeconds(0.04f); Assert.AreEqual(2, basin.State); Assert.IsFalse(g.Solved, "one sluice reaches the mark but the second gate is still shut");
            s2.Interact(); yield return new WaitForSeconds(0.04f); Assert.AreEqual(4, basin.State, "both sluices flood past the door");
            drain.Interact(); yield return new WaitForSeconds(0.04f); drain.Interact(); yield return new WaitForSeconds(0.04f);
            Assert.AreEqual(2, basin.State); Assert.IsTrue(g.Solved, "draining back to the middle mark opens the chamber");
            Assert.Less(Mathf.Abs(basin.SurfaceY - 2 * basin.levelHeight), 0.01f);
        }

        [UnityTest]
        public IEnumerator Sound_Lock_Wrong_Order_Restarts_Quietly()
        {
            var lockEl = Make<SequenceLock>("bells"); lockEl.required.AddRange(new[] { 2, 0, 1 }); var g = Group("bells", (lockEl, 1)); yield return null;
            lockEl.Press(2); lockEl.Press(1); Assert.AreEqual(0, lockEl.State); Assert.AreEqual(0, lockEl.Progress, "a wrong strike starts again");
            lockEl.Press(2); lockEl.Press(2); Assert.AreEqual(1, lockEl.Progress, "a repeated first note still counts as a start");
            lockEl.Press(0); lockEl.Press(1); Assert.AreEqual(1, lockEl.State); Assert.IsTrue(g.Solved);
        }

        [UnityTest]
        public IEnumerator Carry_Place_And_Shadow_Match()
        {
            var piece = Make<Carryable>("lamp_piece", new Vector3(200f, 0f, 200f)); piece.itemKey = "lamp_piece";
            var gear = Make<Carryable>("gear_small", new Vector3(201f, 0f, 200f)); gear.itemKey = "gear_small";
            var stand = Make<PlacementSocket>("lamp_stand", new Vector3(203f, 0f, 200f)); stand.acceptsKey = "lamp_piece";
            var peg = Make<PlacementSocket>("peg", new Vector3(204f, 0f, 200f)); peg.acceptsKey = "gear_big"; peg.takesAnyItem = true;
            var carrier = new GameObject("carrier").transform; made.Add(carrier.gameObject);
            var lamp = Make<Carryable>("standing_lamp", new Vector3(210f, 0f, 200f)); var spot = new GameObject("robber_lamp_spot").transform; made.Add(spot.gameObject); spot.position = new Vector3(215f, 0f, 203f);
            var match = Make<ProximityMatch>("shadow_match"); match.subject = lamp.transform; match.target = spot; match.tolerance = 0.5f;
            var g = Group("evidence", (stand, 1), (match, 1)); yield return null;

            gear.PickUp(carrier); Assert.AreSame(gear, Carryable.Held);
            Assert.AreEqual("", stand.Prompt, "the lamp stand does not take a gear");
            peg.Interact(); Assert.AreEqual(2, peg.State, "a wrong gear fits the peg but is marked wrong"); Assert.IsNull(Carryable.Held);
            gear.PickUp(carrier); Assert.AreEqual(0, peg.State, "taking it off empties the peg"); gear.Drop();
            piece.PickUp(carrier); stand.Interact(); Assert.AreEqual(1, stand.State); Assert.IsFalse(g.Solved);
            lamp.PickUp(carrier); carrier.position = new Vector3(215.2f, 0f, 203.1f) - lamp.holdOffset; yield return null; yield return null;
            match.Check(); Assert.AreEqual(0, match.State, "a lamp still in the hands does not count");
            lamp.Drop(); lamp.transform.position = new Vector3(215.2f, 0f, 203.1f); match.Check();
            Assert.AreEqual(1, match.State); Assert.IsTrue(g.Solved);
        }

        [UnityTest]
        public IEnumerator Light_Beam_Bounces_Off_Mirrors_To_A_Socket_And_Is_Blocked()
        {
            Vector3 o = new Vector3(300f, 1f, 300f);
            var em = Make<LightBeamEmitter>("sun_gap", o); em.transform.rotation = Quaternion.LookRotation(Vector3.forward);
            var mGo = Cube("mirror", o + Vector3.forward * 10f, new Vector3(2f, 2f, 0.1f)); mGo.transform.rotation = Quaternion.LookRotation(Vector3.back);   // faces the emitter: the beam goes straight back
            var mirror = mGo.AddComponent<LightMirror>(); mirror.steps = 16; mirror.turnSeconds = 0.01f;
            var sGo = Cube("socket", o + Vector3.forward * 10f + Vector3.left * 12f, Vector3.one); var socket = sGo.AddComponent<LightSocket>();
            var g = Group("light", (socket, 1)); yield return null; Physics.SyncTransforms();
            Assert.IsNull(em.Trace(), "the beam returns to the emitter, not the socket");
            // turn the mirror two stops (45 degrees): the beam leaves along -X
            mirror.Interact(); yield return new WaitForSeconds(0.04f); mirror.Interact(); yield return new WaitForSeconds(0.04f); Physics.SyncTransforms();
            Assert.AreSame(socket, em.Trace(), "after two stops the mirror sends the beam to the socket");
            yield return null; yield return null; Assert.AreEqual(1, socket.State); Assert.IsTrue(g.Solved);
            var spoke = Cube("wheel_spoke", o + Vector3.forward * 10f + Vector3.left * 6f, new Vector3(0.5f, 3f, 3f)); Physics.SyncTransforms();
            Assert.IsNull(em.Trace(), "a wheel spoke in the way blocks the beam");
        }

        [Test]
        public void Score_Grades_And_High_Scores()
        {
            var perfect = new ScoreSystem.ZoneScore { zone = "z", puzzleSolved = true, items = 1, maxCombo = 10, seconds = 200f };
            Assert.AreEqual(ScoreSystem.MaxPoints(), ScoreSystem.Points(perfect)); Assert.AreEqual("S", ScoreSystem.Grade(perfect));
            var rough = new ScoreSystem.ZoneScore { zone = "z", puzzleSolved = true, hintsUsed = 3, spotted = 4, damage = 80f, items = 1, maxCombo = 2, seconds = 900f };
            Assert.Less(ScoreSystem.Points(rough), ScoreSystem.Points(perfect)); Assert.AreEqual("C", ScoreSystem.Grade(rough));
            var mid = new ScoreSystem.ZoneScore { zone = "z", puzzleSolved = true, hintsUsed = 1, spotted = 1, damage = 20f, items = 1, maxCombo = 6, seconds = 400f };
            Assert.AreEqual("A", ScoreSystem.Grade(mid));
            ScoreSystem.ItemRecovered("bazaar"); ScoreSystem.PuzzleSolved("bazaar", 0, 100f); int r1 = ScoreSystem.SubmitRun(1); Assert.AreEqual(1, r1);
            ScoreSystem.ResetRun(); Assert.AreEqual(1, ScoreSystem.HighScores.Count, "New Game keeps the high-score table");
            int r2 = ScoreSystem.SubmitRun(1); Assert.AreEqual(2, r2, "a worse run ranks below the first");
        }

        [Test]
        public void Evidence_Board_Shows_Only_What_Was_Found_And_Names_Once()
        {
            Assert.AreEqual(0, EvidenceBoard.Found); Assert.IsFalse(EvidenceBoard.CanName);
            StringAssert.Contains("Nothing yet", EvidenceBoard.Body()); StringAssert.DoesNotContain("ledger", EvidenceBoard.Body());
            PuzzleSave.Set("evidence.inside_lock", 1); PuzzleSave.Set("evidence.ledger", 1);
            StringAssert.Contains("ledger", EvidenceBoard.Body()); StringAssert.DoesNotContain("key ring", EvidenceBoard.Body());
            Assert.IsFalse(EvidenceBoard.Name(2), "two pieces are not enough to name anyone");
            PuzzleSave.Set("evidence.key_ring", 1); Assert.IsTrue(EvidenceBoard.CanName);
            StringAssert.DoesNotContain("Madhav", EvidenceBoard.Body().Split(new[] { "NAME THE INSIDE MAN" }, System.StringSplitOptions.None)[0], "the evidence text itself never gives the name");
            Assert.IsTrue(EvidenceBoard.Name(2)); Assert.IsTrue(EvidenceBoard.NamedCorrectly);
            Assert.IsFalse(EvidenceBoard.Name(0), "the choice cannot be changed");
            PuzzleSave.Clear(); Assert.AreEqual(-1, EvidenceBoard.Named, "New Game forgets the accusation");
        }

        [Test]
        public void Checkpoint_Save_And_Restore()
        {
            Assert.IsFalse(Checkpoints.Has);
            Checkpoints.Save("vaults", "before_fight", new Vector3(5f, -6f, 7f), 90f, 1234f);
            var who = new GameObject("who").transform; made.Add(who.gameObject);
            Assert.IsTrue(Checkpoints.Restore(who)); Assert.AreEqual(new Vector3(5f, -6f, 7f), who.position); Assert.AreEqual(90f, who.eulerAngles.y, 0.01f);
            Assert.AreEqual("vaults", Checkpoints.Current.zone); Assert.AreEqual(1234f, Checkpoints.Current.clockSeconds);
        }
    }
}
