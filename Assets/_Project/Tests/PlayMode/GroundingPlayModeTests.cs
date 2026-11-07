using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using LegacyOfTemples.Characters;
using LegacyOfTemples.Gameplay;

namespace LegacyOfTemples.Tests
{
    public class GroundingPlayModeTests : InputTestFixture
    {
        private Keyboard keyboard;
        private GameObject direction;
        private string EvidenceRoot => Path.GetFullPath(Path.Combine(Application.dataPath,
            new DirectoryInfo(Application.dataPath).Parent.Name == "My project" ? "../../Docs/QA/2026-11-06" : "../Docs/QA/2026-11-06"));

        public override void Setup()
        {
            base.Setup();
            keyboard = InputSystem.AddDevice<Keyboard>();
            Time.timeScale = 1;
            Directory.CreateDirectory(EvidenceRoot);
        }

        public override void TearDown()
        {
            if (direction != null) Object.Destroy(direction);
            Time.timeScale = 1;
            base.TearDown();
        }

        [UnityTest] public IEnumerator CourtyardThirtySeconds() => Walk("Courtyard", 15f);
        [UnityTest] public IEnumerator SanctumThirtySeconds() => Walk("Sanctum", 6f);
        [UnityTest] public IEnumerator PuzzleWallThirtySeconds() => Walk("PuzzleWall", 5f);
        [UnityTest] public IEnumerator ShrineCrimeThirtySeconds() => Walk("ShrineCrime", 4f);
        [UnityTest] public IEnumerator JunglePathThirtySeconds() => Walk("JunglePath", 18f);

