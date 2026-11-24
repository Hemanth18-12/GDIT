using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using LegacyOfTemples.Core;
using LegacyOfTemples.Gameplay;

namespace LegacyOfTemples.UI
{
    /// <summary>
    /// Plays a voice clip for every dialogue line and story bark. The clip comes from the voice database first
    /// (Docs/VOICE_LINES.csv, same id as the story line), then the older Resources/Voice files, then a labelled missing-recording slot (subtitles remain available).
    /// Playback goes through the VoiceManager queue, so story lines never overlap other voices. Volume: "Settings_VoiceVol".
    /// </summary>
    public sealed class VoicePlayer : MonoBehaviour
    {
        public static VoicePlayer Instance { get; private set; }
        public string LastPlayed { get; private set; } = "";
        private AudioSource source;
        public AudioSource Source => VoiceManager.Instance != null ? VoiceManager.Instance.Source2D : source;
        private readonly HashSet<string> missing = new HashSet<string>();

        public static float Volume
        {
            get => PlayerPrefs.GetFloat("Settings_VoiceVol", 1f);
            set { PlayerPrefs.SetFloat("Settings_VoiceVol", Mathf.Clamp01(value)); PlayerPrefs.Save(); }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (Instance != null) return;
            var go = new GameObject("VoicePlayer");
            DontDestroyOnLoad(go);
            Instance = go.AddComponent<VoicePlayer>();
        }

        private void Awake()
        {
            source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false; source.spatialBlend = 0f; source.priority = 0;
            UIManager.OnSubtitleLine += OnLine;
            StoryDialogueSystem.OnBark += OnBark;
            EventBus.OnDialogueEnd += _ => VoiceManager.StopStory();
        }

        private void OnDestroy() { UIManager.OnSubtitleLine -= OnLine; StoryDialogueSystem.OnBark -= OnBark; }

        private static string SpeakerId(string name)
        {
            foreach (var s in StoryData.File.speakers ?? new StorySpeaker[0]) if (s.name == name) return s.id;
            return null;
        }

        public static string Hash(string text)
        {
            using (var md5 = MD5.Create())
            {
                var b = md5.ComputeHash(Encoding.UTF8.GetBytes(text));
                var sb = new StringBuilder();
                for (int i = 0; i < 6; i++) sb.Append(b[i].ToString("x2"));
                return sb.ToString();
            }
        }

        /// <summary>Last tier spoken (calm / firm / shout): the audio mixer follows it.</summary>
        public static string LastTier { get; private set; } = "calm";
        public static event Action<string, string> OnVoice; // (speaker id, tier)

        private void OnLine(string speaker, string text)
        {
            VoiceManager.StopStory();
            if (text.StartsWith("(")) return; // a prompt, not speech
            var line = StoryData.LineByText(text);
            Play(line != null ? line.s : SpeakerId(speaker) ?? speaker, line, text, true);
        }

        private void OnBark(StoryLine line) { Play(line.s, line, line.t, false); }

        /// <summary>Recorded file "<id>.wav" wins, then the "<id>_tts.wav" placeholder, then the old text hash, then a silent missing-recording slot.</summary>
        private void Play(string id, StoryLine line, string text, bool dialogue)
        {
            AudioClip clip = null;
            if (line == null)
            {
                var recorded = VoiceLibrary.FindText(text, id);
                if (recorded != null) line = new StoryLine { id = recorded.id, s = id, t = text };
            }
            if (line != null && !string.IsNullOrEmpty(line.id))
            {
                clip = VoiceLibrary.Clip(line.id);   // the current voice set (Docs/VOICE_LINES.csv)
                if (clip == null) clip = Resources.Load<AudioClip>($"Voice/{id}/{line.id}");
                if (clip == null) clip = Resources.Load<AudioClip>($"Voice/{id}/{line.id}_tts");
            }
            if (clip == null) clip = Resources.Load<AudioClip>($"Voice/{id}/{Hash(text)}");
            if (clip == null)
            {
                string slot = line != null && !string.IsNullOrEmpty(line.id) ? line.id : Hash(text);
                LastPlayed = "MISSING RECORDING: " + id + "/" + slot;
                if (missing.Add(LastPlayed)) Debug.Log("[Voice] " + LastPlayed + ". Subtitles only: " + text);
                source.Stop();
                return;
            }
            LastTier = line != null && !string.IsNullOrEmpty(line.tier) ? line.tier : "calm";
            OnVoice?.Invoke(id, LastTier);
            float tierVol = LastTier == "shout" ? 1f : LastTier == "firm" ? 0.9f : 0.8f;
            if (VoiceManager.Instance != null) VoiceManager.PlayStory(line != null ? line.id : "", id, text, clip, LastTier, dialogue, tierVol);
            else { source.clip = clip; source.volume = Volume * tierVol; source.Play(); }
            LastPlayed = id + ":" + clip.name;
        }

    }
}
