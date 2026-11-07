using System;
using System.Collections.Generic;
using UnityEngine;

namespace LegacyOfTemples.Gameplay
{
    [Serializable] public class StorySpeaker { public string id, name, role, color, npc; }
    [Serializable] public class StoryLine { public string s, t; }
    [Serializable] public class StoryChoice { public string text; public StoryLine[] lines; }
    [Serializable] public class StoryConversation { public string id, scene, npc; public StoryLine[] lines; public StoryChoice[] choices; }
    [Serializable] public class StoryCompletion { public string type, id; public string[] ids; }
    [Serializable] public class StoryObjective
    {
        public string id, scene, title, instruction, keys, target, area;
        public StoryCompletion complete;
        public string[] hints;
    }
    [Serializable] public class StoryEnding { public string text, title; }
    [Serializable] public class StoryFile
    {
        public StorySpeaker[] speakers;
        public StoryConversation[] conversations;
        public StoryObjective[] objectives;
        public StoryEnding ending;
    }

    /// <summary>Loads Resources/Story/story.json once (data-driven script, see Docs/STORY_SCRIPT.md).</summary>
    public static class StoryData
    {
        private static StoryFile file;
        private static Dictionary<string, StorySpeaker> speakers;
        private static Dictionary<string, StoryConversation> conversations;

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
                    if (file.speakers != null) foreach (var s in file.speakers) speakers[s.id] = s;
                    if (file.conversations != null) foreach (var c in file.conversations) conversations[c.id] = c;
                    if (ta == null) Debug.LogError("[StoryData] Resources/Story/story.json missing");
                }
                return file;
            }
        }

        public static StorySpeaker Speaker(string id) { _ = File; return id != null && speakers.TryGetValue(id, out var s) ? s : null; }
        public static StoryConversation Conversation(string id) { _ = File; return id != null && conversations.TryGetValue(id, out var c) ? c : null; }
        public static StoryObjective[] Objectives => File.objectives ?? new StoryObjective[0];

        /// <summary>Format a line for the dialogue box: "Speaker Name: text" (UIManager splits it per line).</summary>
        public static string Format(StoryLine l)
        {
            var sp = Speaker(l.s);
            return (sp != null ? sp.name : l.s) + ": " + l.t;
        }
    }
}
