using System;
using UnityEngine;

namespace LegacyOfTemples.Core
{
    /// <summary>
    /// One owner for Space/Enter: while any card, dialogue or menu is open, gameplay input (jump, move, interact)
    /// is blocked so Space only advances the UI. Stays blocked for 3 frames after the UI closes so the
    /// same key press that closed a card cannot also make the hero jump.
    /// </summary>
    public static class InputGate
    {
        public static Func<bool> UiOpen;
        /// <summary>Frame until which Enter belongs to a mechanism (the Sun Wall centre wheel), not to "interact".</summary>
        public static int EnterReservedUntil = -1;
        private static int lastUiFrame = -100;

        public static bool GameplayBlocked
        {
            get
            {
                if (UiOpen != null && UiOpen()) lastUiFrame = Time.frameCount;
                return Time.frameCount - lastUiFrame <= 3;
            }
        }
    }
}
