using System.Collections.Generic;
using UnityEngine;

namespace LegacyOfTemples.Gameplay.Puzzles
{
    /// <summary>Pure puzzle rules with no scene objects, so they can be tested quickly.</summary>
    public static class PuzzleLogic
    {
        /// <summary>Next position of a disc with a fixed number of stops.</summary>
        public static int NextStep(int current, int steps, int direction = 1) => steps <= 0 ? 0 : ((current + direction) % steps + steps) % steps;

        /// <summary>Water level in a basin: each open inflow adds its gain, each drain step removes one level.</summary>
        public static int WaterLevel(IList<int> inflowStates, IList<int> gains, int drainSteps, int maxLevel)
        {
            int level = 0;
            for (int i = 0; i < inflowStates.Count; i++) if (inflowStates[i] > 0) level += i < gains.Count ? gains[i] : 1;
            return Mathf.Clamp(level - drainSteps, 0, maxLevel);
        }

        /// <summary>True when the last presses equal the required order.</summary>
        public static bool SequenceMatches(IList<int> history, IList<int> required)
        {
            if (required.Count == 0 || history.Count < required.Count) return false;
            int o = history.Count - required.Count;
            for (int i = 0; i < required.Count; i++) if (history[o + i] != required[i]) return false;
            return true;
        }

        /// <summary>True while the presses so far could still become the required order (used for the soft reset on a wrong press).</summary>
        public static bool SequenceIsPrefix(IList<int> history, IList<int> required)
        {
            if (history.Count > required.Count) return false;
            for (int i = 0; i < history.Count; i++) if (history[i] != required[i]) return false;
            return true;
        }

        /// <summary>Direction of a light beam after bouncing off a mirror with the given facing.</summary>
        public static Vector3 Reflect(Vector3 direction, Vector3 mirrorNormal) => Vector3.Reflect(direction.normalized, mirrorNormal.normalized);

        /// <summary>Grid cell a pushed block moves to: one step along the main axis pointing away from the pusher.</summary>
        public static Vector3 PushDirection(Vector3 pusher, Vector3 block)
        {
            Vector3 d = block - pusher; d.y = 0f;
            if (d.sqrMagnitude < 1e-6f) return Vector3.forward;
            return Mathf.Abs(d.x) > Mathf.Abs(d.z) ? new Vector3(Mathf.Sign(d.x), 0f, 0f) : new Vector3(0f, 0f, Mathf.Sign(d.z));
        }
    }
}