        [UnityTest]
        public IEnumerator CourtyardStairsAndJump()
        {
            yield return SceneManager.LoadSceneAsync("Courtyard");
            yield return null;
            foreach (var c in Object.FindObjectsByType<OpeningCinematic>()) c.EndCinematic();
            var player = Object.FindAnyObjectByType<PlayerController>();
            player.enabled = true;
            player.Teleport(new Vector3(0, 0.1f, 5), Quaternion.identity);
            direction = new GameObject("QA stair input direction");
            direction.transform.rotation = Quaternion.Euler(0, 180, 0);
            player.SetCameraTransform(direction.transform);
            Press(keyboard.wKey);
            float elapsed = 0, high = 0;
            while (player.transform.position.z > 0.6f && elapsed < 5)
            {
                yield return null;
                elapsed += Time.unscaledDeltaTime;
                high = Mathf.Max(high, player.transform.position.y);
            }
            Release(keyboard.wKey);
            yield return new WaitForSeconds(0.3f);
            Assert.That(player.transform.position.z, Is.LessThan(1), "Player could not climb visible steps");
            Assert.That(high, Is.GreaterThan(0.4f), "Climbing must gain real height");
            float beforeJump = player.GetComponent<CharacterGrounding>().FeetY;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Space));
            yield return null;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            float apex = beforeJump;
            for (float t=0; t<1.8f; t+=Time.unscaledDeltaTime)
            {
                yield return null;
                apex = Mathf.Max(apex, player.GetComponent<CharacterGrounding>().FeetY);
            }
            float landed = player.GetComponent<CharacterGrounding>().FeetY;
            string result = $"Stairs: climbedRootY={high:F4}, jumpRise={apex-beforeJump:F4}, landingError={Mathf.Abs(landed-beforeJump):F4}\n";
            File.WriteAllText(Path.Combine(EvidenceRoot, "stairs-and-jump.txt"), result);
            Assert.That(apex-beforeJump, Is.GreaterThan(0.5f), "Ground snap must not pin a jumping player");
            Assert.That(Mathf.Abs(landed-beforeJump), Is.LessThanOrEqualTo(0.05f), result);
            direction.transform.rotation = Quaternion.identity;
            Press(keyboard.wKey);
            elapsed = 0;
            while (player.transform.position.z < 4.8f && elapsed < 5)
            {
                yield return null;
                elapsed += Time.unscaledDeltaTime;
            }
            Release(keyboard.wKey);
            yield return new WaitForSeconds(0.3f);
            Assert.That(player.GetComponent<CharacterGrounding>().FeetY, Is.InRange(-0.05f, 0.05f), "Feet must return to plaza height after descending");
        }

        [UnityTest]
        public IEnumerator RenderedPoseProbe()
        {
            yield return SceneManager.LoadSceneAsync("JunglePath");
            yield return null;
            var player = Object.FindAnyObjectByType<PlayerController>();
            var animator = player.GetComponentInChildren<Animator>();
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            foreach (var c in Object.FindObjectsByType<OpeningCinematic>()) c.EndCinematic();
            yield return new WaitForSeconds(1);
            var report = new System.Text.StringBuilder();
            CharacterMeshBounds.TryMeasure(player.transform, out var bounds);
            report.AppendLine($"root={player.transform.position:F5}, visual={animator.transform.localPosition:F5}, baked={bounds}, size={bounds.size:F5}, groundingFootOffset={player.GetComponent<CharacterGrounding>().FootOffset:F5}");
            foreach (var bone in new[] { HumanBodyBones.Hips, HumanBodyBones.LeftFoot, HumanBodyBones.RightFoot, HumanBodyBones.Head })
                report.AppendLine($"{bone}: {animator.GetBoneTransform(bone).position:F5}");
            foreach (var renderer in player.GetComponentsInChildren<Renderer>()) report.AppendLine($"{renderer.name}: {renderer.bounds}");
            File.WriteAllText(Path.Combine(EvidenceRoot, "rendered-pose-probe.txt"), report.ToString());
            Debug.Log(report);
        }

        private IEnumerator Walk(string scene, float z)
        {
            yield return SceneManager.LoadSceneAsync(scene);
            yield return null;
            yield return null;
            Assert.That(Object.FindObjectsByType<PlayerController>().Length, Is.EqualTo(1), "Exactly one authored player must be active");
            Assert.That(Object.FindObjectsByType<AudioListener>().Length, Is.EqualTo(1), "Scenes must not load Courtyard over themselves");
            foreach (var c in Object.FindObjectsByType<OpeningCinematic>()) c.EndCinematic();
            var player = Object.FindAnyObjectByType<PlayerController>();
            Assert.That(player, Is.Not.Null, $"{scene}: player missing");
            player.enabled = true;
            var cc = player.GetComponent<CharacterController>();
            player.Teleport(new Vector3(0, 0.05f, z), Quaternion.identity);
            direction = new GameObject("QA input direction");
            player.SetCameraTransform(direction.transform);
            var animator = player.GetComponentInChildren<Animator>();
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            yield return new WaitForSeconds(0.2f);
            var diagnostics = new System.Text.StringBuilder();
            CharacterMeshBounds.TryMeasure(player.transform, out var posedBounds);
            diagnostics.AppendLine($"{scene} root={player.transform.position:F5}, posedBounds={posedBounds}, size={posedBounds.size:F5}, controller={cc.height}, center={cc.center}");
            foreach (var bone in new[] { HumanBodyBones.Hips, HumanBodyBones.LeftFoot, HumanBodyBones.RightFoot, HumanBodyBones.Head })
                diagnostics.AppendLine($"{bone}: {animator.GetBoneTransform(bone).position:F5}");
            foreach (var renderer in player.GetComponentsInChildren<Renderer>()) diagnostics.AppendLine($"{renderer.name}: {renderer.bounds}");
            File.WriteAllText(Path.Combine(EvidenceRoot, scene + "-posed-mesh.txt"), diagnostics.ToString());
            Assert.That(animator.applyRootMotion, Is.False, "Capsule and Animator must not both translate the player");
            Press(keyboard.wKey);
            float elapsed = 0, sampleAt = 0, maxError = 0, travelled = 0, lowest = float.PositiveInfinity;
            int samples = 0, missingGround = 0;
            Vector3 previous = player.transform.position;
            // A real, camera-relative W press drives the normal PlayerController. Short
            // turns keep each patrol inside the walkable scene, without teleporting.
            var waypoints = new[] { new Vector3(1.2f, 0, z), new Vector3(1.2f, 0, z-2),
                new Vector3(-1.2f, 0, z-2), new Vector3(-1.2f, 0, z) };
            int waypoint = 0;
            while (elapsed < 30f)
            {
                var delta = waypoints[waypoint] - player.transform.position;
                delta.y = 0;
                if (delta.magnitude < 0.35f) { waypoint = (waypoint + 1) % waypoints.Length; delta = waypoints[waypoint] - player.transform.position; delta.y = 0; }
                direction.transform.rotation = Quaternion.LookRotation(delta.normalized);
                yield return null;
                elapsed += Time.unscaledDeltaTime;
                travelled += Vector3.Distance(previous, player.transform.position);
                previous = player.transform.position;
                lowest = Mathf.Min(lowest, player.transform.position.y);
                if (elapsed >= sampleAt)
                {
                    sampleAt += 0.15f;
                    Assert.That(CharacterMeshBounds.TryMeasure(player.transform, out var mesh), Is.True);
                    float feet = player.transform.TransformPoint(new Vector3(0, mesh.min.y, 0)).y;
                    var origin = player.transform.position + Vector3.up * 2;
                    bool groundFound = false;
                    float groundY = float.NegativeInfinity;
                    foreach (var hit in Physics.RaycastAll(origin, Vector3.down, 5, ~0, QueryTriggerInteraction.Ignore))
                    {
                        if (hit.transform.IsChildOf(player.transform) || hit.collider.GetComponentInParent<NPCDialogueTrigger>() != null || hit.normal.y < 0.7f) continue;
                        groundY = Mathf.Max(groundY, hit.point.y);
                        groundFound = true;
                    }
                    if (!groundFound) missingGround++;
                    else maxError = Mathf.Max(maxError, Mathf.Abs(feet-groundY));
                    samples++;
                }
                if (elapsed > 8 && elapsed-Time.unscaledDeltaTime <= 8)
                    ScreenCapture.CaptureScreenshot(Path.Combine(EvidenceRoot, $"playmode-{scene}.png"));
            }
            Release(keyboard.wKey);
            string result = $"{scene}: seconds={elapsed:F2}, distance={travelled:F2}m, samples={samples}, maxActualFootError={maxError:F5}m, missingGround={missingGround}, lowestRootY={lowest:F5}, validatedAnimation={player.HasValidatedAnimation}, skin={cc.skinWidth:F3}\n";
            File.AppendAllText(Path.Combine(EvidenceRoot, "grounding-playmode-results.txt"), result);
            Debug.Log(result);
            Assert.That(missingGround, Is.Zero, result);
            Assert.That(travelled, Is.GreaterThan(10), "A stationary bot cannot verify walking");
            Assert.That(lowest, Is.GreaterThan(-1), "Player fell below the world");
            Assert.That(maxError, Is.LessThanOrEqualTo(0.05f), result);
        }
    }
}
