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
            EventBus.OnDialogueEnd += _ => source.Stop();
        }

        private void OnDestroy() { UIManager.OnSubtitleLine -= OnLine; }

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

        private void OnLine(string speaker, string text)
        {
            source.Stop();
            string id = SpeakerId(speaker);
            AudioClip clip = id != null ? Resources.Load<AudioClip>($"Voice/{id}/{Hash(text)}") : null;
            if (clip == null) clip = Blip(id ?? speaker, text.Length);
            source.clip = clip;
            source.volume = Volume;
            source.Play();
            LastPlayed = (id ?? speaker) + ":" + clip.name;
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
