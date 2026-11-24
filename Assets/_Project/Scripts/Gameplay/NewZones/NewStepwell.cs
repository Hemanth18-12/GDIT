using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LegacyOfTemples.Core;
using LegacyOfTemples.Characters;
using LegacyOfTemples.World;
using LegacyOfTemples.Gameplay.Puzzles;

namespace LegacyOfTemples.Gameplay
{
    /// <summary>
    /// Replacement Zone 4: Stepwell and Monsoon Shrine (night, heavy rain).
    /// 1. Sluice valve sequence puzzle to raise the water level in the stepwell basin.
    /// 2. Floating wooden raft rises with the water surface, creating a crossing bridge to the high Monsoon Shrine.
    /// 3. Combat on wet reflective stone against Knife Man and his accomplice.
    /// 4. On defeat, recovers Treasure 4: Monsoon Ceremonial Vessel + Temple Key Ring.
    /// </summary>
    public sealed class NewStepwell : MonoBehaviour
    {
        public const string Zone = "new_stepwell";
        public static NewStepwell Instance { get; private set; }

        public enum Stage { SluiceValves, CrossRaft, Fight, Recover, Complete }
        public Stage Now { get; private set; }
        public bool Built { get; private set; }

        public WaterValve[] Valves { get; private set; }
        public WaterBasin Basin { get; private set; }
        public Transform Raft { get; private set; }
        public TreasurePickup Vessel { get; private set; }
        public PuzzleGroup Group { get; private set; }

        public Vector3 ArenaCenter => new Vector3(-55f, 4.0f, 147f);
        GameObject rainEffect;
        Light moonLight;

        void Awake() { Instance = this; Build(); }
        void OnDestroy() { if (Instance == this) Instance = null; }

