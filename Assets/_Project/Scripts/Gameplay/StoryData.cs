using System;
using System.Collections.Generic;
using UnityEngine;

namespace LegacyOfTemples.Gameplay
{
    // v2 schema (see Docs/IMPLEMENTATION_PLAN.md, built by Tools/story/build_story.py)
    [Serializable] public class StorySpeaker { public string id, name, role, color, npc; }
    [Serializable] public class StoryLine { public string id, s, t, tier, when, trigger; }
    [Serializable] public class StoryChoice { public string id, text, effects, next, when; public StoryLine[] lines; }
    [Serializable] public class StoryConversation { public string id, scene, npc, prompt; public StoryLine[] lines; public StoryChoice[] choices; }
    [Serializable] public class StoryCompletion { public string type, id; public string[] ids; }
    [Serializable] public class StoryObjective
    {
        public string id, chapter, scene, title, instruction, keys, target, area, start, when;
        public StoryCompletion complete;
        public string[] hints;
    }
    [Serializable] public class StoryChapter { public string id, title, scene, image; public int n, budget; public float weather; }
    [Serializable] public class StoryClue { public string id, title, text, note; }
    [Serializable] public class StoryCodex { public string id, title, unlock, text; }
    [Serializable] public class StoryEnding { public string id, text, title; }
    [Serializable] public class StoryFile
    {
        public int version;
        public StorySpeaker[] speakers;
        public StoryChapter[] chapters;
        public StoryConversation[] conversations;
        public StoryObjective[] objectives;
        public StoryLine[] barks;
        public StoryClue[] clues;
        public StoryCodex[] codex;
        public string[] tips;
        public StoryEnding[] endings;
        public StoryEnding ending;
    }

    /// <summary>Loads Resources/Story/story.json once (data-driven script).</summary>
    public static class StoryData
    {
        private static StoryFile file;
        private static Dictionary<string, StorySpeaker> speakers;
        private static Dictionary<string, StoryConversation> conversations;
        private static Dictionary<string, StoryLine> lineById, lineByText, barkByTrigger;
        private static Dictionary<string, StoryChapter> chapters;
        private static Dictionary<string, StoryClue> clues;

        public static StoryFile File
        {
            get
            {
                if (file == null)
                {
                    var ta = Resources.Load<TextAsset>("Story/story");
                    file = ta != null ? JsonUtility.FromJson<StoryFile>(ta.text) : new StoryFile();
                    speakers = new Dictionary<string, StorySpeaker>();
                    conversations = new Dictionary<string, StoryConversation>();
                    lineById = new Dictionary<string, StoryLine>(); lineByText = new Dictionary<string, StoryLine>(); barkByTrigger = new Dictionary<string, StoryLine>();
                    chapters = new Dictionary<string, StoryChapter>(); clues = new Dictionary<string, StoryClue>();
                    foreach (var s in file.speakers ?? new StorySpeaker[0]) speakers[s.id] = s;
                    foreach (var c in file.chapters ?? new StoryChapter[0]) chapters[c.id] = c;
                    foreach (var c in file.clues ?? new StoryClue[0]) clues[c.id] = c;
                    foreach (var c in file.conversations ?? new StoryConversation[0])
                    {
                        conversations[c.id] = c;
                        foreach (var l in c.lines ?? new StoryLine[0]) Index(l);
                        foreach (var ch in c.choices ?? new StoryChoice[0]) foreach (var l in ch.lines ?? new StoryLine[0]) Index(l);
                    }
                    foreach (var b in file.barks ?? new StoryLine[0]) { Index(b); if (!string.IsNullOrEmpty(b.trigger)) barkByTrigger[b.trigger] = b; }
                    if (ta == null) Debug.LogError("[StoryData] Resources/Story/story.json missing");
                }
                return file;
            }
        }

        private static void Index(StoryLine l) { if (string.IsNullOrEmpty(l.id)) return; lineById[l.id] = l; lineByText[l.t] = l; }

