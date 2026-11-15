using System.Collections.Generic;
using UnityEngine;

namespace LegacyOfTemples.Gameplay
{
    /// <summary>
    /// Score and grades. Points for puzzles solved without hints, staying unseen, speed, taking no damage,
    /// treasures recovered, combos and hidden collectibles. A grade (S, A, B, C) per zone and overall, and a local high-score table.
    /// </summary>
    public static class ScoreSystem
    {
        [System.Serializable]
        public class ZoneScore
        {
            public string zone; public bool puzzleSolved; public int hintsUsed; public float seconds; public float damage;
            public int spotted; public int items; public int maxCombo; public int collectibles; public bool finished;
        }
        [System.Serializable] public class HighScore { public string date; public int total; public string grade; public int items; public float seconds; }
        [System.Serializable] class Table { public List<HighScore> rows = new List<HighScore>(); public List<ZoneScore> zones = new List<ZoneScore>(); }

        public const int PuzzlePoints = 1000, HintCost = 250, StealthPoints = 500, SpottedCost = 150, TimePoints = 500, NoDamagePoints = 500, ItemPoints = 1000, ComboPoints = 50, ComboCap = 500, CollectiblePoints = 100;
        public const float ParSeconds = 420f;
        const string Key = "lot_v3_scores";
        static readonly Dictionary<string, ZoneScore> zones = new Dictionary<string, ZoneScore>();
        static List<HighScore> table; static bool loaded;

        static void Load()
        {
            if (loaded) return; loaded = true;
            var t = JsonUtility.FromJson<Table>(PlayerPrefs.GetString(Key, "{}")) ?? new Table();
            table = t.rows ?? new List<HighScore>();
            if (t.zones != null) foreach (var z in t.zones) zones[z.zone] = z;
        }
        static void Save()
        {
            var t = new Table { rows = table }; foreach (var z in zones.Values) t.zones.Add(z);
            PlayerPrefs.SetString(Key, JsonUtility.ToJson(t)); PlayerPrefs.Save();
        }

        public static ZoneScore Zone(string zone)
        {
            Load();
            if (!zones.TryGetValue(zone, out var z)) { z = new ZoneScore { zone = zone }; zones[zone] = z; }
            return z;
        }
        public static IEnumerable<ZoneScore> Zones { get { Load(); return zones.Values; } }

        public static void AddHint(string zone) { Zone(zone).hintsUsed++; Save(); }
        public static void AddDamage(string zone, float amount) { Zone(zone).damage += amount; }
        public static void Spotted(string zone) { Zone(zone).spotted++; }
        public static void ItemRecovered(string zone) { Zone(zone).items++; Save(); }
        public static void Combo(string zone, int hits) { var z = Zone(zone); if (hits > z.maxCombo) z.maxCombo = hits; }
        public static void Collectible(string zone) { Zone(zone).collectibles++; Save(); }
        public static void AddTime(string zone, float seconds) { Zone(zone).seconds += seconds; }
        public static void PuzzleSolved(string zone, int hints, float seconds) { var z = Zone(zone); z.puzzleSolved = true; if (hints > z.hintsUsed) z.hintsUsed = hints; Save(); }
        public static void FinishZone(string zone) { Zone(zone).finished = true; Save(); }

        // ---------- pure rules (tested) ----------
        public static int Points(ZoneScore z)
        {
            int p = 0;
            if (z.puzzleSolved) p += Mathf.Max(PuzzlePoints - HintCost * z.hintsUsed, HintCost);
            p += Mathf.Max(StealthPoints - SpottedCost * z.spotted, 0);
            p += Mathf.RoundToInt(TimePoints * Mathf.Clamp01(1f - Mathf.Max(0f, z.seconds - ParSeconds) / ParSeconds));
            p += Mathf.Max(NoDamagePoints - Mathf.RoundToInt(z.damage * 5f), 0);
            p += ItemPoints * z.items + Mathf.Min(ComboPoints * z.maxCombo, ComboCap) + CollectiblePoints * z.collectibles;
            return p;
        }
        /// <summary>Best possible points for a zone with one treasure and the given number of collectibles.</summary>
        public static int MaxPoints(int collectibles = 0) => PuzzlePoints + StealthPoints + TimePoints + NoDamagePoints + ItemPoints + ComboCap + CollectiblePoints * collectibles;
        public static string Grade(float ratio) => ratio >= 0.9f ? "S" : ratio >= 0.75f ? "A" : ratio >= 0.55f ? "B" : "C";
        public static string Grade(ZoneScore z, int collectibles = 0) => Grade(Points(z) / (float)MaxPoints(collectibles));

        public static int Total { get { int t = 0; foreach (var z in Zones) t += Points(z); return t; } }
        public static string OverallGrade(int zoneCount = 6) { Load(); return Grade(Total / (float)(MaxPoints() * Mathf.Max(1, zoneCount))); }

        /// <summary>Store this run in the high-score table (best ten kept) and return its rank (1 = best).</summary>
        public static int SubmitRun(int zoneCount = 6)
        {
            Load();
            int items = 0; float secs = 0f; foreach (var z in zones.Values) { items += z.items; secs += z.seconds; }
            var row = new HighScore { date = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm"), total = Total, grade = OverallGrade(zoneCount), items = items, seconds = secs };
            table.Add(row); table.Sort((a, b) => b.total.CompareTo(a.total));
            int rank = table.IndexOf(row) + 1;
            if (table.Count > 10) table.RemoveRange(10, table.Count - 10);
            Save(); return rank;
        }
        public static IReadOnlyList<HighScore> HighScores { get { Load(); return table; } }

        /// <summary>New Game: forget the current run's zone scores (the high-score table stays).</summary>
        public static void ResetRun() { Load(); zones.Clear(); Save(); }
        /// <summary>Forget everything, including high scores (tests and "reset progress").</summary>
        public static void ResetAll() { zones.Clear(); table = new List<HighScore>(); loaded = true; PlayerPrefs.DeleteKey(Key); }
    }
}
