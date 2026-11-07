using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using LegacyOfTemples.Characters;

namespace LegacyOfTemples.Tests
{
    /// <summary>Courtyard: hero spawns on the floor, one hop stays under 0.9 m and lands, 10 s walk never climbs the temple.</summary>
    public class HeroScaleGroundTest
    {
        [UnityTest]
        public IEnumerator HeroStandsHopsAndWalksOnFloor()
        {
            yield return SceneManager.LoadSceneAsync("Courtyard");
            yield return new WaitForSeconds(2f);
            var pc = Object.FindAnyObjectByType<PlayerController>();
            Assert.IsNotNull(pc, "no hero");
            var g = pc.GetComponent<CharacterGrounding>();
            Assert.IsTrue(g.TryFindGround(0.5f, 1.5f, out RaycastHit floor), "no floor under hero");
            float gap = g.FeetY - floor.point.y;
            Debug.Log($"[HeroTest] feet-floor gap {gap:0.000} m");
            Assert.Less(Mathf.Abs(gap), 0.02f, "feet not on floor");

            for (float w = 0; w < 40f && !pc.enabled; w += Time.deltaTime) yield return null; // opening cinematic owns the hero
            Debug.Log($"[HeroTest] player enabled={pc.enabled}, timeScale={Time.timeScale}");
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus; // batch mode has no window focus
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            var kb = InputSystem.AddDevice<Keyboard>();
            InputSystem.QueueStateEvent(kb, new KeyboardState()); yield return null; yield return null;
            Debug.Log($"[HeroTest] Keyboard.current is test device: {Keyboard.current == kb}");
            float y0 = pc.transform.position.y, peak = 0f;
            InputSystem.QueueStateEvent(kb, new KeyboardState(Key.Space)); yield return null; yield return null;
            InputSystem.QueueStateEvent(kb, new KeyboardState()); 
            for (float t = 0; t < 2f; t += Time.deltaTime) { yield return null; peak = Mathf.Max(peak, pc.transform.position.y - y0); }
            float back = pc.transform.position.y - y0;
            Debug.Log($"[HeroTest] hop peak {peak:0.000} m, after landing {back:0.000} m");
            Assert.Greater(peak, 0.3f, "Space did not hop");
            Assert.LessOrEqual(peak, 0.9f, "hop too high");
            Assert.Less(Mathf.Abs(back), 0.05f, "did not return to the floor");

            float maxRise = 0f; Vector3 p0 = pc.transform.position; float nextLog = 0f;
            Debug.Log($"[HeroTest] walk start {p0}");
            for (float t = 0; t < 10f; t += Time.deltaTime)
            {
                InputSystem.QueueStateEvent(kb, new KeyboardState(Key.W));
                yield return null;
                maxRise = Mathf.Max(maxRise, pc.transform.position.y - y0);
                if (t >= nextLog) { nextLog += 1f; Debug.Log($"[HeroTest]  t={t:0.0} pos={pc.transform.position} groundedCC={pc.GetComponent<CharacterController>().isGrounded}"); }
            }
            InputSystem.RemoveDevice(kb);
            Debug.Log($"[HeroTest] walk 10 s toward temple: max rise {maxRise:0.000} m, moved to {pc.transform.position}");
            Assert.Less(maxRise, 1.0f, "climbed onto the temple / roof");
        }
    }
}
