using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using LegacyOfTemples.Core;

namespace LegacyOfTemples.World.Editor
{
    /// <summary>
    /// Builds Resources/VoiceDB/VoiceDatabase.asset from Docs/VOICE_LINES.csv (the single source of truth).
    /// Clip per line: your own recording in Assets/_Project/Audio/Voice/Recorded/&lt;id&gt;.* first (or its cleaned copy made by
    /// Tools/tts/generate_voices.py), otherwise the generated clip in the CSV's clip column. Menu: Tools > Voice > Build database.
    /// </summary>
    public static class VoiceDatabaseBuilder
    {
        const string AssetPath = "Assets/_Project/Resources/VoiceDB/VoiceDatabase.asset";
        const string RecordedDir = "Assets/_Project/Audio/Voice/Recorded/";
        static readonly HashSet<string> Speakers = new HashSet<string> { "aryan", "pujari", "meera", "madhav", "bhaskar", "kishore", "courier", "robber", "villager_m", "villager_f", "narrator" };

        [MenuItem("Tools/Voice/Build database from CSV")]
        public static void Build()
        {
            string csv = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "Docs", "VOICE_LINES.csv"));
            if (!File.Exists(csv)) { Debug.LogError("[VoiceDB] missing " + csv); return; }
            var rows = Parse(File.ReadAllText(csv));
            var head = rows[0]; var idx = new Dictionary<string, int>(); for (int i = 0; i < head.Count; i++) idx[head[i]] = i;
            string manifest = File.Exists("Assets/_Project/Audio/Voice/manifest.json") ? File.ReadAllText("Assets/_Project/Audio/Voice/manifest.json") : "";
            var db = AssetDatabase.LoadAssetAtPath<VoiceDatabase>(AssetPath);
            if (db == null) { Directory.CreateDirectory(Path.GetDirectoryName(AssetPath)); db = ScriptableObject.CreateInstance<VoiceDatabase>(); AssetDatabase.CreateAsset(db, AssetPath); }
            db.lines.Clear(); var ids = new HashSet<string>(); int missing = 0, recorded = 0; var problems = new StringBuilder();
            for (int r = 1; r < rows.Count; r++)
            {
                var c = rows[r]; if (c.Count < head.Count) continue;
                string F(string k) => c[idx[k]];
                var l = new VoiceLine
                {
                    id = F("id"), speaker = F("speaker"), text = F("text"), trigger = F("trigger"), conditions = F("conditions"),
                    priority = int.Parse(F("priority")), cooldown = float.Parse(F("cooldown"), System.Globalization.CultureInfo.InvariantCulture),
                    chance = float.Parse(F("chance"), System.Globalization.CultureInfo.InvariantCulture), once = F("once") == "1", subtitle = F("subtitle") != "0", fx = F("fx")
                };
                if (!ids.Add(l.id)) problems.AppendLine("duplicate id " + l.id);
                if (!Speakers.Contains(l.speaker)) problems.AppendLine($"{l.id}: unknown speaker {l.speaker}");
                if (l.priority < 1 || l.priority > 5) problems.AppendLine($"{l.id}: priority {l.priority} outside 1-5");
                // your recording wins; if the voice script already cleaned it, the cleaned copy in the speaker folder is used
                string rec = null; foreach (var e in new[] { ".wav", ".ogg", ".mp3", ".aiff", ".aif" }) if (File.Exists(RecordedDir + l.id + e)) { rec = RecordedDir + l.id + e; break; }
                bool cleaned = rec != null && manifest.Contains("\"" + l.id + "\"") && manifest.IndexOf("\"source\": \"recording\"", manifest.IndexOf("\"" + l.id + "\"")) > 0;
                string path = rec != null && !cleaned ? rec : F("clip");
                l.clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                if (rec != null) recorded++;
                if (l.clip == null) { missing++; problems.AppendLine($"{l.id}: no clip at {path}"); }
                db.lines.Add(l);
            }
            db.builtFrom = "Docs/VOICE_LINES.csv"; db.builtAt = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm");
            EditorUtility.SetDirty(db); AssetDatabase.SaveAssets();
            var triggers = new HashSet<string>(); foreach (var l in db.lines) triggers.Add(l.trigger);
            Debug.Log($"[VoiceDB] built {db.lines.Count} lines, {triggers.Count} triggers, {recorded} from your recordings, {missing} missing clips");
            if (problems.Length > 0) Debug.LogError("[VoiceDB] problems:\n" + problems);
        }

        /// <summary>RFC-4180 style CSV: quoted fields may hold commas, quotes ("") and line breaks.</summary>
        static List<List<string>> Parse(string text)
        {
            var rows = new List<List<string>>(); var row = new List<string>(); var f = new StringBuilder(); bool q = false;
            for (int i = 0; i < text.Length; i++)
            {
                char ch = text[i];
                if (q) { if (ch == '"') { if (i + 1 < text.Length && text[i + 1] == '"') { f.Append('"'); i++; } else q = false; } else f.Append(ch); continue; }
                if (ch == '"') q = true;
                else if (ch == ',') { row.Add(f.ToString()); f.Clear(); }
                else if (ch == '\n' || ch == '\r') { if (ch == '\r' && i + 1 < text.Length && text[i + 1] == '\n') i++; row.Add(f.ToString()); f.Clear(); if (row.Count > 1 || row[0].Length > 0) rows.Add(row); row = new List<string>(); }
                else f.Append(ch);
            }
            if (f.Length > 0 || row.Count > 0) { row.Add(f.ToString()); rows.Add(row); }
            return rows;
        }

        /// <summary>A small scene with a button for every voice trigger (not part of the game build).</summary>
        [MenuItem("Tools/Voice/Make test scene")]
        public static void MakeTestScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            var go = new GameObject("VoiceTest"); go.AddComponent<LegacyOfTemples.UI.VoiceTestPanel>();
            EditorSceneManager.SaveScene(scene, "Assets/_Project/Scenes/Dev/VoiceTestScene.unity");
            Debug.Log("[VoiceDB] test scene saved: Assets/_Project/Scenes/Dev/VoiceTestScene.unity");
        }
    }

    /// <summary>Import settings for voice clips: mono, Vorbis; short lines compressed in memory, long lines streamed.</summary>
    public sealed class VoiceImportSettings : AssetPostprocessor
    {
        void OnPreprocessAudio()
        {
            if (!assetPath.Replace('\\', '/').Contains("/_Project/Audio/Voice/")) return;
            var imp = (AudioImporter)assetImporter; imp.forceToMono = true; imp.loadInBackground = false;
            long bytes = new FileInfo(assetPath).Length; float seconds = bytes / (44100f * 2f);   // mono 16-bit 44.1 kHz WAV
            var s = imp.defaultSampleSettings;
            s.loadType = seconds > 6f ? AudioClipLoadType.Streaming : AudioClipLoadType.CompressedInMemory;
            s.compressionFormat = AudioCompressionFormat.Vorbis; s.quality = 0.7f; s.preloadAudioData = s.loadType != AudioClipLoadType.Streaming;
            s.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
            imp.defaultSampleSettings = s;
        }
    }
}
