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
            if (kb != null && kb.mKey.wasPressedThisFrame)
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

            if (isOpen && playerTransform == null)
            {
                GameObject player = GameObject.FindWithTag("Player");
                if (player != null) playerTransform = player.transform;
            }
        }

        public void ToggleMap()
        {
            isOpen = !isOpen;
            EventBus.RaiseOverlayMenuToggled(isOpen);
            if (isOpen)
            {
                GameObject player = GameObject.FindWithTag("Player");
                if (player != null) playerTransform = player.transform;
            }
        }

        private void LateUpdate()
        {
            if (!isOpen) return;
            string cur = SceneManager.GetActiveScene().name;
            string[,] route = {
                { "Courtyard", "Courtyard", "Temple steps, the Pujari and Meera" },
                { "Sanctum", "Storage Hall", "Empty stands, broken lock" },
                { "ShrineCrime", "Outer Shrine", "Red silk, broken lamp" },
                { "PuzzleWall", "Sun Wall", "Four symbols and the centre wheel" },
                { "JunglePath", "Jungle Path", "Where the trail ends" } };
            var sb = new System.Text.StringBuilder();
            var story = StoryDialogueSystem.Instance;
            string goalScene = story != null && story.Current != null ? story.Current.scene : "";
            for (int i = 0; i < route.GetLength(0); i++)
            {
                bool here = route[i, 0] == cur;
                string name = here ? $"<color={ScreenText.Gold}><b>{route[i, 1]}  (YOU ARE HERE)</b></color>" : $"<b>{route[i, 1]}</b>";
                string goal = route[i, 0] == goalScene && story?.Current != null ? $"\n   <color={ScreenText.Cyan}>Goal: {story.Current.title}</color>" : "";
                sb.Append(i + 1).Append(". ").Append(name).Append("\n   ").Append(route[i, 2]).Append(goal).Append('\n');
                if (i < route.GetLength(0) - 1) sb.Append("   |\n");
            }
            ScreenText.Panel("map", "TEMPLE MAP", sb.ToString(), PanelAnchor.Center, 1000f,
                ScreenText.Key("M") + " / " + ScreenText.Key("Esc") + " Close    " + ScreenText.Key("H") + " Guide", true, 60);
        }

    }
}

