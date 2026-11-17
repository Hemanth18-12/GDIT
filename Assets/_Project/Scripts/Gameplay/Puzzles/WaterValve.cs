using UnityEngine;

namespace LegacyOfTemples.Gameplay.Puzzles
{
    /// <summary>A sluice wheel or drain wheel. A disc whose stops mean "how far open" (0 = shut).</summary>
    public class WaterValve : RotatingDisc
    {
        void Reset() { steps = 2; verb = "Turn wheel"; }
        protected override string DefaultSound(int state) => "sfx_gear_click";
    }
}
