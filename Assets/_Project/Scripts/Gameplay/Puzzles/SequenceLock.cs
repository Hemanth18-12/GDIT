using System.Collections.Generic;

namespace LegacyOfTemples.Gameplay.Puzzles
{
    /// <summary>A lock opened by striking things in the right order. State 1 = open. A wrong strike quietly starts the order again.</summary>
    public class SequenceLock : PuzzleElement
    {
        public List<int> required = new List<int>();
        readonly List<int> history = new List<int>();
        public int Progress => history.Count;

        public void Press(int index)
        {
            if (locked || State == 1) return;
            history.Add(index);
            if (PuzzleLogic.SequenceMatches(history, required)) { history.Clear(); SetState(1); return; }
            if (!PuzzleLogic.SequenceIsPrefix(history, required))
            {   // wrong strike: keep it only if it could be the first of a new try
                history.Clear();
                if (required.Count > 0 && index == required[0]) history.Add(index);
            }
        }
        public override void ResetState() { history.Clear(); base.ResetState(); }
        protected override void Apply(int state, bool instant) { }
    }
}
