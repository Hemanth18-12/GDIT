using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using LegacyOfTemples.Characters;
using LegacyOfTemples.Core;
using LegacyOfTemples.Gameplay;

namespace LegacyOfTemples.UI
{
    /// <summary>
    /// The whole tutorial: five small hints, one at a time, that never block play.
    /// Move, Look, Interact, Journal/Map, Help. Each one goes away when the player does it.
    /// Everything else is in the CONTROLS panel (main menu and pause).
    /// </summary>
    public sealed class HintTutorial : MonoBehaviour
    {
        public const int Count = 5;
        private int shownStep = -1; private float shownFor;
        public const float MaxSeconds = 10f;
        public static int Step { get => PlayerPrefs.GetInt("Hints_Step", Count); private set { PlayerPrefs.SetInt("Hints_Step", value); PlayerPrefs.Save(); } }
        public static bool Showing { get; private set; }
        public static void Restart() { Step = 0; }

        private static readonly string[] Text =
        {
            "to move",
            "to look around",
            "near a person or object to interact",
            "for your journal and map",
            "when you need help",
        };
        private static readonly string[] Keys = { "W A S D", "Mouse", "E", "J / M", "H" };

        private PlayerController player; private Vector3 lastPos; private float moved, looked, doneAt = -1f, quiet, lastYaw = -1000f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            var go = new GameObject("HintTutorial"); DontDestroyOnLoad(go); go.AddComponent<HintTutorial>();
        }

        private void OnEnable() { SceneManager.sceneLoaded += OnScene; }
        private void OnDisable() { SceneManager.sceneLoaded -= OnScene; }

        private void OnScene(Scene s, LoadSceneMode m)
        {
            player = null; quiet = 0f;
            if (s.name != "MainMenu" && PlayerPrefs.GetInt("Force_Tutorial_On_Start", 0) == 1)
            {   // a new game was started from the menu
                PlayerPrefs.SetInt("Force_Tutorial_On_Start", 0); Restart();
            }
        }

        private void Update()
        {
            Showing = false;
            int step = Step;
            if (step >= Count || SceneManager.GetActiveScene().name == "MainMenu") return;
            if (player == null) { player = FindAnyObjectByType<PlayerController>(); if (player == null) return; lastPos = player.transform.position; }
            bool free = player.enabled && Time.timeScale > 0f && !InputGate.GameplayBlocked && !SceneLoader.IsLoading && !CombatState.InCombat;
            if (!free) { quiet = 0f; lastPos = player.transform.position; return; }
            quiet += Time.unscaledDeltaTime; if (quiet < 0.8f) return; // let the screen settle after a card or cutscene

            if (doneAt >= 0f)
            {   // a short tick, then the next hint
                ScreenText.Panel("hint_tutorial", null, $"<color={ScreenText.Green}><b>Done</b></color>", PanelAnchor.BottomCenter, 160f, null, false, 3);
                Showing = true;
                if (Time.unscaledTime - doneAt > 0.7f) { doneAt = -1f; moved = looked = 0f; Step = step + 1; }
                return;
            }

            if (shownStep != step) { shownStep = step; shownFor = 0f; }
            shownFor += Time.unscaledDeltaTime;
            if (shownFor > MaxSeconds) { Step = step + 1; moved = looked = 0f; return; }   // V3: a hint never sits on screen for long; it moves on by itself
            ScreenText.Panel("hint_tutorial", null, $"{ScreenText.Key(Keys[step])}  {Text[step]}", PanelAnchor.BottomCenter, 420f, null, false, 3);
            Showing = true;

            var kb = Keyboard.current; var mouse = Mouse.current; var gp = Gamepad.current; bool done = false;
            switch (step)
            {
                case 0:
                    Vector3 d = player.transform.position - lastPos; d.y = 0f; moved += d.magnitude; lastPos = player.transform.position;
                    done = moved > 2f; break;
                case 1:
                    var cam = Camera.main;   // any way of turning the view counts: mouse, stick, touch
                    if (cam != null) { float yaw = cam.transform.eulerAngles.y; if (lastYaw > -999f) looked += Mathf.Abs(Mathf.DeltaAngle(lastYaw, yaw)) * 6f; lastYaw = yaw; }
                    if (mouse != null) looked += mouse.delta.ReadValue().magnitude;
                    if (gp != null) looked += gp.rightStick.ReadValue().magnitude * 600f * Time.unscaledDeltaTime;
                    looked += VirtualInput.Look.magnitude;
                    done = looked > 250f; break;
                case 2:
                    done = player.CurrentInteractable != null && ((kb != null && kb.eKey.wasPressedThisFrame) || (gp != null && gp.buttonWest.wasPressedThisFrame) || VirtualInput.Down("interact")); break;
                case 3:
                    done = kb != null && (kb.jKey.wasPressedThisFrame || kb.mKey.wasPressedThisFrame); break;
                case 4:
                    done = kb != null && (kb.hKey.wasPressedThisFrame || kb.tabKey.wasPressedThisFrame); break;
            }
            if (done) doneAt = Time.unscaledTime;
        }
    }
}
