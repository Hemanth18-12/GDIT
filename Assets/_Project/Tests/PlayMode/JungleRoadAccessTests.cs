using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using LegacyOfTemples.Characters;
using LegacyOfTemples.Core;
using LegacyOfTemples.Gameplay;
using LegacyOfTemples.Gameplay.Vehicles;
using LegacyOfTemples.UI;

namespace LegacyOfTemples.Tests
{
    public class JungleRoadAccessTests
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
        public IEnumerator RepairThenImpact_StillTracksEveryDamageZone()
        {
            yield return LoadOriginal();
            var jeep = Jeep.Instance;
            jeep.RepairAndResetToRoad();
            var points = new[] { new Vector3(0f, 1f, 2f), new Vector3(0f, 1f, -2f),
                new Vector3(-1f, 1f, 0f), new Vector3(1f, 1f, 0f), new Vector3(0f, 2f, 0f) };
            foreach (var point in points)
                Assert.DoesNotThrow(() => jeep.ApplyDamage(point, 1f, jeep.transform.TransformPoint(point), Vector3.forward));
            Assert.AreEqual(5f, jeep.TotalDamage, 0.01f);
            foreach (var zone in new[] { "front", "rear", "left", "right", "top" }) Assert.AreEqual(1f, jeep.ZoneDamage[zone]);
            jeep.RepairAndResetToRoad();
            Assert.AreEqual(5, jeep.ZoneDamage.Count);
            Assert.IsTrue(jeep.ZoneDamage.Values.All(value => value == 0f));
            Assert.DoesNotThrow(() => jeep.ApplyDamage(points[0], 1f, jeep.transform.position, Vector3.forward));
        }

        [UnityTest, Timeout(90000)]
        public IEnumerator JungleEntrance_IsClearForJeep_AfterSceneReload()
        {
            for (int load = 0; load < 2; load++)
            {
                yield return LoadOriginal();
                var trail = JungleChase.Instance;
                Assert.IsTrue(trail.Built);
                Vector3 a = trail.Road[0] + Vector3.forward * 8f + Vector3.up;
                Vector3 b = trail.Road[2] + Vector3.up;
                var hits = Physics.BoxCastAll(a, new Vector3(1f, 0.7f, 0.3f), (b-a).normalized,
                    Quaternion.identity, Vector3.Distance(a,b), ~0, QueryTriggerInteraction.Ignore);
                var barriers = hits.Where(h => h.collider.name.StartsWith("TempleBoundary")).ToArray();
                foreach (var wall in Object.FindObjectsByType<BoxCollider>())
                    if (wall.name.StartsWith("TempleBoundary")) Debug.Log($"[JungleAccess] load {load}: {wall.name} bounds={wall.bounds}");
                Debug.Log($"[JungleAccess] load {load}: road {a}->{b}; boundary blockers={string.Join(", ", barriers.Select(h=>h.collider.name))}");
                Assert.IsEmpty(barriers, "An invisible temple boundary must not cross the jungle road entrance");
            }
        }
    }
}
