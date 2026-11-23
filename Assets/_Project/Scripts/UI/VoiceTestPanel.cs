using System.Collections.Generic;
using UnityEngine;
using LegacyOfTemples.Core;

namespace LegacyOfTemples.UI
{
    /// <summary>
    /// VoiceTestScene: one button per voice trigger, so every line can be heard and the queue, priorities, cooldowns and
    /// frequency setting can be tried without playing the game. "Ignore conditions" lets zone/jeep/fight lines play here.
    /// </summary>
    public sealed class VoiceTestPanel : MonoBehaviour
    {
        readonly List<string> triggers = new List<string>(); Vector2 scroll; bool ignore = true;

        void Start()
        {
            var db = VoiceLibrary.Db; var seen = new HashSet<string>();
            if (db != null) foreach (var l in db.lines) if (l.trigger != "story" && seen.Add(l.trigger)) triggers.Add(l.trigger);
            triggers.Sort();
            if (FindAnyObjectByType<AudioListener>() == null) gameObject.AddComponent<AudioListener>();
        }

        void OnGUI()
        {
            VoiceManager.TestMode = ignore;
            GUILayout.BeginArea(new Rect(10, 10, 360, Screen.height - 20));
            GUILayout.Label($"<b>Voice test</b>  ({triggers.Count} triggers)", new GUIStyle(GUI.skin.label) { richText = true, fontSize = 16 });
            ignore = GUILayout.Toggle(ignore, "Ignore conditions and chance (test mode)");
            if (GUILayout.Button("Frequency: " + GameOptions.VoiceFrequencyNames[GameOptions.VoiceFrequency])) GameOptions.VoiceFrequency = (GameOptions.VoiceFrequency + 1) % 3;
            if (GUILayout.Button("Subtitles: " + (GameOptions.Subtitles ? "ON" : "OFF"))) GameOptions.Subtitles = !GameOptions.Subtitles;
            if (GUILayout.Button("Stop all")) VoiceManager.StopAll("test button");
            scroll = GUILayout.BeginScrollView(scroll);
            foreach (var t in triggers) if (GUILayout.Button(t)) VoiceManager.Fire(t, null, "enemy=robber;evidence=list;speaker=villager_f");
            GUILayout.EndScrollView();
            GUILayout.EndArea();
            var log = VoiceManager.Log; var sb = new System.Text.StringBuilder();
            for (int i = Mathf.Max(0, log.Count - 14); i < log.Count; i++) sb.AppendLine($"{log[i].trigger}: {log[i].result} {log[i].line}  ({log[i].reason})");
            GUI.Label(new Rect(390, 10, Screen.width - 400, 400), sb.ToString());
        }
    }
}