        void Build()
        {
            var r = transform;
            Vector3 stepwellCenter = new Vector3(-55f, 0f, 125f);

            // Stepwell deep stone tank excavation: 24m x 32m, 8m deep
            MarketArt.Box(r, "Stepwell Tank Floor", stepwellCenter + new Vector3(0f, -6f, 0f), new Vector3(24f, 0.5f, 32f), "sandstone_blocks_08");
            MarketArt.Box(r, "Stepwell Wall West", stepwellCenter + new Vector3(-12f, -1f, 0f), new Vector3(1f, 10f, 32f), "sandstone_blocks_08");
            MarketArt.Box(r, "Stepwell Wall East", stepwellCenter + new Vector3(12f, -1f, 0f), new Vector3(1f, 10f, 32f), "sandstone_blocks_08");
            MarketArt.Box(r, "Stepwell Wall South", stepwellCenter + new Vector3(0f, -1f, -16f), new Vector3(24f, 10f, 1f), "sandstone_blocks_08");
            MarketArt.Box(r, "Stepwell Wall North", stepwellCenter + new Vector3(0f, -1f, 16f), new Vector3(24f, 10f, 1f), "sandstone_blocks_08");

            // Multi-tier steps descending into the water tank
            for (int tier = 0; tier < 8; tier++)
            {
                float y = -0.5f - tier * 0.7f;
                float inset = tier * 1.1f;
                MarketArt.Box(r, "StepTierSouth_" + tier, stepwellCenter + new Vector3(0f, y, -15f + inset), new Vector3(20f - inset, 0.7f, 1.2f), "sandstone_blocks_08");
                MarketArt.Box(r, "StepTierNorth_" + tier, stepwellCenter + new Vector3(0f, y, 15f - inset), new Vector3(20f - inset, 0.7f, 1.2f), "sandstone_blocks_08");
            }

            // High Monsoon Shrine terrace on the North ledge (y = 3.5)
            var shrine = MarketArt.Box(r, "Monsoon Shrine Terrace", stepwellCenter + new Vector3(0f, 3.5f, 22f), new Vector3(18f, 0.8f, 12f), "mossy_sandstone");
            MarketArt.Box(shrine.transform, "Shrine Canopy", new Vector3(0f, 4f, 0f), new Vector3(16f, 0.6f, 10f), "sandstone_blocks_08");
            for (int i = -1; i <= 1; i += 2)
            {
                MarketArt.Box(shrine.transform, "Shrine Pillar", new Vector3(i * 7f, 2f, 4f), new Vector3(1.2f, 4f, 1.2f), "sandstone_blocks_08");
                MarketArt.Box(shrine.transform, "Shrine Pillar", new Vector3(i * 7f, 2f, -4f), new Vector3(1.2f, 4f, 1.2f), "sandstone_blocks_08");
            }

            // Section 1: Three Sluice Valves
            Valves = new WaterValve[3];
            Vector3[] valvePositions = {
                stepwellCenter + new Vector3(-8f, 0.8f, -14f),
                stepwellCenter + new Vector3(0f, 0.8f, -14f),
                stepwellCenter + new Vector3(8f, 0.8f, -14f)
            };

            for (int i = 0; i < 3; i++)
            {
                var vObj = ZoneKit.Round(r, "SluiceValve_" + i, valvePositions[i], new Vector3(0.7f, 0.18f, 0.7f), ZoneKit.Tinted("clay_plaster", new Color(0.7f, 0.45f, 0.2f), 0.5f), true);
                ZoneKit.Reach(vObj, 0.8f);
                var valve = vObj.AddComponent<WaterValve>();
                valve.elementId = "nz4.valve." + i;
                valve.steps = 2;
                valve.verb = "Open sluice";
                valve.radius = 2.4f;
                Valves[i] = valve;
                MarketArt.Sign(r, valvePositions[i] + Vector3.up * 1f, "SLUICE " + (i + 1), 1.2f);
            }

            // Water Basin
            var basinObj = new GameObject("Stepwell Water Basin");
            basinObj.transform.SetParent(r, false);
            basinObj.transform.localPosition = stepwellCenter + new Vector3(0f, -5.5f, 0f);

            var waterMesh = ZoneKit.Part(basinObj.transform, "Water Surface", Vector3.zero, new Vector3(22f, 0.1f, 30f), ZoneKit.Tinted("stony_dirt_path", new Color(0.12f, 0.25f, 0.35f, 0.85f), 0.95f), false);

            // Floating Wooden Raft
            var raftObj = MarketArt.Box(basinObj.transform, "Floating Wooden Raft", new Vector3(0f, 0.4f, 4f), new Vector3(6.5f, 0.6f, 10f), "weathered_planks");
            Raft = raftObj.transform;

            Basin = basinObj.AddComponent<WaterBasin>();
            Basin.elementId = "nz4.basin";
            Basin.inflows = new List<WaterValve> { Valves[0], Valves[1], Valves[2] };
            Basin.maxLevel = 3;
            Basin.levelHeight = 2.5f;
            Basin.riseSpeed = 2.0f;
            Basin.surface = waterMesh.transform;
            Basin.floating = new List<Transform> { Raft };

            // Treasure: Monsoon Ceremonial Vessel
            var vesObj = ArtUpgrade.Place(r, "brass_pot_02", stepwellCenter + new Vector3(0f, 4.4f, 22f), 1.3f, 0f, false);
            vesObj.name = "Treasure — Monsoon Vessel";
            ZoneKit.Reach(vesObj, 0.8f);
            Vessel = vesObj.AddComponent<TreasurePickup>();
            Vessel.treasureName = "Monsoon Vessel (stepwell)";
            Vessel.zoneId = Zone;

            // Puzzle Group
            Group = new GameObject("Stepwell puzzle group").AddComponent<PuzzleGroup>();
            Group.transform.SetParent(r, false);
            Group.transform.localPosition = stepwellCenter;
            Group.puzzleId = "nz4.water_puzzle";
            Group.zoneId = Zone;
            Group.hintRadius = 150f;
            Group.conditions.Add(new PuzzleGroup.Condition { element = Valves[0], state = 1 });
            Group.conditions.Add(new PuzzleGroup.Condition { element = Valves[1], state = 1 });
            Group.conditions.Add(new PuzzleGroup.Condition { element = Valves[2], state = 1 });
            Group.conditions.Add(new PuzzleGroup.Condition { element = Basin, state = 3 });
            Group.hints = new[] {
                "Turn the three sluice valves along the south ledge to release mountain runoff into the tank.",
                "As the water level rises, the wooden raft will float to the surface.",
                "Cross the floating raft when the tank is flooded to reach the high Monsoon Shrine terrace."
            };

            // Zone Atmosphere (Night & Rain)
            var look = new GameObject("Stepwell night rain volume").AddComponent<ZoneLook>();
            look.transform.SetParent(r, false);
            look.transform.localPosition = stepwellCenter + Vector3.up * 8f;
            look.zone = ZoneLook.Zone.Stepwell;
            look.size = new Vector3(100f, 40f, 100f);
            look.Build();
            ZoneAtmosphere.Register(look, Quaternion.Euler(60f, -120f, 0f));

            Built = true;
            Debug.Log("[NewStepwell] built: multi-tier tank, sluice valves, rising raft, monsoon shrine & wet-stone brawl");
        }

