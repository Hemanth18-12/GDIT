using System.Collections.Generic;
using UnityEngine;

namespace LegacyOfTemples.Core
{
    /// <summary>
    /// 3D sound effects at a point in the world (fight hits, footsteps of other characters, puzzle parts, doors).
    /// A small pool of audio sources routed through the game mixer's effects group, so volume settings apply.
    /// </summary>
    public static class GameSfx
    {
        static readonly List<AudioSource> pool = new List<AudioSource>(); static int next; static GameObject host;
        public static int Played { get; private set; }
        public static string Last { get; private set; } = "";
        /// <summary>Every clip name played so far, with how many times (for the test report).</summary>
        public static readonly Dictionary<string, int> Counts = new Dictionary<string, int>();

        static AudioSource Take()
        {
            if (host == null) { host = new GameObject("GameSfx"); Object.DontDestroyOnLoad(host); pool.Clear(); }
            if (pool.Count < 24)
            {
                var g = new GameObject("Sfx" + pool.Count); g.transform.SetParent(host.transform, false);
                var s = g.AddComponent<AudioSource>(); s.playOnAwake = false; s.spatialBlend = 1f; s.rolloffMode = AudioRolloffMode.Linear; s.minDistance = 2f; s.maxDistance = 32f; s.dopplerLevel = 0f;
                AudioManager.Route(s, "SFX"); pool.Add(s); return s;
            }
            next = (next + 1) % pool.Count; return pool[next];
        }

        /// <summary>Play a clip from the audio library at a world position. pitchJitter varies each play so repeats do not sound identical.</summary>
        public static void At(string clip, Vector3 pos, float volume = 1f, float pitchJitter = 0.06f, float range = 32f, float pitch = 1f)
        {
            var am = AudioManager.Instance; var c = am != null ? am.GetClip(clip) : null; if (c == null) { Debug.LogWarning("[GameSfx] missing clip " + clip); return; }
            var s = Take(); s.transform.position = pos; s.maxDistance = range; s.pitch = pitch * Random.Range(1f - pitchJitter, 1f + pitchJitter);
            s.PlayOneShot(c, Mathf.Clamp01(volume) * am.MasterVolume * am.SFXVolume);
            Played++; Last = clip; Counts[clip] = Counts.TryGetValue(clip, out int k) ? k + 1 : 1;
        }

        /// <summary>The surface under a point (stone, wet, wood, gravel, grass, mud) for footsteps, from the collider and material names.</summary>
        public static string Surface(Vector3 pos)
        {
            if (!Physics.Raycast(pos + Vector3.up * 0.5f, Vector3.down, out RaycastHit h, 2f, ~0, QueryTriggerInteraction.Ignore)) return "stone";
            var r = h.collider.GetComponent<Renderer>();
            string n = (h.collider.name + " " + (r != null && r.sharedMaterial != null ? r.sharedMaterial.name : "")).ToLowerInvariant();
            if (n.Contains("wood") || n.Contains("plank") || n.Contains("raft")) return "wood";
            if (n.Contains("gravel") || n.Contains("dirt_path") || n.Contains("stony")) return "gravel";
            if (n.Contains("grass")) return "grass";
            if (n.Contains("mud") || n.Contains("leaves") || n.Contains("laterite") || n.Contains("jungle")) return "mud";
            if (n.Contains("tank") || n.Contains("wet") || n.Contains("ramp")) return "wet";
            return "stone";
        }

        static readonly Dictionary<string, int> variants = new Dictionary<string, int> { { "stone", 3 }, { "wet", 3 }, { "wood", 2 }, { "gravel", 2 }, { "grass", 2 }, { "mud", 2 } };
        /// <summary>One footstep of a non-player character at a position.</summary>
        public static void Footstep(Vector3 pos, float volume = 0.45f)
        {
            string s = Surface(pos); At($"fs_{s}_{Random.Range(1, variants[s] + 1)}", pos, volume, 0.08f, 20f);
        }
    }
}
