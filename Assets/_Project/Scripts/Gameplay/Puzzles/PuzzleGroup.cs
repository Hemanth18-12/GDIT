using System;
using System.Collections.Generic;
using UnityEngine;
using LegacyOfTemples.Core;

namespace LegacyOfTemples.Gameplay.Puzzles
{
    /// <summary>
    /// One puzzle: a set of parts and the states they must be in. No marker, no answer text.
    /// Gives three hint tiers (asked with H), can be reset, remembers that it was solved, and reports to the score.
    /// </summary>
    public class PuzzleGroup : MonoBehaviour
    {
        [Serializable] public struct Condition { public PuzzleElement element; public int state; }

        public string puzzleId = "puzzle";
        public string zoneId = "zone";
        public List<Condition> conditions = new List<Condition>();
        [Tooltip("Hint 1: where to look. Hint 2: what the mechanism does. Hint 3: the step.")]
        public string[] hints = new string[3];
        [Tooltip("Parts stop responding once the puzzle is solved.")]
        public bool lockWhenSolved = true;
        public float hintRadius = 25f;

        public bool Solved { get; private set; }
        public int HintsUsed { get; private set; }
        public float SecondsSpent { get; private set; }
        public event Action<PuzzleGroup> OnSolved;
        public static readonly List<PuzzleGroup> All = new List<PuzzleGroup>();
        string SolvedKey => "solved." + puzzleId;
        string HintKey => "hints." + puzzleId;

        void OnEnable() { All.Add(this); }
        void OnDisable() { All.Remove(this); }

        void Start()
        {
            foreach (var c in conditions) if (c.element != null) { c.element.Restore(); c.element.Changed += OnPartChanged; }
            HintsUsed = PuzzleSave.Get(HintKey, 0);
            if (PuzzleSave.Get(SolvedKey, 0) == 1) { Solved = true; LockParts(true); } else Evaluate();
        }
        void OnDestroy() { foreach (var c in conditions) if (c.element != null) c.element.Changed -= OnPartChanged; }
        void Update() { if (!Solved) SecondsSpent += Time.deltaTime; }

        void OnPartChanged(PuzzleElement e) { Evaluate(); }

        public bool ConditionsMet()
        {
            if (conditions.Count == 0) return false;
            foreach (var c in conditions) if (c.element == null || c.element.State != c.state) return false;
            return true;
        }

        public void Evaluate()
        {
            if (Solved || !ConditionsMet()) return;
            Solved = true; PuzzleSave.Set(SolvedKey, 1);
            LockParts(true);
            ScoreSystem.PuzzleSolved(zoneId, HintsUsed, SecondsSpent);
            Debug.Log($"[Puzzle] {puzzleId} solved in {SecondsSpent:0.0}s with {HintsUsed} hint(s)");
            AudioManager.Instance?.PlaySFX("sfx_puzzle_success", 0.85f);
            OnSolved?.Invoke(this);
        }

        void LockParts(bool on) { if (lockWhenSolved) foreach (var c in conditions) if (c.element != null) c.element.locked = on; }

        /// <summary>Next hint tier, or the last one again. Each new tier is counted for the score.</summary>
        public string NextHint()
        {
            if (hints == null || hints.Length == 0) return "";
            int tier = Mathf.Clamp(HintsUsed, 0, hints.Length - 1);
            if (HintsUsed < hints.Length) { HintsUsed++; PuzzleSave.Set(HintKey, HintsUsed); ScoreSystem.AddHint(zoneId); DawnClock.Instance?.Penalty(1f, "puzzle hint"); }
            return hints[tier] ?? "";
        }

        /// <summary>Put every part back to its start. A solved puzzle stays solved unless force is set (New Game).</summary>
        public void ResetPuzzle(bool force = false)
        {
            if (Solved && !force) return;
            Solved = false; PuzzleSave.Set(SolvedKey, 0); LockParts(false);
            foreach (var c in conditions) if (c.element != null) c.element.ResetState();
            if (force) { HintsUsed = 0; PuzzleSave.Set(HintKey, 0); SecondsSpent = 0f; }
        }

        /// <summary>The unsolved puzzle nearest to a point (the one H gives hints for), or null.</summary>
        public static PuzzleGroup Nearest(Vector3 position)
        {
            PuzzleGroup best = null; float bd = float.MaxValue;
            foreach (var g in All)
            {
                if (g == null || g.Solved) continue;
                float d = Vector3.Distance(position, g.transform.position);
                if (d < g.hintRadius && d < bd) { bd = d; best = g; }
            }
            return best;
        }
    }
}
