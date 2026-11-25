using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using LegacyOfTemples.Core;

namespace LegacyOfTemples.Gameplay
{
    public class TempleMapOverlay : MonoBehaviour
    {
        public static TempleMapOverlay Instance { get; private set; }

        private bool isOpen = false;
        public bool IsOpen => isOpen;
        private Transform playerTransform;


        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Update()
        {
            var kb = Keyboard.current;
            if ((kb != null && kb.mKey.wasPressedThisFrame) || VirtualInput.Down("map"))
            {
                ToggleMap();
            }

            var gp = Gamepad.current;
            if (gp != null && gp.dpad.down.wasPressedThisFrame)
            {
                ToggleMap();
            }

            if (isOpen && kb != null && kb.escapeKey.wasPressedThisFrame && (EventBus.EscapeConsumedFrame = Time.frameCount) > 0)
            {
                ToggleMap();
            }

            if (isOpen && NewWorldDirector.Instance != null && kb != null && kb.tKey.wasPressedThisFrame)
            {
                if (NewWorldDirector.Instance.GetComponent<PujariLoreDirector>().QuickReturn(true)) ToggleMap();
            }

            if (isOpen && playerTransform == null)
            {
                GameObject player = GameObject.FindWithTag("Player");
                if (player != null) playerTransform = player.transform;
            }
        }

        public void ToggleMap()
        {
            isOpen = !isOpen; travelMsg = "";
            EventBus.RaiseOverlayMenuToggled(isOpen);
            if (isOpen)
            {
                GameObject player = GameObject.FindWithTag("Player");
                if (player != null) playerTransform = player.transform;
            }
        }

        private string travelMsg = "";
        /// <summary>Fast travel: only to places the story has reached, never during a fight; costs 2 minutes on the Dawn Clock (design: LEVEL_DESIGN_V3).</summary>
        private void TravelTo(int i)
        {
            if (i < 0 || i >= V3Places.Count) return;
            if (Brawl.Active != null) { travelMsg = "Not during a fight."; return; }
            if (!V3Places.Unlocked(i)) { travelMsg = "Not discovered yet."; return; }
            string why = V3Places.TeleportTo(i);
            if (why != null) { travelMsg = why; return; }
            DawnClock.Instance?.Penalty(2f, "fast travel");
            travelMsg = ""; ToggleMap();
        }

        private void LateUpdate()
        {
            if (!isOpen) return;
            var kb = Keyboard.current;
            if (kb != null)
            {
                Key[] keys = { Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5, Key.Digit6, Key.Digit7, Key.Digit8 };
                for (int k = 0; k < keys.Length; k++) if (kb[keys[k]].wasPressedThisFrame) { TravelTo(k); if (!isOpen) return; }
            }
            var sb = new System.Text.StringBuilder();
            var story = StoryDialogueSystem.Instance;
            for (int i = 0; i < V3Places.Count; i++)
            {
                bool open = V3Places.Unlocked(i);
                bool goal = story != null && story.Current != null && story.Current.id == V3Places.Objectives[i] && i != 7;
                string name = open ? $"{ScreenText.Key((i + 1).ToString())} <b>{V3Places.Names[i]}</b>" : $"<color=#8a8a8a>{i + 1}  {V3Places.Names[i]}  (not reached yet)</color>";
                sb.Append(name);
                if (goal) sb.Append($"   <color={ScreenText.Cyan}>Goal: {story.Current.title}</color>");
                sb.Append('\n');
            }
            if (NewWorldDirector.Instance != null) sb.Append("\n[T] Central Temple Courtyard — Pujari Ananda (carried relics)\n");
            if (travelMsg != "") sb.Append($"\n<color={ScreenText.Coral}>{travelMsg}</color>");
            ScreenText.Panel("map", "MAP: FAST TRAVEL", sb.ToString(), PanelAnchor.Center, 900f,
                ScreenText.Key("1-8") + " Travel (2 min)    " + ScreenText.Key("M") + " / " + ScreenText.Key("Esc") + " Close", true, 60);
        }
    }
}

