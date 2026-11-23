using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using LegacyOfTemples.Core;
using LegacyOfTemples.Gameplay.Puzzles;
using LegacyOfTemples.World;

namespace LegacyOfTemples.Gameplay
{
    /// <summary>
    /// V3 zone 4 (Stepwell and Monsoon Shrine), built as a walled stone tank: night, rain, wet stone.
    /// Two sluice wheels on the rim fill the tank and lift a raft to the rim. Too much water hides the carved panel in the east wall,
    /// so from the raft the player works the drain until the water rests on the middle mark; there the raft meets the panel's ledge.
    /// Behind the panel: the Monsoon Vessel and a dropped temple key ring.
    /// </summary>
    public sealed class StepwellPuzzle : MonoBehaviour
    {
        public const string ZoneId = "stepwell";
        public const float Rim = 5.2f;
        public static StepwellPuzzle Instance { get; private set; }
        public bool Built { get; private set; }
        public PuzzleGroup Group { get; private set; }
        public WaterValve SluiceA { get; private set; }
        public WaterValve SluiceB { get; private set; }
        public WaterValve Drain { get; private set; }
        public WaterBasin Basin { get; private set; }
        public PuzzleLever Panel { get; private set; }
        public TreasurePickup Vessel { get; private set; }
        public Transform Raft { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot() { SceneManager.sceneLoaded += (s, m) => { if (s.name == Constants.Courtyard) Spawn(); }; if (SceneManager.GetActiveScene().name == Constants.Courtyard) Spawn(); }
        static void Spawn() { if (Instance == null) new GameObject("V3_Zone4_Stepwell").AddComponent<StepwellPuzzle>(); }
        void Awake() { Instance = this; }
        void OnDestroy() { if (Instance == this) Instance = null; }

        IEnumerator Start()
        {
            float wait = 0f; while ((TempleSite.Instance == null || TempleSite.Footprint.size.sqrMagnitude < 1f) && wait < 20f) { wait += Time.deltaTime; yield return null; }
            var b = TempleSite.Footprint; if (b.size.sqrMagnitude < 1f) yield break;
            var cands = new System.Collections.Generic.List<(Vector3, Quaternion)>();
            foreach (float dz in new[] { 0f, 28f, -28f, 14f, -14f }) cands.Add((new Vector3(b.max.x + 13.5f, Village.Top, b.center.z + dz + 1f), Quaternion.identity));
            if (!ZoneKit.ClearGround(cands, new Vector3(12.2f, 3f, 9.8f), out var at, out var face)) Debug.LogWarning("[Stepwell] no clear ground east of the temple; using the last candidate");
            Build(at, face);
        }

        WaterValve Wheel(Transform parent, string name, Vector3 pos, int steps, string verb, Material bronze, float yaw)
        {
            var hub = ZoneKit.Part(parent, name, pos, new Vector3(0.3f, 0.3f, 0.3f), bronze); hub.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            var disc = ZoneKit.Round(hub.transform, "Wheel", new Vector3(0f, 0f, -0.7f), new Vector3(3f, 0.16f, 3f), bronze); disc.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            for (int k = 0; k < 3; k++) { var sp = ZoneKit.Part(hub.transform, "Grip", new Vector3(0f, 0f, -0.95f), new Vector3(3.4f, 0.28f, 0.28f), bronze, false); sp.transform.localRotation = Quaternion.Euler(0f, 0f, k * 60f); }
            ZoneKit.Reach(hub, 1.1f);
            var v = hub.AddComponent<WaterValve>(); v.elementId = "z4." + name; v.steps = steps; v.axis = Vector3.forward; v.verb = verb; v.radius = 2.4f; v.turnSeconds = 0.5f; return v;
        }

        public void Build(Vector3 at, Quaternion facing)
        {
            transform.SetPositionAndRotation(at, facing); var root = transform;
            const string wall = "mossy_sandstone";
            var bronze = ZoneKit.Tinted("clay_plaster", new Color(0.55f, 0.42f, 0.2f), 0.8f); bronze.SetFloat("_Metallic", 1f);
            // the tank: four thick walls with a walkway on top, a floor, a ramp up outside and a ramp out inside (so nobody is ever stuck)
            Village.Box(root, "Tank_N", new Vector3(0f, Rim / 2f, 6.8f), new Vector3(15.2f, Rim, 1.6f), wall, 2.4f);
            Village.Box(root, "Tank_S", new Vector3(0f, Rim / 2f, -6.8f), new Vector3(15.2f, Rim, 1.6f), wall, 2.4f);
            Village.Box(root, "Tank_W", new Vector3(-6.8f, Rim / 2f, 0f), new Vector3(1.6f, Rim, 12f), wall, 2.4f);
            Village.Box(root, "Tank_E_Low", new Vector3(6.8f, 1.3f, 0f), new Vector3(1.6f, 2.6f, 12f), wall, 2.4f);
            Village.Box(root, "Tank_E_UpN", new Vector3(6.8f, 3.9f, 3.75f), new Vector3(1.6f, 2.6f, 4.5f), wall, 2.4f);
            Village.Box(root, "Tank_E_UpS", new Vector3(6.8f, 3.9f, -3.75f), new Vector3(1.6f, 2.6f, 4.5f), wall, 2.4f);
            Village.Box(root, "Tank_E_Lintel", new Vector3(6.8f, 5.0f, 0f), new Vector3(1.6f, 0.4f, 3f), wall, 2.4f);
            Village.Box(root, "Tank_Floor", new Vector3(0f, 0.05f, 0f), new Vector3(12f, 0.1f, 12f), wall, 2.4f);
            var ramp = Village.Box(root, "Ramp_Out", new Vector3(1f, 2.5f, -8.5f), new Vector3(10.4f, 0.25f, 1.8f), "red_sandstone_pavement", 2f); ramp.transform.localRotation = Quaternion.Euler(0f, 0f, -30f);
            Village.Box(root, "Ramp_Landing", new Vector3(-5.3f, 5.08f, -8.5f), new Vector3(3.6f, 0.25f, 1.8f), "red_sandstone_pavement", 2f);
            var inner = Village.Box(root, "Ramp_In", new Vector3(-5.25f, 2.5f, 0.6f), new Vector3(1.4f, 0.25f, 10.4f), "red_sandstone_pavement", 2f); inner.transform.localRotation = Quaternion.Euler(30f, 0f, 0f);
            // The ramp's high end stops at z=-3.9, while the rim starts at z=-6.
            // Join them with a visible walkable landing instead of an invisible jump gap.
            Village.Box(root, "Ramp_RimBridge", new Vector3(-5.25f, 5.08f, -5f), new Vector3(1.4f, 0.25f, 2.25f), "red_sandstone_pavement", 2f);
            // carved water-line marks on the west wall (what the player reads)
            for (int i = 1; i <= 4; i++) ZoneKit.Part(root, "WaterMark_" + i, new Vector3(5.97f, 0.3f + i * 1.2f, -4.2f), new Vector3(0.05f, i == 2 ? 0.12f : 0.05f, i == 2 ? 2.2f : 1.2f), ZoneKit.Tinted("clay_plaster", i == 2 ? new Color(0.95f, 0.85f, 0.6f) : new Color(0.35f, 0.33f, 0.3f), 0.2f), false);
            // the hidden chamber behind the east wall
            Village.Box(root, "Chamber_Floor", new Vector3(9.1f, 2.5f, 0f), new Vector3(3f, 0.2f, 3.4f), wall, 2f);
            Village.Box(root, "Chamber_Back", new Vector3(10.7f, 3.8f, 0f), new Vector3(0.3f, 2.6f, 3.6f), wall, 2f);
            Village.Box(root, "Chamber_N", new Vector3(9.1f, 3.8f, 1.65f), new Vector3(3.2f, 2.6f, 0.3f), wall, 2f);
            Village.Box(root, "Chamber_S", new Vector3(9.1f, 3.8f, -1.65f), new Vector3(3.2f, 2.6f, 0.3f), wall, 2f);
            Village.Box(root, "Chamber_Roof", new Vector3(9.1f, 5.15f, 0f), new Vector3(3.6f, 0.2f, 3.8f), wall, 2f, false);
            var hinge = new GameObject("PanelHinge").transform; hinge.SetParent(root, false); hinge.localPosition = new Vector3(6.05f, 3.75f, -1.5f);
            var panel = Village.Box(hinge, "CarvedPanel", new Vector3(0f, 0f, 1.5f), new Vector3(0.16f, 2.3f, 3f), "sandstone_blocks_08", 1.5f); ZoneKit.Reach(panel, 2.2f);
            Panel = panel.AddComponent<PuzzleLever>(); Panel.elementId = "z4.panel"; Panel.sound = "sfx_door_stone"; Panel.handle = hinge; Panel.pulledEuler = new Vector3(0f, 82f, 0f); Panel.verb = "Push the carved panel"; Panel.radius = 3.2f; Panel.locked = true;

            // water, raft, sluices and the drain
            var water = ZoneKit.Part(root, "Water", new Vector3(0f, 0.3f, 0f), new Vector3(12f, 0.05f, 12f), ZoneKit.Glass(new Color(0.07f, 0.17f, 0.22f, 0.78f), 0.96f), false);
            var raft = Village.Box(root, "Raft", new Vector3(4.45f, 0.42f, 0f), new Vector3(2.9f, 0.25f, 2.9f), "weathered_planks", 1.2f); Raft = raft.transform;
            var rrb = raft.AddComponent<Rigidbody>(); rrb.isKinematic = true;
            for (int i = -1; i <= 1; i += 2) Village.Box(raft.transform, "RaftRail", new Vector3(-0.45f, 2.2f, i * 0.45f), new Vector3(0.05f, 3.6f, 0.05f), "medieval_wood", 1f, false);
            SluiceA = Wheel(root, "sluice_south", new Vector3(-2.5f, Rim + 0.75f, -6.8f), 2, "Turn the sluice wheel", bronze, 0f);
            SluiceB = Wheel(root, "sluice_north", new Vector3(2.5f, Rim + 0.75f, 6.8f), 2, "Turn the sluice wheel", bronze, 180f);
            Village.Box(root, "SluicePost_S", new Vector3(-2.5f, Rim + 0.35f, -6.8f), new Vector3(0.25f, 0.7f, 0.25f), wall, 1f, false);
            Village.Box(root, "SluicePost_N", new Vector3(2.5f, Rim + 0.35f, 6.8f), new Vector3(0.25f, 0.7f, 0.25f), wall, 1f, false);
            Drain = Wheel(raft.transform, "drain_chain_wheel", new Vector3(-0.42f, 3.6f, 0f), 5, "Work the drain chain", bronze, 90f);
            Drain.transform.localScale = new Vector3(0.3f / 2.9f, 0.3f / 0.25f, 0.3f / 2.9f); Drain.radius = 2.6f;
            var basinGo = new GameObject("Basin"); basinGo.transform.SetParent(root, false);
            Basin = basinGo.AddComponent<WaterBasin>(); Basin.elementId = "z4.basin"; Basin.inflows.Add(SluiceA); Basin.inflows.Add(SluiceB); Basin.inflowGains.Add(2); Basin.inflowGains.Add(2);
            Basin.drain = Drain; Basin.maxLevel = 4; Basin.levelHeight = 1.2f; Basin.riseSpeed = 0.9f; Basin.surface = water.transform; Basin.floating.Add(raft.transform);

            // the treasure and the dropped key ring
            var vessel = ArtUpgrade.Place(root, "brass_vase_03", root.TransformPoint(new Vector3(9.8f, 2.6f, 0.3f)), 1.6f, 0f, false);
            if (vessel == null) vessel = ZoneKit.Round(root, "Vessel", new Vector3(9.8f, 2.9f, 0.3f), new Vector3(0.4f, 0.3f, 0.4f), bronze);
            foreach (var c in vessel.GetComponentsInChildren<Collider>()) Destroy(c); ZoneKit.Reach(vessel, 2.2f);
            Vessel = vessel.AddComponent<TreasurePickup>(); Vessel.treasureName = "the Monsoon Vessel"; Vessel.zoneId = ZoneId; Vessel.enabled = false;
            ZoneKit.Round(root, "KeyRing", new Vector3(8.6f, 2.62f, -0.8f), new Vector3(0.22f, 0.012f, 0.22f), bronze);
            ArtUpgrade.Place(root, "brass_diya_lantern", root.TransformPoint(new Vector3(-6.8f, Rim, -5.6f)), 1.5f, 0f, false);
            ArtUpgrade.Place(root, "brass_diya_lantern", root.TransformPoint(new Vector3(6.8f, Rim, 5.6f)), 1.5f, 0f, false);
            foreach (var p in new[] { new Vector3(-6.8f, Rim + 0.5f, -5.6f), new Vector3(6.8f, Rim + 0.5f, 5.6f), new Vector3(9.6f, 4.4f, -1f) })
            { var lg = new GameObject("ShrineLamp"); lg.transform.SetParent(root, false); lg.transform.localPosition = p; var l = lg.AddComponent<Light>(); l.type = LightType.Point; l.color = new Color(1f, 0.66f, 0.32f); l.range = 9f; l.intensity = 7f; l.shadows = LightShadows.None; lg.AddComponent<LightFlicker>(); }

            // rain over the tank (this zone only)
            var rg = new GameObject("Rain"); rg.transform.SetParent(root, false); rg.transform.localPosition = new Vector3(0f, 16f, 0f);
            var ps = rg.AddComponent<ParticleSystem>(); ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main; main.startLifetime = 1.1f; main.startSpeed = 0f; main.gravityModifier = 2.6f; main.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.035f); main.maxParticles = 3000; main.startColor = new Color(0.75f, 0.85f, 1f, 0.5f); main.simulationSpace = ParticleSystemSimulationSpace.World;
            var em = ps.emission; em.rateOverTime = 1400f * Mathf.Max(0.3f, GraphicsPresets.Current.effectScale);
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(30f, 0.1f, 30f);
            var pr = rg.GetComponent<ParticleSystemRenderer>(); pr.renderMode = ParticleSystemRenderMode.Stretch; pr.lengthScale = 6f; pr.velocityScale = 0.06f; var rm = Fx.Res("Rain"); if (rm != null) pr.sharedMaterial = rm;
            ps.Play();

            // art pass: coping along the tank, the twin pillars at the corners, lamp stands on the rim
            ZoneDress.Trim(root, "Tank_N", "Tank_S", "Tank_W", "Chamber_");
            foreach (var c in new[] { new Vector3(-7.2f, Rim, -7.2f), new Vector3(7.2f, Rim, 7.2f), new Vector3(-7.2f, Rim, 7.2f) }) ZoneDress.Prop(root, "SM_V3_Pillar", c, 45f, new Vector3(1.1f, 1.25f, 1.1f));
            ZoneDress.Lamp(root, new Vector3(-6.8f, Rim, 1.5f), 8f, 4f);
            // rain-soaked stone: darker and glossy, with puddles around the tank (the reflection probe gives them something to reflect)
            var wet = ZoneKit.Tinted("mossy_sandstone", new Color(0.55f, 0.56f, 0.6f), 0.82f);
            foreach (var t in root.GetComponentsInChildren<MeshRenderer>())
                if (t.name.StartsWith("Tank_") || t.name.StartsWith("Ramp_") || t.name.StartsWith("Chamber_") || t.name == "SM_V3_Coping" || t.name == "SM_V3_Plinth") t.sharedMaterial = wet;
            var puddle = Fx.Res("Puddle");
            if (puddle != null)
                for (int i = 0; i < 9; i++)
                {
                    var q = GameObject.CreatePrimitive(PrimitiveType.Quad); Destroy(q.GetComponent<Collider>()); q.name = "Puddle"; q.transform.SetParent(root, false);
                    float a = i * 40f * Mathf.Deg2Rad; q.transform.localPosition = new Vector3(Mathf.Cos(a) * (10.5f + (i % 3)), 0.02f, Mathf.Sin(a) * (10f + (i % 2) * 2f));
                    q.transform.localRotation = Quaternion.Euler(90f, i * 37f, 0f); q.transform.localScale = new Vector3(1.6f + (i % 3) * 0.7f, 1.1f + (i % 2) * 0.6f, 1f);
                    q.GetComponent<Renderer>().sharedMaterial = puddle;
                }
            var gg = new GameObject("Puzzle_Stepwell"); gg.transform.SetParent(root, false); gg.transform.localPosition = new Vector3(0f, Rim, 0f);
            Group = gg.AddComponent<PuzzleGroup>(); Group.puzzleId = "z4_stepwell"; Group.zoneId = ZoneId; Group.hintRadius = 26f; Group.lockWhenSolved = false;
            Group.hints = new[] { "The east wall has old water marks. One is longer than the others.", "Too much water hides the carved panel. The raft carries a drain chain.", "Open both sluice wheels on the rim, step onto the raft, then work the drain chain twice so the water rests on the long mark, and push the panel." };
            Group.conditions.Add(new PuzzleGroup.Condition { element = Basin, state = 2 }); Group.conditions.Add(new PuzzleGroup.Condition { element = Panel, state = 1 });
            Basin.Changed += _ => Refresh(); Panel.Changed += _ => Refresh();

            var lookGo = new GameObject("ZoneLook_Stepwell"); lookGo.transform.SetParent(root, false); lookGo.transform.localPosition = new Vector3(1f, 9f, -1f);
            var look = lookGo.AddComponent<ZoneLook>(); look.zone = ZoneLook.Zone.Stepwell; look.size = new Vector3(27f, 30f, 24f); look.blendDistance = 6f; look.Build();
            ZoneAtmosphere.Register(look, facing * Quaternion.Euler(50f, 35f, 0f));
            Physics.SyncTransforms(); Built = true; Refresh();
            Debug.Log($"[Stepwell] zone 4 puzzle built at {at:0.0}");
        }

