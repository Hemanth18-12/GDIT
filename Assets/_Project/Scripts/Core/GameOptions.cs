using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LegacyOfTemples.Core
{
    /// <summary>Player options shared by the main menu and the pause menu (stored in PlayerPrefs).</summary>
    public static class GameOptions
    {
        public static event Action OnChanged;
        private static int Get(string k, int d) => PlayerPrefs.GetInt(k, d);
        private static void Set(string k, int v) { PlayerPrefs.SetInt(k, v); PlayerPrefs.Save(); OnChanged?.Invoke(); }

        // ---- difficulty (Docs/LEVEL_DESIGN.md) ----
        public static readonly string[] DifficultyNames = { "Easy", "Normal", "Hard" };
        public static int Difficulty { get => Mathf.Clamp(Get("Settings_Difficulty", 1), 0, 2); set => Set("Settings_Difficulty", Mathf.Clamp(value, 0, 2)); }
        private static float D(float e, float n, float h) => Difficulty == 0 ? e : Difficulty == 1 ? n : h;
        public static float SecondsPerStoryMinute => D(12f, 9f, 7f);
        public static float PenaltyScale => D(0.5f, 1f, 1.5f);
        public static float SightRange => D(9f, 12f, 15f);
        public static float SightHalfAngle => D(30f, 35f, 45f);
        public static float DetectSeconds => D(2.0f, 1.2f, 0.7f);
        public static float CounterWindow => D(0.35f, 0.25f, 0.15f);
        public static float DamageTaken => D(0.7f, 1f, 1.3f);
        public static float GuardScale => D(0.8f, 1f, 1.25f);
        public static float WallResetSeconds => D(120f, 90f, 70f);
        public static int GuideCostMinutes => Difficulty == 0 ? 0 : 1;

        // ---- weather quality ----
        public static readonly string[] WeatherNames = { "Off", "Low", "High" };
        public static int WeatherQuality { get => Mathf.Clamp(Get("Settings_Weather", 2), 0, 2); set => Set("Settings_Weather", Mathf.Clamp(value, 0, 2)); }

        // ---- on-screen controls ----
        public static readonly string[] TouchNames = { "Off", "In fights", "Always" };
        public static int TouchMode { get => Mathf.Clamp(Get("Settings_Touch", 1), 0, 2); set => Set("Settings_Touch", Mathf.Clamp(value, 0, 2)); }
        public static readonly string[] SizeNames = { "Small", "Medium", "Large" };
        public static int TouchSize { get => Mathf.Clamp(Get("Settings_TouchSize", 1), 0, 2); set => Set("Settings_TouchSize", Mathf.Clamp(value, 0, 2)); }
        public static float TouchScale => TouchSize == 0 ? 0.85f : TouchSize == 1 ? 1f : 1.15f;
        public static readonly string[] OpacityNames = { "40%", "60%", "80%" };
        public static int TouchOpacityIndex { get => Mathf.Clamp(Get("Settings_TouchOpacity", 1), 0, 2); set => Set("Settings_TouchOpacity", Mathf.Clamp(value, 0, 2)); }
        public static float TouchOpacity => 0.4f + 0.2f * TouchOpacityIndex;
    }

    /// <summary>All scene changes go through here so the loading screen can take over (falls back to a direct load).</summary>
    public static class SceneLoader
    {
        public static Action<string> Handler;
        public static bool IsLoading;
        public static void Load(string scene)
        {
            if (IsLoading) return;
            if (Handler != null) Handler(scene); else SceneManager.LoadScene(scene);
        }
    }

    /// <summary>Virtual input written by the on-screen controls and read by gameplay next to keyboard and gamepad.</summary>
    public static class VirtualInput
    {
        public static Vector2 Move, Look;
        public static bool Sprint, Block, CrouchHeld, TouchVisible;
        private static readonly System.Collections.Generic.Dictionary<string, int> pressed = new System.Collections.Generic.Dictionary<string, int>();
        /// <summary>Called by a button or a bot: the action reads as "pressed" for the whole of the next frame.</summary>
        public static void Press(string action) { pressed[action] = Time.frameCount + 1; }
        public static bool Down(string action) => pressed.TryGetValue(action, out int f) && f == Time.frameCount;
        public static void Clear() { Move = Look = Vector2.zero; Sprint = Block = CrouchHeld = false; pressed.Clear(); }
    }
}
