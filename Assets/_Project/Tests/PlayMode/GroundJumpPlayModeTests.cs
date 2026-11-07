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
    /// <summary>
    /// In every map: walk 30 s and press Space 20 times. The feet must stay within [floor, floor + 1.0 m],
    /// return to the floor after each hop, and never fall below the world. (Same check also runs in the
    /// built app via AuditBot "GROUND TEST".)
    /// </summary>
    public class GroundJumpPlayModeTests
    {
        [UnityTest] public IEnumerator Courtyard() => Run("Courtyard");
        [UnityTest] public IEnumerator Sanctum() => Run("Sanctum");
        [UnityTest] public IEnumerator ShrineCrime() => Run("ShrineCrime");
        [UnityTest] public IEnumerator PuzzleWall() => Run("PuzzleWall");
        [UnityTest] public IEnumerator JunglePath() => Run("JunglePath");

        private static IEnumerator Run(string scene)
        {
            yield return SceneManager.LoadSceneAsync(scene);
            yield return new WaitForSeconds(1.5f);
            var pc = Object.FindAnyObjectByType<PlayerController>();
            Assert.IsNotNull(pc, "player missing in " + scene);
            var kb = InputSystem.AddDevice<Keyboard>();
            float floor = pc.transform.position.y, maxRel = 0f, minRel = 0f, t = 0f, nextJump = 1f;
            int jumps = 0, landings = 0; bool air = false;
            Key[] dirs = { Key.W, Key.D, Key.S, Key.A };
            while (t < 30f)
            {
                Key dir = dirs[(int)(t / 2.5f) % 4];
                bool jump = t >= nextJump && jumps < 20;
                if (jump) { jumps++; nextJump = t + 1.4f; }
                InputSystem.QueueStateEvent(kb, jump ? new KeyboardState(dir, Key.Space) : new KeyboardState(dir));
                yield return null;
                t += Time.deltaTime;
                float rel = pc.transform.position.y - floor;
                maxRel = Mathf.Max(maxRel, rel); minRel = Mathf.Min(minRel, rel);
                if (rel > 0.15f) air = true; else if (air) { air = false; landings++; }
            }
            InputSystem.RemoveDevice(kb);
            Assert.Greater(minRel, -0.1f, scene + ": fell below the floor");
            Assert.Less(maxRel, 1.0f, scene + ": rose more than 1 m above the floor (climbing or floating)");
            Assert.GreaterOrEqual(landings, 10, scene + ": did not land after jumps");
        }
    }
}
