using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using LegacyOfTemples.Core;
using LegacyOfTemples.Characters;

namespace LegacyOfTemples.Gameplay
{
    /// <summary>
    /// F9 demo menu for presentations: go to any of the eight V3 places (the story moves forward to that place's step),
    /// including the final fight in the ruin court; skip the cutscene; restore health; complete the current step.
    /// Only in development builds, or in a release build when Settings > Demo mode is On. It never moves the story back.
    /// </summary>
    public sealed class DemoMenu : MonoBehaviour
    {
        public static DemoMenu Instance { get; private set; }
        public static bool Open { get; private set; }
        public static bool Allowed => Debug.isDebugBuild || GameOptions.DemoMode;
        private string msg = "";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            if (Instance != null) return;
            var g = new GameObject("DemoMenu"); DontDestroyOnLoad(g); Instance = g.AddComponent<DemoMenu>();
        }

        private void Update()
        {
            var kb = Keyboard.current; if (kb == null) return;
            if (kb.f9Key.wasPressedThisFrame) { if (Allowed) { Open = !Open; msg = ""; } else Debug.Log("[Demo] F9 ignored: release build and Demo mode is Off"); }
            if (!Open) return;
            if (!Allowed || SceneManager.GetActiveScene().name != "Courtyard") { Open = false; return; }
            Key[] digits = { Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5, Key.Digit6, Key.Digit7, Key.Digit8 };
            for (int i = 0; i < digits.Length; i++) if (kb[digits[i]].wasPressedThisFrame) StartCoroutine(Jump(i));
            if (kb.kKey.wasPressedThisFrame) { IntroCinematic.SkipNow = IntroCinematic.Playing; msg = IntroCinematic.Playing ? "Skipping the cutscene." : "No cutscene is playing."; }
            if (kb.uKey.wasPressedThisFrame) { Brawl.DemoRestorePlayer(); msg = Brawl.Active != null ? "Health restored." : "Health is full (no fight running)."; }
            if (kb.pKey.wasPressedThisFrame) { var st = StoryDialogueSystem.Instance; if (st != null && st.Current != null) { string t = st.Current.title; st.DebugCompleteCurrent(); msg = "Completed: " + t; } }
            if (kb.escapeKey.wasPressedThisFrame) { Open = false; EventBus.EscapeConsumedFrame = Time.frameCount; }

            var story = StoryDialogueSystem.Instance;
            var sb = new System.Text.StringBuilder("<b>Go to</b> (the story moves forward to that place if needed):\n");
            for (int i = 0; i < V3Places.Count; i++) sb.Append(ScreenText.Key((i + 1).ToString())).Append(' ').Append(V3Places.Names[i]).Append('\n');
            sb.Append('\n').Append(ScreenText.Key("K")).Append(" Skip the cutscene    ").Append(ScreenText.Key("U")).Append(" Restore health\n");
            sb.Append(ScreenText.Key("P")).Append(" Complete this step: ").Append(story?.Current != null ? story.Current.title : "(none)").Append('\n');
            if (msg != "") sb.Append($"\n<color={ScreenText.Cyan}>{msg}</color>");
            ScreenText.Panel("demo", "DEMO MENU", sb.ToString(), PanelAnchor.Center, 820f, ScreenText.Key("F9") + " / " + ScreenText.Key("Esc") + " close", true, 70);
        }

        /// <summary>Move the story forward (never back) to the place's step, one step per frame (the story applies each
        /// completion on the next frame), then teleport there. Place 8 also ends the jeep chase so the ruin-court fight can start.</summary>
        private System.Collections.IEnumerator Jump(int i)
        {
            var story = StoryDialogueSystem.Instance; if (story == null) yield break;
            if (Brawl.Active != null) { msg = "Finish or leave the fight first."; yield break; }
            int target = -1;
            for (int k = 0; k < StoryData.Objectives.Length; k++) if (StoryData.Objectives[k].id == V3Places.Objectives[i]) { target = k; break; }
            int guard = 0;
            while (target >= 0 && !story.IsComplete && story.ObjectiveIndex < target && guard++ < 30) { Debug.Log($"[Demo] completing {story.Current?.id}"); story.DebugCompleteCurrent(); yield return null; yield return null; }
            yield return null;
            var jeep = Vehicles.Jeep.Instance; if (jeep != null && jeep.Occupied) yield return jeep.GetOut();
            if (i == 7) JungleChase.Instance?.DemoSkipChase();
            string why = V3Places.TeleportTo(i);
            msg = why ?? ""; if (why == null) Open = false;
            Debug.Log($"[Demo] go to {V3Places.Names[i]}: objective now {story.Current?.id} {(why ?? "ok")}");
        }
    }
}
