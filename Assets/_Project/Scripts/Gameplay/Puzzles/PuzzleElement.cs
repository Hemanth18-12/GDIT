using System;
using UnityEngine;

namespace LegacyOfTemples.Gameplay.Puzzles
{
    /// <summary>
    /// Base of every physical puzzle part (disc, lever, plate, valve, socket ...). A part has one integer state.
    /// It never shows a marker or an answer: it only moves, and a <see cref="PuzzleGroup"/> decides when the puzzle is solved.
    /// </summary>
    public abstract class PuzzleElement : MonoBehaviour
    {
        [Tooltip("Unique id, used for saving. Leave empty to use the object's name.")]
        public string elementId;
        [Tooltip("State the part starts in and returns to on reset.")]
        public int startState;
        [Tooltip("Locked parts ignore the player (used once a puzzle is solved).")]
        public bool locked;

        public int State { get; private set; }
        public event Action<PuzzleElement> Changed;
        public string SaveId => "el." + (string.IsNullOrEmpty(elementId) ? name : elementId);
        bool restored;

        protected virtual void Awake() { Restore(); }

        /// <summary>Load the saved state (or the start state) once. Safe to call early from a group.</summary>
        public void Restore()
        {
            if (restored) return; restored = true;
            State = PuzzleSave.Get(SaveId, startState);
            Apply(State, true);
        }

        public void SetState(int value, bool save = true, bool instant = false)
        {
            if (!restored) Restore();
            if (value == State) return;
            State = value;
            if (save) PuzzleSave.Set(SaveId, State);
            Apply(State, instant);
            Changed?.Invoke(this);
        }

        public virtual void ResetState() { SetState(startState, true, true); }

        /// <summary>Show the state in the world (rotate, slide, raise ...). instant = no animation.</summary>
        protected abstract void Apply(int state, bool instant);
    }
}
