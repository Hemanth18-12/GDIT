using UnityEngine;

namespace LegacyOfTemples.Gameplay.Puzzles
{
    /// <summary>A bronze mirror on a pivot. Light beams bounce off its forward face. It is a disc with fine stops.</summary>
    public class LightMirror : RotatingDisc
    {
        void Reset() { steps = 16; verb = "Turn mirror"; }
        protected override string DefaultSound(int state) => "sfx_gear_click";
        public Vector3 Normal => (visual != null ? visual : transform).forward;
    }
}
