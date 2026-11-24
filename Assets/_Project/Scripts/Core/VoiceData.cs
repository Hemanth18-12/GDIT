using System;
using System.Collections.Generic;
using UnityEngine;

namespace LegacyOfTemples.Core
{
    /// <summary>One spoken line. Built from Docs/VOICE_LINES.csv (the single source of truth) by the editor tool VoiceDatabaseBuilder.</summary>
    [Serializable]
    public sealed class VoiceLine
    {
        public string id, speaker, text, trigger, conditions;
        [Tooltip("1 low chatter, 2 reaction, 3 gameplay, 4 story bark, 5 critical (conversation, intro, ending)")]
        public int priority = 2;
        [Tooltip("Seconds before the same trigger may speak again")]
        public float cooldown;
        [Range(0f, 1f)] public float chance = 1f;
        public bool once, subtitle = true;
        public string fx;
        public AudioClip clip;
    }

    /// <summary>Finds lines and clips by id. Used by the voice manager, the story voice player and the intro.</summary>
    public static class VoiceLibrary
    {
        static VoiceDatabase db; static Dictionary<string, VoiceLine> byId;
        public static VoiceDatabase Db
        {
            get
            {
                if (db == null)
                {
                    db = Resources.Load<VoiceDatabase>("VoiceDB/VoiceDatabase"); byId = new Dictionary<string, VoiceLine>();
                    if (db != null) foreach (var l in db.lines) if (l != null && !string.IsNullOrEmpty(l.id)) byId[l.id] = l;
                }
                return db;
            }
        }
        public static VoiceLine Line(string id) { if (Db == null || string.IsNullOrEmpty(id)) return null; byId.TryGetValue(id, out var l); return l; }
        public static AudioClip Clip(string id) => Line(id)?.clip;
        public static VoiceLine FindText(string text, string speaker)
        {
            if (Db == null) return null;
            foreach (var line in Db.lines) if (line.text == text && line.speaker == speaker) return line;
            return null;
        }
    }

    /// <summary>Gameplay asks for a voice line here (any assembly can call it); the voice manager decides if and what to say.</summary>
    public static class VoiceEvents
    {
        /// <summary>(trigger, speaker transform or null, context such as "enemy=kishore")</summary>
        public static event Action<string, Transform, string> OnFire;
        public static void Fire(string trigger, Transform speaker = null, string context = null) => OnFire?.Invoke(trigger, speaker, context);
    }
}
