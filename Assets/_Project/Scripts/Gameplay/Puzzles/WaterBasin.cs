using System.Collections.Generic;
using UnityEngine;

namespace LegacyOfTemples.Gameplay.Puzzles
{
    /// <summary>
    /// A basin (the stepwell). Open inflow valves raise the water, the drain lowers it. State = water level in steps.
    /// The water surface and anything floating on it move smoothly to the new level.
    /// </summary>
    public class WaterBasin : PuzzleElement
    {
        public List<WaterValve> inflows = new List<WaterValve>();
        public List<int> inflowGains = new List<int>();
        public WaterValve drain;
        public int maxLevel = 4;
        public float levelHeight = 1.5f;
        public float riseSpeed = 0.8f;
        public Transform surface;
        public List<Transform> floating = new List<Transform>();
        float baseY, y; bool baseSet; readonly List<float> floatOffsets = new List<float>();

        protected override void Start()
        {
            base.Start();
            foreach (var v in inflows) if (v != null) { v.Restore(); v.Changed += _ => Recompute(); }
            if (drain != null) { drain.Restore(); drain.Changed += _ => Recompute(); }
            Recompute();
        }

        public void Recompute()
        {
            var states = new List<int>(); foreach (var v in inflows) states.Add(v != null ? v.State : 0);
            SetState(PuzzleLogic.WaterLevel(states, inflowGains, drain != null ? drain.State : 0, maxLevel), false);
        }

        protected override string DefaultSound(int state) => "sfx_drip";
        public float SurfaceY => y;
        protected override void Apply(int state, bool instant)
        {
            var s = surface != null ? surface : transform;
            if (!baseSet) { baseY = s.position.y; y = baseY; baseSet = true; foreach (var f in floating) floatOffsets.Add(f != null ? f.position.y - baseY : 0f); }
            if (instant) { y = baseY + state * levelHeight; Place(); }
        }
        void Update()
        {
            if (!baseSet) return;
            float target = baseY + State * levelHeight;
            if (Mathf.Approximately(y, target)) return;
            y = Mathf.MoveTowards(y, target, riseSpeed * Time.deltaTime); Place();
        }
        void Place()
        {
            var s = surface != null ? surface : transform;
            var p = s.position; p.y = y; s.position = p;
            for (int i = 0; i < floating.Count; i++) if (floating[i] != null) { var fp = floating[i].position; fp.y = y + floatOffsets[i]; floating[i].position = fp; }
        }
    }
}
