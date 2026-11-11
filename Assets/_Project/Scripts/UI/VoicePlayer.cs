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
    /// Plays a voice clip for every dialogue line while it types out. Clips are synthesized placeholders
    /// (macOS `say`) in Resources/Voice/&lt;speakerId&gt;/&lt;md5(text)[0..12]&gt;.wav. Lines without a clip get soft
    /// per-character "talk blips". Volume: PlayerPrefs "Settings_VoiceVol".
    /// </summary>
    public sealed class VoicePlayer : MonoBehaviour
    {
        public static VoicePlayer Instance { get; private set; }
        public string LastPlayed { get; private set; } = "";
        private AudioSource source;
        public AudioSource Source => source;
        private readonly Dictionary<string, AudioClip> blips = new Dictionary<string, AudioClip>();

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
            EventBus.OnDialogueEnd += _ => source.Stop();
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
            source.Stop();
            if (text.StartsWith("(")) return; // a prompt, not speech
            var line = StoryData.LineByText(text);
            Play(line != null ? line.s : SpeakerId(speaker) ?? speaker, line, text);
        }

        private void OnBark(StoryLine line) { Play(line.s, line, line.t); }

        /// <summary>Recorded file "<id>.wav" wins, then the "<id>_tts.wav" placeholder, then the old text hash, then blips.</summary>
        private void Play(string id, StoryLine line, string text)
        {
            AudioClip clip = null;
            if (line != null && !string.IsNullOrEmpty(line.id))
            {
                clip = Resources.Load<AudioClip>($"Voice/{id}/{line.id}");
                if (clip == null) clip = Resources.Load<AudioClip>($"Voice/{id}/{line.id}_tts");
            }
            if (clip == null) clip = Resources.Load<AudioClip>($"Voice/{id}/{Hash(text)}");
            if (clip == null) clip = Blip(id, text.Length);
            LastTier = line != null && !string.IsNullOrEmpty(line.tier) ? line.tier : "calm";
            OnVoice?.Invoke(id, LastTier);
            source.clip = clip;
            source.volume = Volume * (LastTier == "shout" ? 1f : LastTier == "firm" ? 0.9f : 0.8f);
            source.Play();
            LastPlayed = id + ":" + clip.name;
        }

        /// <summary>Fallback talk blips with a different pitch per character.</summary>
        private AudioClip Blip(string who, int chars)
        {
            int count = Mathf.Clamp(chars / 6, 3, 14);
            string key = who + count;
            if (blips.TryGetValue(key, out var c)) return c;
            float pitch = who == "pujari" ? 150f : who == "meera" ? 330f : who == "aryan" ? 200f : who == "bhaskar" ? 120f : who == "kishore" ? 170f : 240f;
            const int rate = 22050;
            int per = rate / 9;
            var data = new float[per * count];
            var rng = new System.Random(key.GetHashCode());
            for (int k = 0; k < count; k++)
            {
                float f = pitch * (0.9f + 0.2f * (float)rng.NextDouble());
                for (int i = 0; i < per * 0.6f; i++)
                {
                    float env = Mathf.Sin(Mathf.PI * i / (per * 0.6f));
                    data[k * per + i] = 0.18f * env * Mathf.Sin(2f * Mathf.PI * f * i / rate);
                }
            }
            c = AudioClip.Create("blip_" + key, data.Length, 1, rate, false);
            c.SetData(data, 0);
            blips[key] = c;
            return c;
        }
    }
}
