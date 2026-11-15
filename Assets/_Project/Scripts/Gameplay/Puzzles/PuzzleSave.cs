using System.Collections.Generic;
using UnityEngine;

namespace LegacyOfTemples.Gameplay.Puzzles
{
    /// <summary>Small saved store for puzzle states (one integer per element id). Kept in PlayerPrefs as one JSON string.</summary>
    public static class PuzzleSave
    {
        const string Key = "lot_v3_puzzles";
        [System.Serializable] class Entry { public string id; public int value; }
        [System.Serializable] class Box { public List<Entry> items = new List<Entry>(); }
        static Dictionary<string, int> data;

        static void Ensure()
        {
            if (data != null) return;
            data = new Dictionary<string, int>();
            var box = JsonUtility.FromJson<Box>(PlayerPrefs.GetString(Key, "{}"));
            if (box != null && box.items != null) foreach (var e in box.items) data[e.id] = e.value;
        }
        static void Flush()
        {
            var box = new Box(); foreach (var kv in data) box.items.Add(new Entry { id = kv.Key, value = kv.Value });
            PlayerPrefs.SetString(Key, JsonUtility.ToJson(box)); PlayerPrefs.Save();
        }
        public static bool Has(string id) { Ensure(); return data.ContainsKey(id); }
        public static int Get(string id, int fallback = 0) { Ensure(); return data.TryGetValue(id, out int v) ? v : fallback; }
        public static void Set(string id, int value) { Ensure(); if (data.TryGetValue(id, out int old) && old == value) return; data[id] = value; Flush(); }
        /// <summary>Forget everything whose id starts with the prefix ("" = everything). Used by New Game and by a puzzle's reset.</summary>
        public static void Clear(string prefix = "")
        {
            Ensure();
            var gone = new List<string>(); foreach (var k in data.Keys) if (k.StartsWith(prefix)) gone.Add(k);
            foreach (var k in gone) data.Remove(k);
            Flush();
        }
    }
}
