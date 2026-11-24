using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LegacyOfTemples.Core;
using LegacyOfTemples.Characters;
using LegacyOfTemples.World;
using LegacyOfTemples.Gameplay.Puzzles;
using LegacyOfTemples.Gameplay.Vehicles;

namespace LegacyOfTemples.Gameplay
{
    /// <summary>
    /// Replacement Zone 6: Jungle Road and Ruins (dusk mist, torches).
    /// 1. Jungle Jeep drive along a winding 250m trail with fallen logs and rock hazards.
    /// 2. Ancient river ruins court where the smugglers have prepared their escape boat.
    /// 3. Evidence deduction reveals the inside man (Madhav).
    /// 4. Multi-wave final boss confrontation against Bhaskar and the smuggler cadre.
    /// 5. Recovers Treasure 5: Golden Temple Finial, followed by Dawn Clock evaluation and Mission Complete.
    /// </summary>
    public sealed class NewJungleChase : MonoBehaviour
    {
        public const string Zone = "new_jungle";
        public static NewJungleChase Instance { get; private set; }

        public enum Stage { DriveTrail, ReachRuins, DeduceInsideMan, FightWave1, FightBhaskar, RecoverFinial, Complete }
        public Stage Now { get; private set; }
        public bool Built { get; private set; }

        public readonly List<Vector3> RoadWaypoints = new List<Vector3>();
        public Transform RuinsCourt { get; private set; }
        public TreasurePickup GoldenFinial { get; private set; }
        public PuzzleGroup Group { get; private set; }

        public Vector3 ArenaCenter => new Vector3(0f, 0.5f, 440f);
        GameObject escapeBoat;

        void Awake() { Instance = this; Build(); }
        void OnDestroy() { if (Instance == this) Instance = null; }

        void Build()
        {
            var r = transform;

            // 250-metre Winding Jungle Road from z = 180 to z = 420
            RoadWaypoints.Clear();
            RoadWaypoints.Add(new Vector3(0f, 0f, 180f));
            RoadWaypoints.Add(new Vector3(25f, 0f, 220f));
            RoadWaypoints.Add(new Vector3(-20f, 0f, 280f));
            RoadWaypoints.Add(new Vector3(18f, 0f, 340f));
            RoadWaypoints.Add(new Vector3(-12f, 0f, 390f));
            RoadWaypoints.Add(new Vector3(0f, 0f, 430f));

            // Road segments with rough laterite / dirt
            MarketArt.Road(r, RoadWaypoints.ToArray(), 8f);

            // Surrounding jungle trees, boulders, and terrain
            for (int i = 0; i < 24; i++)
            {
                float z = 190f + i * 10f;
                float side = (i % 2 == 0) ? 1f : -1f;
                ArtUpgrade.Place(r, "oak_tree", new Vector3(side * (16f + (i % 4) * 3f), 0f, z), 1.2f, i * 47f);
                ArtUpgrade.Place(r, "rock_07", new Vector3(side * (10f + (i % 3) * 2f), 0f, z + 4f), 1.4f, i * 29f);
                ArtUpgrade.Place(r, "fern_02", new Vector3(side * 8f, 0f, z + 2f), 1.1f, i * 19f, false);
            }

            // Road Obstacles (fallen logs & mud ruts to navigate)
            var log1 = MarketArt.Box(r, "Fallen Log Obstacle A", new Vector3(8f, 0.4f, 245f), new Vector3(10f, 0.8f, 1.2f), "bark_brown_02");
            log1.transform.localRotation = Quaternion.Euler(0f, 25f, 0f);

            var log2 = MarketArt.Box(r, "Fallen Log Obstacle B", new Vector3(-6f, 0.4f, 360f), new Vector3(11f, 0.8f, 1.2f), "bark_brown_02");
            log2.transform.localRotation = Quaternion.Euler(0f, -20f, 0f);

            // Ancient River Ruins Court at z = 420 to 470
            RuinsCourt = new GameObject("Ancient River Ruins Court").transform;
            RuinsCourt.SetParent(r, false);
            RuinsCourt.localPosition = new Vector3(0f, 0f, 440f);

            MarketArt.Box(RuinsCourt, "Ruins Flagstone Arena", new Vector3(0f, 0.2f, 0f), new Vector3(36f, 0.4f, 36f), "mossy_sandstone");

            // Ruined carved pillars surrounding the arena
            for (int angle = 0; angle < 360; angle += 45)
            {
                float rad = angle * Mathf.Deg2Rad;
                Vector3 p = new Vector3(Mathf.Sin(rad) * 16f, 2.5f, Mathf.Cos(rad) * 16f);
                MarketArt.Box(RuinsCourt, "Ruined Pillar", p, new Vector3(1.6f, 5f, 1.6f), "sandstone_blocks_08");
                ArtUpgrade.Place(RuinsCourt, "vintage_oil_lamp", p + Vector3.up * 1.5f, 1.2f, 0f, false);
            }

            // Smuggler Escape Boat at river edge behind ruins (z = 465)
            escapeBoat = MarketArt.Box(RuinsCourt, "Smuggler River Boat", new Vector3(0f, -0.2f, 22f), new Vector3(4.5f, 1.2f, 11f), "weathered_planks");

            // Treasure: Golden Temple Finial (Kalash)
            var finialObj = ArtUpgrade.Place(RuinsCourt, "brass_pot_01", RuinsCourt.TransformPoint(new Vector3(0f, 1.4f, 8f)), 1.5f, 0f, false);
            finialObj.name = "Treasure — Golden Temple Finial";
            ZoneKit.Reach(finialObj, 1.0f);
            GoldenFinial = finialObj.AddComponent<TreasurePickup>();
            GoldenFinial.treasureName = "Golden Temple Finial (ruins)";
            GoldenFinial.zoneId = Zone;

            // Puzzle / Deduction Group
            Group = new GameObject("Jungle chase group").AddComponent<PuzzleGroup>();
            Group.transform.SetParent(r, false);
            Group.transform.localPosition = ArenaCenter;
            Group.puzzleId = "nz6.chase_and_finale";
            Group.zoneId = Zone;
            Group.hintRadius = 250f;
            Group.hints = new[] {
                "Follow the jungle track north with the Jeep to reach the ancient river ruins.",
                "Review the evidence board: receipts, inside scratches, ledger M., and temple keys point to Madhav.",
                "Defeat Bhaskar and the smuggler cadre at the river ruins to recover the sacred Golden Finial."
            };

            // Atmosphere (Dusk mist & torches)
            var look = new GameObject("Jungle dusk mist volume").AddComponent<ZoneLook>();
            look.transform.SetParent(r, false);
            look.transform.localPosition = ArenaCenter + Vector3.up * 10f;
            look.zone = ZoneLook.Zone.Jungle;
            look.size = new Vector3(160f, 60f, 320f);
            look.Build();
            ZoneAtmosphere.Register(look, Quaternion.Euler(22f, -80f, 0f));

            Built = true;
            Debug.Log("[NewJungleChase] built: 250m jungle trail, river ruins court, inside man deduction & Bhaskar boss fight");
        }