        public static StorySpeaker Speaker(string id) { _ = File; return id != null && speakers.TryGetValue(id, out var s) ? s : null; }
        public static StoryConversation Conversation(string id) { _ = File; return id != null && conversations.TryGetValue(id, out var c) ? c : null; }
        public static StoryLine Line(string id) { _ = File; return id != null && lineById.TryGetValue(id, out var l) ? l : null; }
        public static StoryLine LineByText(string text) { _ = File; return text != null && lineByText.TryGetValue(text, out var l) ? l : null; }
        public static StoryLine Bark(string trigger) { _ = File; return trigger != null && barkByTrigger.TryGetValue(trigger, out var l) ? l : null; }
        public static StoryChapter Chapter(string id) { _ = File; return id != null && chapters.TryGetValue(id, out var c) ? c : null; }
        public static StoryClue Clue(string id) { _ = File; return id != null && clues.TryGetValue(id, out var c) ? c : null; }
        public static StoryObjective[] Objectives => File.objectives ?? new StoryObjective[0];
        public static StoryEnding Ending(string id) { foreach (var e in File.endings ?? new StoryEnding[0]) if (e.id == id) return e; return File.ending; }

        /// <summary>Format a line for the dialogue box: "Speaker Name: text" (UIManager splits it per line).</summary>
        public static string Format(StoryLine l)
        {
            var sp = Speaker(l.s);
            return (sp != null ? sp.name : l.s) + ": " + l.t;
        }
    }

    /// <summary>Story flags (trust_meera, has_message, ...) with tiny condition and effect languages.</summary>
    public static class StoryFlags
    {
        private static readonly Dictionary<string, int> values = new Dictionary<string, int>();
        public static Action<int> ClockPenalty; // set by DawnClock: story minutes lost
        public static event Action<string, int> OnChanged;

        public static int Get(string k) => values.TryGetValue(k, out int v) ? v : 0;
        public static void Set(string k, int v)
        {
            if (k == "trust_meera") v = Mathf.Clamp(v, -2, 3);
            values[k] = v; OnChanged?.Invoke(k, v);
        }
        public static void Clear() { values.Clear(); }

        public static string Serialize()
        {
            var parts = new List<string>();
            foreach (var kv in values) parts.Add(kv.Key + "=" + kv.Value);
            return string.Join(";", parts);
        }
        public static void Deserialize(string s)
        {
            values.Clear();
            foreach (var p in (s ?? "").Split(';')) { int i = p.IndexOf('='); if (i > 0 && int.TryParse(p.Substring(i + 1), out int v)) values[p.Substring(0, i)] = v; }
        }

        /// <summary>"a+1;b=1;clock-2"</summary>
        public static void Apply(string effects)
        {
            if (string.IsNullOrEmpty(effects)) return;
            foreach (var raw in effects.Split(';'))
            {
                string e = raw.Trim(); if (e.Length == 0) continue;
                int i = e.IndexOfAny(new[] { '+', '-', '=' }); if (i <= 0) continue;
                string k = e.Substring(0, i); if (!int.TryParse(e.Substring(i + 1), out int n)) continue;
                if (k == "clock") { if (e[i] == '-') ClockPenalty?.Invoke(n); continue; }
                Set(k, e[i] == '=' ? n : Get(k) + (e[i] == '+' ? n : -n));
            }
        }

        /// <summary>"a>=2", "a==0 &amp;&amp; b==1", "a" (non-zero), "!a". Empty = true.</summary>
        public static bool Eval(string cond)
        {
            if (string.IsNullOrWhiteSpace(cond)) return true;
            foreach (var part in cond.Split(new[] { "&&" }, StringSplitOptions.None)) if (!EvalOne(part.Trim())) return false;
            return true;
        }
        private static bool EvalOne(string c)
        {
            foreach (var op in new[] { ">=", "<=", "==", "!=", ">", "<" })
            {
                int i = c.IndexOf(op, StringComparison.Ordinal);
                if (i <= 0) continue;
                int a = Get(c.Substring(0, i).Trim()); int.TryParse(c.Substring(i + op.Length).Trim(), out int b);
                switch (op) { case ">=": return a >= b; case "<=": return a <= b; case "==": return a == b; case "!=": return a != b; case ">": return a > b; default: return a < b; }
            }
            return c.StartsWith("!") ? Get(c.Substring(1).Trim()) == 0 : Get(c) != 0;
        }
    }
}
