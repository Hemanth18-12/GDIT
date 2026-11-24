using System;
using UnityEngine;
using LegacyOfTemples.Core;

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
        [Tooltip("Sound when the part moves (a clip name in the audio library). Empty = the part's usual sound.")]
        public string sound;
        public float soundVolume = 0.8f;
        /// <summary>The part's usual sound for a new state (null = silent).</summary>
        protected virtual string DefaultSound(int state) => null;

        public int State { get; private set; }
        public event Action<PuzzleElement> Changed;
        public string SaveId => "el." + (string.IsNullOrEmpty(elementId) ? name : elementId);
        bool restored, started; string restoredId;

        protected virtual void Awake() { }
        /// <summary>Visuals are first shown here, after whoever created the part has set its fields (steps, handle, surface ...).</summary>
        protected virtual void Start() { Restore(); started = true; Apply(State, true); }

        /// <summary>Load the saved state (or the start state) once. Safe to call early from a group.</summary>
        public void Restore()
        {
            if (restored && restoredId == SaveId) return; restored = true; restoredId = SaveId;
            State = PuzzleSave.Get(SaveId, startState);
            if (started) Apply(State, true);
        }

        public void SetState(int value, bool save = true, bool instant = false)
        {
            if (!restored) Restore();
            if (value == State) return;
            State = value;
            if (save) PuzzleSave.Set(SaveId, State);
            if (started) Apply(State, instant);
            if (started && !instant) { string snd = string.IsNullOrEmpty(sound) ? DefaultSound(State) : sound; if (!string.IsNullOrEmpty(snd)) GameSfx.At(snd, transform.position, soundVolume); }
            Changed?.Invoke(this);
        }

        public virtual void ResetState() { SetState(startState, true, true); }

        /// <summary>Show the state in the world (rotate, slide, raise ...). instant = no animation.</summary>
        protected abstract void Apply(int state, bool instant);
    }
}
