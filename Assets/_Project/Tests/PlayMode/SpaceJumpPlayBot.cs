using System.Collections;
using System.IO;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using LegacyOfTemples.Characters;
using LegacyOfTemples.UI;

namespace LegacyOfTemples.Tests
{
    /// <summary>Space play-bot: single press, hold 3 s, 20 presses, Space during dialogue and the tutorial card. Per-frame log to Docs/QA/space_jump_log.txt.</summary>
    public class SpaceJumpPlayBot
    {
        private PlayerController pc; private CharacterGrounding g; private Keyboard kb; private Animator anim;
        private readonly StringBuilder log = new StringBuilder();
        private float floorY;

        private void Frame(string tag, float t)
        {
            var st = anim != null ? anim.GetCurrentAnimatorStateInfo(0) : default;
            bool snap = g.TryFindGround(0.5f, 1.5f, out RaycastHit h);
            log.Append($"{tag} t={t:0.000} y={pc.transform.position.y - floorY:0.000} vy={pc.VerticalVelocity:0.00} ccGrounded={pc.GetComponent<CharacterController>().isGrounded} " +
                       $"anim={(anim != null ? st.shortNameHash.ToString() : "-")} rootMotion={(anim != null && anim.applyRootMotion)} rmDeltaY={(anim != null ? anim.deltaPosition.y : 0f):0.000} snapHit={snap} jumps={pc.JumpCount}\n");
        }

        private IEnumerator Run(string tag, float seconds, System.Func<float, bool> spaceDown, float[] stats)
        {
            float peak = 0f, landT = -1f, t = 0f; bool left = false;
            while (t < seconds)
            {
                InputSystem.QueueStateEvent(kb, spaceDown(t) ? new KeyboardState(Key.Space) : new KeyboardState());
                yield return null; t += Time.deltaTime;
                float y = pc.transform.position.y - floorY; peak = Mathf.Max(peak, y);
                if (y > 0.05f) left = true; else if (left && landT < 0f) landT = t;
                Frame(tag, t);
            }
            stats[0] = peak; stats[1] = landT; stats[2] = pc.transform.position.y - floorY;
        }

        [UnityTest]
        public IEnumerator SpaceBehaviour()
        {
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            yield return SceneManager.LoadSceneAsync("Courtyard");
            pc = null;
            for (float w = 0; w < 40f && (pc == null || !pc.enabled); w += Time.deltaTime) { pc = Object.FindAnyObjectByType<PlayerController>(); yield return null; }
            TutorialManager.Instance?.SkipTutorial();
            UIManager.Instance?.DismissHowToPlay();
            yield return new WaitForSeconds(2f);
            g = pc.GetComponent<CharacterGrounding>(); anim = pc.GetComponentInChildren<Animator>();
            Assert.IsTrue(g.TryFindGround(0.5f, 1.5f, out RaycastHit fl)); floorY = fl.point.y;
            kb = InputSystem.AddDevice<Keyboard>();
            InputSystem.QueueStateEvent(kb, new KeyboardState()); yield return null; yield return null;
            var r = new StringBuilder("# Space play-bot results\n");
            var s = new float[3];

            int j0 = pc.JumpCount;
            yield return Run("single", 3f, t => t < 0.1f, s);
            r.Append($"single press: peak {s[0]:0.000} m, back on floor at {s[1]:0.00} s, end {s[2]:0.000} m, jumps {pc.JumpCount - j0}\n");
            Assert.LessOrEqual(s[0], 0.9f); Assert.Greater(s[0], 0.4f); Assert.That(s[1] > 0f && s[1] <= 1.2f, "landing time"); Assert.AreEqual(1, pc.JumpCount - j0);

            j0 = pc.JumpCount;
            yield return Run("hold", 3f, t => true, s);
            r.Append($"hold 3 s: peak {s[0]:0.000} m, end {s[2]:0.000} m, jumps {pc.JumpCount - j0}\n");
            Assert.LessOrEqual(s[0], 0.9f); Assert.Less(Mathf.Abs(s[2]), 0.05f); Assert.AreEqual(1, pc.JumpCount - j0, "holding must jump once");

            j0 = pc.JumpCount;
            yield return Run("x20", 20 * 0.35f + 1.5f, t => t < 20 * 0.35f && (t % 0.35f) < 0.06f, s);
            r.Append($"20 presses (every 0.35 s, many in mid-air): peak {s[0]:0.000} m, end {s[2]:0.000} m, jumps {pc.JumpCount - j0}\n");
            Assert.LessOrEqual(s[0], 0.9f); Assert.Less(Mathf.Abs(s[2]), 0.05f);

            UIManager.Instance.StartDialogue("Pujari Ananda", new[] { "Test line one.", "Test line two.", "Test line three." });
            yield return new WaitForSeconds(0.5f);
            j0 = pc.JumpCount;
            int presses = 0; bool wasDown = false;
            yield return Run("dialogue", 3f, t => { bool down = UIManager.Instance.IsDialogueActive && (t % 0.5f) < 0.06f; if (down && !wasDown) presses++; wasDown = down; return down; }, s);
            r.Append($"Space x{presses} during dialogue (last press closes it): peak {s[0]:0.000} m, jumps {pc.JumpCount - j0}, dialogue advanced/closed={!UIManager.Instance.IsDialogueActive}\n");
            Flush(r);
            Assert.AreEqual(0, pc.JumpCount - j0, "Space during dialogue must not jump");

            var tm = TutorialManager.Instance;
            if (tm != null)
            {
                tm.StartTutorial();
                yield return new WaitForSeconds(0.6f);
                j0 = pc.JumpCount;
                bool card = tm.IsActive && tm.CurrentStep == TutorialManager.TutorialStep.Welcome;
                yield return Run("tutorial", 0.5f, t => t < 0.1f, s);
                r.Append($"Space on tutorial card (card shown={card}): jumps {pc.JumpCount - j0}, step now {tm.CurrentStep}\n");
                Assert.AreEqual(0, pc.JumpCount - j0, "Space on the tutorial card must not jump");
                tm.SkipTutorial();
            }
            InputSystem.RemoveDevice(kb);
            Flush(r);
        }

        private void Flush(StringBuilder r)
        {
            Debug.Log("[SpaceBot]\n" + r);
            string dir = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Docs/QA"));
            File.WriteAllText(Path.Combine(dir, "space_jump_results.txt"), r.ToString());
            File.WriteAllText(Path.Combine(dir, "space_jump_log.txt"), log.ToString());
        }
    }
}
