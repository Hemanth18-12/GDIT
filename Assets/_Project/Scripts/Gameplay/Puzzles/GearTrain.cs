using UnityEngine;

namespace LegacyOfTemples.Gameplay.Puzzles
{
    /// <summary>Meshed gears transmit motion only when each sized gear is seated on its matching peg.</summary>
    public sealed class GearTrain : PuzzleElement
    {
        public PlacementSocket[] pegs;
        public RotatingDisc crank;
        public Transform[] wheels;
        public int[] teeth;
        public float degreesPerSecond = 35f;
        public bool Connected
        {
            get
            {
                if (pegs == null || pegs.Length == 0 || crank == null || crank.State == 0) return false;
                foreach (var peg in pegs) if (peg == null || peg.State != 1) return false;
                return true;
            }
        }
        void Update()
        {
            if (locked) return;
            SetState(Connected ? 1 : 0);
            if (!Connected || wheels == null) return;
            int input = teeth != null && teeth.Length > 0 ? Mathf.Max(1, teeth[0]) : 12;
            for (int i = 0; i < wheels.Length; i++)
                if (wheels[i] != null)
                {
                    int count = teeth != null && i < teeth.Length ? Mathf.Max(1, teeth[i]) : input;
                    wheels[i].Rotate(Vector3.forward, degreesPerSecond * Time.deltaTime * input / count * (i % 2 == 0 ? 1f : -1f), Space.Self);
                }
        }
        protected override string DefaultSound(int state) => state == 1 ? "sfx_gear_click" : null;
        protected override void Apply(int state, bool instant) { }
    }
}