        void Update()
        {   // the knife man and his helper wait at the foot of the ramp for whoever comes out with the Vessel
            if (!Built || !Vessel.Taken || Brawl.WonBefore(ZoneId) || Brawl.Active != null) return;
            var pc = ZoneKit.Player; Vector3 spot = transform.TransformPoint(new Vector3(9f, 0f, -12f));
            if (pc != null && Vector3.Distance(pc.transform.position, spot) < 9f && pc.transform.position.y < spot.y + 1.5f)
                Brawl.Begin(ZoneId, spot, 6f, new[] { Brawl.Robber("the knife man", 2, BrawlType.Knife, 0.92f, new Color(0.5f, 0.6f, 0.75f)), Brawl.Robber("his helper", 1, BrawlType.Brawler, 0.95f, new Color(0.6f, 0.6f, 0.65f)) }, null);
        }

        void Refresh()
        {
            if (!Built) return;
            Panel.locked = Basin.State != 2 && Panel.State == 0;
            if (Panel.State == 1 && !Vessel.enabled && !Vessel.Taken)
            {
                Vessel.enabled = true;
                if (PuzzleSave.Get("evidence.key_ring") == 0) { PuzzleSave.Set("evidence.key_ring", 1); ObjectiveBanner.Show("NOTED", "A temple key ring on the chamber floor.", 4f); }
            }
        }
    }
}