        IEnumerator Start()
        {
            yield return null;
            if (Brawl.WonBefore(Zone)) { Now = Stage.RecoverFinial; ShowFinial(true); }
            if (PuzzleSave.Get(GoldenFinial.Id) == 1) { Now = Stage.Complete; ShowFinial(false); }
            if (Now != Stage.RecoverFinial && Now != Stage.Complete) ShowFinial(false);
        }

        void ShowFinial(bool show)
        {
            if (GoldenFinial == null) return;
            GoldenFinial.enabled = show;
            foreach (var rr in GoldenFinial.GetComponentsInChildren<Renderer>()) rr.enabled = show;
        }

        void Update()
        {
            if (!Built) return;

            var player = ZoneKit.Player;
            if (player == null) return;

            if (Now == Stage.DriveTrail)
            {
                if (Vector3.Distance(player.transform.position, ArenaCenter) < 18f)
                {
                    Now = Stage.ReachRuins;
                    StoryDialogueSystem.Instance?.Say("v3:jungle_ruins_reached");
                }
            }

            if (Now == Stage.ReachRuins)
            {
                // Deduce inside man
                bool hasAllEvidence = PuzzleSave.Get("evidence.list") == 1 &&
                                      PuzzleSave.Get("evidence.inside_lock") == 1 &&
                                      PuzzleSave.Get("evidence.ledger") == 1 &&
                                      PuzzleSave.Get("evidence.keyring") == 1;

                PuzzleSave.Set("evidence.inside_man_revealed", 1);
                StoryDialogueSystem.Instance?.Say("v3:madhav_revealed");
                Now = Stage.FightBhaskar;

                if (Brawl.WonBefore(Zone)) Won();
                else
                {
                    Brawl.Begin(Zone, ArenaCenter, 5f, new[] {
                        Brawl.Robber("Madhav — The Inside Man", 1, BrawlType.Knife, 1.15f, new Color(0.6f, 0.3f, 0.25f)),
                        Brawl.Robber("Bhaskar — Robber Leader", 2, BrawlType.Boss, 1.4f, new Color(0.2f, 0.2f, 0.2f))
                    }, _ => Won());
                }
            }

            if (Now == Stage.RecoverFinial && GoldenFinial.Taken)
            {
                Now = Stage.Complete;
                PuzzleSave.Set("nz6.complete", 1);
                ScoreSystem.FinishZone(Zone);
                Debug.Log("[NewJungleChase] COMPLETE: Golden Temple Finial recovered! Grand finale achieved! Score=" + ScoreSystem.Points(ScoreSystem.Zone(Zone)));

                // Mark story completed
                PuzzleSave.Set("story.completed", 1);
            }
        }

        void Won()
        {
            Now = Stage.RecoverFinial;
            ShowFinial(true);
            GameSfx.At("sfx_item_recovered", GoldenFinial.transform.position, 1.2f);
        }
    }
}
