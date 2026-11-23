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

        // ---- graphics and camera (V3) ----
        public static readonly string[] GraphicsNames = { "Low", "Medium", "High", "Ultra" };
        /// <summary>Graphics preset. High is the default.</summary>
        public static int Graphics { get => Mathf.Clamp(Get("Settings_Graphics", 2), 0, 3); set => Set("Settings_Graphics", Mathf.Clamp(value, 0, 3)); }
        public static bool Fullscreen { get => Get("Settings_Fullscreen", 1) == 1; set => Set("Settings_Fullscreen", value ? 1 : 0); }
        public static readonly string[] SensitivityNames = { "Slow", "Normal", "Fast", "Very fast" };
        public static int SensitivityIndex { get => Mathf.Clamp(Get("Settings_Sensitivity", 1), 0, 3); set => Set("Settings_Sensitivity", Mathf.Clamp(value, 0, 3)); }
        public static float LookSensitivity => SensitivityIndex == 0 ? 0.6f : SensitivityIndex == 1 ? 1f : SensitivityIndex == 2 ? 1.4f : 1.9f;
        public static bool InvertY { get => Get("Settings_InvertY", 0) == 1; set => Set("Settings_InvertY", value ? 1 : 0); }

        // ---- weather quality ----
        public static readonly string[] WeatherNames = { "Off", "Low", "High" };
        /// <summary>Demo Mode: allows the F9 demo menu in a release build (it is always available in development builds).</summary>
        public static bool DemoMode { get => Get("Settings_DemoMode", 0) == 1; set => Set("Settings_DemoMode", value ? 1 : 0); }
        /// <summary>How often the game speaks event lines: 0 Low, 1 Normal (default), 2 High. Story lines always play.</summary>
        public static readonly string[] VoiceFrequencyNames = { "Low", "Normal", "High" };
        public static int VoiceFrequency { get => Mathf.Clamp(Get("Settings_VoiceFreq", 1), 0, 2); set => Set("Settings_VoiceFreq", Mathf.Clamp(value, 0, 2)); }
        public static bool Subtitles { get => PlayerPrefs.GetInt("Settings_Subtitles", 1) == 1; set { PlayerPrefs.SetInt("Settings_Subtitles", value ? 1 : 0); PlayerPrefs.Save(); } }
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

    /// <summary>Virtual input written by the on-screen controls (and bots) and read by gameplay next to keyboard and gamepad.</summary>
    public static class VirtualInput
    {
        public static Vector2 Move, Look;
        public static bool Sprint, Block, CrouchHeld, TouchVisible;
        private static readonly System.Collections.Generic.HashSet<string> pending = new System.Collections.Generic.HashSet<string>(), active = new System.Collections.Generic.HashSet<string>();
        /// <summary>A button press: every reader sees it as "pressed" for the whole of the next frame.</summary>
        public static void Press(string action) { pending.Add(action); }
        public static bool Down(string action) => active.Contains(action);
        /// <summary>Called first thing every frame.</summary>
        public static void Tick() { active.Clear(); foreach (var a in pending) active.Add(a); pending.Clear(); }
        public static void Clear() { Move = Look = Vector2.zero; Sprint = Block = CrouchHeld = false; pending.Clear(); active.Clear(); }
    }

    [DefaultExecutionOrder(-10000)]
    public sealed class VirtualInputTicker : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap() { var go = new GameObject("VirtualInputTicker"); DontDestroyOnLoad(go); go.AddComponent<VirtualInputTicker>(); }
        private void Update() { VirtualInput.Tick(); }
    }
}