        IEnumerator Start()
        {
            yield return null;
            if (Brawl.WonBefore(Zone)) { Now = Stage.Recover; ShowVessel(true); }
            if (PuzzleSave.Get(Vessel.Id) == 1) { Now = Stage.Complete; ShowVessel(false); }
            if (Now != Stage.Recover && Now != Stage.Complete) ShowVessel(false);
        }

        void ShowVessel(bool show)
        {
            if (Vessel == null) return;
            Vessel.enabled = show;
            foreach (var rr in Vessel.GetComponentsInChildren<Renderer>()) rr.enabled = show;
        }

        void Update()
        {
            if (!Built) return;

            bool valvesOpen = Valves[0].State == 1 && Valves[1].State == 1 && Valves[2].State == 1;
            if (valvesOpen && Basin.State < 3)
            {
                Basin.Recompute();
                GameSfx.At("sfx_water_flow", Basin.transform.position, 0.85f);
                StoryDialogueSystem.Instance?.Say("v3:stepwell_water_rising");
            }

            if (Now == Stage.SluiceValves && Basin.State == 3) Now = Stage.CrossRaft;

            if (Now == Stage.CrossRaft)
            {
                var player = ZoneKit.Player;
                if (player != null && Vector3.Distance(player.transform.position, ArenaCenter) < 7f)
                {
                    Now = Stage.Fight;
                    StoryDialogueSystem.Instance?.Say("v3:stepwell_ambush");
                    if (Brawl.WonBefore(Zone)) Won();
                    else Brawl.Begin(Zone, ArenaCenter, 3.5f, new[] {
                        Brawl.Robber("Knife Smuggler", 1, BrawlType.Knife, 1.05f, new Color(0.45f, 0.5f, 0.55f)),
                        Brawl.Robber("Stepwell Lookout", 2, BrawlType.Brawler, 1.0f, new Color(0.6f, 0.5f, 0.45f))
                    }, _ => Won());
                }
            }

            if (Now == Stage.Recover && Vessel.Taken)
            {
                Now = Stage.Complete;
                PuzzleSave.Set("evidence.keyring", 1);
                PuzzleSave.Set("nz4.complete", 1);
                ScoreSystem.FinishZone(Zone);
                Debug.Log("[NewStepwell] COMPLETE: Monsoon Vessel + Key Ring recovered! Score=" + ScoreSystem.Points(ScoreSystem.Zone(Zone)));
            }
        }

        void Won()
        {
            Now = Stage.Recover;
            ShowVessel(true);
            GameSfx.At("sfx_item_recovered", Vessel.transform.position, 1f);
        }
    }
}
