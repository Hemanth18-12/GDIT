using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using LegacyOfTemples.Gameplay.Puzzles;
using LegacyOfTemples.Characters;

namespace LegacyOfTemples.Tests
{
    public class NewToolkitTests
    {
        GameObject root;
        T Make<T>(string id) where T : Component { var g = new GameObject(id); g.transform.SetParent(root.transform); return g.GetComponent<T>() ?? g.AddComponent<T>(); }
        [SetUp] public void Setup() { PuzzleSave.Clear("nztest"); PuzzleSave.Clear("el.nztest"); root = new GameObject("toolkit tests"); }
        [TearDown] public void Down() { Object.Destroy(root); PuzzleSave.Clear("nztest"); PuzzleSave.Clear("el.nztest"); }
        [UnityTest] public IEnumerator RuntimeIds_Restore_AfterConfiguration()
        {
            PuzzleSave.Set("el.nztest.disc", 3);
            var disc = Make<RotatingDisc>("unconfigured runtime name"); disc.elementId = "nztest.disc"; disc.steps = 4;
            yield return null;
            Assert.AreEqual(3, disc.State, "Awake must not restore before runtime builder assigns the stable ID");
            Assert.Less(Quaternion.Angle(disc.transform.localRotation, Quaternion.Euler(0, 270, 0)), 0.1f);
            disc.ResetState(); Assert.AreEqual(0, PuzzleSave.Get(disc.SaveId));
        }
        [UnityTest] public IEnumerator GearTrain_TransmitsOnlyThroughCorrectSeats_AndResets()
        {
            var a = Make<PlacementSocket>("nztest.peg"); a.acceptsKey = "nztest.small"; a.takesAnyItem = true;
            var small = Make<Carryable>("small"); small.itemKey = "nztest.small";
            var wrong = Make<Carryable>("wrong"); wrong.itemKey = "nztest.big";
            var crank = Make<RotatingDisc>("nztest.crank"); crank.steps = 2;
            var train = Make<GearTrain>("nztest.train"); train.pegs = new[] { a }; train.crank = crank;
            train.wheels = new[] { Make<Transform>("input"), Make<Transform>("output") }; train.teeth = new[] { 12, 24 };
            yield return null;
            a.Place(wrong); crank.Interact(); yield return new WaitForSeconds(0.05f); Assert.AreEqual(0, train.State);
            a.ResetState(); a.Place(small); yield return new WaitForSeconds(0.1f); Assert.AreEqual(1, train.State);
            Assert.Greater(Quaternion.Angle(Quaternion.identity, train.wheels[1].localRotation), 0f);
            crank.ResetState(); yield return null; Assert.AreEqual(0, train.State);
        }
        [UnityTest] public IEnumerator Socket_RestoresActualItem_NotOnlySolvedFlag()
        {
            var socket = Make<PlacementSocket>("nztest.savedSocket"); socket.acceptsKey = "nztest.savedGear";
            var item = Make<Carryable>("gear"); item.itemKey = "nztest.savedGear";
            yield return null; socket.Place(item); Object.Destroy(socket.gameObject); yield return null;
            var restored = Make<PlacementSocket>("nztest.savedSocket"); restored.acceptsKey = "nztest.savedGear";
            yield return null; yield return null; yield return null;
            Assert.AreSame(item, restored.Item); Assert.AreEqual(1, restored.State);
            restored.ResetState(); Assert.AreEqual(0, restored.State); Assert.IsNull(restored.Item);
        }
        [UnityTest] public IEnumerator Rope_Crosses_AndBlockedLandingReturnsSafely()
        {
            LogAssert.Expect(LogType.Error, "Cannot calibrate player feet: no readable mesh vertices.");
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube); floor.transform.SetParent(root.transform); floor.transform.position = new Vector3(800f, -0.5f, 800f); floor.transform.localScale = new Vector3(20f, 1f, 20f);
            var hero = Make<PlayerController>("rope rider"); hero.transform.position = new Vector3(800f, 0.05f, 800f);
            var capsule = hero.GetComponent<CharacterController>(); capsule.height = 1.8f; capsule.radius = 0.3f; capsule.center = Vector3.up * 0.9f;
            var rope = Make<RopeSwing>("nztest.rope"); rope.actor = hero; rope.seconds = 0.08f; rope.landing = Make<Transform>("landing"); rope.landing.position = new Vector3(804f, 0.05f, 800f);
            yield return null; hero.Teleport(new Vector3(800f, 0.05f, 800f), Quaternion.identity); rope.Interact();
            yield return new WaitForSeconds(0.18f);
            Assert.AreEqual(1, rope.State); Assert.IsTrue(hero.enabled); Assert.IsTrue(capsule.enabled); Assert.Less(Vector3.Distance(hero.transform.position, rope.landing.position), 0.2f);
            rope.ResetState(); hero.Teleport(new Vector3(800f, 0.05f, 800f), Quaternion.identity);
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube); wall.transform.SetParent(root.transform); wall.transform.position = rope.landing.position + Vector3.up; wall.transform.localScale = Vector3.one * 2f; Physics.SyncTransforms();
            rope.Interact(); yield return new WaitForSeconds(0.18f);
            Assert.AreEqual(0, rope.State); Assert.Less(Vector3.Distance(hero.transform.position, new Vector3(800f, 0.05f, 800f)), 0.2f); Assert.IsTrue(capsule.enabled);
        }
        [UnityTest] public IEnumerator Brace_RequiresRelease_RestoresAndResetsGeometry()
        {
            var latch = Make<PuzzleLever>("nztest.latch"); var wall = Make<DestructibleWall>("nztest.wall"); wall.release = latch;
            wall.intact = Make<Transform>("intact").gameObject; wall.rubble = Make<Transform>("rubble").gameObject;
            yield return null; wall.Interact(); Assert.AreEqual(0, wall.State);
            latch.Interact(); wall.Interact(); Assert.AreEqual(1, wall.State); Assert.IsFalse(wall.intact.activeSelf); Assert.IsTrue(wall.rubble.activeSelf);
            wall.ResetState(); Assert.IsTrue(wall.intact.activeSelf); Assert.IsFalse(wall.rubble.activeSelf);
        }
    }
}
